using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý cửa sổ chi tiết nhân viên
    /// Cung cấp chức năng xem, chỉnh sửa thông tin nhân viên và quản lý tài khoản
    /// Bao gồm quản lý lịch hẹn, đặt lại mật khẩu và tạo tài khoản mới
    /// Triển khai IDataErrorInfo để validation dữ liệu nhân viên
    /// Hỗ trợ touched-based validation để UX tốt hơn
    /// </summary>
    public class StaffDetailsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties

        /// <summary>
        /// Cờ hiệu xác định nhân viên có phải là bác sĩ không
        /// Dựa trên RoleId hoặc tên vai trò chứa "Bác sĩ"
        /// </summary>
        private bool _isDoctor = false;
        public bool IsDoctor
        {
            get => _isDoctor;
            set
            {
                _isDoctor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tài khoản người dùng hiện tại
        /// Được sử dụng để kiểm tra quyền chỉnh sửa và xóa nhân viên
        /// </summary>
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                UpdatePermissions(); // Cập nhật quyền UI khi tài khoản thay đổi
            }
        }

        /// <summary>
        /// Quyền chỉnh sửa thông tin nhân viên
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canEditStaff = false;
        public bool CanEditStaff
        {
            get => _canEditStaff;
            set
            {
                _canEditStaff = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cập nhật quyền UI dựa trên vai trò của tài khoản hiện tại
        /// Admin và Manager có quyền chỉnh sửa nhân viên
        /// </summary>
        private void UpdatePermissions()
        {
            // Mặc định không có quyền gì
            CanEditStaff = false;

            // Kiểm tra xem tài khoản hiện tại có tồn tại không
            if (CurrentAccount == null)
                return;

            // Kiểm tra quyền dựa trên vai trò
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Admin và Manager có quyền đầy đủ
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                CanEditStaff = true;
            }

            // Buộc command CanExecute được đánh giá lại
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Danh sách lịch hẹn của nhân viên (hiển thị thân thiện)
        /// Chứa thông tin đã được format để hiển thị trong DataGrid
        /// </summary>
        private ObservableCollection<AppointmentDisplayInfo>? _doctorAppointmentsDisplay;
        public ObservableCollection<AppointmentDisplayInfo> DoctorAppointmentsDisplay
        {
            get => _doctorAppointmentsDisplay ??= new ObservableCollection<AppointmentDisplayInfo>();
            set
            {
                _doctorAppointmentsDisplay = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tham chiếu đến cửa sổ chứa ViewModel này
        /// Được sử dụng để đóng cửa sổ
        /// </summary>
        private Window? _window;

        /// <summary>
        /// Đối tượng nhân viên chính được quản lý
        /// Khi thay đổi sẽ tự động tải dữ liệu liên quan
        /// </summary>
        private Staff? _doctor;
        public Staff? Doctor
        {
            get => _doctor;
            set
            {
                _doctor = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadRelatedData();
                }
            }
        }

        /// <summary>
        /// Error property cho IDataErrorInfo - trả về null vì validation per-property
        /// </summary>
        public string? Error => null;

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

        // === THUỘC TÍNH THÔNG TIN NHÂN VIÊN ===

        /// <summary>
        /// ID nhân viên - chỉ đọc
        /// </summary>
        private int _StaffId;
        public int StaffId
        {
            get => _StaffId;
            set
            {
                _StaffId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Họ và tên nhân viên - có validation và theo dõi touched state
        /// Trường bắt buộc với độ dài tối thiểu 2 ký tự
        /// </summary>
        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    // Thêm vào touched fields khi có thay đổi và giá trị không rỗng
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_fullName))
                        _touchedFields.Add(nameof(FullName));
                    else
                        _touchedFields.Remove(nameof(FullName)); // Xóa nếu rỗng

                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Chuyên khoa được chọn cho nhân viên
        /// Chỉ áp dụng cho bác sĩ, các vai trò khác có thể để null
        /// </summary>
        private DoctorSpecialty? _selectedSpecialty;
        public DoctorSpecialty? SelectedSpecialty
        {
            get => _selectedSpecialty;
            set
            {
                _selectedSpecialty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chuyên khoa có sẵn trong hệ thống
        /// Sử dụng cho ComboBox chọn chuyên khoa
        /// </summary>
        private ObservableCollection<DoctorSpecialty> _specialtyList = new();
        public ObservableCollection<DoctorSpecialty> SpecialtyList
        {
            get => _specialtyList;
            set
            {
                _specialtyList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số điện thoại nhân viên - có validation và theo dõi touched state
        /// Trường bắt buộc với định dạng số điện thoại Việt Nam
        /// </summary>
        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_phone))
                        _touchedFields.Add(nameof(Phone));
                    else
                        _touchedFields.Remove(nameof(Phone));
                    _phone = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Lịch làm việc của nhân viên - có validation định dạng phức tạp
        /// Hỗ trợ nhiều định dạng: "T2-T6: 8h-17h", "T2, T3: 7h-13h", "T2: 8h-12h, 13h-17h"
        /// </summary>
        private string _schedule = string.Empty;
        public string Schedule
        {
            get => _schedule;
            set
            {
                if (_schedule != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_schedule))
                        _touchedFields.Add(nameof(Schedule));
                    else
                        _touchedFields.Remove(nameof(Schedule));

                    _schedule = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Địa chỉ nhân viên - trường tùy chọn
        /// </summary>
        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_address))
                        _touchedFields.Add(nameof(Address));
                    else
                        _touchedFields.Remove(nameof(Address));

                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Email nhân viên - có validation định dạng email và theo dõi touched state
        /// Trường bắt buộc
        /// </summary>
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_email))
                        _touchedFields.Add(nameof(Email));
                    else
                        _touchedFields.Remove(nameof(Email));

                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Đường dẫn tới chứng chỉ/bằng cấp - có validation URL và theo dõi touched state
        /// Trường tùy chọn nhưng nếu nhập phải đúng định dạng URL
        /// </summary>
        private string _certificateLink = string.Empty;
        public string CertificateLink
        {
            get => _certificateLink;
            set
            {
                if (_certificateLink != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_certificateLink))
                        _touchedFields.Add(nameof(CertificateLink));
                    else
                        _touchedFields.Remove(nameof(CertificateLink));

                    _certificateLink = value;
                    OnPropertyChanged();
                }
            }
        }

        // === THÔNG TIN TÀI KHOẢN ===

        /// <summary>
        /// Tên đăng nhập - chỉ đọc, lấy từ tài khoản liên kết
        /// </summary>
        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Vai trò/chức vụ - chỉ đọc, lấy từ bảng Roles
        /// </summary>
        private string _role = string.Empty;
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cờ hiệu xác định nhân viên có tài khoản đăng nhập không
        /// Ảnh hưởng đến hiển thị UI và khả năng thực thi commands
        /// </summary>
        private bool _hasAccount;
        public bool HasAccount
        {
            get => _hasAccount;
            set
            {
                _hasAccount = value;
                OnPropertyChanged(nameof(_hasAccount));
                // Refresh trạng thái can-execute của commands
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Tên đăng nhập mới cho việc tạo tài khoản
        /// Có validation và theo dõi touched state
        /// </summary>
        private string _newUsername = string.Empty;
        public string NewUsername
        {
            get => _newUsername;
            set
            {
                if (_newUsername != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_newUsername))
                        _touchedFields.Add(nameof(NewUsername));
                    else
                        _touchedFields.Remove(nameof(NewUsername));

                    _newUsername = value;
                    OnPropertyChanged();
                    // Refresh AddDoctorAccountCommand can-execute khi username thay đổi
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // === THUỘC TÍNH LỌC LỊCH HẸN ===

        /// <summary>
        /// Ngày bắt đầu cho việc lọc lịch hẹn
        /// Mặc định là 1 tháng trước
        /// </summary>
        private DateTime _appointmentStartDate = DateTime.Today.AddMonths(-1);
        public DateTime AppointmentStartDate
        {
            get => _appointmentStartDate;
            set
            {
                _appointmentStartDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày kết thúc cho việc lọc lịch hẹn
        /// Mặc định là ngày hiện tại
        /// </summary>
        private DateTime _appointmentEndDate = DateTime.Today;
        public DateTime AppointmentEndDate
        {
            get => _appointmentEndDate;
            set
            {
                _appointmentEndDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách các trạng thái lịch hẹn để lọc
        /// Bao gồm "Tất cả", "Đang chờ", "Đang khám", "Đã khám", "Đã hủy"
        /// </summary>
        private ObservableCollection<string> _appointmentStatusList = new();
        public ObservableCollection<string> AppointmentStatusList
        {
            get => _appointmentStatusList;
            set
            {
                _appointmentStatusList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Trạng thái lịch hẹn được chọn để lọc
        /// Mặc định là "Tất cả"
        /// </summary>
        private string _selectedAppointmentStatus = string.Empty;
        public string SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách lịch hẹn gốc của nhân viên
        /// Sử dụng làm nguồn dữ liệu cho DoctorAppointmentsDisplay
        /// </summary>
        private ObservableCollection<Appointment> _doctorAppointments = new();
        public ObservableCollection<Appointment> DoctorAppointments
        {
            get => _doctorAppointments;
            set
            {
                _doctorAppointments = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands

        /// <summary>
        /// Lệnh xử lý khi cửa sổ được load
        /// </summary>
        public ICommand LoadedWindowCommand { get; set; }

        /// <summary>
        /// Lệnh cập nhật thông tin nhân viên
        /// </summary>
        public ICommand UpdateDoctorInfoCommand { get; set; }

        /// <summary>
        /// Lệnh xóa nhân viên (soft delete)
        /// </summary>
        public ICommand DeleteDoctorCommand { get; set; }

        /// <summary>
        /// Lệnh đặt lại mật khẩu cho tài khoản nhân viên
        /// </summary>
        public ICommand ChangePasswordCommand { get; set; }

        /// <summary>
        /// Lệnh tìm kiếm lịch hẹn của nhân viên
        /// </summary>
        public ICommand SearchAppointmentsCommand { get; set; }

        /// <summary>
        /// Lệnh xem chi tiết lịch hẹn
        /// </summary>
        public ICommand ViewAppointmentDetailsCommand { get; set; }

        /// <summary>
        /// Lệnh tạo tài khoản mới cho nhân viên
        /// </summary>
        public ICommand AddDoctorAccountCommand { get; set; }
        #endregion

        /// <summary>
        /// Constructor khởi tạo StaffDetailsWindowViewModel
        /// Thiết lập commands, dữ liệu ban đầu và lấy tài khoản hiện tại
        /// </summary>
        public StaffDetailsWindowViewModel()
        {
            InitializeCommands();
            InitializeData();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
        }

        #region Initialization

        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        private void InitializeCommands()
        {
            // Command xử lý khi cửa sổ được load - lưu reference đến window
            LoadedWindowCommand = new RelayCommand<Window>((w) => _window = w, (p) => true);

            // Command cập nhật thông tin nhân viên - yêu cầu quyền và validation
            UpdateDoctorInfoCommand = new RelayCommand<object>(
                (p) => ExecuteUpdateDoctorInfo(),
                (p) => CanUpdateDoctorInfo()
            );

            // Command xóa nhân viên - yêu cầu có nhân viên được chọn
            DeleteDoctorCommand = new RelayCommand<object>(
                (p) => ExecuteDeleteDoctor(),
                (p) => Doctor != null
            );

            // Command tìm kiếm lịch hẹn - yêu cầu có nhân viên được chọn
            SearchAppointmentsCommand = new RelayCommand<object>(
                (p) => LoadAppointments(),
                (p) => Doctor != null
            );

            // Command xem chi tiết lịch hẹn - yêu cầu có lịch hẹn được chọn
            ViewAppointmentDetailsCommand = new RelayCommand<AppointmentDisplayInfo>(
                (p) => ViewAppointmentDetails(p),
                (p) => p != null
            );

            // Command đặt lại mật khẩu - chỉ enable khi có tài khoản
            ChangePasswordCommand = new RelayCommand<object>(
               (p) => ExecuteResetPassword(),
               (p) => Doctor != null && HasAccount
           );

            // Command tạo tài khoản mới - yêu cầu điều kiện phức tạp
            AddDoctorAccountCommand = new RelayCommand<object>(
                (p) => ExecuteAddDoctorAccount(),
                (p) => CanAddDoctorAccount()
            );
        }

        /// <summary>
        /// Khởi tạo dữ liệu ban đầu cho ViewModel
        /// Thiết lập danh sách trạng thái lịch hẹn và collections
        /// </summary>
        private void InitializeData()
        {
            // Khởi tạo danh sách trạng thái lịch hẹn
            AppointmentStatusList = new ObservableCollection<string>
            {
                "Tất cả",
                "Đang chờ",
                "Đang khám",
                "Đã khám",
                "Đã hủy"
            };

            SelectedAppointmentStatus = "Tất cả";

            // Khởi tạo collections để tránh null reference
            DoctorAppointments = new ObservableCollection<Appointment>();
            DoctorAppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
        }

        /// <summary>
        /// Tải tất cả dữ liệu liên quan đến nhân viên
        /// Bao gồm thông tin cá nhân, chuyên khoa, vai trò, tài khoản và lịch hẹn
        /// </summary>
        private void LoadRelatedData()
        {
            if (Doctor == null) return;

            // Tải thông tin nhân viên cơ bản
            StaffId = Doctor.StaffId;
            FullName = Doctor.FullName;
            Phone = Doctor.Phone ?? string.Empty;
            Email = Doctor.Email ?? string.Empty;
            Schedule = Doctor.Schedule ?? string.Empty;
            Address = Doctor.Address ?? string.Empty;
            CertificateLink = Doctor.CertificateLink ?? string.Empty;

            // Tải danh sách chuyên khoa
            SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                DataProvider.Instance.Context.DoctorSpecialties
                .Where(s => s.IsDeleted != true)
                .ToList()
            );

            // Thiết lập chuyên khoa được chọn
            SelectedSpecialty = SpecialtyList.FirstOrDefault(s => s.SpecialtyId == Doctor.SpecialtyId);

            // Lấy tên vai trò của nhân viên từ database
            var staffRole = DataProvider.Instance.Context.Roles
                .FirstOrDefault(r => r.RoleId == Doctor.RoleId && r.IsDeleted != true);

            Role = staffRole?.RoleName ?? string.Empty;

            // Thiết lập thuộc tính IsDoctor dựa trên vai trò
            IsDoctor = Doctor.RoleId == 1 || (staffRole?.RoleName?.Contains("Bác sĩ") == true);

            // Kiểm tra xem nhân viên có tài khoản đăng nhập không
            var account = DataProvider.Instance.Context.Accounts
                .FirstOrDefault(a => a.StaffId == Doctor.StaffId && a.IsDeleted != true);

            if (account != null)
            {
                UserName = account.Username;
                NewUsername = account.Username; // Đồng bộ với username hiện tại
                HasAccount = true;  // Thiết lập HasAccount = true khi có tài khoản
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                UserName = string.Empty;
                HasAccount = false;  // Thiết lập HasAccount = false khi không có tài khoản
                CommandManager.InvalidateRequerySuggested();
            }

            // Tải lịch hẹn
            LoadAppointments();

            // Reset trạng thái validation
            _touchedFields.Clear();
            _isValidating = false;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Indexer cho IDataErrorInfo - thực hiện validation cho từng property
        /// Chỉ validate khi người dùng đã tương tác với form hoặc khi submit
        /// </summary>
        public string? this[string columnName]
        {
            get
            {
                // Chỉ validate khi user đã tương tác với form hoặc khi submit
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string? error = null;

                switch (columnName)
                {
                    case nameof(FullName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ và tên không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ và tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Phone))
                        {
                            error = "Số điện thoại không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(Phone) &&
                                !Regex.IsMatch(Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(Schedule):
                        if (!string.IsNullOrWhiteSpace(Schedule) && !IsValidScheduleFormat(Schedule))
                        {
                            error = "Lịch làm việc không đúng định dạng. Vui lòng nhập theo mẫu:\n" +
                                    "- T2, T3, T4: 7h-13h\n" +
                                    "- T2-T7: 7h-11h\n" +
                                    "- T2: 9h-11h\n" +
                                    "- T2, T3, T4: 8h-12h, 13h30-17h";
                        }
                        break;

                    case nameof(CertificateLink):
                        // Chỉ validate nếu người dùng đã nhập gì đó
                        if (!string.IsNullOrWhiteSpace(CertificateLink) && !IsValidUrl(CertificateLink))
                        {
                            error = "Link chứng chỉ không đúng định dạng URL";
                        }
                        break;

                    case nameof(NewUsername):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(NewUsername))
                        {
                            error = "Tên đăng nhập không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(NewUsername) && NewUsername.Trim().Length < 4)
                        {
                            error = "Tên đăng nhập phải có ít nhất 4 ký tự";
                        }
                        // Có thể thêm validation kiểm tra ký tự đặc biệt nếu cần
                        break;

                    case nameof(Email):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Email))
                        {
                            error = "Email không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(Email) && !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
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
                return !string.IsNullOrEmpty(this[nameof(FullName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Phone)]) ||
                       !string.IsNullOrEmpty(this[nameof(Email)]) ||
                       !string.IsNullOrEmpty(this[nameof(Schedule)]) ||
                       !string.IsNullOrEmpty(this[nameof(CertificateLink)]);
            }
        }

        /// <summary>
        /// Kiểm tra xem có lỗi validation cho username không
        /// Dùng riêng cho việc tạo tài khoản mới
        /// </summary>
        public bool HasUsernameErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(NewUsername)]);
            }
        }

        /// <summary>
        /// Validation cho định dạng lịch làm việc
        /// Hỗ trợ nhiều định dạng phức tạp cho lịch làm việc
        /// Ví dụ: "T2, T3, T4: 7h-13h", "T2-T7: 8h-17h", "T2: 8h-12h, 13h30-17h"
        /// </summary>
        /// <param name="schedule">Chuỗi lịch làm việc cần validate</param>
        /// <returns>True nếu định dạng hợp lệ</returns>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Lịch rỗng là hợp lệ (không bắt buộc)

            // Hỗ trợ nhiều pattern

            // Pattern 1: "T2, T3, T4: 7h-13h"
            string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

            // Pattern 2: "T2, T3, T4, T5, T6: 8h-12h, 13h30-17h"
            string pattern2 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)*$";

            // Pattern 3: "T2-T7: 7h-11h"
            string pattern3 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

            // Kiểm tra xem có pattern nào match không
            if (Regex.IsMatch(schedule, pattern1) ||
                Regex.IsMatch(schedule, pattern2) ||
                Regex.IsMatch(schedule, pattern3))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validation cho URL
        /// Kiểm tra xem chuỗi có phải là URL hợp lệ không
        /// </summary>
        /// <param name="url">Chuỗi URL cần validate</param>
        /// <returns>True nếu là URL hợp lệ</returns>
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // URL rỗng là hợp lệ (không bắt buộc)

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Kiểm tra điều kiện có thể cập nhật thông tin nhân viên
        /// Yêu cầu quyền chỉnh sửa, thông tin cơ bản đầy đủ và có chuyên khoa
        /// </summary>
        /// <returns>True nếu có thể cập nhật</returns>
        private bool CanUpdateDoctorInfo()
        {
            return CanEditStaff &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedSpecialty != null;
        }

        /// <summary>
        /// Thực hiện cập nhật thông tin nhân viên
        /// Bao gồm validation đầy đủ, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void ExecuteUpdateDoctorInfo()
        {
            try
            {
                if (Doctor == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!", "Lỗi");
                    return;
                }

                // Bật validation cho tất cả các trường
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Email));
                _touchedFields.Add(nameof(Schedule));
                _touchedFields.Add(nameof(CertificateLink));

                // Kích hoạt validation cho các trường bắt buộc
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Schedule));
                OnPropertyChanged(nameof(CertificateLink));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowError(
                        "Vui lòng sửa các lỗi nhập liệu trước khi cập nhật thông tin bác sĩ.",
                        "Lỗi thông tin");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra email đã tồn tại chưa (ngoại trừ nhân viên hiện tại)
                        bool emailExits = DataProvider.Instance.Context.Staffs
                                        .Any(d => d.Email == Email.Trim() && d.StaffId != Doctor.StaffId && d.IsDeleted == false);
                        if (emailExits)
                        {
                            MessageBoxService.ShowError(
                                        "Email đã tồn tại. Vui lòng sử dụng email khác.",
                                        "Lỗi Dữ Liệu");
                            return;
                        }

                        // Kiểm tra số điện thoại đã tồn tại chưa (ngoại trừ nhân viên hiện tại)
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && d.StaffId != Doctor.StaffId && d.IsDeleted == false);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowError(
                                "Số điện thoại này đã được sử dụng bởi một bác sĩ khác.",
                                "Lỗi Dữ Liệu");
                            return;
                        }

                        // Lấy thông tin nhân viên cần cập nhật
                        var doctorToUpdate = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == Doctor.StaffId);

                        if (doctorToUpdate != null)
                        {
                            // Cập nhật thông tin
                            doctorToUpdate.FullName = FullName.Trim();
                            doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                            doctorToUpdate.CertificateLink = CertificateLink.Trim();
                            doctorToUpdate.Email = Email.Trim();
                            doctorToUpdate.Schedule = Schedule.Trim();
                            doctorToUpdate.Phone = Phone.Trim();
                            doctorToUpdate.Address = Address.Trim();

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowMessage(
                                "Đã cập nhật thông tin bác sĩ thành công!",
                                "Thành Công");
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy thông tin bác sĩ!",
                                "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi cập nhật thông tin bác sĩ: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Thực hiện xóa mềm nhân viên
        /// Kiểm tra ràng buộc lịch hẹn và đồng thời xóa tài khoản liên kết
        /// </summary>
        private void ExecuteDeleteDoctor()
        {
            try
            {
                if (Doctor == null) return;

                // Kiểm tra xem nhân viên có lịch hẹn đang chờ hoặc đang khám không
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.StaffId == Doctor.StaffId &&
                             (a.Status == "Đang chờ" || a.Status == "Đang khám") &&
                             a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa bác sĩ này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng giải quyết các lịch hẹn hiện tại trước khi xóa.",
                        "Cảnh Báo");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowSuccess(
                    $"Bạn có chắc muốn xóa bác sĩ {FullName} không?\n" +
                    "Lưu ý: Tài khoản liên kết với bác sĩ này cũng sẽ bị xóa.",
                    "Xác Nhận Xóa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm và đánh dấu xóa nhân viên
                        var doctorToDelete = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == Doctor.StaffId);

                        if (doctorToDelete != null)
                        {
                            // Đánh dấu xóa mềm nhân viên
                            doctorToDelete.IsDeleted = true;

                            // Đồng thời xóa mềm tài khoản liên kết
                            var accountToDelete = DataProvider.Instance.Context.Accounts
                                .FirstOrDefault(a => a.StaffId == Doctor.StaffId);

                            if (accountToDelete != null)
                            {
                                accountToDelete.IsDeleted = true;
                            }

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                "Đã xóa bác sĩ thành công!",
                                "Thành Công");

                            // Đóng cửa sổ
                            _window?.Close();
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy thông tin bác sĩ!",
                                "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa bác sĩ: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Thực hiện đặt lại mật khẩu cho tài khoản nhân viên
        /// Mật khẩu mới được đặt thành "1111" (đã hash)
        /// </summary>
        private void ExecuteResetPassword()
        {
            try
            {
                if (Doctor == null || !HasAccount)
                {
                    MessageBoxService.ShowError(
                        "Không thể đặt lại mật khẩu. Bác sĩ này chưa có tài khoản!",
                        "Lỗi");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc muốn đặt lại mật khẩu cho tài khoản này không?\n" +
                    "Mật khẩu mới sẽ là: 1111",
                    "Xác Nhận Đặt Lại Mật Khẩu");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm tài khoản liên kết với nhân viên
                        var account = DataProvider.Instance.Context.Accounts
                            .FirstOrDefault(a => a.StaffId == Doctor.StaffId && a.IsDeleted != true);

                        if (account != null)
                        {
                            // Đặt lại mật khẩu thành "1111"
                            string hashedPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode("1111"));
                            account.Password = hashedPassword;

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                "Đã đặt lại mật khẩu thành công!\n" +
                                "Mật khẩu mới: 1111",
                                "Thành Công");
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy tài khoản liên kết với bác sĩ này!",
                                "Lỗi");

                            // Cập nhật lại trạng thái HasAccount
                            HasAccount = false;
                            CommandManager.InvalidateRequerySuggested();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi đặt lại mật khẩu: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Tải danh sách lịch hẹn của nhân viên
        /// Lọc theo khoảng thời gian và trạng thái, tạo display objects thân thiện
        /// </summary>
        private void LoadAppointments()
        {
            try
            {
                if (Doctor == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.StaffId == Doctor.StaffId &&
                               a.IsDeleted != true &&
                               a.AppointmentDate.Date >= AppointmentStartDate.Date &&
                               a.AppointmentDate.Date <= AppointmentEndDate.Date);

                // Áp dụng filter theo trạng thái nếu không phải "Tất cả"
                if (!string.IsNullOrEmpty(SelectedAppointmentStatus) && SelectedAppointmentStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus);
                }

                var appointments = query.OrderByDescending(a => a.AppointmentDate)
                                        .ThenBy(a => a.AppointmentDate.TimeOfDay)
                                        .ToList();

                // Lưu danh sách lịch hẹn gốc để tham chiếu
                DoctorAppointments = new ObservableCollection<Appointment>(appointments);

                // Tạo các object hiển thị thân thiện với thời gian đã format và lý do
                DoctorAppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>(
                    appointments.Select(a => new AppointmentDisplayInfo
                    {
                        AppointmentId = a.AppointmentId,
                        PatientName = a.Patient.FullName,
                        AppointmentDate = a.AppointmentDate.Date,
                        AppointmentTimeString = a.AppointmentDate.ToString("HH:mm"),
                        Status = a.Status,
                        Reason = a.Notes ?? string.Empty,
                        OriginalAppointment = a
                    })
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}",
                    "Lỗi"
                      );
            }
        }

        /// <summary>
        /// Hiển thị chi tiết lịch hẹn trong popup
        /// Bao gồm thông tin bệnh nhân, ngày giờ, trạng thái và lý do khám
        /// </summary>
        /// <param name="appointmentInfo">Thông tin lịch hẹn cần hiển thị</param>
        private void ViewAppointmentDetails(AppointmentDisplayInfo appointmentInfo)
        {
            // Hiển thị chi tiết lịch hẹn với ngày và giờ từ DateTime field
            MessageBoxService.ShowInfo($"Chi tiết lịch hẹn của bệnh nhân {appointmentInfo.PatientName}\n" +
                           $"Ngày: {appointmentInfo.AppointmentDate.ToString("dd/MM/yyyy")}\n" +
                           $"Giờ: {appointmentInfo.AppointmentTimeString}\n" +
                           $"Trạng thái: {appointmentInfo.Status}\n" +
                           $"Lý do khám: {appointmentInfo.Reason}",
                           "Chi tiết lịch hẹn"
                            );
        }

        #region Account Management

        /// <summary>
        /// Kiểm tra điều kiện có thể tạo tài khoản cho nhân viên
        /// Yêu cầu chưa có tài khoản, username hợp lệ và có vai trò
        /// </summary>
        /// <returns>True nếu có thể tạo tài khoản</returns>
        private bool CanAddDoctorAccount()
        {
            if (HasAccount)
                return false;
            // Chỉ cho phép thêm khi chưa có tài khoản và username hợp lệ
            bool isValidUsername = !string.IsNullOrWhiteSpace(NewUsername) && NewUsername.Trim().Length >= 4 && !HasUsernameErrors;

            return Doctor != null && !HasAccount && isValidUsername && !string.IsNullOrEmpty(Role);
        }

        /// <summary>
        /// Thực hiện tạo tài khoản mới cho nhân viên
        /// Kiểm tra trùng lặp username và sử dụng transaction
        /// Mật khẩu mặc định là "1111" (đã hash)
        /// </summary>
        private void ExecuteAddDoctorAccount()
        {
            try
            {
                if (Doctor == null) return;

                // Bật validation cho trường tên đăng nhập
                _isValidating = true;
                _touchedFields.Add(nameof(NewUsername));

                // Kích hoạt validation
                OnPropertyChanged(nameof(NewUsername));

                // Kiểm tra lỗi validation
                if (HasUsernameErrors)
                {
                    MessageBoxService.ShowError(
                        "Tên đăng nhập không hợp lệ. Vui lòng nhập tên đăng nhập có ít nhất 4 ký tự.",
                        "Lỗi thông tin");
                    return;
                }

                // Kiểm tra tên đăng nhập đã tồn tại chưa
                bool usernameExists = DataProvider.Instance.Context.Accounts
                    .Any(a => a.Username == NewUsername.Trim() && a.IsDeleted != true);

                if (usernameExists)
                {
                    MessageBoxService.ShowError(
                        "Tên đăng nhập đã tồn tại. Vui lòng chọn tên đăng nhập khác.",
                        "Lỗi Dữ Liệu");
                    return;
                }

                // Yêu cầu xác nhận từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn tạo tài khoản cho {FullName} không?\n" +
                    $"Tên đăng nhập: {NewUsername.Trim()}\n" +
                    $"Mật khẩu mặc định: 1111\n" +
                    $"Vai trò: {Role}",
                    "Xác Nhận Tạo Tài Khoản");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tạo tài khoản mới với mật khẩu mặc định "1111"
                        var defaultPassword = "1111";
                        var newAccount = new Account
                        {
                            Username = NewUsername.Trim(),
                            Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(defaultPassword)),
                            StaffId = Doctor.StaffId,
                            Role = Role, // Sử dụng vai trò của nhân viên
                            IsLogined = false,
                            IsDeleted = false
                        };

                        // Thêm tài khoản vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Accounts.Add(newAccount);
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi mọi thứ thành công
                        transaction.Commit();

                        MessageBoxService.ShowSuccess(
                            "Đã tạo tài khoản thành công với mật khẩu mặc định là \"1111\".",
                            "Thành Công");

                        // Cập nhật giao diện để phản ánh tài khoản mới
                        UserName = NewUsername.Trim();
                        HasAccount = true;  // Cập nhật trạng thái tài khoản

                        // Làm mới giao diện
                        OnPropertyChanged(nameof(HasAccount));
                        CommandManager.InvalidateRequerySuggested();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tạo tài khoản: {ex.Message}",
                    "Lỗi");
            }
        }
        #endregion

        #endregion
    }
}