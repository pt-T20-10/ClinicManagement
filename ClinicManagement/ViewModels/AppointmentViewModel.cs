using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;
using System.Xml.Linq;
using ClinicManagement.Services;

namespace ClinicManagement.ViewModels
{
    public class AppointmentViewModel : BaseViewModel, IDataErrorInfo
    {
        public string? Error => null;
        #region Properties

        #region TypeAppointment
        private ObservableCollection<AppointmentType> _ListAppointmentType;
        public ObservableCollection<AppointmentType> ListAppointmentType
        {
            get => _ListAppointmentType;
            set
            {
                _ListAppointmentType = value;
                OnPropertyChanged();
            }
        }

        // Change property declarations to be nullable
        private AppointmentType? _SelectedAppointmentType;
        public AppointmentType? SelectedAppointmentType
        {
            get => _SelectedAppointmentType;
            set
            {
                _SelectedAppointmentType = value;
                OnPropertyChanged(nameof(SelectedAppointmentType));
                if (value != null)
                {
                    TypeDisplayName = value.TypeName;
                    TypeDescription = value.Description;
                    TypePrice = value.Price;
                }
            }
        }

        private string? _TypeDescription;
        public string? TypeDescription
        {
            get => _TypeDescription;
            set
            {
                _TypeDescription = value;
                OnPropertyChanged();
            }
        }

        private string? _TypeDisplayName;
        public string? TypeDisplayName
        {
            get => _TypeDisplayName;
            set
            {
                _TypeDisplayName = value;
                OnPropertyChanged();
            }
        }
        private decimal? _TypePrice;
        public decimal? TypePrice
        {
            get => _TypePrice;
            set
            {
                _TypePrice = value;
                OnPropertyChanged();
            }
        }

        #endregion

                #region Appointment Form Properties
                // Patient search
        private string _patientSearch;
        // Patient search
        public string PatientSearch
        {
            get => _patientSearch;
            set
            {
                _patientSearch = value;
                OnPropertyChanged();
                SearchPatients();
                // Remove the auto-find code that caused stack overflow
            }
        }

        private ObservableCollection<Patient> _searchedPatients;
        public ObservableCollection<Patient> SearchedPatients
        {
            get => _searchedPatients ??= new ObservableCollection<Patient>();
            set
            {
                _searchedPatients = value;
                OnPropertyChanged();
            }
        }

        private Patient? _selectedPatient;
        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();
                if (value != null)
                {
                    PatientPhone = value.Phone ?? string.Empty;
                }
                // Remove the call to ValidateFormSequence() to prevent date/time reset
                // Just update the validation flag
                _isPatientInfoValid = value != null;
            }
        }


        // Doctor selection
        private ObservableCollection<Staff> _doctorList;
        public ObservableCollection<Staff> DoctorList
        {
            get => _doctorList;
            set
            {
                _doctorList = value;
                OnPropertyChanged();
            }
        }

        private Staff? _selectedDoctor;
        public Staff? SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
                // Remove the date/time reset and ValidateFormSequence call
                // Just update the validation flag
                _isStaffselected = value != null;
            }
        }

        // Appointment type selection
        private ObservableCollection<AppointmentType> _appointmentTypes;
        public ObservableCollection<AppointmentType> AppointmentTypes
        {
            get => _appointmentTypes;
            set
            {
                _appointmentTypes = value;
                OnPropertyChanged();
            }
        }

        // Date selection
        private DateTime? _appointmentDate = DateTime.Today;
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                _appointmentDate = value;
                OnPropertyChanged();
                if (value.HasValue)
                {
                    // Reset time when date changes to ensure validation
                    SelectedAppointmentTime = null;
                }
            }
        }

        // Patient phone
        private string _patientPhone;
        public string PatientPhone
        {
            get => _patientPhone;
            set
            {
                _patientPhone = value;
                OnPropertyChanged();
                _touchedFields.Add(nameof(PatientPhone));
                // Remove the auto-find code that caused stack overflow
            }
        }

        // Appointment notes
        private string _appointmentNote;
        public string AppointmentNote
        {
            get => _appointmentNote;
            set
            {
                _appointmentNote = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Appointment Properties
        private ObservableCollection<Appointment> _Appointments = new();
        public ObservableCollection<Appointment> Appointments
        {
            get => _Appointments;
            set
            {
                _Appointments = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<AppointmentDisplayInfo>? _AppointmentsDisplay;
        public ObservableCollection<AppointmentDisplayInfo> AppointmentsDisplay
        {
            get => _AppointmentsDisplay ??= new ObservableCollection<AppointmentDisplayInfo>();
            set
            {
                _AppointmentsDisplay = value;
                OnPropertyChanged();
            }
        }

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

        private string _selectedAppointmentStatus = string.Empty;
        public string SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
                // Auto-reload appointments when status changes
                LoadAppointments();
            }
        }

        private DateTime? _selectedAppointmentTime;
        public DateTime? SelectedAppointmentTime
        {
            get => _selectedAppointmentTime;
            set
            {
                _selectedAppointmentTime = value;
                OnPropertyChanged();
            }
        }
        // Add these to your existing commands in AppointmentViewModel.cs
        public ICommand ConfirmTimeSelectionCommand { get; set; }
        public ICommand CancelTimeSelectionCommand { get; set; }

        // Fix the property name - consistent naming
        private DateTime _filterDate = DateTime.Today;
        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
                // Auto-reload appointments when date changes
                LoadAppointments();
            }
        }

        // Search text for appointment list
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        // AppontmentType Commands
        public ICommand AddAppointmentTypeCommand { get; set; }
        public ICommand EditAppointmentTypeCommand { get; set; }
        public ICommand DeleteAppointmentTypeCommand { get; set; }
        public ICommand RefreshTypeCommand { get; set; }

        // Appointment Form Commands
        public ICommand CancelCommand { get; set; }
        public ICommand AddAppointmentCommand { get; set; }
        public ICommand SelectPatientCommand { get; set; }
        public ICommand FindPatientCommand { get; set; }
        public ICommand CancelAppointmentCommand { get; set; }

        // Appointment List Commands
        public ICommand SearchCommand { get; set; }
        public ICommand SearchAppointmentsCommand { get; set; }
        public ICommand OpenAppointmentDetailsCommand { get; set; }
        #endregion

        #region Validation
  
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        // Validation sequence flags
        private bool _isStaffselected = false;
        private bool _isPatientInfoValid = false;
        private bool _isDateTimeValid = false;
        #endregion

        #endregion

     
           public AppointmentViewModel()
        {
            LoadData(); 
        }
        public void LoadData()
        {
            _filterDate = DateTime.Today;
           
            _appointmentNote = string.Empty;
            _searchText = string.Empty;

            // Initialize collections
            _ListAppointmentType = new ObservableCollection<AppointmentType>();
            _searchedPatients = new ObservableCollection<Patient>();
            _doctorList = new ObservableCollection<Staff>();
            _appointmentTypes = new ObservableCollection<AppointmentType>();

            // These can be null
            _selectedPatient = null!;
            _selectedDoctor = null!;
            _SelectedAppointmentType = null!;

            LoadAppointmentTypeData();
            InitializeCommands();
            InitializeData();

            // Initialize validation collections
            _touchedFields = new HashSet<string>();
        }

        private void LoadAppointmentTypeData()
        {
            ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                    .Where(a => (bool)!a.IsDeleted)
                    .ToList()
                );

            // Initialize AppointmentTypes for the form
            AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);
        }

        private void InitializeCommands()
        {
            AddAppointmentTypeCommand = new RelayCommand<object>(
               (p) => AddAppontmentType(),
               (p) => !string.IsNullOrEmpty(TypeDisplayName)
            );

            EditAppointmentTypeCommand = new RelayCommand<object>(
                (p) => EditAppontmentType(),
                (p) => SelectedAppointmentType != null && !string.IsNullOrEmpty(TypeDisplayName)
            );

            DeleteAppointmentTypeCommand = new RelayCommand<object>(
                (p) => DeleteAppointmentType(),
                (p) => SelectedAppointmentType != null
            );
            RefreshTypeCommand = new RelayCommand<object>
          ((p) => ExecuteRefreshType(),
              (p) => true
          );



            // Initialize form commands
            CancelCommand = new RelayCommand<object>(
                (p) => ClearAppointmentForm(),
                (p) => true
            );

            AddAppointmentCommand = new RelayCommand<object>(
                (p) => AddNewAppointment(),
                (p) => CanAddAppointment()
            );

            SelectPatientCommand = new RelayCommand<Patient>(
                (p) => SelectPatient(p),
                (p) => p != null
            );

            // Initialize search commands
            SearchCommand = new RelayCommand<object>(
                (p) => SearchAppointments(),
                (p) => true
            );

            SearchAppointmentsCommand = new RelayCommand<object>(
                (p) => SearchAppointments(),
                (p) => true
            );
            // Initialize time picker commands
          
            FindPatientCommand = new RelayCommand<object>(
              (p) => FindOrCreatePatient(),
              (p) => !string.IsNullOrWhiteSpace(PatientSearch) && !string.IsNullOrWhiteSpace(PatientPhone)
           );

            CancelTimeSelectionCommand = new RelayCommand<object>(
                (p) => {  },
                (p) => true
            );
            OpenAppointmentDetailsCommand = new RelayCommand<AppointmentDisplayInfo>(
               (p) => OpenAppointmentDetails(p),
               (p) => p!=null
               );
            CancelAppointmentCommand = new RelayCommand<AppointmentDisplayInfo>(
               (p) => CancelAppointmentDirectly(p),
               (p) => p != null && p.Status != "Đã hủy" && p.Status != "Đã khám" && p.Status != "Đang khám"
           );
        }
        private void CancelAppointmentDirectly(AppointmentDisplayInfo appointmentInfo)
        {
            try
            {
                if (appointmentInfo?.OriginalAppointment == null)
                    return;

                // Hiển thị hộp thoại xác nhận và yêu cầu lý do hủy
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Vui lòng nhập lý do hủy lịch hẹn:",
                    "Xác nhận hủy",
                    appointmentInfo.OriginalAppointment.Notes ?? "");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng nhập lý do hủy lịch hẹn.",
                        "Thiếu thông tin");
                    return;
                }

                 bool  result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy"
                    );

                if (result)
                {
                    var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                        .FirstOrDefault(a => a.AppointmentId == appointmentInfo.OriginalAppointment.AppointmentId);

                    if (appointmentToUpdate != null)
                    {
                        appointmentToUpdate.Status = "Đã hủy";
                        appointmentToUpdate.Notes = reason;
                        DataProvider.Instance.Context.SaveChanges();

                        // Refresh appointments list
                        LoadAppointments();

                        MessageBoxService.ShowSuccess(
                            "Đã hủy lịch hẹn thành công!",
                            "Thông báo"
                          );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi hủy lịch hẹn: {ex.Message}",
                    "Lỗi");
            }
        }
        private void OpenAppointmentDetails(AppointmentDisplayInfo appointmentInfo)
        {
            try
            {
                if (appointmentInfo == null || appointmentInfo.OriginalAppointment == null)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng chọn một lịch hẹn để xem chi tiết.",
                        "Thông báo");
                    return;
                }

                // Load appointment with all related data
                var fullAppointment = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Staff)
                        .ThenInclude(d => d.Specialty)
                    .Include(a => a.AppointmentType)
                    .FirstOrDefault(a => a.AppointmentId == appointmentInfo.AppointmentId);

                if (fullAppointment == null)
                {
                    MessageBoxService.ShowError(
                        "Không thể tải thông tin chi tiết lịch hẹn."
                   );
                    return;
                }

                // Create and setup details window
                var detailsWindow = new AppointmentDetailsWindow();
                var viewModel = detailsWindow.DataContext as AppointmentDetailsViewModel;

                if (viewModel != null)
                {
                    viewModel.OriginalAppointment = fullAppointment;
                    detailsWindow.ShowDialog(); // Show as modal dialog

                    // Refresh appointments list after dialog closes
                    LoadAppointments();
                }
                else
                {
                    MessageBoxService.ShowError(
                        "Không thể khởi tạo cửa sổ chi tiết lịch hẹn."
                  );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi mở chi tiết lịch hẹn: {ex.Message}");
            }
        }


        private void SelectPatient(Patient patient)
        {
            SelectedPatient = patient;
            PatientSearch = patient?.FullName ?? string.Empty;
            SearchedPatients.Clear();
        }

        private void SearchPatients()
        {
            if (string.IsNullOrWhiteSpace(PatientSearch) || PatientSearch.Length < 2)
            {
                SearchedPatients.Clear();
                return;
            }

            try
            {
                string searchLower = PatientSearch.ToLower().Trim();
                var patients = DataProvider.Instance.Context.Patients
                    .Where(p => p.IsDeleted != true &&
                           (p.FullName.ToLower().Contains(searchLower) ||
                           (p.InsuranceCode != null && p.InsuranceCode.ToLower().Contains(searchLower)) ||
                           (p.Phone != null && p.Phone.Contains(searchLower))))
                    .OrderBy(p => p.FullName)
                    .Take(5)
                    .ToList();

                SearchedPatients = new ObservableCollection<Patient>(patients);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm bệnh nhân: {ex.Message}"
            );
            }
        }

        private void FindOrCreatePatient(bool silentMode = false)
        {
            try
            {
                // Validate input data before proceeding
                if (string.IsNullOrWhiteSpace(PatientSearch))
                {
                    if (!silentMode)
                    {
                        MessageBoxService.ShowWarning(
                            "Vui lòng nhập tên hoặc mã bảo hiểm của bệnh nhân.",
                            "Thiếu thông tin");
                    }
                    return;
                }

                // Check if it looks like an insurance code (contains digits and no spaces)
                bool looksLikeInsuranceCode = PatientSearch.Any(char.IsDigit) && !PatientSearch.Contains(" ");

                // Only validate phone if not searching by insurance code
                if (!looksLikeInsuranceCode && string.IsNullOrWhiteSpace(PatientPhone))
                {
                    if (!silentMode)
                    {
                        MessageBoxService.ShowWarning(
                            "Vui lòng nhập số điện thoại của bệnh nhân.",
                            "Thiếu thông tin"
                         );
                    }
                    return;
                }

                // Validate phone number format only if it's provided
                if (!string.IsNullOrWhiteSpace(PatientPhone) &&
                    !Regex.IsMatch(PatientPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                {
                    if (!silentMode)
                    {
                        MessageBoxService.ShowWarning(
                            "Số điện thoại không đúng định dạng. Vui lòng nhập số điện thoại hợp lệ (VD: 0901234567).",
                            "Số điện thoại không hợp lệ"
                      );
                    }
                    return;
                }

                // First, try to find an existing patient
                var patient = FindPatient();

                if (patient != null)
                {
                    // Patient found, set as selected
                    SelectedPatient = patient;
                    if (!silentMode)
                    {
                        MessageBoxService.ShowInfo(
                            $"Đã tìm thấy bệnh nhân: {patient.FullName}"
                      
                          );
                    }
                    return;
                }

                // If no patient found and in silent mode, just return without message or creating patient
                if (silentMode)
                    return;

                // Special message for insurance code search
                if (looksLikeInsuranceCode)
                {
                    var result = MessageBoxService.ShowQuestion(
                        $"Không tìm thấy bệnh nhân với mã bảo hiểm '{PatientSearch.Trim()}'.\n" +
                        "Bạn có muốn tạo hồ sơ bệnh nhân mới không?",
                        "Mã bảo hiểm không tồn tại"
                  );

                    if (!result)
                        return;

                    // If creating new patient with insurance code, we need to get more info
                    var nameResult = Microsoft.VisualBasic.Interaction.InputBox(
                        "Nhập họ tên bệnh nhân:", "Thông tin bệnh nhân", "");

                    if (string.IsNullOrWhiteSpace(nameResult))
                    {
                        MessageBoxService.ShowWarning(
                            "Không thể tạo bệnh nhân mới vì thiếu họ tên.");
                        return;
                    }

                    // Get phone number if it's not provided
                    string phone = PatientPhone;
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        var phoneResult = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập số điện thoại bệnh nhân:", "Thông tin bệnh nhân", "");

                        if (string.IsNullOrWhiteSpace(phoneResult) ||
                            !Regex.IsMatch(phoneResult.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            MessageBoxService.ShowWarning(
                                "Số điện thoại không hợp lệ. Không thể tạo bệnh nhân mới.",
                                "Số điện thoại không hợp lệ");
                            return;
                        }
                        phone = phoneResult.Trim();
                    }

                    // Create new patient with the insurance code
                    var newPatient = new Patient
                    {
                        FullName = nameResult.Trim(),
                        Phone = phone,
                        InsuranceCode = PatientSearch.Trim(),
                        IsDeleted = false,
                        CreatedAt = DateTime.Now,
                        PatientTypeId = 1  // Default patient type
                    };

                    // Save new patient to database
                    DataProvider.Instance.Context.Patients.Add(newPatient);
                    DataProvider.Instance.Context.SaveChanges();

                    // Set as selected patient
                    SelectedPatient = newPatient;
                    PatientPhone = phone;

                    MessageBoxService.ShowSuccess(
                        $"Đã tạo bệnh nhân mới: {newPatient.FullName}",
                        "Thành công");

                    return;
                }

                // Standard patient creation for name search
                var standardResult = MessageBoxService.ShowQuestion(
                    "Không tìm thấy bệnh nhân với thông tin đã nhập. Bạn có muốn tạo mới không?",
                    "Tạo bệnh nhân mới?"
                
                   );

                if (standardResult)
                {
                    
     
                    string name = PatientSearch.Trim();
                    string insuranceCode = null;

                    // If it looks like an insurance code (contains digits and is less than 20 chars),
                    // ask for patient's name
                    if (PatientSearch.Any(char.IsDigit) && PatientSearch.Length < 20)
                    {
                        var nameResult = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập họ tên bệnh nhân:", "Thông tin bệnh nhân", "");

                        if (string.IsNullOrWhiteSpace(nameResult))
                        {
                             MessageBoxService.ShowWarning(
                                "Không thể tạo bệnh nhân mới vì thiếu họ tên.",
                                "Thiếu thông tin"
                                
                                );
                            return; // User cancelled or provided empty name
                        }

                        name = nameResult.Trim();
                        insuranceCode = PatientSearch.Trim();
                    }

                    // Additional validation for name
                    if (name.Length < 2)
                    {
                        MessageBoxService.ShowWarning(
                            "Tên bệnh nhân phải có ít nhất 2 ký tự.",
                            "Tên không hợp lệ");
                        return;
                    }

                    // Create and save new patient
                    var newPatient = new Patient
                    {
                        FullName = name,
                        Phone = PatientPhone.Trim(),
                        InsuranceCode = insuranceCode,
                        IsDeleted = false,
                        CreatedAt = DateTime.Now,
                        PatientTypeId = 1  // Default patient type
                    };

                    // Check if patient with same name and phone already exists
                    var existingPatient = DataProvider.Instance.Context.Patients
                        .FirstOrDefault(p =>
                            p.IsDeleted != true &&
                            p.Phone == PatientPhone.Trim() &&
                            p.FullName.ToLower() == name.ToLower());

                    if (existingPatient != null)
                    {
                        MessageBoxService.ShowWarning(
                            $"Đã tồn tại bệnh nhân '{existingPatient.FullName}' với số điện thoại này.",
                            "Bệnh nhân đã tồn tại");

                        // Set the existing patient as selected
                        SelectedPatient = existingPatient;
                        return;
                    }

                    // Save new patient to database
                    DataProvider.Instance.Context.Patients.Add(newPatient);
                    DataProvider.Instance.Context.SaveChanges();

                    // Set as selected patient
                    SelectedPatient = newPatient;

                    MessageBoxService.ShowSuccess(
                        $"Đã tạo bệnh nhân mới: {newPatient.FullName}");
                }
            }
            catch (DbUpdateException dbEx)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi lưu thông tin bệnh nhân: {dbEx.InnerException?.Message ?? dbEx.Message}",
                    "Lỗi cơ sở dữ liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm hoặc tạo bệnh nhân: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Finds a patient based on PatientSearch (name or insurance code) and Phone
        /// </summary>
        private Patient? FindPatient()
        {
            if (string.IsNullOrWhiteSpace(PatientSearch))
                return null;

            // First try to find by exact match of insurance code only (without requiring phone)
            var patientByInsurance = DataProvider.Instance.Context.Patients
                .FirstOrDefault(p =>
                    p.IsDeleted != true &&
                    p.InsuranceCode == PatientSearch.Trim());

            if (patientByInsurance != null)
                return patientByInsurance;

            // If no match by insurance code, require phone number for the rest of the searches
            if (string.IsNullOrWhiteSpace(PatientPhone))
                return null;

            // Try by name + phone
            var patient = DataProvider.Instance.Context.Patients
                .FirstOrDefault(p =>
                    p.IsDeleted != true &&
                    p.Phone == PatientPhone.Trim() &&
                    p.FullName.ToLower() == PatientSearch.Trim().ToLower());

            if (patient != null)
                return patient;

            // Try partial name match + phone if full name match failed
            patient = DataProvider.Instance.Context.Patients
                .FirstOrDefault(p =>
                    p.IsDeleted != true &&
                    p.Phone == PatientPhone.Trim() &&
                    p.FullName.ToLower().Contains(PatientSearch.Trim().ToLower()));

            return patient;
        }

        private void InitializeData()
        {
            // Initialize appointment status list
            AppointmentStatusList = new ObservableCollection<string>
            {
                "Tất cả",
                "Đang chờ",
                "Đã xác nhận",
                "Đang khám",
                "Đã khám",
                "Đã hủy"
            };

            SelectedAppointmentStatus = "Tất cả";

            // Load Staffs for dropdown
            LoadStaffs();

            // Initialize other properties
            PatientSearch = string.Empty;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
            SearchText = string.Empty;

            // Load appointments for today
            LoadAppointments();
        }

        private void LoadStaffs()
        {
            try
            {
                var Staffs = DataProvider.Instance.Context.Staffs
                    .Where(d => d.IsDeleted != true)
                    .OrderBy(d => d.FullName)
                    .ToList();

                DoctorList = new ObservableCollection<Staff>(Staffs);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}",
                    "Lỗi");

                DoctorList = new ObservableCollection<Staff>();
            }
        }

        private void LoadAppointments()
        {
            try
            {
                // Ensure FilterDate has a value, default to today
                if (_filterDate == default(DateTime))
                {
                    _filterDate = DateTime.Today;
                }

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient) // Ensure loading Patient to get FullName
                    .Include(a => a.Staff) // Include Doctor information
                    .Include(a => a.AppointmentType) // Include AppointmentType information
                    .Where(a =>
                        a.IsDeleted != true &&
                        a.AppointmentDate.Date == _filterDate.Date);

                // Apply status filter if not "Tất cả"
                if (!string.IsNullOrEmpty(SelectedAppointmentStatus) && SelectedAppointmentStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus);
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower().Trim();
                    query = query.Where(a =>
                        (a.Patient.FullName != null && a.Patient.FullName.ToLower().Contains(searchLower)) ||
                        (a.Staff.FullName != null && a. Staff.FullName.ToLower().Contains(searchLower))
                    );
                }

                var appointments = query
                    .OrderBy(a => a.AppointmentDate.TimeOfDay) // Sort by time of day
                    .ThenBy(a => a.PatientId) // If same time, sort by PatientId
                    .ToList();

                // Create display-friendly objects with formatted time and reason
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>(
                    appointments.Select(a => new AppointmentDisplayInfo
                    {
                        AppointmentId = a.AppointmentId,
                        PatientName = a.Patient?.FullName ?? "N/A",
                        AppointmentDate = a.AppointmentDate.Date,
                        AppointmentTimeString = a.AppointmentDate.ToString("HH:mm"),
                        Status = a.Status ?? "Chưa xác định",
                        Reason = a.Notes ?? string.Empty,
                        OriginalAppointment = a
                    })
                );

                // Trigger property changed to update UI
                OnPropertyChanged(nameof(AppointmentsDisplay));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}");

                // Ensure AppointmentsDisplay is not null when there's an error
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
            }
        }

        private void SearchAppointments()
        {
            // Simply call LoadAppointments which already handles search filtering
            LoadAppointments();
        }

        private bool CanAddAppointment()
        {
            // Check if we're using insurance code search (no phone required)
            bool usingInsuranceCode = !string.IsNullOrWhiteSpace(PatientSearch) &&
                                       PatientSearch.Any(char.IsDigit) &&
                                       !PatientSearch.Contains(" ");

            // If insurance code search, we don't need phone
            bool hasPatient = SelectedPatient != null ||
                              (usingInsuranceCode && !string.IsNullOrWhiteSpace(PatientSearch)) ||
                              (!string.IsNullOrWhiteSpace(PatientSearch) && !string.IsNullOrWhiteSpace(PatientPhone));

            return SelectedDoctor != null &&
                   hasPatient &&
                   AppointmentDate.HasValue &&
                   SelectedAppointmentTime.HasValue &&
                   SelectedAppointmentType != null;
        }

        private bool IsAppointmentTimeValid()
        {
            if (SelectedDoctor == null || !AppointmentDate.HasValue || !SelectedAppointmentTime.HasValue)
                return false;

            // Combine date and time
            DateTime appointmentDateTime = AppointmentDate.Value.Date
                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

            // Check if appointment is in the past
            if (appointmentDateTime < DateTime.Now)
                return false;

            // Check if patient already has an appointment at this time (with any doctor)
            if (SelectedPatient != null)
            {
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == SelectedPatient.PatientId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                // Check for exact match first (same hour and minute)
                if (patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    return false;

                // Check for overlapping appointments within 30 minutes
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                foreach (var existingAppointment in patientAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference > 0)
                        return false;
                }
            }

            // Parse doctor's schedule to check if the appointment time is within working hours
            if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
            {
                // Get the day of week for the appointment
                DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                // Parse working hours
                var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                // Check if doctor works on this day
                if (!workingDays.Contains(dayCode))
                    return false;

                // Check if time is within working hours
                TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                if (appointmentTime < startTime || appointmentTime > endTime)
                    return false;

                // Get doctor's appointments for that day
                var doctorAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.StaffId == SelectedDoctor.StaffId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                // Check for exact match first (same hour and minute)
                if (doctorAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    return false;

                // Check for overlapping appointments within 30 minutes
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
                foreach (var existingAppointment in doctorAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference % 30 != 0)
                        return false;
                }
            }

            return true;
        }

        private string ConvertDayOfWeekToVietnameseCode(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                DayOfWeek.Sunday => "CN",
                _ => string.Empty
            };
        }

        private (List<string> WorkingDays, TimeSpan StartTime, TimeSpan EndTime) ParseWorkingSchedule(string schedule)
        {
            List<string> workingDays = new List<string>();
            TimeSpan startTime = TimeSpan.Zero;
            TimeSpan endTime = TimeSpan.Zero;
            try
            {
                // Example format: "T2-T6: 8h-17h" or "T2, T3, T4: 7h-13h" or "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
                string[] parts = schedule.Split(':');
                if (parts.Length < 2)
                    return (workingDays, startTime, endTime);

                // Parse days
                string daysSection = parts[0].Trim();
                if (daysSection.Contains('-'))
                {
                    // Range format: "T2-T6"
                    string[] dayRange = daysSection.Split('-');
                    if (dayRange.Length == 2)
                    {
                        string startDay = dayRange[0].Trim();
                        string endDay = dayRange[1].Trim();
                        // Convert to day numbers
                        int startDayNum = ConvertVietNameseCodeToDayNumber(startDay);
                        int endDayNum = ConvertVietNameseCodeToDayNumber(endDay);
                        for (int i = startDayNum; i <= endDayNum; i++)
                        {
                            workingDays.Add(ConvertDayNumberToVietnameseCode(i));
                        }
                    }
                }
                else if (daysSection.Contains(','))
                {
                    // List format: "T2, T3, T4"
                    string[] daysList = daysSection.Split(',');
                    foreach (string day in daysList)
                    {
                        workingDays.Add(day.Trim());
                    }
                }
                else
                {
                    // Single day format: "T2"
                    workingDays.Add(daysSection);
                }

                // Parse time section - join all parts after the first ':'
                string timeSection = string.Join(":", parts.Skip(1)).Trim();

                // Handle multiple time ranges (e.g., "8h-12h, 13h30-17h")
                // For now, we'll take the first and last time to get the overall working period
                var timeRanges = timeSection.Split(',');
                if (timeRanges.Length > 0)
                {
                    // Get first time range for start time
                    var firstRange = timeRanges[0].Trim();
                    var firstRangeParts = firstRange.Split('-');
                    if (firstRangeParts.Length >= 2)
                    {
                        startTime = ParseTimeString(firstRangeParts[0].Trim());
                    }

                    // Get last time range for end time
                    var lastRange = timeRanges[timeRanges.Length - 1].Trim();
                    var lastRangeParts = lastRange.Split('-');
                    if (lastRangeParts.Length >= 2)
                    {
                        endTime = ParseTimeString(lastRangeParts[lastRangeParts.Length - 1].Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                System.Diagnostics.Debug.WriteLine($"Error parsing schedule '{schedule}': {ex.Message}");
                // In case of parsing errors, return empty results
                workingDays.Clear();
            }
            return (workingDays, startTime, endTime);
        }

        private TimeSpan ParseTimeString(string timeStr)
        {
            // Remove 'h' suffix if present
            timeStr = timeStr.Replace("h", "").Trim();

            // Try to parse as hour:minute format first (e.g., "8:30", "13:30")
            timeStr = timeStr.Replace('.', ':'); // Replace dots with colons for consistency
            if (timeStr.Contains(':'))
            {
                string[] parts = timeStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int hrs) && int.TryParse(parts[1], out int mins))
                {
                    if (hrs >= 0 && hrs <= 23 && mins >= 0 && mins <= 59)
                        return new TimeSpan(hrs, mins, 0);
                }
            }

            // Try to parse as hour only (e.g., "8" -> 08:00, "17" -> 17:00)
            if (int.TryParse(timeStr, out int hours))
            {
                if (hours >= 0 && hours <= 23)
                    return new TimeSpan(hours, 0, 0);
            }

            // Try to parse as standard time format (e.g., "08:00:00")
            if (TimeSpan.TryParse(timeStr + ":00", out TimeSpan result))
                return result;

            return TimeSpan.Zero; // Default if parsing fails
        }

        private int ConvertVietNameseCodeToDayNumber(string code)
        {
            return code switch
            {
                "T2" => 2,
                "T3" => 3,
                "T4" => 4,
                "T5" => 5,
                "T6" => 6,
                "T7" => 7,
                "CN" => 8, // Treating Sunday as day 8 for ordering purposes
                _ => 0
            };
        }

        private string ConvertDayNumberToVietnameseCode(int dayNumber)
        {
            return dayNumber switch
            {
                2 => "T2",
                3 => "T3",
                4 => "T4",
                5 => "T5",
                6 => "T6",
                7 => "T7",
                8 => "CN",
                _ => string.Empty
            };
        }

        // Update the AddNewAppointment method to use the silentMode parameter
        private void AddNewAppointment()
        {
            try
            {
                // Enable validation for required fields
                _isValidating = true;

                // Validate patient selection or find/create patient if needed
                if (SelectedPatient == null &&
                    !string.IsNullOrWhiteSpace(PatientSearch) &&
                    !string.IsNullOrWhiteSpace(PatientPhone))
                {
                    // Find patient silently (true for silentMode)
                    FindOrCreatePatient(true);

                    // If patient still not found, show the normal dialog
                    if (SelectedPatient == null)
                    {
                        FindOrCreatePatient(false);
                        if (SelectedPatient == null) return;
                    }
                }
                else if (!ValidatePatientSelection())
                {
                    return;
                }

                // Validate doctor selection
                if (!ValidateStaffselection()) 
                    return;

                // Validate appointment type
                if (!ValidateAppointmentType())
                    return;

                // Validate date/time selection
                if (!ValidateDateTimeSelection())
                    return;

                // All validations passed, confirm creation
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có muốn tạo lịch hẹn cho bệnh nhân {SelectedPatient.FullName} với bác sĩ {SelectedDoctor.FullName} vào {AppointmentDate?.ToString("dd/MM/yyyy")} lúc {SelectedAppointmentTime?.ToString("HH:mm")} không?",
                    "Xác nhận");

                if (!result)
                    return;

                // Fix for correct date/time storage in database
                // Create appointment - explicitly preserve both date and time components
                DateTime appointmentDateTime;
                if (AppointmentDate.HasValue && SelectedAppointmentTime.HasValue)
                {
                    // Create a fresh DateTime with proper date components
                    appointmentDateTime = new DateTime(
                        AppointmentDate.Value.Year,
                        AppointmentDate.Value.Month,
                        AppointmentDate.Value.Day,
                        SelectedAppointmentTime.Value.Hour,
                        SelectedAppointmentTime.Value.Minute,
                        0);
                }
                else
                {
                    // Fallback if something went wrong
                    appointmentDateTime = DateTime.Now;
                }

                Appointment newAppointment = new Appointment
                {
                    PatientId = SelectedPatient.PatientId,
                    StaffId = SelectedDoctor.StaffId,
                    AppointmentDate = appointmentDateTime,  // Use the properly constructed date/time
                    AppointmentTypeId = SelectedAppointmentType.AppointmentTypeId,
                    Status = "Đang chờ",
                    Notes = AppointmentNote,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                // Debug output to verify the correct date/time is being saved
                System.Diagnostics.Debug.WriteLine($"Saving appointment date/time: {newAppointment.AppointmentDate:yyyy-MM-dd HH:mm:ss}");

                // Save to database
                DataProvider.Instance.Context.Appointments.Add(newAppointment);
                DataProvider.Instance.Context.SaveChanges();

                MessageBoxService.ShowSuccess(
                    "Đã tạo lịch hẹn thành công!");

                // Clear form and refresh appointment list
                ClearAppointmentForm();
                LoadAppointments();
            }
            catch (DbUpdateException dbEx)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi lưu lịch hẹn vào cơ sở dữ liệu: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tạo lịch hẹn: {ex.Message}"
                   );
            }
        }

        /// <summary>
        /// Validates patient selection and shows specific error if invalid
        /// </summary>
        private bool ValidatePatientSelection()
        {
            _touchedFields.Add(nameof(PatientSearch));
            _touchedFields.Add(nameof(PatientPhone));

            if (SelectedPatient == null)
            {
                string errorMessage = string.IsNullOrWhiteSpace(PatientSearch) ?
                    "Vui lòng chọn hoặc nhập thông tin bệnh nhân." :
                    "Không tìm thấy bệnh nhân với thông tin đã nhập. Vui lòng kiểm tra lại.";

                MessageBoxService.ShowWarning(
                    errorMessage,
                    "Lỗi - Thông tin bệnh nhân"
              );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates doctor selection and shows specific error if invalid
        /// </summary>
        private bool ValidateStaffselection()
        {
            if (SelectedDoctor == null)
            {
                MessageBoxService.ShowWarning(
                    "Vui lòng chọn bác sĩ khám.",
                    "Lỗi - Thông tin bác sĩ");

                return false;
            }

            return true;
        }
        /// <summary>
        /// Validates appointment type selection and shows specific error if invalid
        /// </summary>
        private bool ValidateAppointmentType()
        {
            if (SelectedAppointmentType == null)
            {
                MessageBoxService.ShowWarning(
                    "Vui lòng chọn loại lịch hẹn.",
                    "Lỗi - Thông tin lịch hẹn");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates date and time selection and shows specific error messages
        /// </summary>
        private bool ValidateDateTimeSelection()
        {
            // Check if date is selected
            if (!AppointmentDate.HasValue)
            {
                MessageBoxService.ShowWarning(
                    "Vui lòng chọn ngày hẹn.",
                    "Lỗi - Ngày hẹn");

                return false;
            }

            // Check if date is in the past
            if (AppointmentDate.Value.Date < DateTime.Today)
            {
                MessageBoxService.ShowWarning(
                    "Ngày hẹn không hợp lệ. Vui lòng chọn ngày hiện tại hoặc trong tương lai.",
                    "Lỗi - Ngày hẹn");

                return false;
            }

            // Check if time is selected
            if (!SelectedAppointmentTime.HasValue)
            {
                 MessageBoxService.ShowError(
                    "Vui lòng chọn giờ hẹn.",
                    "Lỗi - Giờ hẹn"
                    
                    );

                return false;
            }

            // Combine date and time
            DateTime appointmentDateTime = AppointmentDate.Value.Date
                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

            // Check if appointment time is in the past
            if (appointmentDateTime < DateTime.Now)
            {
                 MessageBoxService.ShowError(
                    "Thời gian hẹn đã qua. Vui lòng chọn thời gian trong tương lai.",
                    "Lỗi - Thời gian hẹn"
                    
                    );

                return false;
            }

            // Check if patient already has an appointment at this time (with any doctor)
            if (SelectedPatient != null)
            {
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == SelectedPatient.PatientId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                // Check for exact match first (same hour and minute)
                bool hasExactSameTime = patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute);

                if (hasExactSameTime)
                {
                     MessageBoxService.ShowError(
                        $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn vào lúc " +
                        $"{appointmentDateTime.ToString("HH:mm")}.\n" +
                        $"Vui lòng chọn thời gian khác.",
                        "Lỗi - Trùng lịch"
                        
                        );

                    return false;
                }

                // Check for overlapping appointments within 30 minutes
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                foreach (var existingAppointment in patientAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference > 0)
                    {
                         MessageBoxService.ShowError(
                            $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn khác vào lúc " +
                            $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
                            $"Vui lòng chọn thời gian cách ít nhất 30 phút.",
                            "Lỗi - Trùng lịch"
                            
                            );

                        return false;
                    }
                }
            }


            // Check doctor's schedule
            if (!string.IsNullOrWhiteSpace(SelectedDoctor?.Schedule))
            {
                // Get the day of week for the appointment
                DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                // Parse working hours
                var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                // Check if doctor works on this day
                if (!workingDays.Contains(dayCode))
                {
                     MessageBoxService.ShowError(
                        $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)}).",
                        "Lỗi - Lịch làm việc"
                        
                        );

                    return false;
                }

                // Check if time is within working hours
                TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                if (appointmentTime < startTime || appointmentTime > endTime)
                {
                     MessageBoxService.ShowError(
                        $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName}.\n" +
                        $"Thời gian làm việc: {startTime.ToString("hh\\:mm")} - {endTime.ToString("hh\\:mm")}.",
                        "Lỗi - Giờ làm việc"
                        
                        );

                    return false;
                }

                // Get doctor's appointments for that day
                var doctorAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.StaffId == SelectedDoctor.StaffId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                // Check for exact match first (same hour and minute)
                bool hasExactSameTime = doctorAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute);

                if (hasExactSameTime)
                {
                     MessageBoxService.ShowError(
                        $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
                        $"{appointmentDateTime.ToString("HH:mm")}.\n" +
                        $"Vui lòng chọn thời gian khác.",
                        "Lỗi - Trùng lịch"
                        
                        );

                    return false;
                }

                // Check for close appointments (less than 30 minutes)
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                foreach (var existingAppointment in doctorAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference > 0 && timeDifference % 30 != 0)
                    {
                         MessageBoxService.ShowError(
                            $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
                            $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
                            $"Vui lòng chọn thời gian cách ít nhất 30 phút hoặc đúng khung giờ 30 phút.",
                            "Lỗi - Trùng lịch"
                            
                            );

                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Convert DayOfWeek to Vietnamese day name
        /// </summary>
        private string GetVietnameseDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ hai",
                DayOfWeek.Tuesday => "Thứ ba",
                DayOfWeek.Wednesday => "Thứ tư",
                DayOfWeek.Thursday => "Thứ năm",
                DayOfWeek.Friday => "Thứ sáu",
                DayOfWeek.Saturday => "Thứ bảy",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => string.Empty
            };
        }

        private void ClearAppointmentForm()
        {
            PatientSearch = string.Empty;
            SelectedPatient = null;
            SelectedDoctor = null;
            SelectedAppointmentType = null;
            AppointmentDate = DateTime.Today;
            SelectedAppointmentTime = null;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;

            // Reset validation flags
            _isStaffselected = false;
            _isPatientInfoValid = false;
            _isDateTimeValid = false;

            // Clear touched fields to reset validation
            _touchedFields.Clear();
            _isValidating = false;
        }

        #region Validation Implementation
        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the form or when submitting
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(PatientSearch):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(PatientSearch))
                            error = "Vui lòng nhập tên hoặc mã bảo hiểm của bệnh nhân";
                        break;

                    case nameof(PatientPhone):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(PatientPhone))
                            error = "Vui lòng nhập số điện thoại";
                        else if (!string.IsNullOrWhiteSpace(PatientPhone) &&
                                !Regex.IsMatch(PatientPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                            error = "Số điện thoại không hợp lệ";
                        break;

                    case nameof(AppointmentDate):
                        if (!AppointmentDate.HasValue)
                            error = "Vui lòng chọn ngày hẹn";
                        else if (AppointmentDate.Value < DateTime.Today)
                            error = "Ngày hẹn không hợp lệ";
                        break;

                    case nameof(SelectedAppointmentTime):
                        if (!SelectedAppointmentTime.HasValue)
                            error = "Vui lòng chọn giờ hẹn";
                        else if (_isStaffselected && _isPatientInfoValid &&
                                 AppointmentDate.HasValue && !IsAppointmentTimeValid())
                            error = "Giờ hẹn không phù hợp với lịch làm việc của bác sĩ";
                        break;
                }

                return error;
            }
        }
        #endregion

        #region AppoinmentType Methods
        private void AddAppontmentType()
        {
            try
            {
                // Confirm dialog
                 bool  result =  MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm loaị lịch hẹn '{TypeDisplayName}' không?",
                    "Xác Nhận Thêm"
                     
                  );

                if (!result)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                     MessageBoxService.ShowWarning("Loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newAppointmentType = new AppointmentType
                {
                    TypeName = TypeDisplayName,
                    Description = TypeDescription ?? "",
                    Price = TypePrice ?? 0,
                    IsDeleted = false
                };

                DataProvider.Instance.Context.AppointmentTypes.Add(newAppointmentType);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                ExecuteRefreshType();

                 MessageBoxService.ShowSuccess(
                    "Đã thêm loại lịch hẹn thành công!",
                    "Thành Công"
                    
                     );
            }
            catch (DbUpdateException ex)
            {
                 MessageBoxService.ShowError(
                    $"Không thể thêm loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
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

        private void EditAppontmentType()
        {
            try
            {
                // Confirm dialog
                 bool  result =  MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa loại lịch hẹn '{SelectedAppointmentType.TypeName}' thành '{TypeDisplayName}' không?",
                    "Xác Nhận Sửa"
                     
                  );

                if (!result)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() &&
                              s.AppointmentTypeId != SelectedAppointmentType.AppointmentTypeId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                     MessageBoxService.ShowWarning("Tên loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Update specialty
                var appointmenttypeToUpdate = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToUpdate == null)
                {
                     MessageBoxService.ShowWarning("Không tìm thấy loại lịch hẹn cần sửa.");
                    return;
                }

                appointmenttypeToUpdate.TypeName = TypeDisplayName;
                appointmenttypeToUpdate.Price = TypePrice ?? 0;  
                appointmenttypeToUpdate.Description = TypeDescription ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                // Update doctor list as specialty names may have changed
                LoadAppointmentTypeData();
                ExecuteRefreshType();

                 MessageBoxService.ShowSuccess(
                    "Đã cập nhật loại lịch hẹn thành công!",
                    "Thành Công"
                    
                     );
            }
            catch (DbUpdateException ex)
            {
                 MessageBoxService.ShowError(
                    $"Không thể sửa loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
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

        private void DeleteAppointmentType()
        {
            try
            {

                // Confirm deletion
                 bool  result =  MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa loại lịch hẹn '{SelectedAppointmentType.TypeName}' không?",
                    "Xác Nhận Xóa"
                     
                    );

                if (!result)
                    return;

                // Soft delete the specialty
                var appointmenttypeToDelete = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToDelete == null)
                {
                     MessageBoxService.ShowWarning("Không tìm thấy loại lịch hẹn cần xóa.");
                    return;
                }

                appointmenttypeToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                  ExecuteRefreshType();

                 MessageBoxService.ShowSuccess(
                    "Đã xóa loại lịch hẹn thành công.",
                    "Thành Công"
                    
                     );
            }
            catch (Exception ex)
            {
                 MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa loại lịch hẹn: {ex.Message}",
                    "Lỗi"
                    
                     );
            }
        }
        private void ExecuteRefreshType()
        {
            SelectedAppointmentType = null;
            TypeDisplayName = string.Empty;
            TypeDescription = string.Empty;
            TypePrice = null;
        }
        #endregion
    }
}
