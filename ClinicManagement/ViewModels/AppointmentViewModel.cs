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
        #endregion

        #region Appointment Form Properties
        // Patient search
        private string _patientSearch;
        public string PatientSearch
        {
            get => _patientSearch;
            set
            {
                _patientSearch = value;
                OnPropertyChanged();
                SearchPatients();
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
                // Reset appointment date and time when patient changes
                ValidateFormSequence();
            }
        }

        // Doctor selection
        private ObservableCollection<Doctor> _doctorList;
        public ObservableCollection<Doctor> DoctorList
        {
            get => _doctorList;
            set
            {
                _doctorList = value;
                OnPropertyChanged();
            }
        }

        private Doctor? _selectedDoctor;
        public Doctor? SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
                // Reset appointment date and time when doctor changes
                if (value != null)
                {
                    ValidateFormSequence();
                }
                else
                {
                    AppointmentDate = null;
                    SelectedAppointmentTime = null;
                }
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

        // Appointment Form Commands
        public ICommand CancelCommand { get; set; }
        public ICommand AddAppointmentCommand { get; set; }
        public ICommand SelectPatientCommand { get; set; }
        public ICommand FindPatientCommand { get; set; }

        // Appointment List Commands
        public ICommand SearchCommand { get; set; }
        public ICommand SearchAppointmentsCommand { get; set; }
        public ICommand OpenAppointmentDetailsCommand { get; set; }
        #endregion

        #region Validation
  
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        // Validation sequence flags
        private bool _isDoctorSelected = false;
        private bool _isPatientInfoValid = false;
        private bool _isDateTimeValid = false;
        #endregion

        #endregion

     
           public AppointmentViewModel()
        {
            _filterDate = DateTime.Today;
            _patientSearch = string.Empty;
            _patientPhone = string.Empty;
            _appointmentNote = string.Empty;
            _searchText = string.Empty;

            // Initialize collections
            _ListAppointmentType = new ObservableCollection<AppointmentType>();
            _searchedPatients = new ObservableCollection<Patient>();
            _doctorList = new ObservableCollection<Doctor>();
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
            OpenAppointmentDetailsCommand = new RelayCommand<Appointment>(
               (p) => OpenAppointmentDetails(),
               (p) => true
               );
        }

        private void OpenAppointmentDetails()
        {
          
            var detailsWindow = new AppointmentDetailsWindow();
            detailsWindow.ShowDialog();
            LoadAppointments();


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
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tìm kiếm bệnh nhân: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FindOrCreatePatient()
        {
            try
            {
                // Validate input data before proceeding
                if (string.IsNullOrWhiteSpace(PatientSearch))
                {
                    MessageBox.Show(
                        "Vui lòng nhập tên hoặc mã bảo hiểm của bệnh nhân.",
                        "Thiếu thông tin",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PatientPhone))
                {
                    MessageBox.Show(
                        "Vui lòng nhập số điện thoại của bệnh nhân.",
                        "Thiếu thông tin",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validate phone number format
                if (!Regex.IsMatch(PatientPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                {
                    MessageBox.Show(
                        "Số điện thoại không đúng định dạng. Vui lòng nhập số điện thoại hợp lệ (VD: 0901234567).",
                        "Số điện thoại không hợp lệ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // First, try to find an existing patient
                var patient = FindPatient();

                if (patient != null)
                {
                    // Patient found, set as selected
                    SelectedPatient = patient;
                    MessageBox.Show(
                        $"Đã tìm thấy bệnh nhân: {patient.FullName}",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // If no patient found, ask if user wants to create a new one
                var result = MessageBox.Show(
                    "Không tìm thấy bệnh nhân với thông tin đã nhập. Bạn có muốn tạo mới không?",
                    "Tạo bệnh nhân mới?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Determine if PatientSearch is a name or insurance code
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
                            MessageBox.Show(
                                "Không thể tạo bệnh nhân mới vì thiếu họ tên.",
                                "Thiếu thông tin",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return; // User cancelled or provided empty name
                        }

                        name = nameResult.Trim();
                        insuranceCode = PatientSearch.Trim();
                    }

                    // Additional validation for name
                    if (name.Length < 2)
                    {
                        MessageBox.Show(
                            "Tên bệnh nhân phải có ít nhất 2 ký tự.",
                            "Tên không hợp lệ",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
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
                        MessageBox.Show(
                            $"Đã tồn tại bệnh nhân '{existingPatient.FullName}' với số điện thoại này.",
                            "Bệnh nhân đã tồn tại",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        // Set the existing patient as selected
                        SelectedPatient = existingPatient;
                        return;
                    }

                    // Save new patient to database
                    DataProvider.Instance.Context.Patients.Add(newPatient);
                    DataProvider.Instance.Context.SaveChanges();

                    // Set as selected patient
                    SelectedPatient = newPatient;

                    MessageBox.Show(
                        $"Đã tạo bệnh nhân mới: {newPatient.FullName}",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu thông tin bệnh nhân: {dbEx.InnerException?.Message ?? dbEx.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tìm kiếm hoặc tạo bệnh nhân: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Finds a patient based on PatientSearch (name or insurance code) and Phone
        /// </summary>
        private Patient? FindPatient()
        {
            if (string.IsNullOrWhiteSpace(PatientSearch) || string.IsNullOrWhiteSpace(PatientPhone))
                return null;

            // First try to find by exact match of insurance code + phone
            var patient = DataProvider.Instance.Context.Patients
                .FirstOrDefault(p =>
                    p.IsDeleted != true &&
                    p.Phone == PatientPhone.Trim() &&
                    p.InsuranceCode == PatientSearch.Trim());

            if (patient != null)
                return patient;

            // Then try by name + phone
            patient = DataProvider.Instance.Context.Patients
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

            // Load doctors for dropdown
            LoadDoctors();

            // Initialize other properties
            PatientSearch = string.Empty;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
            SearchText = string.Empty;

            // Load appointments for today
            LoadAppointments();
        }

        private void LoadDoctors()
        {
            try
            {
                var doctors = DataProvider.Instance.Context.Doctors
                    .Where(d => d.IsDeleted != true)
                    .OrderBy(d => d.FullName)
                    .ToList();

                DoctorList = new ObservableCollection<Doctor>(doctors);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                DoctorList = new ObservableCollection<Doctor>();
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
                    .Include(a => a.Doctor) // Include Doctor information
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
                        (a.Doctor.FullName != null && a.Doctor.FullName.ToLower().Contains(searchLower))
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
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Ensure AppointmentsDisplay is not null when there's an error
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
            }
        }

        private void ValidateFormSequence()
        {
            _isDoctorSelected = SelectedDoctor != null;
            _isPatientInfoValid = ValidatePatientInfo();

            // Only if doctor and patient are selected, we'll check date/time
            if (_isDoctorSelected && _isPatientInfoValid)
            {
                AppointmentDate = DateTime.Today;
            }
            else
            {
                AppointmentDate = null;
                SelectedAppointmentTime = null;
            }
        }

        private bool ValidatePatientInfo()
        {
            bool isValid = false;

            // Case 1: Selected patient from dropdown
            if (SelectedPatient != null)
            {
                isValid = true;
            }
            // Case 2: Manual input (insurance code + phone)
            else if (!string.IsNullOrWhiteSpace(PatientSearch) && !string.IsNullOrWhiteSpace(PatientPhone))
            {
                // Search for patient with matching insurance code or phone number
                var patient = DataProvider.Instance.Context.Patients
                    .FirstOrDefault(p =>
                        (p.InsuranceCode == PatientSearch.Trim() || p.FullName == PatientSearch.Trim()) &&
                        p.Phone == PatientPhone.Trim() &&
                        p.IsDeleted != true);

                if (patient != null)
                {
                    SelectedPatient = patient;
                    isValid = true;
                }
                else
                {
                    // Add validation error
                    _touchedFields.Add(nameof(PatientSearch));
                    _touchedFields.Add(nameof(PatientPhone));
                    isValid = false;
                }
            }

            return isValid;
        }

        private void SearchAppointments()
        {
            // Simply call LoadAppointments which already handles search filtering
            LoadAppointments();
        }

        private bool CanAddAppointment()
        {
            // Validate form sequence (doctor -> patient -> date -> time)
            return _isDoctorSelected &&
                   _isPatientInfoValid &&
                   AppointmentDate.HasValue &&
                   SelectedAppointmentTime.HasValue &&
                   SelectedAppointmentType != null &&
                   IsAppointmentTimeValid();
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

                // Check for overlapping appointments
                var appointmentDate = appointmentDateTime.Date;

                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                // Get doctor's appointments for that day
                var doctorAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.DoctorId == SelectedDoctor.DoctorId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                // Check if any appointment is within 30 minutes of the requested time
                foreach (var existingAppointment in doctorAppointments)
                {
                    // Calculate time difference in minutes
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30) // Within 30 minutes
                    {
                        MessageBox.Show(
                            $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
                            $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
                            $"Vui lòng chọn thời gian cách ít nhất 30 phút.",
                            "Lỗi - Trùng lịch",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return false;
                    }
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

        private void AddNewAppointment()
        {
            try
            {
                // Enable validation for required fields
                _isValidating = true;

                // Check each validation requirement separately and show specific errors
                if (!ValidatePatientSelection())
                    return;

                if (!ValidateDoctorSelection())
                    return;

                if (!ValidateAppointmentType())
                    return;

                if (!ValidateDateTimeSelection())
                    return;

                // All validations passed, confirm creation
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có muốn tạo lịch hẹn cho bệnh nhân {SelectedPatient.FullName} với bác sĩ {SelectedDoctor.FullName} vào {AppointmentDate?.ToString("dd/MM/yyyy")} lúc {SelectedAppointmentTime?.ToString("HH:mm")} không?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Create appointment
                DateTime appointmentDateTime = AppointmentDate!.Value.Date
                    .Add(new TimeSpan(SelectedAppointmentTime!.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

                Appointment newAppointment = new Appointment
                {
                    PatientId = SelectedPatient.PatientId,
                    DoctorId = SelectedDoctor.DoctorId,
                    AppointmentDate = appointmentDateTime,
                    AppointmentTypeId = SelectedAppointmentType.AppointmentTypeId,
                    Status = "Đang chờ",
                    Notes = AppointmentNote,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                // Save to database
                DataProvider.Instance.Context.Appointments.Add(newAppointment);
                DataProvider.Instance.Context.SaveChanges();

                MessageBox.Show(
                    "Đã tạo lịch hẹn thành công!",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Clear form and refresh appointment list
                ClearAppointmentForm();
                LoadAppointments();
            }
            catch (DbUpdateException dbEx)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu lịch hẹn vào cơ sở dữ liệu: {dbEx.InnerException?.Message ?? dbEx.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tạo lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

                MessageBox.Show(
                    errorMessage,
                    "Lỗi - Thông tin bệnh nhân",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates doctor selection and shows specific error if invalid
        /// </summary>
        private bool ValidateDoctorSelection()
        {
            if (SelectedDoctor == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn bác sĩ khám.",
                    "Lỗi - Thông tin bác sĩ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

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
                MessageBox.Show(
                    "Vui lòng chọn loại lịch hẹn.",
                    "Lỗi - Thông tin lịch hẹn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

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
                MessageBox.Show(
                    "Vui lòng chọn ngày hẹn.",
                    "Lỗi - Ngày hẹn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            // Check if date is in the past
            if (AppointmentDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show(
                    "Ngày hẹn không hợp lệ. Vui lòng chọn ngày hiện tại hoặc trong tương lai.",
                    "Lỗi - Ngày hẹn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            // Check if time is selected
            if (!SelectedAppointmentTime.HasValue)
            {
                MessageBox.Show(
                    "Vui lòng chọn giờ hẹn.",
                    "Lỗi - Giờ hẹn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            // Combine date and time
            DateTime appointmentDateTime = AppointmentDate.Value.Date
                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

            // Check if appointment time is in the past
            if (appointmentDateTime < DateTime.Now)
            {
                MessageBox.Show(
                    "Thời gian hẹn đã qua. Vui lòng chọn thời gian trong tương lai.",
                    "Lỗi - Thời gian hẹn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            // Check doctor's schedule
            if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
            {
                // Get the day of week for the appointment
                DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                // Parse working hours
                var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                // Check if doctor works on this day
                if (!workingDays.Contains(dayCode))
                {
                    MessageBox.Show(
                        $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)}).",
                        "Lỗi - Lịch làm việc",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                // Check if time is within working hours
                TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                if (appointmentTime < startTime || appointmentTime > endTime)
                {
                    MessageBox.Show(
                        $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName}.\n" +
                        $"Thời gian làm việc: {startTime.Hours}h - {endTime.Hours}h.",
                        "Lỗi - Giờ làm việc",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
                }

                // Check for overlapping appointments
                bool hasOverlappingAppointment = DataProvider.Instance.Context.Appointments
                    .Any(a =>
                        a.DoctorId == SelectedDoctor.DoctorId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date);
                     

                if (hasOverlappingAppointment)
                {
                    MessageBox.Show(
                        $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn khác vào thời gian này.\n" +
                        "Vui lòng chọn thời gian khác.",
                        "Lỗi - Trùng lịch",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return false;
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

        private bool ValidateForm()
        {
            _touchedFields.Add(nameof(PatientSearch));
            _touchedFields.Add(nameof(PatientPhone));

            // Update validation flags
            _isDoctorSelected = SelectedDoctor != null;
            _isPatientInfoValid = ValidatePatientInfo();
            _isDateTimeValid = AppointmentDate.HasValue &&
                             SelectedAppointmentTime.HasValue &&
                             IsAppointmentTimeValid();

            return _isDoctorSelected &&
                   _isPatientInfoValid &&
                   _isDateTimeValid &&
                   SelectedAppointmentType != null;
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
            _isDoctorSelected = false;
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
                        else if (_isDoctorSelected && _isPatientInfoValid &&
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
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm loaị lịch hẹn '{TypeDisplayName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newAppointmentType = new AppointmentType
                {
                    TypeName = TypeDisplayName,
                    Description = TypeDescription ?? "",
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

                // Clear fields
                TypeDisplayName = "";
                TypeDescription = "";

                MessageBox.Show(
                    "Đã thêm loại lịch hẹn thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể thêm loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditAppontmentType()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa loại lịch hẹn '{SelectedAppointmentType.TypeName}' thành '{TypeDisplayName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() &&
                              s.AppointmentTypeId != SelectedAppointmentType.AppointmentTypeId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Tên loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Update specialty
                var appointmenttypeToUpdate = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy loại lịch hẹn cần sửa.");
                    return;
                }

                appointmenttypeToUpdate.TypeName = TypeDisplayName;
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

                MessageBox.Show(
                    "Đã cập nhật loại lịch hẹn thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteAppointmentType()
        {
            try
            {

                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa loại lịch hẹn '{SelectedAppointmentType.TypeName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the specialty
                var appointmenttypeToDelete = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToDelete == null)
                {
                    MessageBox.Show("Không tìm thấy loại lịch hẹn cần xóa.");
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

                // Clear selection and fields
                SelectedAppointmentType = null;
                TypeDisplayName = "";
                TypeDescription = "";

                MessageBox.Show(
                    "Đã xóa loại lịch hẹn thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa loại lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
