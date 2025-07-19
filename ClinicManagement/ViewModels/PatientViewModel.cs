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
    /// ViewModel chính quản lý bệnh nhân và loại bệnh nhân
    /// Cung cấp chức năng CRUD cho loại bệnh nhân, quản lý danh sách bệnh nhân
    /// Hỗ trợ lọc, tìm kiếm, xuất Excel và nâng cấp tự động bệnh nhân VIP
    /// Triển khai IDataErrorInfo để validation dữ liệu loại bệnh nhân
    /// </summary>
    public class PatientViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties  

        #region DisplayProperties

        /// <summary>
        /// Tài khoản người dùng hiện tại
        /// Được sử dụng để kiểm tra quyền thao tác với loại bệnh nhân
        /// </summary>
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // Cập nhật quyền mỗi khi tài khoản thay đổi
                UpdatePermissions();
            }
        }

        // === QUYỀN THAO TÁC VỚI LOẠI BỆNH NHÂN ===

        /// <summary>
        /// Quyền thêm loại bệnh nhân mới
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canAddPatientType = false;
        public bool CanAddPatientType
        {
            get => _canAddPatientType;
            set
            {
                _canAddPatientType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Quyền chỉnh sửa loại bệnh nhân
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canEditPatientType = false;
        public bool CanEditPatientType
        {
            get => _canEditPatientType;
            set
            {
                _canEditPatientType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Quyền xóa loại bệnh nhân
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canDeletePatientType = false;
        public bool CanDeletePatientType
        {
            get => _canDeletePatientType;
            set
            {
                _canDeletePatientType = value;
                OnPropertyChanged();
            }
        }

        // === THUỘC TÍNH LOẠI BỆNH NHÂN ===

        /// <summary>
        /// Loại bệnh nhân được chọn từ danh sách
        /// Tự động cập nhật các field thông tin khi thay đổi
        /// </summary>
        private PatientType? _SelectedType;
        public PatientType? SelectedType
        {
            get => _SelectedType;
            set
            {
                _SelectedType = value;
                OnPropertyChanged(nameof(SelectedType));

                if (SelectedType != null)
                {
                    PatientTypeId = SelectedType.PatientTypeId;
                    TypeName = SelectedType.TypeName;
                    Discount = SelectedType.Discount.ToString();
                }
            }
        }

        /// <summary>
        /// ID loại bệnh nhân - tự động được gán khi chọn SelectedType
        /// </summary>
        private int _PatientTypeId;
        public int PatientTypeId
        {
            get => _PatientTypeId;
            set
            {
                _PatientTypeId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tên loại bệnh nhân - trường bắt buộc
        /// Có validation và theo dõi touched state để UX tốt hơn
        /// </summary>
        private string _TypeName;
        public string TypeName
        {
            get => _TypeName;
            set
            {
                bool wasEmpty = string.IsNullOrWhiteSpace(_TypeName);
                bool isEmpty = string.IsNullOrWhiteSpace(value);

                // Thêm vào touched fields chỉ khi người dùng tương tác với giá trị không rỗng
                if (wasEmpty && !isEmpty)
                    _touchedFields.Add(nameof(TypeName));

                // Nếu field trở thành rỗng, xóa khỏi touched fields và xóa lỗi validation
                if (!wasEmpty && isEmpty)
                {
                    _touchedFields.Remove(nameof(TypeName));
                    // Buộc xóa lỗi bằng cách kích hoạt refresh validation
                    _isValidating = false; // Tạm thời tắt validation khi xóa dữ liệu
                }

                _TypeName = value;
                OnPropertyChanged();

                // Nếu field đang được validate, cập nhật trạng thái validation
                if (_touchedFields.Contains(nameof(TypeName)))
                    OnPropertyChanged(nameof(Error));
            }
        }

        /// <summary>
        /// Tỷ lệ giảm giá của loại bệnh nhân - trường bắt buộc
        /// Có validation để đảm bảo giá trị từ 0-100
        /// </summary>
        private string _Discount;
        public string Discount
        {
            get => _Discount;
            set
            {
                bool wasEmpty = string.IsNullOrWhiteSpace(_Discount);
                bool isEmpty = string.IsNullOrWhiteSpace(value);

                // Thêm vào touched fields chỉ khi người dùng tương tác với giá trị không rỗng
                if (wasEmpty && !isEmpty)
                    _touchedFields.Add(nameof(Discount));

                // Nếu field trở thành rỗng, xóa khỏi touched fields và xóa lỗi validation
                if (!wasEmpty && isEmpty)
                {
                    _touchedFields.Remove(nameof(Discount));
                    // Buộc xóa lỗi bằng cách kích hoạt refresh validation
                    _isValidating = false; // Tạm thời tắt validation khi xóa dữ liệu
                }

                _Discount = value;
                OnPropertyChanged();

                // Nếu field đang được validate, cập nhật trạng thái validation
                if (_touchedFields.Contains(nameof(Discount)))
                    OnPropertyChanged(nameof(Error));
            }
        }

        /// <summary>
        /// Danh sách tất cả bệnh nhân trong hệ thống
        /// Được lọc và hiển thị dựa trên các điều kiện filter
        /// </summary>
        private ObservableCollection<Patient> _PatientList;
        public ObservableCollection<Patient> PatientList
        {
            get => _PatientList;
            set
            {
                _PatientList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách loại bệnh nhân (Thường, VIP, Bảo hiểm y tế, etc.)
        /// Sử dụng cho ComboBox và các thao tác CRUD
        /// </summary>
        private ObservableCollection<PatientType> _PatientTypeList;
        public ObservableCollection<PatientType> PatientTypeList
        {
            get => _PatientTypeList;
            set
            {
                _PatientTypeList = value;
                OnPropertyChanged();
            }
        }

        // === VALIDATION PROPERTIES ===

        /// <summary>
        /// Error property cho IDataErrorInfo - trả về null vì validation per-property
        /// </summary>
        public string Error => null;

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
        #endregion

        #region Filter Properties

        /// <summary>
        /// Danh sách tất cả bệnh nhân gốc (không được filter)
        /// Sử dụng làm nguồn dữ liệu cho việc lọc
        /// </summary>
        private ObservableCollection<Patient> _AllPatients;

        /// <summary>
        /// Ngày được chọn để lọc bệnh nhân theo ngày đăng ký
        /// Tự động kích hoạt lọc khi thay đổi
        /// </summary>
        private DateTime? _SelectedDate;
        public DateTime? SelectedDate
        {
            get => _SelectedDate;
            set
            {
                _SelectedDate = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi ngày
                ExecuteAutoFilter();
            }
        }

        /// <summary>
        /// Từ khóa tìm kiếm bệnh nhân
        /// Tìm theo tên hoặc mã bảo hiểm y tế
        /// Tự động kích hoạt lọc khi thay đổi
        /// </summary>
        private string _SearchText;
        public string SearchText
        {
            get => _SearchText;
            set
            {
                _SearchText = value;
                OnPropertyChanged();
                ExecuteAutoFilter();
            }
        }

        /// <summary>
        /// ID loại bệnh nhân được chọn để lọc
        /// Được set tự động bởi các checkbox filter
        /// </summary>
        private int? _FilterPatientTypeId;
        public int? FilterPatientTypeId
        {
            get => _FilterPatientTypeId;
            set
            {
                _FilterPatientTypeId = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi loại bệnh nhân
                ExecuteAutoFilter();
            }
        }

        // === CHECKBOX FILTERS ===
        // Các checkbox để lọc nhanh theo loại bệnh nhân

        /// <summary>
        /// Checkbox lọc bệnh nhân VIP
        /// Tự động set FilterPatientTypeId và uncheck các checkbox khác
        /// </summary>
        private bool _IsVIPSelected;
        public bool IsVIPSelected
        {
            get => _IsVIPSelected;
            set
            {
                _IsVIPSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("VIP");
                }
            }
        }

        /// <summary>
        /// Checkbox hiển thị tất cả bệnh nhân
        /// Xóa tất cả filter khi được chọn
        /// </summary>
        private bool _IsAllSelected;
        public bool IsAllSelected
        {
            get => _IsAllSelected;
            set
            {
                _IsAllSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    IsVIPSelected = false;
                    FilterPatientTypeId = null; // Xóa filter theo loại
                    ExecuteAutoFilter();
                }
            }
        }

        /// <summary>
        /// Checkbox lọc bệnh nhân có bảo hiểm y tế
        /// Tự động set FilterPatientTypeId và uncheck các checkbox khác
        /// </summary>
        private bool _IsInsuranceSelected;
        public bool IsInsuranceSelected
        {
            get => _IsInsuranceSelected;
            set
            {
                _IsInsuranceSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Bảo hiểm y tế");
                }
            }
        }

        /// <summary>
        /// Checkbox lọc bệnh nhân thường
        /// Tự động set FilterPatientTypeId và uncheck các checkbox khác
        /// </summary>
        private bool _IsNormalSelected;
        public bool IsNormalSelected
        {
            get => _IsNormalSelected;
            set
            {
                _IsNormalSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsInsuranceSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Thường");
                }
            }
        }
        #endregion

        /// <summary>
        /// Bệnh nhân được chọn trong danh sách
        /// Sử dụng để xem chi tiết thông tin bệnh nhân
        /// </summary>
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(); }
        }

        #endregion

        #region Command

        // === CRUD COMMANDS CHO LOẠI BỆNH NHÂN ===
        public ICommand AddCommand { get; set; }                    // Thêm loại bệnh nhân mới
        public ICommand EditCommand { get; set; }                   // Chỉnh sửa loại bệnh nhân
        public ICommand DeleteCommand { get; set; }                 // Xóa loại bệnh nhân
        public ICommand RefreshTypeDataCommand { get; set; }        // Làm mới form loại bệnh nhân

        // === PATIENT MANAGEMENT COMMANDS ===
        public ICommand AddPatientCommand { get; set; }             // Thêm bệnh nhân mới
        public ICommand OpenPatientDetailsCommand { get; set; }     // Xem chi tiết bệnh nhân
        public ICommand LoadedUCCommand { get; set; }               // Command khi UserControl được tải

        // === SPECIAL FEATURES ===
        public ICommand AutoUpgradePatientsCommand { get; private set; } // Nâng cấp tự động bệnh nhân VIP
        public ICommand ExportExcelCommand { get; private set; }    // Xuất danh sách ra Excel

        // === FILTER COMMANDS ===
        public ICommand SearchCommand { get; set; }                 // Tìm kiếm bệnh nhân
        public ICommand VIPSelectedCommand { get; set; }            // Lọc bệnh nhân VIP
        public ICommand InsuranceSelectedCommand { get; set; }      // Lọc bệnh nhân bảo hiểm
        public ICommand NormalSelectedCommand { get; set; }         // Lọc bệnh nhân thường
        public ICommand AllSelectedCommand { get; set; }            // Hiển thị tất cả bệnh nhân
        public ICommand ResetFiltersCommand { get; set; }           // Reset tất cả filter
        #endregion

        /// <summary>
        /// Constructor khởi tạo PatientViewModel
        /// Thiết lập commands, tải dữ liệu và cấu hình account loading
        /// </summary>
        public PatientViewModel()
        {
            LoadData();                         // Tải dữ liệu ban đầu
            InitializTypeCommands();            // Khởi tạo commands cho loại bệnh nhân
            InitializeFilterCommands();         // Khởi tạo commands cho filter
            IsAllSelected = true;               // Mặc định hiển thị tất cả bệnh nhân

            // Khởi tạo LoadedUCCommand để tải tài khoản đúng cách
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
                        }
                    }
                },
                (userControl) => true
            );
        }

        #region Type Commands

        /// <summary>
        /// Khởi tạo các command liên quan đến loại bệnh nhân
        /// Bao gồm thêm, sửa, xóa, làm mới và xuất Excel
        /// </summary>
        private void InitializTypeCommands()
        {
            // Command thêm loại bệnh nhân - yêu cầu quyền và validation
            AddCommand = new RelayCommand<object>(
            (p) => ExecuteAddNewType(),
            (p) => CanAddPatientType && CanExecuteAddCommand()
        );

            // Command chỉnh sửa loại bệnh nhân - yêu cầu quyền và validation
            EditCommand = new RelayCommand<object>(
                (p) => ExecuteEditType(),
                (p) => CanEditPatientType && CanExecuteEditCommand()
            );

            // Command xóa loại bệnh nhân - yêu cầu quyền và có item được chọn
            DeleteCommand = new RelayCommand<object>(
                (p) => ExecuteDeleteType(),
                (p) => CanDeletePatientType && CanExecuteDeleteCommand()
            );

            // Command thêm bệnh nhân mới - mở cửa sổ AddPatientWindow
            AddPatientCommand = new RelayCommand<object>(
                   (p) =>
                   {
                       // Mở cửa sổ thêm bệnh nhân mới
                       AddPatientWindow addPatientWindow = new AddPatientWindow();
                       addPatientWindow.ShowDialog();
                       // Refresh dữ liệu sau khi thêm bệnh nhân mới
                       LoadData();
                   },
                   (p) => true
               );

            // Command làm mới form loại bệnh nhân - xóa tất cả input
            RefreshTypeDataCommand = new RelayCommand<object>(
                (p) =>
                {
                    TypeName = string.Empty;
                    Discount = string.Empty;
                    SelectedType = null;
                },
                (p) => true
            );

            // Command xuất Excel - chỉ khi có dữ liệu bệnh nhân
            ExportExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => PatientList != null && PatientList.Count > 0
            );
        }

        #endregion

        /// <summary>
        /// Thực hiện thêm loại bệnh nhân mới
        /// Bao gồm validation, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void ExecuteAddNewType()
        {
            try
            {
                // Bật validation cho tất cả field khi user submit form
                _isValidating = true;
                _touchedFields.Add(nameof(TypeName));
                _touchedFields.Add(nameof(Discount));

                // Kích hoạt validation bằng cách thông báo property changes
                OnPropertyChanged(nameof(TypeName));
                OnPropertyChanged(nameof(Discount));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi trước khi thêm loại bệnh nhân mới.", "Lỗi thông tin");
                    return;
                }

                // Kiểm tra loại bệnh nhân đã tồn tại chưa
                bool isExist = DataProvider.Instance.Context.PatientTypes
                      .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() && pt.IsDeleted == false);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Loại bệnh nhân này đã tồn tại.", "Trùng dữ liệu");
                    return;
                }

                // Thử parse giá trị discount trước khi bắt đầu transaction
                if (!decimal.TryParse(Discount, out decimal parsedDiscount))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập giảm giá hợp lệ (số thực).", "Lỗi nhập liệu");
                    return;
                }

                // Xác nhận với người dùng trước khi tiếp tục
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm loại bệnh nhân '{TypeName}' không?",
                    "Xác Nhận Thêm"
                );

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tạo và lưu loại bệnh nhân mới
                        var newPatientType = new PatientType()
                        {
                            TypeName = TypeName.Trim(),
                            Discount = parsedDiscount,
                            IsDeleted = false
                        };

                        DataProvider.Instance.Context.PatientTypes.Add(newPatientType);
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi tất cả thao tác thành công
                        transaction.Commit();

                        // Hiển thị thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm loại bệnh nhân thành công!",
                            "Thành Công"
                        );

                        // Refresh dữ liệu sau khi thêm thành công
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi xảy ra
                        transaction.Rollback();

                        // Ném lại exception để xử lý ở catch block bên ngoài
                        throw;
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi liên quan đến database
                MessageBoxService.ShowError(
                     $"Không thể thêm loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
                     "Lỗi Cơ Sở Dữ Liệu"
                );
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                MessageBoxService.ShowError(
                     $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                     "Lỗi"
                );
            }
        }

        /// <summary>
        /// Thực hiện chỉnh sửa loại bệnh nhân
        /// Kiểm tra trùng lặp (ngoại trừ chính nó) và sử dụng transaction
        /// </summary>
        private void ExecuteEditType()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa loại bệnh nhân '{TypeName}' không?",
                    "Xác Nhận Sửa"
                );

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm loại bệnh nhân cần sửa
                        var patientTypeToUpdate = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại bệnh nhân cần sửa.");
                            return;
                        }

                        // Kiểm tra TypeName mới có trùng với loại khác không (ngoại trừ chính nó)
                        bool isExist = DataProvider.Instance.Context.PatientTypes
                            .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() &&
                                  pt.PatientTypeId != SelectedType.PatientTypeId);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Loại bệnh nhân này đã tồn tại.");
                            return;
                        }

                        // Kiểm tra và parse Discount
                        if (decimal.TryParse(Discount, out decimal parsedDiscount))
                        {
                            patientTypeToUpdate.TypeName = TypeName;
                            patientTypeToUpdate.Discount = parsedDiscount;

                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thao tác thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                 "Đã cập nhật loại bệnh nhân thành công!",
                                 "Thành Công"
                            );

                            // Refresh dữ liệu sau khi chỉnh sửa
                            LoadData();
                        }
                        else
                        {
                            MessageBoxService.ShowWarning("Vui lòng nhập giảm giá hợp lệ (số thực).");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi xảy ra
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                        throw;
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                     $"Không thể sửa loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
                     "Lỗi Cơ Sở Dữ Liệu"
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                     $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                     "Lỗi"
                );
            }
        }

        /// <summary>
        /// Thực hiện xóa mềm loại bệnh nhân
        /// Đánh dấu IsDeleted = true thay vì xóa khỏi database
        /// </summary>
        private void ExecuteDeleteType()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa loại bệnh nhân '{SelectedType?.TypeName}' không?",
                    "Xác Nhận Xóa"
                );

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm đối tượng cần xóa
                        var patientTypeToDelete = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại bệnh nhân để xóa.");
                            return;
                        }

                        // Đánh dấu IsDeleted = true (xóa mềm)
                        patientTypeToDelete.IsDeleted = true;

                        // Lưu thay đổi vào cơ sở dữ liệu
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi mọi thao tác thành công
                        transaction.Commit();

                        MessageBoxService.ShowSuccess(
                            "Đã xóa (ẩn) loại bệnh nhân thành công.",
                            "Xóa Thành Công"
                        );

                        // Refresh dữ liệu sau khi xóa
                        LoadData();
                        SelectedType = null;
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi xảy ra
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện có thể thực hiện lệnh Add
        /// Yêu cầu TypeName không rỗng và không có lỗi validation
        /// </summary>
        /// <returns>True nếu có thể thực hiện Add</returns>
        private bool CanExecuteAddCommand()
        {
            if (TypeName == null || string.IsNullOrWhiteSpace(TypeName) || HasErrors)
                return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra điều kiện có thể thực hiện lệnh Edit
        /// Yêu cầu có SelectedType, TypeName không rỗng, không có lỗi và có thay đổi
        /// </summary>
        /// <returns>True nếu có thể thực hiện Edit</returns>
        private bool CanExecuteEditCommand()
        {
            if (SelectedType == null)
                return false;

            if (string.IsNullOrWhiteSpace(TypeName))
                return false;

            if (HasErrors)
                return false;

            // Chỉ kiểm tra so sánh TypeName nếu SelectedType không null
            if (TypeName == SelectedType.TypeName)
                return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra điều kiện có thể thực hiện lệnh Delete
        /// Yêu cầu có SelectedType và TypeName không rỗng
        /// </summary>
        /// <returns>True nếu có thể thực hiện Delete</returns>
        private bool CanExecuteDeleteCommand()
        {
            if (SelectedType == null)
                return false;

            if (string.IsNullOrWhiteSpace(TypeName))
                return false;

            return true;
        }

        /// <summary>
        /// Cập nhật quyền dựa trên vai trò của tài khoản hiện tại
        /// Chỉ Admin và Manager mới có quyền thao tác với loại bệnh nhân
        /// </summary>
        private void UpdatePermissions()
        {
            // Mặc định không có quyền gì
            CanAddPatientType = false;
            CanEditPatientType = false;
            CanDeletePatientType = false;

            // Kiểm tra xem tài khoản hiện tại có tồn tại không
            if (CurrentAccount == null)
                return;

            // Kiểm tra quyền dựa trên vai trò
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Thiết lập quyền dựa trên vai trò
            switch (role)
            {
                case UserRoles.Admin:
                case UserRoles.Manager:
                    CanAddPatientType = true;
                    CanEditPatientType = true;
                    CanDeletePatientType = true;
                    break;

                case UserRoles.Doctor:
                case UserRoles.Pharmacist:
                case UserRoles.Cashier:
                default:
                    // Các vai trò không phải quản trị không có quyền thay đổi loại bệnh nhân
                    CanAddPatientType = false;
                    CanEditPatientType = false;
                    CanDeletePatientType = false;
                    break;
            }

            // Buộc command CanExecute được đánh giá lại
            CommandManager.InvalidateRequerySuggested();
        }

        #region Data Loading Methods

        /// <summary>
        /// Tải tất cả dữ liệu cần thiết cho ViewModel
        /// Bao gồm bệnh nhân, loại bệnh nhân và thiết lập command
        /// </summary>
        public void LoadData()
        {
            // Tải danh sách bệnh nhân gốc với thông tin loại bệnh nhân
            _AllPatients = new ObservableCollection<Patient>(
                DataProvider.Instance.Context.Patients
                .Include(p => p.PatientType)
                .Where(p => p.IsDeleted != true)
                .ToList()
            );

            // Copy vào danh sách hiển thị
            PatientList = new ObservableCollection<Patient>(_AllPatients);

            // Tải danh sách loại bệnh nhân
            PatientTypeList = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => (bool)!pt.IsDeleted)
                .ToList()
            );

            // Thiết lập command xem chi tiết bệnh nhân
            OpenPatientDetailsCommand = new RelayCommand<Patient>(
                (p) => OpenPatientDetails(p),
                (p) => p != null
                );

            // Kiểm tra và nâng cấp bệnh nhân VIP tự động
            CheckAndUpgradePatients();
        }

        /// <summary>
        /// Mở cửa sổ chi tiết bệnh nhân
        /// Tải đầy đủ thông tin bệnh nhân và refresh dữ liệu sau khi đóng
        /// </summary>
        /// <param name="patient">Bệnh nhân cần xem chi tiết</param>
        private void OpenPatientDetails(Patient patient)
        {
            if (patient == null) return;

            var viewModel = new PatientDetailsWindowViewModel();
            viewModel.Patient = patient;  // Sẽ tự động thiết lập tất cả thuộc tính liên quan

            var detailsWindow = new PatientDetailsWindow
            {
                DataContext = viewModel
            };
            detailsWindow.ShowDialog();
            LoadData();  // Refresh dữ liệu sau khi cửa sổ chi tiết đóng
        }

        #endregion

        #region Filter Methods

        /// <summary>
        /// Khởi tạo các command liên quan đến filter và tìm kiếm
        /// </summary>
        private void InitializeFilterCommands()
        {
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );
            AllSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteAllFilter(),
                (p) => true
            );

            VIPSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteVIPFilter(),
                (p) => true
            );

            InsuranceSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteInsuranceFilter(),
                (p) => true
            );

            NormalSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteNormalFilter(),
                (p) => true
            );

            ResetFiltersCommand = new RelayCommand<object>(
                (p) => ExecuteResetFilters(),
                (p) => true
            );

            // Command tự động nâng cấp bệnh nhân VIP
            AutoUpgradePatientsCommand = new RelayCommand<object>(
              (p) => CheckAndUpgradePatients(),
              (p) => true
            );
        }

        /// <summary>
        /// Thực hiện tìm kiếm - áp dụng filter hiện tại
        /// </summary>
        private void ExecuteSearch()
        {
            ApplyFilters();
        }

        /// <summary>
        /// Thực hiện filter tất cả - áp dụng filter hiện tại
        /// </summary>
        private void ExecuteAllFilter()
        {
            ApplyFilters();
        }

        /// <summary>
        /// Thực hiện filter VIP - set checkbox VIP
        /// </summary>
        private void ExecuteVIPFilter()
        {
            IsVIPSelected = true;
        }

        /// <summary>
        /// Thực hiện filter bảo hiểm - set checkbox bảo hiểm
        /// </summary>
        private void ExecuteInsuranceFilter()
        {
            IsInsuranceSelected = true;
        }

        /// <summary>
        /// Thực hiện filter thường - set checkbox thường
        /// </summary>
        private void ExecuteNormalFilter()
        {
            IsNormalSelected = true;
        }

        /// <summary>
        /// Thực hiện auto filter khi có thay đổi trong điều kiện lọc
        /// Tự động kích hoạt khi thay đổi ngày hoặc loại bệnh nhân
        /// </summary>
        private void ExecuteAutoFilter()
        {
            ApplyFilters();
        }

        /// <summary>
        /// Reset tất cả filter về trạng thái ban đầu
        /// Hiển thị tất cả bệnh nhân
        /// </summary>
        private void ExecuteResetFilters()
        {
            SelectedDate = null;
            SearchText = string.Empty;
            FilterPatientTypeId = null;
            IsVIPSelected = false;
            IsInsuranceSelected = false;
            IsNormalSelected = false;

            PatientList = new ObservableCollection<Patient>(_AllPatients);
        }

        /// <summary>
        /// Áp dụng tất cả điều kiện filter lên danh sách bệnh nhân
        /// Lọc theo ngày tạo, loại bệnh nhân và từ khóa tìm kiếm
        /// </summary>
        private void ApplyFilters()
        {
            if (_AllPatients == null || _AllPatients.Count == 0)
            {
                PatientList = new ObservableCollection<Patient>();
                return;
            }

            var filteredList = _AllPatients.AsEnumerable();

            // Lọc theo ngày tạo
            if (SelectedDate.HasValue)
            {
                filteredList = filteredList.Where(p => p.CreatedAt?.Date == SelectedDate.Value.Date);
            }

            // Lọc theo loại bệnh nhân
            if (FilterPatientTypeId.HasValue)
            {
                filteredList = filteredList.Where(p => p.PatientTypeId == FilterPatientTypeId.Value);
            }

            // Lọc theo tên hoặc mã bảo hiểm
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = filteredList.Where(p =>
                    (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(p.InsuranceCode) && p.InsuranceCode.ToLower().Contains(searchTerm))
                );
            }

            PatientList = new ObservableCollection<Patient>(filteredList.ToList());
        }

        /// <summary>
        /// Lấy ID loại bệnh nhân theo tên
        /// Sử dụng cho việc set FilterPatientTypeId khi chọn checkbox
        /// </summary>
        /// <param name="typeName">Tên loại bệnh nhân</param>
        /// <returns>ID loại bệnh nhân hoặc null nếu không tìm thấy</returns>
        private int? GetPatientTypeIdByName(string typeName)
        {
            var patientType = PatientTypeList?.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals(typeName.Trim(), StringComparison.OrdinalIgnoreCase) == true);
            return patientType?.PatientTypeId;
        }

        #endregion

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
                    case nameof(TypeName):
                        if (string.IsNullOrWhiteSpace(TypeName) && _touchedFields.Contains(columnName))
                        {
                            error = "Tên loại bệnh nhân không được để trống.";
                        }
                        else if (!string.IsNullOrWhiteSpace(TypeName) && TypeName.Length > 50)
                        {
                            error = "Tên loại bệnh nhân không được quá 50 ký tự.";
                        }
                        break;
                    case nameof(Discount):
                        if (!string.IsNullOrWhiteSpace(Discount) &&
                            (!decimal.TryParse(Discount, out decimal parsedDiscount) || parsedDiscount < 0 || parsedDiscount > 100))
                        {
                            error = "Giảm giá phải là số từ 0 đến 100.";
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Kiểm tra xem có lỗi validation nào không
        /// Chỉ kiểm tra TypeName vì Discount là optional
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(TypeName)]);
            }
        }
        #endregion

        /// <summary>
        /// Xuất danh sách bệnh nhân ra file Excel
        /// Sử dụng ClosedXML với progress dialog và background thread
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
                    FileName = $"DanhSachBenhNhan_{DateTime.Now:dd-MM-yyyy}.xlsx"
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
                                var worksheet = workbook.Worksheets.Add("Danh sách bệnh nhân");

                                // Báo cáo tiến trình: 5% - Tạo workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Thêm tiêu đề (merged cells)
                                worksheet.Cell(1, 1).Value = "DANH SÁCH BỆNH NHÂN";
                                var titleRange = worksheet.Range(1, 1, 1, 9);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Thêm ngày hiện tại
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 9);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Báo cáo tiến trình: 10% - Thêm tiêu đề
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Thêm headers (với khoảng cách 4 cell từ tiêu đề)
                                int headerRow = 6; // Hàng 6 (để lại 3 hàng trống sau tiêu đề)
                                worksheet.Cell(headerRow, 1).Value = "Mã BN";
                                worksheet.Cell(headerRow, 2).Value = "Bảo hiểm y tế";
                                worksheet.Cell(headerRow, 3).Value = "Họ và tên";
                                worksheet.Cell(headerRow, 4).Value = "Ngày sinh";
                                worksheet.Cell(headerRow, 5).Value = "Giới tính";
                                worksheet.Cell(headerRow, 6).Value = "Loại khách hàng";
                                worksheet.Cell(headerRow, 7).Value = "Điện thoại";
                                worksheet.Cell(headerRow, 8).Value = "Địa chỉ";
                                worksheet.Cell(headerRow, 9).Value = "Ngày đăng kí";

                                // Style cho header row
                                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
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
                                int totalPatients = PatientList.Count;

                                // Tạo data range (để áp dụng viền sau)
                                var dataStartRow = row;

                                for (int i = 0; i < totalPatients; i++)
                                {
                                    var patient = PatientList[i];

                                    worksheet.Cell(row, 1).Value = patient.PatientId;
                                    worksheet.Cell(row, 2).Value = patient.InsuranceCode ?? "";
                                    worksheet.Cell(row, 3).Value = patient.FullName ?? "";

                                    if (patient.DateOfBirth.HasValue)
                                        worksheet.Cell(row, 4).Value = patient.DateOfBirth.Value.ToString("dd/MM/yyyy");
                                    else
                                        worksheet.Cell(row, 4).Value = "";

                                    worksheet.Cell(row, 5).Value = patient.Gender ?? "";
                                    worksheet.Cell(row, 6).Value = patient.PatientType?.TypeName ?? "";
                                    worksheet.Cell(row, 7).Value = patient.Phone ?? "";
                                    worksheet.Cell(row, 8).Value = patient.Address ?? "";

                                    if (patient.CreatedAt.HasValue)
                                        worksheet.Cell(row, 9).Value = patient.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm");
                                    else
                                        worksheet.Cell(row, 9).Value = "";

                                    row++;

                                    // Cập nhật tiến trình dựa trên phần trăm bệnh nhân đã xử lý
                                    int progressValue = 20 + (i * 60 / totalPatients);
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));

                                    // Thêm độ trễ nhỏ để hiển thị tiến trình
                                    Thread.Sleep(30);
                                }

                                // Báo cáo tiến trình: 80% - Dữ liệu thêm xong
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Áp dụng viền cho data range
                                if (totalPatients > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, 1, row - 1, 9);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                    // Căn giữa các cột ID, Date, Gender
                                    worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                }

                                // Thêm hàng tổng
                                worksheet.Cell(row + 1, 2).Value = "Tổng số:";
                                worksheet.Cell(row + 1, 3).Value = totalPatients;
                                worksheet.Cell(row + 1, 3).Style.Font.Bold = true;

                                // Auto-fit các cột
                                worksheet.Columns().AdjustToContents();

                                // Đặt độ rộng tối thiểu cho khả năng đọc tốt hơn
                                worksheet.Column(1).Width = 10; // ID
                                worksheet.Column(3).Width = 25; // Name
                                worksheet.Column(6).Width = 15; // Patient Type
                                worksheet.Column(8).Width = 50; // Address

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
                                        $"Đã xuất danh sách bệnh nhân thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                          );
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
                                    "Lỗi"
                                     );
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
                    "Lỗi"
                     );
            }
        }

        /// <summary>
        /// Kiểm tra tất cả bệnh nhân để nâng cấp và đưa lên VIP nếu đủ điều kiện
        /// Điều kiện: chi tiêu >= 8,000,000 VND hoặc có >= 30 hóa đơn đã thanh toán
        /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
        /// </summary>
        public void CheckAndUpgradePatients()
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    // Lấy loại bệnh nhân VIP - sử dụng so sánh string mà EF Core có thể dịch
                    var vipType = DataProvider.Instance.Context.PatientTypes
                        .FirstOrDefault(pt => pt.TypeName.Trim().ToLower() == "vip".ToLower() && pt.IsDeleted != true);

                    if (vipType == null)
                        return;

                    // Lấy loại bệnh nhân thường - sử dụng so sánh string mà EF Core có thể dịch
                    var normalType = DataProvider.Instance.Context.PatientTypes
                        .FirstOrDefault(pt => pt.TypeName.Trim().ToLower() == "thường".ToLower() && pt.IsDeleted != true);

                    if (normalType == null)
                        return;

                    // Lấy tất cả bệnh nhân thường
                    var regularPatients = DataProvider.Instance.Context.Patients
                        .Where(p => p.PatientTypeId == normalType.PatientTypeId && p.IsDeleted != true)
                        .ToList();

                    if (regularPatients.Count == 0)
                        return;

                    // Lấy tất cả hóa đơn đã thanh toán
                    var invoices = DataProvider.Instance.Context.Invoices
                        .Where(i => i.Status == "Đã thanh toán")
                        .ToList();

                    List<string> upgradedPatients = new List<string>();

                    // Kiểm tra từng bệnh nhân thường
                    foreach (var patient in regularPatients)
                    {
                        var patientInvoices = invoices
                            .Where(i => i.PatientId == patient.PatientId)
                            .ToList();

                        decimal totalSpending = patientInvoices.Sum(i => i.TotalAmount);
                        int invoiceCount = patientInvoices.Count;

                        bool qualifiedBySpending = totalSpending >= 8000000; // 8 triệu VND
                        bool qualifiedByCount = invoiceCount >= 30;          // 30 hóa đơn

                        if (qualifiedBySpending || qualifiedByCount)
                        {
                            patient.PatientTypeId = vipType.PatientTypeId;
                            upgradedPatients.Add(patient.FullName);
                        }
                    }

                    // Lưu thay đổi nếu có bệnh nhân được nâng cấp
                    if (upgradedPatients.Count > 0)
                    {
                        DataProvider.Instance.Context.SaveChanges();
                        transaction.Commit();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            string message = $"Đã nâng cấp {upgradedPatients.Count} bệnh nhân lên VIP!\n\n" +
                                            $"Danh sách: {string.Join(", ", upgradedPatients)}";

                            MessageBoxService.ShowSuccess(message, "Nâng cấp thành công");
                            LoadData();
                        });
                    }
                    else
                    {
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.ShowError($"Lỗi khi nâng cấp bệnh nhân: {ex.Message}", "Lỗi");
                    });
                }
            }
        }
    }
}