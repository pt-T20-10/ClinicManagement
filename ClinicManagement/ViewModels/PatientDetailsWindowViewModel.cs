using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
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

        private List<string> _genderOptions;
        public List<string> GenderOptions
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
            InitializeStatusLists();
            InitializeGenderOptions();
        }

        private void InitializeGenderOptions()
        {
            GenderOptions = new List<string> { "Nam", "Nữ", "Khác" };
        }

        private void InitializeStatusLists()
        {
            // Initialize invoice status list
            InvoiceStatusList = new ObservableCollection<StatusItem>
            {
                new StatusItem { Status = "All", DisplayName = "Tất cả" },
                new StatusItem { Status = "Paid", DisplayName = "Đã thanh toán" },
                new StatusItem { Status = "Pending", DisplayName = "Chờ thanh toán" },
                new StatusItem { Status = "Canceled", DisplayName = "Đã hủy" }
            };
            SelectedInvoiceStatus = InvoiceStatusList[0];

            // Initialize appointment status list
            AppointmentStatusList = new ObservableCollection<StatusItem>
            {
                new StatusItem { Status = "All", DisplayName = "Tất cả" },
                new StatusItem { Status = "Scheduled", DisplayName = "Đã hẹn" },
                new StatusItem { Status = "Completed", DisplayName = "Hoàn thành" },
                new StatusItem { Status = "Canceled", DisplayName = "Đã hủy" },
                new StatusItem { Status = "Missed", DisplayName = "Bỏ lỡ" }
            };
            SelectedAppointmentStatus = AppointmentStatusList[0];
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

                    case nameof(Patient.InsuranceCode):
                        if (!string.IsNullOrWhiteSpace(Patient.InsuranceCode) &&
                            !Regex.IsMatch(Patient.InsuranceCode, @"^\d{10}$"))
                        {
                            error = "Mã bảo hiểm phải có 10 chữ số";
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
                MessageBox.Show(fullNameError, "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrEmpty(phoneError))
            {
                MessageBox.Show(phoneError, "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrEmpty(dateOfBirthError))
            {
                MessageBox.Show(dateOfBirthError, "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrEmpty(insuranceCodeError))
            {
                MessageBox.Show(insuranceCodeError, "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (Patient == null)
                return;

            try
            {
                // Load medical records
                MedicalRecords = new ObservableCollection<MedicalRecord>(
                    DataProvider.Instance.Context.MedicalRecords
                        .Include(m => m.Doctor)
                        .Where(m => m.PatientId == Patient.PatientId && m.IsDeleted != true)
                        .OrderByDescending(m => m.RecordDate)
                        .ToList()
                );

                // Load invoices
                Invoices = new ObservableCollection<Invoice>(
                    DataProvider.Instance.Context.Invoices
                        .Where(i => i.PatientId == Patient.PatientId)
                        .OrderByDescending(i => i.InvoiceDate)
                        .ToList()
                );

                // Load appointments
                Appointments = new ObservableCollection<Appointment>(
                    DataProvider.Instance.Context.Appointments
                        .Include(a => a.Doctor)
                        .Where(a => a.PatientId == Patient.PatientId && a.IsDeleted != true)
                        .OrderByDescending(a => a.AppointmentDate)
                        .ToList()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                      (a.Status == "Scheduled" || a.Status == "Confirmed" || a.Status == "InProgress") &&
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
                    .FirstOrDefault(p => p.PatientId == Patient.PatientId);

                if (patientToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin bệnh nhân!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update patient information
                patientToUpdate.FullName = Patient.FullName?.Trim();
                patientToUpdate.DateOfBirth = Patient.DateOfBirth;
                patientToUpdate.Gender = Patient.Gender;
                patientToUpdate.Phone = Patient.Phone?.Trim();
                patientToUpdate.Address = Patient.Address?.Trim();
                patientToUpdate.InsuranceCode = Patient.InsuranceCode?.Trim();

                // Save changes
                DataProvider.Instance.Context.SaveChanges();

                // Refresh patient data from database
                var refreshedPatient = DataProvider.Instance.Context.Patients
                    .Include(p => p.PatientType)
                    .FirstOrDefault(p => p.PatientId == Patient.PatientId);

                if (refreshedPatient != null)
                {
                    Patient = refreshedPatient;
                }

                MessageBox.Show("Thông tin bệnh nhân đã được cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật thông tin: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePatient()
        {
            try
            {
                // Check if patient has active appointments
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.PatientId == Patient.PatientId &&
                          (a.Status == "Scheduled" || a.Status == "Confirmed" || a.Status == "InProgress") &&
                          a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBox.Show(
                        "Không thể xóa bệnh nhân này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng hủy tất cả lịch hẹn trước khi xóa bệnh nhân.",
                        "Cảnh báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa bệnh nhân {Patient.FullName} không?",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var patientToDelete = DataProvider.Instance.Context.Patients
                        .FirstOrDefault(p => p.PatientId == Patient.PatientId);

                    if (patientToDelete != null)
                    {
                        // Soft delete
                        patientToDelete.IsDeleted = true;
                        DataProvider.Instance.Context.SaveChanges();

                        MessageBox.Show("Đã xóa bệnh nhân thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Close the window after deletion
                        _window?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa bệnh nhân: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi lọc hồ sơ bệnh án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterInvoices()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Invoices
                    .Where(i => i.PatientId == Patient.PatientId);

                // Apply date range filter
                if (InvoiceStartDate != DateTime.MinValue && InvoiceEndDate != DateTime.MinValue)
                {
                    // Add one day to EndDate to include the entire end day
                    var endDatePlus = InvoiceEndDate.AddDays(1);
                    query = query.Where(i => i.InvoiceDate >= InvoiceStartDate && i.InvoiceDate < endDatePlus);
                }

                // Filter by status if not "All"
                if (SelectedInvoiceStatus != null && SelectedInvoiceStatus.Status != "All")
                {
                    query = query.Where(i => i.Status == SelectedInvoiceStatus.Status);
                }

                // Order by date descending
                var filteredInvoices = query.OrderByDescending(i => i.InvoiceDate).ToList();

                // Update UI
                Invoices = new ObservableCollection<Invoice>(filteredInvoices);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lọc hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterAppointments()
        {
            try
            {
                if (Patient == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Doctor)
                    .Where(a => a.PatientId == Patient.PatientId && a.IsDeleted != true);

                // Filter by status if not "All"
                if (SelectedAppointmentStatus != null && SelectedAppointmentStatus.Status != "All")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus.Status);
                }

                // Order by date
                var filteredAppointments = query.OrderByDescending(a => a.AppointmentDate).ToList();

                // Update UI
                Appointments = new ObservableCollection<Appointment>(filteredAppointments);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lọc lịch hẹn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPrescription(MedicalRecord record)
        {
            MessageBox.Show(record.Prescription, "Đơn thuốc", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewTestResults(MedicalRecord record)
        {
            MessageBox.Show(record.TestResults, "Kết quả xét nghiệm", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenMedicalRecord(MedicalRecord record)
        {
            // Open medical record details window
            // You would need to implement this window
            MessageBox.Show("Chức năng mở hồ sơ chi tiết đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewInvoice(Invoice invoice)
        {
            // Open invoice details window
            // You would need to implement this window
            MessageBox.Show("Chức năng xem hóa đơn chi tiết đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintInvoice(Invoice invoice)
        {
            // Implement printing functionality
            MessageBox.Show("Chức năng in hóa đơn đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddNewAppointment()
        {
            // Open add appointment window
            // You would need to implement this window
            MessageBox.Show("Chức năng thêm lịch hẹn mới đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditExistingAppointment(Appointment appointment)
        {
            // Open edit appointment window
            // You would need to implement this window
            MessageBox.Show("Chức năng sửa lịch hẹn đang được phát triển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelExistingAppointment(Appointment appointment)
        {
            try
            {
                MessageBoxResult result = MessageBox.Show(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                        .FirstOrDefault(a => a.AppointmentId == appointment.AppointmentId);

                    if (appointmentToUpdate != null)
                    {
                        appointmentToUpdate.Status = "Canceled";
                        DataProvider.Instance.Context.SaveChanges();

                        // Refresh appointments list
                        FilterAppointments();

                        MessageBox.Show("Đã hủy lịch hẹn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi hủy lịch hẹn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class StatusItem
    {
        public string Status { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
