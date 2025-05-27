using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class PatientDetailsWindowViewModel : BaseViewModel
    {
        #region Properties
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
            get => _medicalRecords;
            set
            {
                _medicalRecords = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set
            {
                _invoices = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Appointment> _appointments;
        public ObservableCollection<Appointment> Appointments
        {
            get => _appointments;
            set
            {
                _appointments = value;
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
            }
        }
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
                (p) => LoadWindow(p),
                (p) => true
            );

            UpdatePatientCommand = new RelayCommand<object>(
                (p) => UpdatePatient(),
                (p) => Patient != null
            );

            DeletePatientCommand = new RelayCommand<object>(
                (p) => DeletePatient(),
                (p) => Patient != null
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

        private void LoadWindow(Window window)
        {
            if (window != null)
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

        private void UpdatePatient()
        {
            try
            {
              
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
                        // You can use a callback or an event to notify the owner window to close
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
                var filteredRecords = DataProvider.Instance.Context.MedicalRecords
                    .Include(m => m.Doctor)
                    .Where(m =>
                        m.PatientId == Patient.PatientId &&
                        m.IsDeleted != true &&
                        (m.RecordDate >= StartDate && m.RecordDate <= EndDate.AddDays(1)))
                    .OrderByDescending(m => m.RecordDate)
                    .ToList();

                // Filter by search term if provided
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();
                    filteredRecords = filteredRecords.Where(m =>
                        (m.Diagnosis != null && m.Diagnosis.ToLower().Contains(searchTermLower)) ||
                        (m.Doctor.FullName != null && m.Doctor.FullName.ToLower().Contains(searchTermLower))
                    ).ToList();
                }

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
                var query = DataProvider.Instance.Context.Invoices
                    .Where(i =>
                        i.PatientId == Patient.PatientId &&
                        (i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate.AddDays(1)));

                // Filter by status if not "All"
                if (SelectedInvoiceStatus != null && SelectedInvoiceStatus.Status != "All")
                {
                    query = query.Where(i => i.Status == SelectedInvoiceStatus.Status);
                }

                var filteredInvoices = query.OrderByDescending(i => i.InvoiceDate).ToList();
                Invoices = new ObservableCollection<Invoice>(filteredInvoices);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lọc hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        int patientId = Patient.PatientId;
                        Appointments = new ObservableCollection<Appointment>(
                            DataProvider.Instance.Context.Appointments
                                .Include(a => a.Doctor)
                                .Where(a => a.PatientId == patientId && a.IsDeleted != true)
                                .OrderByDescending(a => a.AppointmentDate)
                                .ToList()
                        );

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
        public string Status { get; set; }
        public string DisplayName { get; set; }
    }
}

