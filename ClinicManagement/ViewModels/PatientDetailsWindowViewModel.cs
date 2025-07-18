using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// Lớp helper để quản lý trạng thái cho các ComboBox trong filter
    /// Chứa giá trị thực tế và tên hiển thị cho người dùng
    /// </summary>
    public class StatusItem
    {
        /// <summary>
        /// Giá trị trạng thái thực tế trong database
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Tên hiển thị cho người dùng
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Constructor tạo StatusItem
        /// </summary>
        /// <param name="status">Giá trị trạng thái thực tế</param>
        /// <param name="displayName">Tên hiển thị</param>
        public StatusItem(string status, string displayName)
        {
            Status = status;
            DisplayName = displayName;
        }
    }

    /// <summary>
    /// ViewModel quản lý cửa sổ chi tiết bệnh nhân
    /// Cung cấp chức năng xem, chỉnh sửa thông tin bệnh nhân và quản lý dữ liệu liên quan
    /// Bao gồm hồ sơ bệnh án, hóa đơn, lịch hẹn với khả năng lọc và tìm kiếm
    /// </summary>
    public class PatientDetailsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties

        /// <summary>
        /// Tham chiếu đến cửa sổ chứa ViewModel này
        /// Được sử dụng để đóng cửa sổ và cập nhật tiêu đề
        /// </summary>
        private Window _window;

        /// <summary>
        /// Cờ hiệu cho biết đang trong quá trình tải dữ liệu
        /// Sử dụng để hiển thị loading indicator trong UI
        /// </summary>
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tài khoản người dùng hiện tại
        /// Được sử dụng để kiểm tra quyền chỉnh sửa và xóa bệnh nhân
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
        /// Quyền chỉnh sửa thông tin bệnh nhân
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canEditPatient = false;
        public bool CanEditPatient
        {
            get => _canEditPatient;
            set
            {
                _canEditPatient = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Quyền xóa bệnh nhân
        /// Chỉ Admin và Manager mới có quyền này
        /// </summary>
        private bool _canDeletePatient = false;
        public bool CanDeletePatient
        {
            get => _canDeletePatient;
            set
            {
                _canDeletePatient = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Đối tượng bệnh nhân chính được quản lý
        /// Khi thay đổi sẽ tự động tải dữ liệu liên quan (hồ sơ, hóa đơn, lịch hẹn)
        /// </summary>
        private Patient _patient;
        public Patient Patient
        {
            get => _patient;
            set
            {
                _patient = value;
                OnPropertyChanged();
                LoadRelatedData(); // Tải dữ liệu liên quan khi bệnh nhân thay đổi
            }
        }

        /// <summary>
        /// Danh sách hồ sơ bệnh án của bệnh nhân
        /// Hỗ trợ lọc theo ngày và tìm kiếm theo chẩn đoán hoặc tên bác sĩ
        /// </summary>
        private ObservableCollection<MedicalRecord> _medicalRecords;
        public ObservableCollection<MedicalRecord> MedicalRecords
        {
            get => _medicalRecords ?? (_medicalRecords = new ObservableCollection<MedicalRecord>());
            set
            {
                _medicalRecords = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách hóa đơn của bệnh nhân
        /// Hỗ trợ lọc theo ngày và trạng thái thanh toán
        /// </summary>
        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices ?? (_invoices = new ObservableCollection<Invoice>());
            set
            {
                _invoices = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách lịch hẹn của bệnh nhân
        /// Hỗ trợ lọc theo trạng thái lịch hẹn
        /// </summary>
        private ObservableCollection<Appointment> _appointments;
        public ObservableCollection<Appointment> Appointments
        {
            get => _appointments ?? (_appointments = new ObservableCollection<Appointment>());
            set
            {
                _appointments = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách các tùy chọn giới tính
        /// Sử dụng cho ComboBox chọn giới tính
        /// </summary>
        private ObservableCollection<string> _genderOptions;
        public ObservableCollection<string> GenderOptions
        {
            get => _genderOptions;
            set
            {
                _genderOptions = value;
                OnPropertyChanged();
            }
        }

        // === THUỘC TÍNH LỌC HỒ SƠ BỆNH ÁN ===

        /// <summary>
        /// Ngày bắt đầu cho việc lọc hồ sơ bệnh án
        /// Mặc định là 1 tháng trước
        /// </summary>
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày kết thúc cho việc lọc hồ sơ bệnh án
        /// Mặc định là ngày hiện tại
        /// </summary>
        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        // === THUỘC TÍNH LỌC HÓA ĐƠN ===

        /// <summary>
        /// Ngày bắt đầu cho việc lọc hóa đơn
        /// Mặc định là 1 tháng trước
        /// </summary>
        private DateTime _invoiceStartDate = DateTime.Now.AddMonths(-1);
        public DateTime InvoiceStartDate
        {
            get => _invoiceStartDate;
            set
            {
                _invoiceStartDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày kết thúc cho việc lọc hóa đơn
        /// Mặc định là ngày hiện tại
        /// </summary>
        private DateTime _invoiceEndDate = DateTime.Now;
        public DateTime InvoiceEndDate
        {
            get => _invoiceEndDate;
            set
            {
                _invoiceEndDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày sinh của bệnh nhân
        /// Helper property để tương thích với DatePicker (DateTime? vs DateOnly?)
        /// </summary>
        private DateTime? _birthDate;
        public DateTime? BirthDate
        {
            get
            {
                if (Patient?.DateOfBirth != null)
                    return new DateTime(Patient.DateOfBirth.Value.Year, Patient.DateOfBirth.Value.Month, Patient.DateOfBirth.Value.Day);
                return null;
            }
            set
            {
                if (value.HasValue)
                    Patient.DateOfBirth = DateOnly.FromDateTime(value.Value);
                else
                    Patient.DateOfBirth = null;

                OnPropertyChanged();
            }
        }

        // === THUỘC TÍNH THÔNG TIN BỆNH NHÂN ===

        /// <summary>
        /// Mã bệnh nhân - chỉ đọc
        /// </summary>
        private int _patientId;
        public int PatientId
        {
            get => _patientId;
            set
            {
                _patientId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Họ và tên bệnh nhân
        /// Có validation bắt buộc nhập và độ dài tối thiểu
        /// </summary>
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày sinh bệnh nhân
        /// Sử dụng cho form input
        /// </summary>
        private DateTime? _dateOfBirth;
        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                _dateOfBirth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số điện thoại bệnh nhân
        /// Có validation định dạng số điện thoại Việt Nam
        /// </summary>
        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Mã bảo hiểm y tế
        /// Có validation định dạng 10 chữ số
        /// </summary>
        private string _insuranceCode;
        public string InsuranceCode
        {
            get => _insuranceCode;
            set
            {
                _insuranceCode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Giới tính bệnh nhân
        /// Lựa chọn từ GenderOptions
        /// </summary>
        private string _gender;
        public string Gender
        {
            get => _gender;
            set
            {
                _gender = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách các loại bệnh nhân
        /// Sử dụng cho ComboBox chọn loại bệnh nhân
        /// </summary>
        private ObservableCollection<PatientType> _patientTypes;
        public ObservableCollection<PatientType> PatientTypes
        {
            get => _patientTypes;
            set
            {
                _patientTypes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ID loại bệnh nhân
        /// Liên kết với SelectedPatientType
        /// </summary>
        private int _patientTypeId;
        public int PatientTypeId
        {
            get => _patientTypeId;
            set
            {
                _patientTypeId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tên loại bệnh nhân
        /// Hiển thị từ SelectedPatientType
        /// </summary>
        private string _typeName;
        public string TypeName
        {
            get => _typeName;
            set
            {
                _typeName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tỷ lệ giảm giá của loại bệnh nhân
        /// Hiển thị từ SelectedPatientType
        /// </summary>
        private string _discount;
        public string Discount
        {
            get => _discount;
            set
            {
                _discount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Địa chỉ bệnh nhân
        /// Trường tùy chọn
        /// </summary>
        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Loại bệnh nhân được chọn trong ComboBox
        /// Tự động cập nhật PatientTypeId, TypeName, Discount khi thay đổi
        /// </summary>
        private PatientType _selectedPatientType;
        public PatientType SelectedPatientType
        {
            get => _selectedPatientType;
            set
            {
                _selectedPatientType = value;
                OnPropertyChanged();
                // Cập nhật các thuộc tính liên quan khi lựa chọn thay đổi
                if (SelectedPatientType != null)
                {
                    PatientTypeId = SelectedPatientType.PatientTypeId;
                    TypeName = SelectedPatientType.TypeName;
                    Discount = SelectedPatientType.Discount.ToString();
                }
            }
        }

        /// <summary>
        /// Từ khóa tìm kiếm trong hồ sơ bệnh án
        /// Tìm theo chẩn đoán hoặc tên bác sĩ
        /// </summary>
        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách các trạng thái hóa đơn để lọc
        /// Bao gồm "Tất cả", "Đã thanh toán", "Chưa thanh toán"
        /// </summary>
        private ObservableCollection<StatusItem> _invoiceStatusList;
        public ObservableCollection<StatusItem> InvoiceStatusList
        {
            get => _invoiceStatusList;
            set
            {
                _invoiceStatusList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Trạng thái hóa đơn được chọn để lọc
        /// Tự động kích hoạt lọc khi thay đổi
        /// </summary>
        private StatusItem _selectedInvoiceStatus;
        public StatusItem SelectedInvoiceStatus
        {
            get => _selectedInvoiceStatus;
            set
            {
                _selectedInvoiceStatus = value;
                OnPropertyChanged();
                FilterInvoices(); // Tự động lọc khi trạng thái thay đổi
            }
        }

        /// <summary>
        /// Danh sách các trạng thái lịch hẹn để lọc
        /// Bao gồm "Tất cả", "Đang chờ", "Đang khám", "Đã khám", "Đã hủy"
        /// </summary>
        private ObservableCollection<StatusItem> _appointmentStatusList;
        public ObservableCollection<StatusItem> AppointmentStatusList
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
        /// Tự động kích hoạt lọc khi thay đổi
        /// </summary>
        private StatusItem _selectedAppointmentStatus;
        public StatusItem SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
                FilterAppointments(); // Tự động lọc khi trạng thái thay đổi
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

        #region Commands

        /// <summary>
        /// Lệnh cập nhật thông tin bệnh nhân
        /// </summary>
        public ICommand UpdatePatientCommand { get; set; }

        /// <summary>
        /// Lệnh xóa bệnh nhân (soft delete)
        /// </summary>
        public ICommand DeletePatientCommand { get; set; }

        /// <summary>
        /// Lệnh lọc hồ sơ bệnh án theo ngày và từ khóa
        /// </summary>
        public ICommand FilterRecordsCommand { get; set; }

        /// <summary>
        /// Lệnh lọc hóa đơn theo ngày và trạng thái
        /// </summary>
        public ICommand FilterInvoicesCommand { get; set; }

        /// <summary>
        /// Lệnh xem đơn thuốc trong hồ sơ bệnh án
        /// </summary>
        public ICommand ViewPrescriptionCommand { get; set; }

        /// <summary>
        /// Lệnh xem kết quả xét nghiệm trong hồ sơ bệnh án
        /// </summary>
        public ICommand ViewTestResultsCommand { get; set; }

        /// <summary>
        /// Lệnh mở chi tiết hồ sơ bệnh án
        /// </summary>
        public ICommand OpenRecordCommand { get; set; }

        /// <summary>
        /// Lệnh xem chi tiết hóa đơn
        /// </summary>
        public ICommand ViewInvoiceCommand { get; set; }

        /// <summary>
        /// Lệnh thêm lịch hẹn mới cho bệnh nhân
        /// </summary>
        public ICommand AddAppointmentCommand { get; set; }

        /// <summary>
        /// Lệnh xem/chỉnh sửa chi tiết lịch hẹn
        /// </summary>
        public ICommand ViewAppointmentCommand { get; set; }

        /// <summary>
        /// Lệnh hủy lịch hẹn
        /// </summary>
        public ICommand CancelAppointmentCommand { get; set; }

        /// <summary>
        /// Lệnh xử lý khi cửa sổ được load
        /// </summary>
        public ICommand LoadedWindowCommand { get; set; }
        #endregion

        /// <summary>
        /// Constructor khởi tạo PatientDetailsWindowViewModel
        /// Thiết lập commands, tùy chọn giới tính và lấy tài khoản hiện tại
        /// </summary>
        public PatientDetailsWindowViewModel()
        {
            InitializeCommands();
            InitializeGenderOptions();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
        }

        /// <summary>
        /// Khởi tạo danh sách các tùy chọn giới tính
        /// </summary>
        private void InitializeGenderOptions()
        {
            GenderOptions = new ObservableCollection<string> { "Nam", "Nữ", "Khác" };
        }

        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        private void InitializeCommands()
        {
            // Command xử lý khi cửa sổ được load
            LoadedWindowCommand = new RelayCommand<Window>(
                (p) => { _window = p; LoadWindow(p); },
                (p) => true
            );

            // Command cập nhật thông tin bệnh nhân - yêu cầu quyền chỉnh sửa
            UpdatePatientCommand = new RelayCommand<object>(
                (p) => UpdatePatient(),
                (p) => CanEditPatient && CanUpdatePatient()
            );

            // Command xóa bệnh nhân - yêu cầu quyền xóa
            DeletePatientCommand = new RelayCommand<object>(
               (p) => DeletePatient(),
               (p) => CanDeletePatient && CanDeletePatientData()
           );

            // Command lọc hồ sơ bệnh án - luôn có thể thực thi
            FilterRecordsCommand = new RelayCommand<object>(
                (p) => FilterMedicalRecords(),
                (p) => true
            );

            // Command lọc hóa đơn - luôn có thể thực thi
            FilterInvoicesCommand = new RelayCommand<object>(
                (p) => FilterInvoices(),
                (p) => true
            );

            // Command xem đơn thuốc - chỉ khi có đơn thuốc
            ViewPrescriptionCommand = new RelayCommand<MedicalRecord>(
                (record) => ViewPrescription(record),
                (record) => record != null && !string.IsNullOrEmpty(record.Prescription)
            );

            // Command xem kết quả xét nghiệm - chỉ khi có kết quả
            ViewTestResultsCommand = new RelayCommand<MedicalRecord>(
                (record) => ViewTestResults(record),
                (record) => record != null && !string.IsNullOrEmpty(record.TestResults)
            );

            // Command mở hồ sơ bệnh án - chỉ khi có hồ sơ
            OpenRecordCommand = new RelayCommand<MedicalRecord>(
                (record) => OpenMedicalRecord(record),
                (record) => record != null
            );

            // Command xem hóa đơn - chỉ khi có hóa đơn
            ViewInvoiceCommand = new RelayCommand<Invoice>(
                (invoice) => ViewInvoice(invoice),
                (invoice) => invoice != null
            );

            // Command thêm lịch hẹn - chỉ khi có bệnh nhân
            AddAppointmentCommand = new RelayCommand<object>(
                (p) => AddNewAppointment(),
                (p) => Patient != null
            );

            // Command xem lịch hẹn - chỉ khi có lịch hẹn
            ViewAppointmentCommand = new RelayCommand<Appointment>(
                (appointment) => ViewAppointment(appointment),
                (appointment) => appointment != null
            );

            // Command hủy lịch hẹn - chỉ với các lịch hẹn có thể hủy
            CancelAppointmentCommand = new RelayCommand<Appointment>(
       (appointment) => CancelExistingAppointment(appointment),
       (appointment) => appointment != null &&
                        appointment.Status != "Đã hủy" &&
                        appointment.Status != "Đã khám" &&
                        appointment.Status != "Đang khám"
   );
        }

        #region Validation

        /// <summary>
        /// Indexer cho IDataErrorInfo - thực hiện validation cho từng property
        /// Chỉ validate khi field đã được touched hoặc đang trong chế độ validating
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                // Chỉ validate khi user đã tương tác với form hoặc khi submit
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                if (Patient == null)
                    return null;

                switch (columnName)
                {
                    case nameof(Patient.FullName):
                        if (string.IsNullOrWhiteSpace(Patient.FullName))
                        {
                            error = "Họ và tên không được để trống";
                        }
                        else if (Patient.FullName.Trim().Length < 2)
                        {
                            error = "Họ và tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Patient.Phone):
                        if (!string.IsNullOrWhiteSpace(Patient.Phone) &&
                                !Regex.IsMatch(Patient.Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(Patient.InsuranceCode):
                        if (!string.IsNullOrWhiteSpace(Patient.InsuranceCode))
                        {
                            if (!Regex.IsMatch(Patient.InsuranceCode.Trim(), @"^\d{10}$"))
                            {
                                error = "Mã BHYT phải có đúng 10 chữ số";
                            }
                        }
                        break;

                    case nameof(Patient.DateOfBirth):
                        if (Patient.DateOfBirth.HasValue)
                        {
                            var today = DateOnly.FromDateTime(DateTime.Today);
                            if (Patient.DateOfBirth > today)
                            {
                                error = "Ngày sinh không thể lớn hơn ngày hiện tại";
                            }
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Thực hiện validation toàn diện cho thông tin bệnh nhân
        /// Được gọi trước khi lưu thông tin
        /// </summary>
        /// <returns>True nếu dữ liệu hợp lệ, False nếu có lỗi</returns>
        private bool ValidatePatient()
        {
            if (Patient == null)
                return false;

            // Bật chế độ validation và đánh dấu tất cả field được touched
            _isValidating = true;
            _touchedFields.Add(nameof(Patient.FullName));
            _touchedFields.Add(nameof(Patient.Phone));
            _touchedFields.Add(nameof(Patient.DateOfBirth));
            _touchedFields.Add(nameof(Patient.InsuranceCode));

            // Kiểm tra từng field và hiển thị lỗi nếu có
            string fullNameError = this[nameof(Patient.FullName)];
            string phoneError = this[nameof(Patient.Phone)];
            string dateOfBirthError = this[nameof(Patient.DateOfBirth)];
            string insuranceCodeError = this[nameof(Patient.InsuranceCode)];

            if (!string.IsNullOrEmpty(fullNameError))
            {
                MessageBoxService.ShowError(fullNameError, "Lỗi dữ liệu");
                return false;
            }

            if (!string.IsNullOrEmpty(phoneError))
            {
                MessageBoxService.ShowError(phoneError, "Lỗi dữ liệu");
                return false;
            }

            if (!string.IsNullOrEmpty(dateOfBirthError))
            {
                MessageBoxService.ShowError(dateOfBirthError, "Lỗi dữ liệu");
                return false;
            }

            if (!string.IsNullOrEmpty(insuranceCodeError))
            {
                MessageBoxService.ShowError(insuranceCodeError, "Lỗi dữ liệu");
                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Thiết lập tiêu đề cửa sổ với tên bệnh nhân
        /// </summary>
        /// <param name="window">Cửa sổ cần cập nhật tiêu đề</param>
        private void LoadWindow(Window window)
        {
            if (window != null && Patient != null)
            {
                window.Title = $"Thông tin chi tiết - {Patient?.FullName}";
            }
        }

        /// <summary>
        /// Tải tất cả dữ liệu liên quan đến bệnh nhân
        /// Bao gồm thông tin cá nhân, loại bệnh nhân, hồ sơ, hóa đơn, lịch hẹn
        /// </summary>
        private void LoadRelatedData()
        {
            if (Patient == null) return;

            // Tải thông tin bệnh nhân vào các property để binding
            PatientId = Patient.PatientId;
            FullName = Patient.FullName ?? string.Empty;
            DateOfBirth = Patient.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
            Phone = Patient.Phone ?? string.Empty;
            InsuranceCode = Patient.InsuranceCode ?? string.Empty;
            Gender = Patient.Gender ?? string.Empty;
            Address = Patient.Address ?? string.Empty;
            PatientTypeId = Patient.PatientType.PatientTypeId;
            TypeName = Patient.PatientType.TypeName ?? string.Empty;
            Discount = Patient.PatientType.Discount?.ToString() ?? string.Empty;

            // Tải danh sách loại bệnh nhân cho ComboBox
            PatientTypes = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => pt.IsDeleted != true)
                .OrderBy(pt => pt.TypeName)
                .ToList()
            );

            // Thiết lập loại bệnh nhân được chọn
            SelectedPatientType = PatientTypes.FirstOrDefault(pt => pt.PatientTypeId == Patient.PatientTypeId);

            // Tải các collection dữ liệu liên quan
            LoadMedicalRecords();
            LoadInvoices();
            LoadAppointments();
        }

        /// <summary>
        /// Tải danh sách hồ sơ bệnh án của bệnh nhân
        /// Sắp xếp theo ngày tạo giảm dần (mới nhất trước)
        /// </summary>
        private void LoadMedicalRecords()
        {
            MedicalRecords = new ObservableCollection<MedicalRecord>(
                DataProvider.Instance.Context.MedicalRecords
                .Include(m => m.Doctor) // Bao gồm thông tin bác sĩ
                .Where(m => m.PatientId == PatientId && m.IsDeleted != true)
                .OrderByDescending(m => m.RecordDate)
                .ToList()
            );
        }

        /// <summary>
        /// Tải danh sách hóa đơn của bệnh nhân
        /// Sắp xếp theo ngày tạo giảm dần và xử lý dữ liệu null
        /// </summary>
        private void LoadInvoices()
        {
            Invoices = new ObservableCollection<Invoice>(
                DataProvider.Instance.Context.Invoices
                .Where(i => i.PatientId == PatientId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList()
                .Select(i => {
                    // Đảm bảo InvoiceType không bao giờ null và trim khoảng trắng
                    if (i.InvoiceType == null)
                    {
                        i.InvoiceType = "Unknown";
                    }
                    else
                    {
                        i.InvoiceType = i.InvoiceType.Trim();
                    }

                    // Trim Status nếu có
                    if (i.Status != null)
                    {
                        i.Status = i.Status.Trim();
                    }

                    return i;
                })
            );
        }

        /// <summary>
        /// Tải danh sách lịch hẹn của bệnh nhân và khởi tạo danh sách filter
        /// Sắp xếp theo ngày hẹn giảm dần và xử lý dữ liệu null
        /// </summary>
        private void LoadAppointments()
        {
            Appointments = new ObservableCollection<Appointment>(
                DataProvider.Instance.Context.Appointments
                .Include(a => a.Staff) // Bao gồm thông tin nhân viên
                .Where(a => a.PatientId == PatientId && a.IsDeleted != true)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList()
                .Select(a => {
                    // Trim Status nếu có
                    if (a.Status != null)
                    {
                        a.Status = a.Status.Trim();
                    }
                    return a;
                })
            );

            // Tạo danh sách trạng thái lịch hẹn với giá trị đã trim
            AppointmentStatusList = new ObservableCollection<StatusItem>
    {
        new StatusItem("", "Tất cả"),
        new StatusItem("Đang chờ", "Đang chờ"),
        new StatusItem("Đang khám", "Đang khám"),
        new StatusItem("Đã khám", "Đã khám"),
        new StatusItem("Đã hủy", "Đã hủy")
    };

            // Tạo danh sách trạng thái hóa đơn với giá trị đã trim
            InvoiceStatusList = new ObservableCollection<StatusItem>
    {
        new StatusItem("", "Tất cả"),
        new StatusItem("Đã thanh toán", "Đã thanh toán"),
        new StatusItem("Chưa thanh toán", "Chưa thanh toán")
    };
        }

        /// <summary>
        /// Cập nhật thông tin bệnh nhân vào cơ sở dữ liệu
        /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
        /// </summary>
        private void UpdatePatient()
        {
            try
            {
                // Validate dữ liệu bệnh nhân trước khi lưu
                if (!ValidatePatient())
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm bệnh nhân trong database
                        var patientToUpdate = DataProvider.Instance.Context.Patients
                            .FirstOrDefault(p => p.PatientId == PatientId);

                        if (patientToUpdate == null)
                        {
                            MessageBoxService.ShowError("Không tìm thấy thông tin bệnh nhân!", "Lỗi");
                            return;
                        }

                        // Cập nhật thông tin bệnh nhân
                        patientToUpdate.FullName = FullName?.Trim();
                        patientToUpdate.DateOfBirth = DateOfBirth.HasValue ? DateOnly.FromDateTime(DateOfBirth.Value) : null;
                        patientToUpdate.PatientTypeId = PatientTypeId;
                        patientToUpdate.Gender = Gender;
                        patientToUpdate.Phone = Phone?.Trim();
                        patientToUpdate.Address = Address?.Trim();
                        patientToUpdate.InsuranceCode = InsuranceCode?.Trim();

                        // Lưu thay đổi vào database
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi tất cả thay đổi thành công
                        transaction.Commit();

                        // Refresh dữ liệu từ database
                        var refreshedPatient = DataProvider.Instance.Context.Patients
                            .Include(p => p.PatientType)
                            .FirstOrDefault(p => p.PatientId == PatientId);

                        MessageBoxService.ShowSuccess("Thông tin bệnh nhân đã được cập nhật thành công!",
                                   "Thông báo");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi xảy ra
                        transaction.Rollback();

                        // Ném lại exception để xử lý ở catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi cập nhật thông tin: {ex.Message}",
                           "Lỗi");
            }
        }

        /// <summary>
        /// Xóa mềm bệnh nhân khỏi hệ thống
        /// Kiểm tra ràng buộc dữ liệu trước khi xóa
        /// </summary>
        private void DeletePatient()
        {
            try
            {
                // Kiểm tra xem bệnh nhân có lịch hẹn đang hoạt động không
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.PatientId == Patient.PatientId &&
                          (a.Status == "Đang chờ" || a.Status == "Đã khám" || a.Status == "Đã hủy") &&
                          a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa bệnh nhân này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng hủy tất cả lịch hẹn trước khi xóa bệnh nhân.",
                        "Cảnh báo");
                    return;
                }

                // Xác nhận với người dùng
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc chắn muốn xóa bệnh nhân {Patient.FullName} không?",
                    "Xác nhận xóa");

                if (result)
                {
                    // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                    using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                    {
                        try
                        {
                            var patientToDelete = DataProvider.Instance.Context.Patients
                                .FirstOrDefault(p => p.PatientId == Patient.PatientId);

                            if (patientToDelete != null)
                            {
                                // Soft delete - đánh dấu IsDeleted = true
                                patientToDelete.IsDeleted = true;
                                DataProvider.Instance.Context.SaveChanges();

                                // Commit transaction khi xóa thành công
                                transaction.Commit();

                                MessageBoxService.ShowSuccess("Đã xóa bệnh nhân thành công!", "Thông báo");

                                // Đóng cửa sổ sau khi xóa
                                _window?.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction nếu có lỗi xảy ra
                            transaction.Rollback();

                            // Ném lại exception để xử lý ở catch bên ngoài
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xóa bệnh nhân: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Lọc hồ sơ bệnh án theo khoảng thời gian và từ khóa tìm kiếm
        /// Hỗ trợ tìm kiếm theo chẩn đoán hoặc tên bác sĩ
        /// </summary>
        private void FilterMedicalRecords()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.MedicalRecords
                    .Include(m => m.Doctor)
                    .Where(m =>
                        m.PatientId == Patient.PatientId &&
                        m.IsDeleted != true);

                // Áp dụng lọc theo khoảng thời gian
                if (StartDate != DateTime.MinValue && EndDate != DateTime.MinValue)
                {
                    // Thêm 1 ngày vào EndDate để bao gồm cả ngày kết thúc
                    var endDatePlus = EndDate.AddDays(1);
                    query = query.Where(m => m.RecordDate >= StartDate && m.RecordDate < endDatePlus);
                }

                // Áp dụng lọc theo từ khóa tìm kiếm
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTerm = SearchTerm.ToLower().Trim();
                    query = query.Where(m =>
                        (m.Diagnosis != null && m.Diagnosis.ToLower().Contains(searchTerm)) ||
                        (m.Doctor.FullName != null && m.Doctor.FullName.ToLower().Contains(searchTerm))
                    );
                }

                // Sắp xếp theo ngày giảm dần
                var filteredRecords = query.OrderByDescending(m => m.RecordDate).ToList();

                // Cập nhật UI
                MedicalRecords = new ObservableCollection<MedicalRecord>(filteredRecords);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc hồ sơ bệnh án: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Lọc hóa đơn theo khoảng thời gian và trạng thái thanh toán
        /// </summary>
        private void FilterInvoices()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Invoices
                    .Where(i => i.PatientId == PatientId);

                // Áp dụng lọc theo khoảng thời gian
                if (InvoiceStartDate != DateTime.MinValue && InvoiceEndDate != DateTime.MinValue)
                {
                    // Thêm 1 ngày vào EndDate để bao gồm cả ngày kết thúc
                    var endDatePlus = InvoiceEndDate.AddDays(1);
                    query = query.Where(i => i.InvoiceDate >= InvoiceStartDate && i.InvoiceDate < endDatePlus);
                }

                // Lọc theo trạng thái - sử dụng trim comparison
                if (SelectedInvoiceStatus != null && !string.IsNullOrEmpty(SelectedInvoiceStatus.Status))
                {
                    var statusToFilter = SelectedInvoiceStatus.Status.Trim();
                    // Sử dụng phương thức so sánh string của EF
                    query = query.Where(i => i.Status.Trim() == statusToFilter);
                }

                // Sắp xếp theo ngày giảm dần
                var filteredInvoices = query.OrderByDescending(i => i.InvoiceDate).ToList();

                // Áp dụng xử lý bổ sung và trim
                Invoices = new ObservableCollection<Invoice>(
                    filteredInvoices.Select(i => {
                        if (i.InvoiceType != null) i.InvoiceType = i.InvoiceType.Trim();
                        if (i.Status != null) i.Status = i.Status.Trim();
                        return i;
                    })
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc hóa đơn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Lọc lịch hẹn theo trạng thái
        /// </summary>
        private void FilterAppointments()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Staff)
                    .Where(a => a.PatientId == PatientId && a.IsDeleted != true);

                // Lọc theo trạng thái với trim
                if (SelectedAppointmentStatus != null && !string.IsNullOrEmpty(SelectedAppointmentStatus.Status))
                {
                    var statusToFilter = SelectedAppointmentStatus.Status.Trim();
                    query = query.Where(a => a.Status.Trim() == statusToFilter);
                }

                // Sắp xếp theo ngày
                var filteredAppointments = query.OrderByDescending(a => a.AppointmentDate).ToList();

                // Xử lý và trim giá trị Status
                Appointments = new ObservableCollection<Appointment>(
                    filteredAppointments.Select(a => {
                        if (a.Status != null) a.Status = a.Status.Trim();
                        return a;
                    })
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Hiển thị đơn thuốc trong popup
        /// </summary>
        /// <param name="record">Hồ sơ bệnh án chứa đơn thuốc</param>
        private void ViewPrescription(MedicalRecord record)
        {
            MessageBoxService.ShowInfo(record.Prescription, "Đơn thuốc");
        }

        /// <summary>
        /// Hiển thị kết quả xét nghiệm trong popup
        /// </summary>
        /// <param name="record">Hồ sơ bệnh án chứa kết quả xét nghiệm</param>
        private void ViewTestResults(MedicalRecord record)
        {
            MessageBoxService.ShowInfo(record.TestResults, "Kết quả xét nghiệm");
        }

        /// <summary>
        /// Mở cửa sổ chi tiết hồ sơ bệnh án
        /// </summary>
        /// <param name="record">Hồ sơ bệnh án cần xem chi tiết</param>
        private void OpenMedicalRecord(MedicalRecord record)
        {
            if (record == null) return;

            // Tạo cửa sổ chi tiết hồ sơ bệnh án
            var medicalRecordWindow = new MedicalRecorDetailsWindow();

            // Tạo ViewModel với hồ sơ được chọn
            var viewModel = new MedicalRecordDetailsViewModel(record);

            // Thiết lập DataContext
            medicalRecordWindow.DataContext = viewModel;

            // Hiển thị dialog
            medicalRecordWindow.ShowDialog();
        }

        /// <summary>
        /// Mở cửa sổ chi tiết hóa đơn
        /// Refresh danh sách hóa đơn sau khi đóng (trong trường hợp có thay đổi)
        /// </summary>
        /// <param name="invoice">Hóa đơn cần xem chi tiết</param>
        private void ViewInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            try
            {
                // Tạo cửa sổ chi tiết hóa đơn mới
                var invoiceDetailsWindow = new InvoiceDetailsWindow();

                // Tạo ViewModel với hóa đơn được chọn
                var viewModel = new InvoiceDetailsViewModel(invoice);

                // Thiết lập DataContext
                invoiceDetailsWindow.DataContext = viewModel;

                // Hiển thị dialog
                invoiceDetailsWindow.ShowDialog();

                // Refresh danh sách hóa đơn sau khi xem (trong trường hợp có thay đổi)
                FilterInvoices();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi mở chi tiết hóa đơn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Thêm lịch hẹn mới cho bệnh nhân
        /// Chuyển đến tab lịch hẹn trong MainWindow và thiết lập bệnh nhân được chọn
        /// </summary>
        private void AddNewAppointment()
        {
            try
            {
                // Lấy MainWindow và TabControl
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                if (tabControl == null) return;

                // Tìm tab lịch hẹn và chuyển đến
                foreach (var item in tabControl.Items)
                {
                    if (item is TabItem tabItem && tabItem.Name == "AppointmentTab")
                    {
                        // Lấy AppointmentViewModel
                        var appointmentVM = Application.Current.Resources["AppointmentVM"] as AppointmentViewModel;
                        if (appointmentVM != null)
                        {
                            // Thiết lập bệnh nhân được chọn trong appointment VM
                            appointmentVM.SelectedPatient = Patient;

                            // Chuyển đến tab lịch hẹn
                            tabControl.SelectedItem = tabItem;

                            // Đóng cửa sổ hiện tại
                            _window?.Close();
                            return;
                        }
                    }
                }

                // Nếu không tìm thấy tab
                MessageBoxService.ShowWarning("Không thể chuyển đến tab Lịch hẹn.", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi thêm lịch hẹn mới: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Xem/chỉnh sửa chi tiết lịch hẹn
        /// Mở cửa sổ AppointmentDetailsWindow với dữ liệu lịch hẹn được chọn
        /// </summary>
        /// <param name="appointment">Lịch hẹn cần xem chi tiết</param>
        private void ViewAppointment(Appointment appointment)
        {
            if (appointment == null) return;

            try
            {
                // Tạo ViewModel chi tiết lịch hẹn với lịch hẹn được chọn
                var appointmentDetailsViewModel = new AppointmentDetailsViewModel(appointment);

                // Tạo và hiển thị cửa sổ
                var appointmentDetailsWindow = new AppointmentDetailsWindow();
                appointmentDetailsWindow.DataContext = appointmentDetailsViewModel;
                appointmentDetailsWindow.ShowDialog();

                // Refresh danh sách lịch hẹn sau khi cửa sổ đóng
                FilterAppointments();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi mở cửa sổ sửa lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Hủy lịch hẹn với lý do
        /// Chỉ cho phép hủy các lịch hẹn chưa hoàn thành hoặc chưa bị hủy
        /// </summary>
        /// <param name="appointment">Lịch hẹn cần hủy</param>
        private void CancelExistingAppointment(Appointment appointment)
        {
            try
            {
                if (appointment == null) return;

                // Hiển thị hộp thoại yêu cầu lý do hủy
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Vui lòng nhập lý do hủy lịch hẹn:",
                    "Xác nhận hủy",
                    appointment.Notes ?? "");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng nhập lý do hủy lịch hẹn.",
                        "Thiếu thông tin");
                    return;
                }

                // Xác nhận với người dùng
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy");

                if (result)
                {
                    var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                        .FirstOrDefault(a => a.AppointmentId == appointment.AppointmentId);

                    if (appointmentToUpdate != null)
                    {
                        appointmentToUpdate.Status = "Đã hủy";  // Sử dụng "Đã hủy" thay vì "Canceled"
                        appointmentToUpdate.Notes = reason;
                        DataProvider.Instance.Context.SaveChanges();

                        // Refresh danh sách lịch hẹn
                        FilterAppointments();

                        MessageBoxService.ShowSuccess(
                            "Đã hủy lịch hẹn thành công!",
                            "Thông báo");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi hủy lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật quyền UI dựa trên vai trò của tài khoản hiện tại
        /// Admin và Manager có quyền chỉnh sửa và xóa bệnh nhân
        /// </summary>
        private void UpdatePermissions()
        {
            // Mặc định không có quyền gì
            CanEditPatient = false;
            CanDeletePatient = false;

            // Kiểm tra xem tài khoản hiện tại có tồn tại không
            if (CurrentAccount == null)
                return;

            // Kiểm tra quyền dựa trên vai trò
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Admin và Manager có quyền đầy đủ
            if (role.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                role.Equals(UserRoles.Manager, StringComparison.OrdinalIgnoreCase))
            {
                CanEditPatient = true;
                CanDeletePatient = true;
            }

            // Buộc command CanExecute được đánh giá lại
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Kiểm tra xem có thể cập nhật thông tin bệnh nhân không
        /// </summary>
        /// <returns>True nếu có bệnh nhân và tên không rỗng</returns>
        private bool CanUpdatePatient()
        {
            return Patient != null && !string.IsNullOrWhiteSpace(Patient.FullName);
        }

        /// <summary>
        /// Kiểm tra xem có thể xóa bệnh nhân không
        /// Chỉ cho phép xóa khi không có lịch hẹn đang trong quá trình khám
        /// </summary>
        /// <returns>True nếu có thể xóa, False nếu có ràng buộc dữ liệu</returns>
        private bool CanDeletePatientData()
        {
            if (Patient == null)
                return false;

            // Chỉ kiểm tra lịch hẹn đang hoạt động (đang khám)
            bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                .Any(a => a.PatientId == Patient.PatientId &&
                      a.Status == "Đang khám" && // Chỉ kiểm tra lịch hẹn đang trong quá trình khám
                      a.IsDeleted != true);

            return !hasActiveAppointments;
        }
    }
}