using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{

    public class StatusItem
    {
        public string Status { get; set; }
        public string DisplayName { get; set; }

        public StatusItem(string status, string displayName)
        {
            Status = status;
            DisplayName = displayName;
        }
    }
    public class PatientDetailsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
       
        #region Properties
        private Window _window;

        private Patient _patient;
        public Patient Patient
        {
            get => _patient;
            set
            {
                _patient = value;
                OnPropertyChanged();
                LoadRelatedData();

            }
        }

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

        // Properties for invoice filtering
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
        // Add this to your PatientDetailsWindowViewModel.cs
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
        // Patient ID - Read Only
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

        // Full Name
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

        // Date of Birth
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

        // Insurance Code
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
        // Gender
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

        // Type Name
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

        // Discount
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

        // Address
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


        // Selected Patient Type
        private PatientType _selectedPatientType;
        public PatientType SelectedPatientType
        {
            get => _selectedPatientType;
            set
            {
                _selectedPatientType = value;
                OnPropertyChanged();
                // Update related properties when selection changes
                if (SelectedPatientType != null)
                {
                    PatientTypeId = SelectedPatientType.PatientTypeId;
                    TypeName = SelectedPatientType.TypeName;
                    Discount = SelectedPatientType.Discount.ToString();
                }
            }
        }

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

        private StatusItem _selectedInvoiceStatus;
        public StatusItem SelectedInvoiceStatus
        {
            get => _selectedInvoiceStatus;
            set
            {
                _selectedInvoiceStatus = value;
                OnPropertyChanged();
                FilterInvoices();
            }
        }

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

        private StatusItem _selectedAppointmentStatus;
        public StatusItem SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
                FilterAppointments();  // Auto-filter when status changes
            }
        }

        // For validation
        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;
        #endregion

        #region Commands
        public ICommand UpdatePatientCommand { get; set; }
        public ICommand DeletePatientCommand { get; set; }
        public ICommand FilterRecordsCommand { get; set; }
        public ICommand FilterInvoicesCommand { get; set; }
        public ICommand ViewPrescriptionCommand { get; set; }
        public ICommand ViewTestResultsCommand { get; set; }
        public ICommand OpenRecordCommand { get; set; }
        public ICommand ViewInvoiceCommand { get; set; }
        public ICommand PrintInvoiceCommand { get; set; }
        public ICommand AddAppointmentCommand { get; set; }
        public ICommand EditAppointmentCommand { get; set; }
        public ICommand CancelAppointmentCommand { get; set; }
        public ICommand LoadedWindowCommand { get; set; }
        #endregion

        public PatientDetailsWindowViewModel()
        {
            InitializeCommands();
         
            InitializeGenderOptions();
        }

        private void InitializeGenderOptions()
        {
            GenderOptions = new ObservableCollection<string> { "Nam", "Nữ", "Khác" };
        }


        private void InitializeCommands()
        {
            LoadedWindowCommand = new RelayCommand<Window>(
                (p) => { _window = p; LoadWindow(p); },
                (p) => true
            );

            UpdatePatientCommand = new RelayCommand<object>(
                (p) => UpdatePatient(),
                (p) => CanUpdatePatient()
            );

            DeletePatientCommand = new RelayCommand<object>(
                (p) => DeletePatient(),
                (p) => CanDeletePatient()
            );

            FilterRecordsCommand = new RelayCommand<object>(
                (p) => FilterMedicalRecords(),
                (p) => true
            );

            FilterInvoicesCommand = new RelayCommand<object>(
                (p) => FilterInvoices(),
                (p) => true
            );

            ViewPrescriptionCommand = new RelayCommand<MedicalRecord>(
                (record) => ViewPrescription(record),
                (record) => record != null && !string.IsNullOrEmpty(record.Prescription)
            );

            ViewTestResultsCommand = new RelayCommand<MedicalRecord>(
                (record) => ViewTestResults(record),
                (record) => record != null && !string.IsNullOrEmpty(record.TestResults)
            );

            OpenRecordCommand = new RelayCommand<MedicalRecord>(
                (record) => OpenMedicalRecord(record),
                (record) => record != null
            );

            ViewInvoiceCommand = new RelayCommand<Invoice>(
                (invoice) => ViewInvoice(invoice),
                (invoice) => invoice != null
            );

            PrintInvoiceCommand = new RelayCommand<Invoice>(
                (invoice) => PrintInvoice(invoice),
                (invoice) => invoice != null
            );

            AddAppointmentCommand = new RelayCommand<object>(
                (p) => AddNewAppointment(),
                (p) => Patient != null
            );

            EditAppointmentCommand = new RelayCommand<Appointment>(
                (appointment) => EditExistingAppointment(appointment),
                (appointment) => appointment != null && appointment.Status != "Completed" && appointment.Status != "Canceled"
            );

            CancelAppointmentCommand = new RelayCommand<Appointment>(
                (appointment) => CancelExistingAppointment(appointment),
                (appointment) => appointment != null && appointment.Status != "Completed" && appointment.Status != "Canceled"
            );
        }

        #region Validation
        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the form or when submitting
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

        private bool ValidatePatient()
        {
            if (Patient == null)
                return false;

            _isValidating = true;
            _touchedFields.Add(nameof(Patient.FullName));
            _touchedFields.Add(nameof(Patient.Phone));
            _touchedFields.Add(nameof(Patient.DateOfBirth));
            _touchedFields.Add(nameof(Patient.InsuranceCode));

            string fullNameError = this[nameof(Patient.FullName)];
            string phoneError = this[nameof(Patient.Phone)];
            string dateOfBirthError = this[nameof(Patient.DateOfBirth)];
            string insuranceCodeError = this[nameof(Patient.InsuranceCode)];

            if (!string.IsNullOrEmpty(fullNameError))
            {
                MessageBoxService.ShowError(fullNameError, "Lỗi dữ liệu"     );
                return false;
            }

            if (!string.IsNullOrEmpty(phoneError))
            {
                MessageBoxService.ShowError(phoneError, "Lỗi dữ liệu"     );
                return false;
            }

            if (!string.IsNullOrEmpty(dateOfBirthError))
            {
                MessageBoxService.ShowError(dateOfBirthError, "Lỗi dữ liệu"     );
                return false;
            }

            if (!string.IsNullOrEmpty(insuranceCodeError))
            {
                MessageBoxService.ShowError(insuranceCodeError, "Lỗi dữ liệu"     );
                return false;
            }

            return true;
        }
        #endregion

        private void LoadWindow(Window window)
        {
            if (window != null && Patient != null)
            {
                window.Title = $"Thông tin chi tiết - {Patient?.FullName}";
            }
        }

        private void LoadRelatedData()
        {
            if (Patient == null) return;

            // Load patient information
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

            // Load patient types
            PatientTypes = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => pt.IsDeleted != true)
                .OrderBy(pt => pt.TypeName)
                .ToList()
            );

            // Set selected patient type
            SelectedPatientType = PatientTypes.FirstOrDefault(pt => pt.PatientTypeId == Patient.PatientTypeId);

            // Load gender options
            GenderOptions = new ObservableCollection<string> { "Nam", "Nữ", "Khác" };

     


            // Load related collections
            LoadMedicalRecords();
            LoadInvoices();
            LoadAppointments();
        }

        private void LoadMedicalRecords()
        {
            MedicalRecords = new ObservableCollection<MedicalRecord>(
                DataProvider.Instance.Context.MedicalRecords
                .Include(m => m.Doctor)
                .Where(m => m.PatientId == PatientId && m.IsDeleted != true)
                .OrderByDescending(m => m.RecordDate)
                .ToList()
            );
        }

        private void LoadInvoices()
        {
            Invoices = new ObservableCollection<Invoice>(
                DataProvider.Instance.Context.Invoices
                .Where(i => i.PatientId == PatientId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToList()
                .Select(i => {
                    // Ensure InvoiceType is never null and trim any whitespace
                    if (i.InvoiceType == null)
                    {
                        i.InvoiceType = "Unknown";
                    }
                    else
                    {
                        i.InvoiceType = i.InvoiceType.Trim();
                    }

                    // Also trim Status if present
                    if (i.Status != null)
                    {
                        i.Status = i.Status.Trim();
                    }

                    return i;
                })
            );
        }



        private void LoadAppointments()
        {
            Appointments = new ObservableCollection<Appointment>(
                DataProvider.Instance.Context.Appointments
                .Include(a => a.Staff)
                .Where(a => a.PatientId == PatientId && a.IsDeleted != true)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList()
                .Select(a => {
                    // Trim Status if present
                    if (a.Status != null)
                    {
                        a.Status = a.Status.Trim();
                    }
                    return a;
                })
            );

            // Create status items with trimmed values
            AppointmentStatusList = new ObservableCollection<StatusItem>
    {
        new StatusItem("", "Tất cả"),
        new StatusItem("Đang chờ", "Đang chờ"),
        new StatusItem("Đang khám", "Đang khám"),
        new StatusItem("Đã khám", "Đã khám"),
        new StatusItem("Đã hủy", "Đã hủy")
    }; 

            // Create invoice status items with trimmed values
            InvoiceStatusList = new ObservableCollection<StatusItem>
    {
        new StatusItem("", "Tất cả"),
        new StatusItem("Đã thanh toán", "Đã thanh toán"),
        new StatusItem("Chưa thanh toán", "Chưa thanh toán")
    };
        }

        // Loading indicator property
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


        private bool CanUpdatePatient()
        {
            return Patient != null && !string.IsNullOrWhiteSpace(Patient.FullName);
        }

        private bool CanDeletePatient()
        {
            if (Patient == null)
                return false;

            // Check if patient has any active appointments
            bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                .Any(a => a.PatientId == Patient.PatientId &&
                      (a.Status == "Đang chờ" || a.Status == "Đã khám" || a.Status == "Đã hủy") &&
                      a.IsDeleted != true);

            return !hasActiveAppointments;
        }

        private void UpdatePatient()
        {
            try
            {
                // Validate patient data
                if (!ValidatePatient())
                    return;

                // Find the patient in database
                var patientToUpdate = DataProvider.Instance.Context.Patients
                    .FirstOrDefault(p => p.PatientId == PatientId);

                if (patientToUpdate == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bệnh nhân!", "Lỗi"    );
                    return;
                }

                // Update patient information
                patientToUpdate.FullName = FullName?.Trim();
                patientToUpdate.DateOfBirth = DateOfBirth.HasValue ? DateOnly.FromDateTime(DateOfBirth.Value) : null;
                patientToUpdate.PatientTypeId = PatientTypeId;
                patientToUpdate.Gender = Gender;
                patientToUpdate.Phone = Phone?.Trim();
                patientToUpdate.Address = Address?.Trim();
                patientToUpdate.InsuranceCode = InsuranceCode?.Trim();

                // Save changes to database
                DataProvider.Instance.Context.SaveChanges();

                // Refresh the current data from database
                var refreshedPatient = DataProvider.Instance.Context.Patients
                    .Include(p => p.PatientType)
                    .FirstOrDefault(p => p.PatientId == PatientId);


                MessageBoxService.ShowSuccess("Thông tin bệnh nhân đã được cập nhật thành công!",
                               "Thông báo"
                                
                                 );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi cập nhật thông tin: {ex.Message}",
                               "Lỗi"
                                
                                );
            }
        }

        private void DeletePatient()
        {
            try
            {
                // Check if patient has active appointments
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.PatientId == Patient.PatientId &&
                          (a.Status == "Đang chờ" || a.Status == "Đã khám" || a.Status == "Đã hủy") &&
                          a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa bệnh nhân này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng hủy tất cả lịch hẹn trước khi xóa bệnh nhân.",
                        "Cánh báo"
                         
                          );
                    return;
                }

                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc chắn muốn xóa bệnh nhân {Patient.FullName} không?",
                    "Xác nhận xóa"
                     
                      );

                if (result)
                {
                    var patientToDelete = DataProvider.Instance.Context.Patients
                        .FirstOrDefault(p => p.PatientId == Patient.PatientId);

                    if (patientToDelete != null)
                    {
                        // Soft delete
                        patientToDelete.IsDeleted = true;
                        DataProvider.Instance.Context.SaveChanges();

                        MessageBoxService.ShowSuccess("Đã xóa bệnh nhân thành công!", "Thông báo"     );

                        // Close the window after deletion
                        _window?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xóa bệnh nhân: {ex.Message}", "Lỗi"    );
            }
        }

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

                // Apply date range filter
                if (StartDate != DateTime.MinValue && EndDate != DateTime.MinValue)
                {
                    // Add one day to EndDate to include the entire end day
                    var endDatePlus = EndDate.AddDays(1);
                    query = query.Where(m => m.RecordDate >= StartDate && m.RecordDate < endDatePlus);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTerm = SearchTerm.ToLower().Trim();
                    query = query.Where(m =>
                        (m.Diagnosis != null && m.Diagnosis.ToLower().Contains(searchTerm)) ||
                        (m.Doctor.FullName != null && m.Doctor.FullName.ToLower().Contains(searchTerm))
                    );
                }

                // Order by date descending
                var filteredRecords = query.OrderByDescending(m => m.RecordDate).ToList();

                // Update UI
                MedicalRecords = new ObservableCollection<MedicalRecord>(filteredRecords);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc hồ sơ bệnh án: {ex.Message}", "Lỗi"    );
            }
        }

        private void FilterInvoices()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Invoices
                    .Where(i => i.PatientId == PatientId);

                // Apply date range filter
                if (InvoiceStartDate != DateTime.MinValue && InvoiceEndDate != DateTime.MinValue)
                {
                    // Add one day to EndDate to include the entire end day
                    var endDatePlus = InvoiceEndDate.AddDays(1);
                    query = query.Where(i => i.InvoiceDate >= InvoiceStartDate && i.InvoiceDate < endDatePlus);
                }

                // Filter by status - Add trim comparison
                if (SelectedInvoiceStatus != null && !string.IsNullOrEmpty(SelectedInvoiceStatus.Status))
                {
                    var statusToFilter = SelectedInvoiceStatus.Status.Trim();
                    // Use EF's string comparison methods
                    query = query.Where(i => i.Status.Trim() == statusToFilter);
                }

                // Order by date descending
                var filteredInvoices = query.OrderByDescending(i => i.InvoiceDate).ToList();

                // Apply additional processing and trimming
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
                MessageBoxService.ShowError($"Lỗi khi lọc hóa đơn: {ex.Message}", "Lỗi"    );
            }
        }




        private void FilterAppointments()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Staff)
                    .Where(a => a.PatientId == PatientId && a.IsDeleted != true);

                // Filter by status with trimming
                if (SelectedAppointmentStatus != null && !string.IsNullOrEmpty(SelectedAppointmentStatus.Status))
                {
                    var statusToFilter = SelectedAppointmentStatus.Status.Trim();
                    query = query.Where(a => a.Status.Trim() == statusToFilter);
                }

                // Order by date
                var filteredAppointments = query.OrderByDescending(a => a.AppointmentDate).ToList();

                // Process and trim Status values
                Appointments = new ObservableCollection<Appointment>(
                    filteredAppointments.Select(a => {
                        if (a.Status != null) a.Status = a.Status.Trim();
                        return a;
                    })
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc lịch hẹn: {ex.Message}", "Lỗi"    );
            }
        }

        private void ViewPrescription(MedicalRecord record)
        {
            MessageBoxService.ShowInfo(record.Prescription, "Đơn thuốc"     );
        }

        private void ViewTestResults(MedicalRecord record)
        {
            MessageBoxService.ShowInfo(record.TestResults, "Kết quả xét nghiệm"    );
        }

        private void OpenMedicalRecord(MedicalRecord record)
        {
            if (record == null) return;

            // First create the window
            var medicalRecordWindow = new MedicalRecorDetailsWindow();

            // Create the view model with the record
            var viewModel = new MedicalRecordDetailsViewModel(record);

            // Set the DataContext
            medicalRecordWindow.DataContext = viewModel;

            // Show dialog
            medicalRecordWindow.ShowDialog();
        }



        private void ViewInvoice(Invoice invoice)
        {
            // Open invoice details window
            // You would need to implement this window
            MessageBoxService.ShowWarning("Chức năng xem hóa đơn chi tiết đang được phát triển.", "Thông báo"     );
        }

        private void PrintInvoice(Invoice invoice)
        {
            // Implement printing functionality
            MessageBoxService.ShowWarning("Chức năng in hóa đơn đang được phát triển.", "Thông báo"     );
        }

        private void AddNewAppointment()
        {
            // Open add appointment window
            // You would need to implement this window
            MessageBoxService.ShowWarning("Chức năng thêm lịch hẹn mới đang được phát triển.", "Thông báo"     );
        }

        private void EditExistingAppointment(Appointment appointment)
        {
            // Open edit appointment window
            // You would need to implement this window
            MessageBoxService.ShowWarning("Chức năng sửa lịch hẹn đang được phát triển.", "Thông báo"     );
        }

        private void CancelExistingAppointment(Appointment appointment)
        {
            try
            {
                 bool  result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy"
                     
                    );

                if (result)
                {
                    var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                        .FirstOrDefault(a => a.AppointmentId == appointment.AppointmentId);

                    if (appointmentToUpdate != null)
                    {
                        appointmentToUpdate.Status = "Canceled";
                        DataProvider.Instance.Context.SaveChanges();

                        // Refresh appointments list
                        FilterAppointments();

                        MessageBoxService.ShowSuccess("Đã hủy lịch hẹn thành công!", "Thông báo"     );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi hủy lịch hẹn: {ex.Message}", "Lỗi"    );
            }

        }

     
    
    }
   
}