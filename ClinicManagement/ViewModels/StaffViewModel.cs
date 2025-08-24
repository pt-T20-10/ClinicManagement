using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý nhân viên và chuyên khoa bác sĩ
    /// Cung cấp chức năng CRUD cho chuyên khoa, xem danh sách nhân viên
    /// Hỗ trợ lọc theo vai trò, chuyên khoa và tìm kiếm
    /// Triển khai IDataErrorInfo để validation dữ liệu chuyên khoa
    /// Bao gồm xuất Excel với progress tracking và permission management
    /// </summary>
    public class StaffViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties

        /// <summary>
        /// Set theo dõi các field đã được người dùng tương tác
        /// Chỉ validate các field này để tránh hiển thị lỗi ngay khi mở form
        /// </summary>
        private HashSet<string> _touchedFields = new HashSet<string>();

        /// <summary>
        /// Cờ bật/tắt validation
        /// True = thực hiện validation, False = bỏ qua validation
        /// </summary>
        private bool _isValidating = false;

        /// <summary>
        /// Error property cho IDataErrorInfo - trả về null vì validation per-property
        /// </summary>
        public string Error => null;

        /// <summary>
        /// Tài khoản người dùng hiện tại
        /// Được sử dụng để kiểm tra quyền thao tác với chuyên khoa
        /// </summary>
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // Kiểm tra quyền mỗi khi tài khoản thay đổi
                UpdatePermissions();
            }
        }

        #region DisplayProperties

        /// <summary>
        /// Danh sách nhân viên được hiển thị sau khi lọc
        /// Chứa thông tin đã include Specialty và Role
        /// </summary>
        private ObservableCollection<Staff> _DoctorList;
        public ObservableCollection<Staff> DoctorList
        {
            get => _DoctorList;
            set
            {
                _DoctorList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chuyên khoa cho CRUD operations
        /// Không bao gồm option "Tất cả"
        /// </summary>
        private ObservableCollection<DoctorSpecialty> _ListSpecialty;
        public ObservableCollection<DoctorSpecialty> ListSpecialty
        {
            get => _ListSpecialty;
            set
            {
                _ListSpecialty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chuyên khoa cho filter dropdown
        /// Bao gồm option "-- Tất cả chuyên khoa --" với SpecialtyId = -1
        /// </summary>
        private ObservableCollection<DoctorSpecialty> _ListSpecialtyForFilter;
        public ObservableCollection<DoctorSpecialty> ListSpecialtyForFilter
        {
            get => _ListSpecialtyForFilter;
            set
            {
                _ListSpecialtyForFilter = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Chuyên khoa được chọn từ danh sách để chỉnh sửa
        /// Tự động cập nhật SpecialtyName và Description khi thay đổi
        /// </summary>
        private DoctorSpecialty _SelectedItem;
        public DoctorSpecialty SelectedItem
        {
            get => _SelectedItem;
            set
            {
                _SelectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));

                if (SelectedItem != null)
                {
                    SpecialtyName = SelectedItem.SpecialtyName;
                    Description = SelectedItem.Description;
                }
            }
        }

        /// <summary>
        /// Chuyên khoa được chọn cho filter
        /// Xử lý đặc biệt cho option "Tất cả" (SpecialtyId = -1)
        /// </summary>
        private DoctorSpecialty _SelectedSpecialty;
        public DoctorSpecialty SelectedSpecialty
        {
            get => _SelectedSpecialty;
            set
            {
                _SelectedSpecialty = value;
                OnPropertyChanged();

                if (value != null)
                {
                    // Xử lý option "Tất cả" (SpecialtyId = -1)
                    if (value.SpecialtyId == -1)
                    {
                        SelectedSpecialtyId = null; // Set thành null để không filter theo chuyên khoa
                    }
                    else
                    {
                        // Trường hợp bình thường: Set ID chuyên khoa được chọn
                        SelectedSpecialtyId = value.SpecialtyId;
                    }
                }
                else
                {
                    SelectedSpecialtyId = null;
                }
            }
        }

        /// <summary>
        /// ID chuyên khoa được chọn cho filter
        /// null = không filter theo chuyên khoa, có giá trị = filter theo chuyên khoa đó
        /// Tự động kích hoạt tìm kiếm khi thay đổi
        /// </summary>
        private int? _SelectedSpecialtyId;
        public int? SelectedSpecialtyId
        {
            get => _SelectedSpecialtyId;
            set
            {
                _SelectedSpecialtyId = value;
                OnPropertyChanged();

                ExecuteSearch();
            }
        }

        /// <summary>
        /// Nhân viên được chọn từ danh sách
        /// Sử dụng để xem chi tiết thông tin nhân viên
        /// </summary>
        private Staff _selectedDoctor;
        public Staff SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tên chuyên khoa cho form thêm/sửa
        /// Có validation và theo dõi touched state
        /// Trường bắt buộc với độ dài 2-50 ký tự
        /// </summary>
        private string _SpecialtyName;
        public string SpecialtyName
        {
            get => _SpecialtyName;
            set
            {
                if (_SpecialtyName != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_SpecialtyName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(SpecialtyName));
                    else if (!wasEmpty && isEmpty)
                    {
                        _touchedFields.Remove(nameof(SpecialtyName));
                        OnPropertyChanged(nameof(Error));
                    }

                    _SpecialtyName = value;
                    OnPropertyChanged();

                    if (_touchedFields.Contains(nameof(SpecialtyName)))
                        OnPropertyChanged(nameof(Error));

                    // Refresh khả năng thực thi command
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Mô tả chuyên khoa cho form thêm/sửa
        /// Có validation và theo dõi touched state
        /// Trường tùy chọn với độ dài tối đa 255 ký tự
        /// </summary>
        private string _Description;
        public string Description
        {
            get => _Description;
            set
            {
                if (_Description != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_Description);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Description));
                    else if (!wasEmpty && isEmpty)
                    {
                        _touchedFields.Remove(nameof(Description));
                        OnPropertyChanged(nameof(Error));
                    }

                    _Description = value;
                    OnPropertyChanged();

                    if (_touchedFields.Contains(nameof(Description)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Từ khóa tìm kiếm nhân viên
        /// Tìm theo tên, số điện thoại hoặc email
        /// Tự động kích hoạt tìm kiếm khi thay đổi
        /// </summary>
        private string _SearchText;
        public string SearchText
        {
            get => _SearchText;
            set
            {
                _SearchText = value;
                OnPropertyChanged();
                ExecuteSearch();
            }
        }

        /// <summary>
        /// Danh sách vai trò cho filter dropdown
        /// Bao gồm option "-- Tất cả vai trò --" với RoleId = -1
        /// </summary>
        private ObservableCollection<Role> _roleList;
        public ObservableCollection<Role> RoleList
        {
            get => _roleList;
            set
            {
                _roleList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Vai trò được chọn cho filter
        /// Xử lý đặc biệt cho option "Tất cả" (RoleId = -1)
        /// Ảnh hưởng đến hiển thị filter chuyên khoa
        /// </summary>
        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();

                if (value != null)
                {
                    // Xử lý option "Tất cả" (RoleId = -1)
                    if (value.RoleId == -1)
                    {
                        SelectedRoleId = null; // Set thành null để không filter theo vai trò
                        IsSpecialtyVisible = false; // Ẩn filter chuyên khoa khi chọn "Tất cả"
                    }
                    else
                    {
                        // Trường hợp bình thường: Cập nhật filter theo vai trò
                        SelectedRoleId = value.RoleId;

                        // Kiểm tra xem vai trò được chọn có phải bác sĩ không
                        IsSpecialtyVisible = value.RoleName.Contains("Bác sĩ");
                    }
                }
                else
                {
                    SelectedRoleId = null;
                    IsSpecialtyVisible = false;
                }
            }
        }

        /// <summary>
        /// ID vai trò được chọn cho filter
        /// null = không filter theo vai trò, có giá trị = filter theo vai trò đó
        /// Tự động kích hoạt tìm kiếm khi thay đổi
        /// </summary>
        private int? _selectedRoleId;
        public int? SelectedRoleId
        {
            get => _selectedRoleId;
            set
            {
                _selectedRoleId = value;
                OnPropertyChanged();
                ExecuteSearch();
            }
        }

        /// <summary>
        /// Thuộc tính kiểm soát hiển thị ComboBox chuyên khoa
        /// Chỉ hiển thị khi role được chọn là bác sĩ
        /// </summary>
        private bool _isSpecialtyVisible = false;
        public bool IsSpecialtyVisible
        {
            get => _isSpecialtyVisible;
            set
            {
                _isSpecialtyVisible = value;
                OnPropertyChanged();

                // Nếu không nên hiển thị chuyên khoa, xóa selection
                if (!value)
                {
                    SelectedSpecialty = null;
                }
            }
        }

        /// <summary>
        /// Quyền thao tác với chuyên khoa
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canModifySpecialties = false;
        public bool CanModifySpecialties
        {
            get => _canModifySpecialties;
            set
            {
                _canModifySpecialties = value;
                OnPropertyChanged();
            }
        }
        #endregion

        /// <summary>
        /// Danh sách tất cả nhân viên gốc (không được filter)
        /// Sử dụng làm nguồn dữ liệu cho việc lọc
        /// </summary>
        private ObservableCollection<Staff> _allStaffs;
        #endregion

        #region Commands

        // === STAFF MANAGEMENT COMMANDS ===
        /// <summary>
        /// Lệnh thêm nhân viên mới - mở cửa sổ AddDoctorWindow
        /// </summary>
        public ICommand AddStaffCommand { get; set; }

        /// <summary>
        /// Lệnh chỉnh sửa thông tin nhân viên
        /// </summary>
        public ICommand EditDoctorCommand { get; set; }

        /// <summary>
        /// Lệnh xóa nhân viên
        /// </summary>
        public ICommand DeleteDoctorCommand { get; set; }

        /// <summary>
        /// Lệnh tìm kiếm nhân viên
        /// </summary>
        public ICommand SearchCommand { get; set; }

        /// <summary>
        /// Lệnh reset tất cả filter về trạng thái ban đầu
        /// </summary>
        public ICommand ResetFiltersCommand { get; set; }

        /// <summary>
        /// Lệnh xử lý khi UserControl được load
        /// </summary>
        public ICommand LoadedUCCommand { get; set; }

        /// <summary>
        /// Lệnh mở cửa sổ chi tiết nhân viên
        /// </summary>
        public ICommand OpenDoctorDetailsCommand { get; set; }

        /// <summary>
        /// Lệnh xuất danh sách nhân viên ra Excel
        /// </summary>
        public ICommand ExportExcelCommand { get; set; }

        /// <summary>
        /// Lệnh làm mới form chuyên khoa
        /// </summary>
        public ICommand RefreshSpecialtyCommand { get; set; }

        // === SPECIALTY MANAGEMENT COMMANDS ===
        /// <summary>
        /// Lệnh thêm chuyên khoa mới
        /// </summary>
        public ICommand AddCommand { get; set; }

        /// <summary>
        /// Lệnh chỉnh sửa chuyên khoa
        /// </summary>
        public ICommand EditCommand { get; set; }

        /// <summary>
        /// Lệnh xóa chuyên khoa
        /// </summary>
        public ICommand DeleteCommand { get; set; }
        #endregion

        /// <summary>
        /// Constructor khởi tạo StaffViewModel
        /// Có thể nhận Account để thiết lập quyền ngay từ đầu
        /// </summary>
        /// <param name="account">Tài khoản hiện tại (tùy chọn)</param>
        public StaffViewModel(Account account = null)
        {
            // Nếu có account được cung cấp, thiết lập nó
            if (account != null)
            {
                CurrentAccount = account;
            }

            // Khởi tạo commands trước
            InitializeCommands();

            // Sau đó tải dữ liệu
            LoadData();

            // Cuối cùng, cập nhật quyền một cách rõ ràng
            UpdatePermissions();
        }

        #region Methods

        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        private void InitializeCommands()
        {
            // Command xử lý khi UserControl được load - lấy account từ MainViewModel
            LoadedUCCommand = new RelayCommand<UserControl>(
        (userControl) => {
            if (userControl != null)
            {
                // Lấy MainViewModel từ Application resources
                var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                if (mainVM != null && mainVM.CurrentAccount != null)
                {
                    // Cập nhật tài khoản hiện tại
                    CurrentAccount = mainVM.CurrentAccount;
                    // CurrentAccount setter sẽ gọi UpdatePermissions()
                }
            }
        },
        (userControl) => true
    );

            // Command reset tất cả filter
            ResetFiltersCommand = new RelayCommand<object>(
               (p) => ExecuteResetFilters(),
               (p) => true
           );

            // Command tìm kiếm nhân viên
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );

            // Command thêm nhân viên mới
            AddStaffCommand = new RelayCommand<object>(
                (p) =>
                {
                    // Mở cửa sổ thêm nhân viên mới
                    var addDoctorWindow = new AddDoctorWindow();
                    addDoctorWindow.ShowDialog();
                    // Refresh dữ liệu sau khi thêm nhân viên mới
                    LoadData();
                },
                (p) => CanModifySpecialties
            );

            // Command mở chi tiết nhân viên
            OpenDoctorDetailsCommand = new RelayCommand<Staff>(
                (p) => OpenDoctorDetails(p),
                (p) => p != null
            );

            // Command xuất Excel
            ExportExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => DoctorList != null && DoctorList.Count > 0
            );

            // Command thêm chuyên khoa - yêu cầu quyền và SpecialtyName không rỗng
            AddCommand = new RelayCommand<object>(
                 (p) => AddSpecialty(),
                 (p) => CanModifySpecialties && !string.IsNullOrEmpty(SpecialtyName)
             );

            // Command chỉnh sửa chuyên khoa - yêu cầu quyền, có item được chọn và SpecialtyName không rỗng
            EditCommand = new RelayCommand<object>(
                (p) => EditSpecialty(),
                (p) => CanModifySpecialties && SelectedItem != null && !string.IsNullOrEmpty(SpecialtyName)
            );

            // Command xóa chuyên khoa - yêu cầu quyền và có item được chọn
            DeleteCommand = new RelayCommand<object>(
                (p) => DeleteSpecialty(),
                (p) => CanModifySpecialties && SelectedItem != null
            );

            // Command làm mới form chuyên khoa
            RefreshSpecialtyCommand = new RelayCommand<object>(
                (p) => RefeshSpecialty(),
                (p) => true
                );
        }

        /// <summary>
        /// Mở cửa sổ chi tiết nhân viên
        /// Tạo ViewModel với thông tin nhân viên và hiển thị cửa sổ
        /// </summary>
        /// <param name="doctor">Nhân viên cần xem chi tiết</param>
        private void OpenDoctorDetails(Staff doctor)
        {
            if (doctor == null) return;

            var detailsWindow = new StaffDetailsWindow
            {
                DataContext = new StaffDetailsWindowViewModel { Doctor = doctor }
            };
            detailsWindow.ShowDialog();
            LoadData(); // Refresh dữ liệu sau khi đóng cửa sổ chi tiết
        }

        /// <summary>
        /// Tải tất cả dữ liệu cần thiết cho ViewModel
        /// Bao gồm nhân viên, chuyên khoa và vai trò với error handling
        /// </summary>
        public void LoadData()
        {
            try
            {
                // Sử dụng AsNoTracking để tăng hiệu suất khi chỉ đọc dữ liệu
                _allStaffs = new ObservableCollection<Staff>(
                    DataProvider.Instance.Context.Staffs
                        .AsNoTracking()
                        .Include(d => d.Specialty)
                        .Include(d => d.Role)
                        .Where(d => (bool)!d.IsDeleted)
                        .ToList()
                        .Select(d => {
                            // Xử lý các thuộc tính string có thể null
                            d.FullName = d.FullName ?? "";
                            d.Phone = d.Phone ?? "";
                            d.Email = d.Email ?? "";
                            d.Schedule = d.Schedule ?? "";
                            d.Address = d.Address ?? "";
                            d.CertificateLink = d.CertificateLink ?? "";

                            return d;
                        })
                );

                DoctorList = new ObservableCollection<Staff>(_allStaffs);

                // Tải chuyên khoa với option "Tất cả"
                var specialties = DataProvider.Instance.Context.DoctorSpecialties
                    .AsNoTracking()
                    .Where(s => (bool)!s.IsDeleted)
                    .ToList()
                    .Select(s => {
                        s.SpecialtyName = s.SpecialtyName ?? "";
                        s.Description = s.Description ?? "";
                        return s;
                    })
                    .ToList();
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(specialties);

                // Thêm option "Tất cả chuyên khoa" vào đầu danh sách filter
                specialties.Insert(0, new DoctorSpecialty
                {
                    SpecialtyId = -1,
                    SpecialtyName = "-- Tất cả chuyên khoa --"
                });

                ListSpecialtyForFilter = new ObservableCollection<DoctorSpecialty>(specialties);

                // Tải vai trò với option "Tất cả"
                var roles = DataProvider.Instance.Context.Roles
                    .AsNoTracking()
                    .Where(r => (bool)!r.IsDeleted)
                    .ToList()
                    .Select(r => {
                        r.RoleName = r.RoleName ?? "";
                        r.Description = r.Description ?? "";
                        return r;
                    })
                    .ToList();

                // Thêm option "Tất cả vai trò" vào đầu danh sách
                roles.Insert(0, new Role
                {
                    RoleId = -1,
                    RoleName = "-- Tất cả vai trò --"
                });

                RoleList = new ObservableCollection<Role>(roles);

            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi");

                // Khởi tạo với collections rỗng để tránh null reference exceptions
                _allStaffs = new ObservableCollection<Staff>();
                DoctorList = new ObservableCollection<Staff>();
                ListSpecialty = new ObservableCollection<DoctorSpecialty>();
                ListSpecialtyForFilter = new ObservableCollection<DoctorSpecialty>();
                RoleList = new ObservableCollection<Role>();
            }
        }

        /// <summary>
        /// Reset tất cả filter về trạng thái ban đầu
        /// Hiển thị tất cả nhân viên
        /// </summary>
        private void ExecuteResetFilters()
        {
            SearchText = string.Empty;

            // Chọn item "Tất cả" trong các ComboBox thay vì set null
            SelectedRole = RoleList?.FirstOrDefault(r => r.RoleId == -1);
            SelectedSpecialty = IsSpecialtyVisible ?
                ListSpecialty?.FirstOrDefault(s => s.SpecialtyId == -1) : null;

            IsSpecialtyVisible = false;

            DoctorList = new ObservableCollection<Staff>(_allStaffs);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và lọc nhân viên
        /// Lọc theo chuyên khoa, vai trò và từ khóa tìm kiếm
        /// </summary>
        private void ExecuteSearch()
        {
            if (_allStaffs == null || _allStaffs.Count == 0)
            {
                DoctorList = new ObservableCollection<Staff>();
                return;
            }

            var filteredList = _allStaffs.AsEnumerable();

            // Lọc theo chuyên khoa nếu được chọn
            if (SelectedSpecialtyId.HasValue)
                filteredList = filteredList.Where(d => d.SpecialtyId == SelectedSpecialtyId && (bool)!d.IsDeleted);

            // Lọc theo vai trò nếu được chọn
            if (SelectedRoleId.HasValue)
                filteredList = filteredList.Where(d => d.RoleId == SelectedRoleId && (bool)!d.IsDeleted);

            // Lọc theo từ khóa tìm kiếm nếu được cung cấp
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = filteredList.Where(d =>
                    (d.FullName != null && d.FullName.ToLower().Contains(searchTerm)) ||
                    (d.Phone != null && d.Phone.ToLower().Contains(searchTerm)) ||
                    (d.Email != null && d.Email.ToLower().Contains(searchTerm))
                );
            }

            DoctorList = new ObservableCollection<Staff>(filteredList);
        }

        /// <summary>
        /// Xuất danh sách nhân viên ra file Excel
        /// Sử dụng ClosedXML với progress dialog và background thread
        /// Tự động điều chỉnh tiêu đề dựa trên filter đang áp dụng
        /// </summary>
        private void ExportToExcel()
        {
            try
            {
                // Tạo dialog chọn nơi lưu file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"DanhSachNhanVien_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Tạo và hiển thị progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Bắt đầu thao tác xuất trong background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                var worksheet = workbook.Worksheets.Add("Danh sách nhân viên");

                                // Báo cáo tiến trình: 5% - Tạo workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Thiết lập cột B làm điểm bắt đầu (tương tự StockMedicineViewModel)
                                int startColumn = 2;
                                int totalColumns = 8;

                                // Xác định tiêu đề dựa trên thiết lập filter
                                string title = "DANH SÁCH NHÂN VIÊN";
                                if (SelectedRoleId.HasValue)
                                {
                                    var roleName = RoleList.FirstOrDefault(r => r.RoleId == SelectedRoleId)?.RoleName;
                                    if (!string.IsNullOrEmpty(roleName))
                                    {
                                        title = $"DANH SÁCH {roleName.ToUpper()}";

                                        // Thêm thông tin chuyên khoa nếu có
                                        if (IsSpecialtyVisible && SelectedSpecialtyId.HasValue)
                                        {
                                            var specialtyName = ListSpecialty.FirstOrDefault(s => s.SpecialtyId == SelectedSpecialtyId)?.SpecialtyName;
                                            if (!string.IsNullOrEmpty(specialtyName) && specialtyName != "-- Tất cả chuyên khoa --")
                                            {
                                                title += $" - {specialtyName.ToUpper()}";
                                            }
                                        }
                                    }
                                }

                                // Thêm tiêu đề (merged cells), bắt đầu từ cột B
                                worksheet.Cell(1, startColumn).Value = title;
                                var titleRange = worksheet.Range(1, startColumn, 1, startColumn + totalColumns - 1);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Thêm ngày hiện tại, bắt đầu từ cột B
                                worksheet.Cell(2, startColumn).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, startColumn, 2, startColumn + totalColumns - 1);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Báo cáo tiến trình: 10% - Thêm tiêu đề
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Thêm headers (với khoảng cách 4 cell từ tiêu đề)
                                int headerRow = 6; // Hàng 6 (để lại 3 hàng trống sau tiêu đề)
                                int column = startColumn;

                                worksheet.Cell(headerRow, column++).Value = "ID";
                                worksheet.Cell(headerRow, column++).Value = "Họ và tên";
                                worksheet.Cell(headerRow, column++).Value = "Vai trò";
                                worksheet.Cell(headerRow, column++).Value = "Chuyên khoa";
                                worksheet.Cell(headerRow, column++).Value = "Điện thoại";
                                worksheet.Cell(headerRow, column++).Value = "Email";
                                worksheet.Cell(headerRow, column++).Value = "Lịch làm việc";
                                worksheet.Cell(headerRow, column++).Value = "Địa chỉ";

                                // Style cho header row
                                var headerRange = worksheet.Range(headerRow, startColumn, headerRow, startColumn + totalColumns - 1);
                                headerRange.Style.Font.Bold = true;
                                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Thêm viền cho header
                                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                // Báo cáo tiến trình: 20% - Headers thêm xong
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(20));

                                // Thêm dữ liệu
                                int row = headerRow + 1; // Bắt đầu dữ liệu từ hàng tiếp theo sau header
                                int totalStaffs = DoctorList.Count;

                                // Tạo data range (để áp dụng viền sau)
                                var dataStartRow = row;

                                for (int i = 0; i < totalStaffs; i++)
                                {
                                    var doctor = DoctorList[i];
                                    column = startColumn;

                                    worksheet.Cell(row, column++).Value = doctor.StaffId;
                                    worksheet.Cell(row, column++).Value = doctor.FullName ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Role?.RoleName ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Specialty?.SpecialtyName ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Phone ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Email ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Schedule ?? "";
                                    worksheet.Cell(row, column++).Value = doctor.Address ?? "";

                                    row++;

                                    // Cập nhật tiến trình dựa trên phần trăm nhân viên đã xử lý
                                    int progressValue = 20 + (i * 60 / totalStaffs);
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));

                                    // Thêm độ trễ nhỏ để hiển thị tiến trình
                                    Thread.Sleep(20);
                                }

                                // Báo cáo tiến trình: 80% - Dữ liệu thêm xong
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Áp dụng viền cho data range
                                if (totalStaffs > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, startColumn, row - 1, startColumn + totalColumns - 1);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                    // Căn giữa một số cột nhất định
                                    worksheet.Column(startColumn).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // ID
                                    worksheet.Column(startColumn + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Role
                                    worksheet.Column(startColumn + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Specialty
                                    worksheet.Column(startColumn + 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Phone
                                }

                                // Thêm hàng tổng
                                worksheet.Cell(row + 1, startColumn).Value = "Tổng số:";
                                worksheet.Cell(row + 1, startColumn + 1).Value = totalStaffs;
                                worksheet.Cell(row + 1, startColumn + 1).Style.Font.Bold = true;

                                // Auto-fit các cột
                                worksheet.Columns().AdjustToContents();

                                // Thiết lập độ rộng tối thiểu để dễ đọc và thiết lập độ rộng cột A để làm khoảng cách
                                worksheet.Column(1).Width = 3; // Cột khoảng cách
                                worksheet.Column(startColumn).Width = 10; // ID
                                worksheet.Column(startColumn + 1).Width = 25; // Name
                                worksheet.Column(startColumn + 3).Width = 20; // Specialty
                                worksheet.Column(startColumn + 6).Width = 80; // Schedule
                                worksheet.Column(startColumn + 7).Width = 50; // Address

                                // Báo cáo tiến trình: 90% - Định dạng hoàn tất
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Lưu workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Báo cáo tiến trình: 100% - File đã lưu
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));

                                // Độ trễ nhỏ để hiển thị 100%
                                Thread.Sleep(300);

                                // Đóng progress dialog
                                Application.Current.Dispatcher.Invoke(() => progressDialog.Close());

                                // Hiển thị thông báo thành công
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBoxService.ShowSuccess(
                                     $"Đã xuất danh sách nhân viên thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                     "Thành công");

                                    // Hỏi xem người dùng có muốn mở file Excel không
                                    if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                    {
                                        try
                                        {
                                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                            {
                                                FileName = saveFileDialog.FileName,
                                                UseShellExecute = true
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Đóng progress dialog khi có lỗi
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError(
                                    $"Lỗi khi xuất Excel: {ex.Message}",
                                    "Lỗi");
                            });
                        }
                    });

                    // Hiển thị dialog - sẽ block cho đến khi dialog đóng
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                                   $"Lỗi khi xuất Excel: {ex.Message}",
                                   "Lỗi");
            }
        }

        #region Specialty Methods

        /// <summary>
        /// Thêm chuyên khoa mới
        /// Bao gồm validation, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void AddSpecialty()
        {
            try
            {
                // Bật validation cho tất cả field
                _isValidating = true;
                _touchedFields.Add(nameof(SpecialtyName));
                _touchedFields.Add(nameof(Description));

                // Kích hoạt validation bằng cách thông báo property changes
                OnPropertyChanged(nameof(SpecialtyName));
                OnPropertyChanged(nameof(Description));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm chuyên khoa.",
                        "Lỗi thông tin");
                    return;
                }

                // Xác nhận từ người dùng trước khi thêm chuyên khoa
                bool result = MessageBoxService.ShowQuestion(
                     $"Bạn có chắc muốn thêm chuyên khoa '{SpecialtyName}' không?",
                     "Xác Nhận Thêm");
                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra chuyên khoa đã tồn tại chưa
                        bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                            .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() && (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Chuyên khoa này đã tồn tại.", "Trùng dữ liệu");
                            return;
                        }

                        // Tạo chuyên khoa mới
                        var newSpecialty = new DoctorSpecialty
                        {
                            SpecialtyName = SpecialtyName,
                            Description = Description ?? "",
                            IsDeleted = false
                        };

                        // Thêm chuyên khoa vào cơ sở dữ liệu
                        DataProvider.Instance.Context.DoctorSpecialties.Add(newSpecialty);
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi thành công
                        transaction.Commit();

                        // Cập nhật danh sách chuyên khoa trong giao diện
                        ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                            DataProvider.Instance.Context.DoctorSpecialties
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        SpecialtyName = "";
                        Description = "";

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm chuyên khoa thành công!",
                            "Thành Công");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi cơ sở dữ liệu
                MessageBoxService.ShowError(
                    $"Không thể thêm chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Chỉnh sửa chuyên khoa đã chọn
        /// Kiểm tra trùng lặp (ngoại trừ chính nó) và sử dụng transaction
        /// </summary>
        private void EditSpecialty()
        {
            try
            {
                // Bật validation cho tất cả field
                _isValidating = true;
                _touchedFields.Add(nameof(SpecialtyName));
                _touchedFields.Add(nameof(Description));

                // Kích hoạt validation bằng cách thông báo property changes
                OnPropertyChanged(nameof(SpecialtyName));
                OnPropertyChanged(nameof(Description));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi sửa chuyên khoa.",
                        "Lỗi thông tin");
                    return;
                }

                // Xác nhận từ người dùng trước khi sửa chuyên khoa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa chuyên khoa '{SelectedItem.SpecialtyName}'không?",
                    "Xác Nhận Sửa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra tên chuyên khoa mới đã tồn tại chưa (trừ chính nó)
                        bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                            .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() &&
                                      s.SpecialtyId != SelectedItem.SpecialtyId &&
                                     (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Tên chuyên khoa này đã tồn tại.", "Trùng dữ liệu");
                            return;
                        }

                        // Tìm chuyên khoa cần cập nhật
                        var specialtyToUpdate = DataProvider.Instance.Context.DoctorSpecialties
                            .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                        if (specialtyToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy chuyên khoa cần sửa.", "Dữ liệu không tồn tại");
                            return;
                        }

                        // Cập nhật thông tin chuyên khoa
                        specialtyToUpdate.SpecialtyName = SpecialtyName;
                        specialtyToUpdate.Description = Description ?? "";

                        // Lưu thay đổi
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi thành công
                        transaction.Commit();

                        // Cập nhật danh sách chuyên khoa trong giao diện
                        ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                            DataProvider.Instance.Context.DoctorSpecialties
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Cập nhật danh sách nhân viên vì tên chuyên khoa có thể đã thay đổi
                        LoadData();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã cập nhật chuyên khoa thành công!",
                            "Thành Công");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi cơ sở dữ liệu
                MessageBoxService.ShowError(
                    $"Không thể sửa chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Xóa chuyên khoa đã chọn (soft delete)
        /// Kiểm tra ràng buộc với nhân viên trước khi xóa
        /// </summary>
        private void DeleteSpecialty()
        {
            try
            {
                // Xác nhận từ người dùng trước khi xóa chuyên khoa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa chuyên khoa '{SelectedItem.SpecialtyName}' không?",
                    "Xác Nhận Xóa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm chuyên khoa cần xóa
                        var specialtyToDelete = DataProvider.Instance.Context.DoctorSpecialties
                            .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                        if (specialtyToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy chuyên khoa cần xóa.", "Dữ liệu không tồn tại");
                            return;
                        }

                        // Thực hiện xóa mềm (soft delete) bằng cách đánh dấu IsDeleted = true
                        specialtyToDelete.IsDeleted = true;

                        // Lưu thay đổi
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi thành công
                        transaction.Commit();

                        // Cập nhật danh sách chuyên khoa trong giao diện
                        ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                            DataProvider.Instance.Context.DoctorSpecialties
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa lựa chọn và làm mới các trường nhập liệu
                        SelectedItem = null;
                        SpecialtyName = "";
                        Description = "";

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã xóa chuyên khoa thành công.",
                            "Thành Công");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa chuyên khoa: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật quyền dựa trên vai trò của tài khoản hiện tại
        /// Chỉ Admin và Manager mới có quyền thao tác với chuyên khoa
        /// </summary>
        private void UpdatePermissions()
        {
            // Mặc định không có quyền gì
            CanModifySpecialties = false;

            if (CurrentAccount == null)
                return;

            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Thiết lập quyền dựa trên vai trò
            switch (role)
            {
                case UserRoles.Admin:
                case UserRoles.Manager:
                    CanModifySpecialties = true;
                    break;

                case UserRoles.Doctor:
                case UserRoles.Pharmacist:
                case UserRoles.Cashier:
                default:
                    CanModifySpecialties = false;
                    break;
            }

            // Buộc command CanExecute được đánh giá lại
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Làm mới form chuyên khoa - xóa tất cả input
        /// </summary>
        private void RefeshSpecialty()
        {
            SpecialtyName = string.Empty;
            Description = string.Empty;
            SelectedSpecialty = null;
        }

        #region Validation

        /// <summary>
        /// Indexer cho IDataErrorInfo - thực hiện validation cho từng property
        /// Chỉ validate khi người dùng đã tương tác với form hoặc khi submit
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                // Chỉ validate khi user đã tương tác với form hoặc khi submit
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(SpecialtyName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(SpecialtyName))
                        {
                            error = "Tên chuyên khoa không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(SpecialtyName))
                        {
                            if (SpecialtyName.Length < 2)
                                error = "Tên chuyên khoa phải có ít nhất 2 ký tự";
                            else if (SpecialtyName.Length > 50)
                                error = "Tên chuyên khoa không được vượt quá 50 ký tự";
                        }
                        break;

                    case nameof(Description):
                        if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 255)
                        {
                            error = "Mô tả không được vượt quá 255 ký tự";
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Kiểm tra xem có lỗi validation nào không
        /// Kết hợp tất cả field có thể có lỗi
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(SpecialtyName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Description)]);
            }
        }

        #endregion

        #endregion
        #endregion
    }
}