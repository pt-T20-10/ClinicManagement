using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Windows;

namespace ClinicManagement.ViewModels
{
    public class ExamineViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();
             
            }
        }

        private string _patienName;
        public string PatienName
        {
            get => _patienName;
            set
            {
                _patienName = value;
                OnPropertyChanged();
                SearchPatient();
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
                SearchPatient();
            }
        }
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

        private DateTime _recordDate = DateTime.Today;
        public DateTime RecordDate
        {
            get => _recordDate;
            set
            {
                _recordDate = value;
                OnPropertyChanged();
            }
        }

        private Doctor _selectedDoctor;
        public Doctor SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
            }
        }

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

        // Medical examination fields
        private string _pulse;
        public string Pulse
        {
            get => _pulse;
            set
            {
                _pulse = value;
                OnPropertyChanged();
            }
        }

        private string _respiration;
        public string Respiration
        {
            get => _respiration;
            set
            {
                _respiration = value;
                OnPropertyChanged();
            }
        }

        private string _temperature;
        public string Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged();
            }
        }

        private string _weight;
        public string Weight
        {
            get => _weight;
            set
            {
                _weight = value;
                OnPropertyChanged();
            }
        }

        private string _systolicPressure;
        public string SystolicPressure
        {
            get => _systolicPressure;
            set
            {
                _systolicPressure = value;
                OnPropertyChanged();
            }
        }

        private string _diastolicPressure;
        public string DiastolicPressure
        {
            get => _diastolicPressure;
            set
            {
                _diastolicPressure = value;
                OnPropertyChanged();
            }
        }

        private string _diagnosis;
        public string Diagnosis
        {
            get => _diagnosis;
            set
            {
                _diagnosis = value;
                OnPropertyChanged();
            }
        }

        private string _doctorAdvice;
        public string DoctorAdvice
        {
            get => _doctorAdvice;
            set
            {
                _doctorAdvice = value;
                OnPropertyChanged();
            }
        }

        private string _testResults;
        public string TestResults
        {
            get => _testResults;
            set
            {
                _testResults = value;
                OnPropertyChanged();
            }
        }

        private string _prescription;
        public string Prescription
        {
            get => _prescription;
            set
            {
                _prescription = value;
                OnPropertyChanged();
            }
        }

        // Related appointment if examination was started from an appointment
        private Appointment _relatedAppointment;
        public Appointment RelatedAppointment
        {
            get => _relatedAppointment;
            set
            {
                _relatedAppointment = value;
                OnPropertyChanged();
            }
        }
        // For appointment type selection
        private ObservableCollection<AppointmentType> _appointmentTypeList;
        public ObservableCollection<AppointmentType> AppointmentTypeList
        {
            get => _appointmentTypeList;
            set
            {
                _appointmentTypeList = value;
                OnPropertyChanged();
            }
        }

        private AppointmentType _selectedAppointmentType;
        public AppointmentType SelectedAppointmentType
        {
            get => _selectedAppointmentType;
            set
            {
                _selectedAppointmentType = value;
                OnPropertyChanged();

                // When appointment type changes, update the fee
                if (_selectedAppointmentType != null && _selectedAppointmentType.Price.HasValue)
                {
                    ExaminationFee = _selectedAppointmentType.Price.Value;
                }
            }
        }

        // For examination fee
        private decimal _examinationFee;
        public decimal ExaminationFee
        {
            get => _examinationFee;
            set
            {
                _examinationFee = value;
                OnPropertyChanged();
            }
        }


        // Validation
        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;
        #endregion

        #region Commands
        public ICommand SaveRecordCommand { get; set; }
        public ICommand PrintRecordCommand { get; set; }
        public ICommand ResetRecordCommand { get; set; }
        public ICommand SearchPatientCommand { get; set; }
        public ICommand AddPatientCommand { get; set; }
        public ICommand ExportPDFCommand { get; set; }

        #endregion

        #region Constructors
        // Constructor for manual patient entry
        public ExamineViewModel()
        {
            InitializeCommands();
            LoadDoctors();
            LoadAppointmentTypes();
        }

        // Constructor for starting examination from an appointment
        public ExamineViewModel(Patient patient, Appointment appointment = null)
        {
            InitializeCommands();
            LoadDoctors();
            LoadAppointmentTypes();
            SelectedPatient = patient;
            PatienName = patient?.FullName;
            Phone = patient?.Phone;

            // If an appointment is provided, set it as related and change its status
            if (appointment != null)
            {
                RelatedAppointment = appointment;
                SelectedDoctor = appointment.Doctor;

                // Update appointment status to "Đang khám"
                UpdateAppointmentStatus(appointment, "Đang khám");
            }
        }
        #endregion

        #region Methods
        private void InitializeCommands()
        {
            SaveRecordCommand = new RelayCommand<object>(
                (p) => SaveRecord(),
                (p) => CanSaveRecord()
            );

            ResetRecordCommand = new RelayCommand<object>(
                (p) => ResetForm(),
                (p) => true
            );

            // Add this new command
            SearchPatientCommand = new RelayCommand<object>(
                (p) => SearchPatientExplicit(),
                (p) => true
            );
            AddPatientCommand = new RelayCommand<object>(
                (p) =>
                {
                    // Open a new window to add a new patient
                    var addPatientWindow = new AddPatientWindow();
                    var viewModel = new AddPatientViewModel();
                    addPatientWindow.DataContext = viewModel;

                    addPatientWindow.ShowDialog();

                    // After window closes, check if a new patient was created
                    if (viewModel.NewPatient != null)
                    {
                        // Do something with the newly created patient
                        var createdPatient = viewModel.NewPatient;
                        SelectedPatient = createdPatient;
                        PatienName = createdPatient.FullName;
                        Phone = createdPatient.Phone;
                        InsuranceCode = createdPatient.InsuranceCode; // Add this line
                        // For example, select this patient in another view
                    }
    

                },
                (p) => true
            );
            ExportPDFCommand = new RelayCommand<object>(
                (p) => ExporttoPDF(),
                (p) => CanExportToPdf()
            );
        }

        private void SearchPatientExplicit()
        {
            try
            {
                // Validate search criteria
                if (string.IsNullOrWhiteSpace(InsuranceCode) &&
                   (string.IsNullOrWhiteSpace(PatienName) || string.IsNullOrWhiteSpace(Phone)))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập mã BHYT hoặc cả tên và số điện thoại để tìm kiếm.", "Thiếu thông tin");
                    return;
                }

                // Check minimum length for search fields
                if (!string.IsNullOrWhiteSpace(PatienName) && PatienName.Length < 2)
                {
                    MessageBoxService.ShowWarning("Tên bệnh nhân cần có ít nhất 2 ký tự.", "Thiếu thông tin");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(Phone) && Phone.Length < 5)
                {
                    MessageBoxService.ShowWarning("Số điện thoại cần có ít nhất 5 ký tự.", "Thiếu thông tin");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(InsuranceCode) && InsuranceCode.Length < 5)
                {
                    MessageBoxService.ShowWarning("Mã BHYT cần có ít nhất 5 ký tự.", "Thiếu thông tin");
                    return;
                }

                var query = DataProvider.Instance.Context.Patients.Where(p => p.IsDeleted != true);

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(InsuranceCode))
                {
                    query = query.Where(p => p.InsuranceCode.Contains(InsuranceCode));
                }
                else
                {
                    // If not searching by insurance, then require both name and phone
                    query = query.Where(p => p.FullName.Contains(PatienName) && p.Phone.Contains(Phone));
                }

                var patient = query.FirstOrDefault();

                if (patient != null)
                {
                    SelectedPatient = patient;

                    // Set the UI fields to match the found patient
                    PatienName = patient.FullName;
                    Phone = patient.Phone;
                    InsuranceCode = patient.InsuranceCode;

                    // Check if there's a pending appointment for this patient
                    var pendingAppointment = DataProvider.Instance.Context.Appointments
                        .Include(a => a.Doctor)
                        .FirstOrDefault(a => a.PatientId == patient.PatientId &&
                                            a.Status == "Đang chờ" &&
                                            a.IsDeleted != true &&
                                            a.AppointmentDate.Date == DateTime.Today);

                    if (pendingAppointment != null && RelatedAppointment == null)
                    {
                        RelatedAppointment = pendingAppointment;
                        SelectedDoctor = pendingAppointment.Doctor;

                        // Ask if the user wants to proceed with this appointment
                         bool  result = MessageBoxService.ShowQuestion(
                            $"Tìm thấy lịch hẹn đang chờ của bệnh nhân {patient.FullName} với bác sĩ {pendingAppointment.Doctor.FullName}.\n" +
                            $"Bạn có muốn tiến hành khám với lịch hẹn này không?",
                            "Tìm thấy lịch hẹn"
                             
                            );

                        if (result)
                        {
                            UpdateAppointmentStatus(pendingAppointment, "Đang khám");
                        }
                        else
                        {
                            RelatedAppointment = null;
                        }
                    }
                }
                else
                {
                    SelectedPatient = null;
                    MessageBoxService.ShowWarning  ("Không tìm thấy bệnh nhân với thông tin đã nhập. Vui lòng kiểm tra lại thông tin tìm kiếm.", "Không tìm thấy");
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm bệnh nhân: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
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

                // Select first doctor by default if available
                if (DoctorList.Count > 0 && SelectedDoctor == null)
                {
                    SelectedDoctor = DoctorList.First();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void SearchPatient()
        {
            // Only auto-search when both name and phone have sufficient characters
            if (string.IsNullOrWhiteSpace(PatienName) || PatienName.Length < 2 ||
                string.IsNullOrWhiteSpace(Phone) || Phone.Length < 5)
            {
                SelectedPatient = null;
                return;
            }

            try
            {
                var query = DataProvider.Instance.Context.Patients
                    .Where(p => p.IsDeleted != true &&
                            p.FullName.Contains(PatienName) &&
                            p.Phone.Contains(Phone));

                var patient = query.FirstOrDefault();

                if (patient != null)
                {
                    SelectedPatient = patient;
                    InsuranceCode = patient.InsuranceCode;

                    // Check if there's a pending appointment for this patient
                    var pendingAppointment = DataProvider.Instance.Context.Appointments
                        .Include(a => a.Doctor)
                        .FirstOrDefault(a => a.PatientId == patient.PatientId &&
                                            a.Status == "Đang chờ" &&
                                            a.IsDeleted != true &&
                                            a.AppointmentDate.Date == DateTime.Today);

                    if (pendingAppointment != null && RelatedAppointment == null)
                    {
                        RelatedAppointment = pendingAppointment;
                        SelectedDoctor = pendingAppointment.Doctor;

                        // Ask if the user wants to proceed with this appointment
                         bool  result = MessageBoxService.ShowInfo(
                            $"Tìm thấy lịch hẹn đang chờ của bệnh nhân {patient.FullName} với bác sĩ {pendingAppointment.Doctor.FullName}.\n" +
                            $"Bạn có muốn tiến hành khám với lịch hẹn này không?",
                            "Tìm thấy lịch hẹn"
                             
                            );

                        if (result)
                        {
                            UpdateAppointmentStatus(pendingAppointment, "Đang khám");
                        }
                        else
                        {
                            RelatedAppointment = null;
                        }
                    }
                }
                else
                {
                    SelectedPatient = null;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm bệnh nhân: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }


        private bool CanSaveRecord()
        {
            return SelectedPatient != null &&
                   SelectedDoctor != null &&
                   !string.IsNullOrWhiteSpace(Diagnosis);
        }

        private void SaveRecord()
        {
            try
            {
                if (SelectedPatient == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn bệnh nhân trước khi lưu hồ sơ.", "Thiếu thông tin");
                    return;
                }

                if (SelectedDoctor == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn bác sĩ khám trước khi lưu hồ sơ.", "Thiếu thông tin");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Diagnosis))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập chẩn đoán trước khi lưu hồ sơ.", "Thiếu thông tin");
                    return;
                }

                // Format vital signs for test results
                string formattedVitalSigns = FormatVitalSigns();

                // Combine vital signs with any existing test results
                string combinedTestResults = formattedVitalSigns;
                if (!string.IsNullOrWhiteSpace(TestResults))
                {
                    combinedTestResults += formattedVitalSigns.Length > 0 ? " " + TestResults : TestResults;
                }

                // Create new medical record
                MedicalRecord newRecord = new MedicalRecord
                {
                    PatientId = SelectedPatient.PatientId,
                    DoctorId = SelectedDoctor.DoctorId,
                    RecordDate = RecordDate,
                    Diagnosis = Diagnosis,
                    DoctorAdvice = DoctorAdvice,
                    TestResults = combinedTestResults,
                    Prescription = Prescription,
                    IsDeleted = false
                };

                // Save to database
                DataProvider.Instance.Context.MedicalRecords.Add(newRecord);
                DataProvider.Instance.Context.SaveChanges();

                // Create invoice for examination
                var invoice = new Invoice
                {
                    PatientId = SelectedPatient.PatientId,
                    MedicalRecordId = newRecord.RecordId,
                    InvoiceDate = DateTime.Now,
                    Status = "Chưa thanh toán",
                    InvoiceType = "Khám bệnh",
                    TotalAmount = ExaminationFee,
                    Discount = SelectedPatient.PatientType.Discount,
                    Tax = 0
                };
                DataProvider.Instance.Context.Invoices.Add(invoice);
                DataProvider.Instance.Context.SaveChanges();

                // Add invoice detail for examination fee
                var invoiceDetail = new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ServiceName = SelectedAppointmentType?.TypeName ?? "Khám bệnh",
                    SalePrice = ExaminationFee,
                    Quantity = 1
                };
                DataProvider.Instance.Context.InvoiceDetails.Add(invoiceDetail);
                DataProvider.Instance.Context.SaveChanges();

                // Update related appointment if exists
                if (RelatedAppointment != null)
                {
                    UpdateAppointmentStatus(RelatedAppointment, "Đã khám");
                }

                MessageBoxService.ShowSuccess("Đã lưu hồ sơ khám bệnh và tạo hóa đơn thành công!", "Thành công");

                // Optional: Reset form after saving
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi lưu hồ sơ: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }


        private string FormatVitalSigns()
        {
            List<string> vitalSigns = new List<string>();

            // Add pulse if available
            if (!string.IsNullOrWhiteSpace(Pulse) && int.TryParse(Pulse, out _))
            {
                vitalSigns.Add($"Mạch: {Pulse} lần/ph");
            }

            // Add respiration if available
            if (!string.IsNullOrWhiteSpace(Respiration) && int.TryParse(Respiration, out _))
            {
                vitalSigns.Add($"Nhịp thở: {Respiration} lần/ph");
            }

            // Add temperature if available
            if (!string.IsNullOrWhiteSpace(Temperature) && decimal.TryParse(Temperature, out _))
            {
                vitalSigns.Add($"Nhiệt độ: {Temperature}°C");
            }

            // Add weight if available
            if (!string.IsNullOrWhiteSpace(Weight) && decimal.TryParse(Weight, out _))
            {
                vitalSigns.Add($"Cân nặng: {Weight}kg");
            }

            // Add blood pressure if available
            if (!string.IsNullOrWhiteSpace(SystolicPressure) || !string.IsNullOrWhiteSpace(DiastolicPressure))
            {
                string bloodPressure = CombineBloodPressure(SystolicPressure, DiastolicPressure);
                if (!string.IsNullOrWhiteSpace(bloodPressure))
                {
                    vitalSigns.Add($"Huyết áp: {bloodPressure}");
                }
            }

            // Join all vital signs with commas
            return vitalSigns.Count > 0 ? string.Join(", ", vitalSigns) + "." : "";
        }




        private void ResetForm()
        {
            // If this is from an appointment, keep the patient and doctor
            if (RelatedAppointment == null)
            {
                SelectedPatient = null;
                PatienName = string.Empty;
                Phone = string.Empty;
                InsuranceCode = string.Empty; // Add this line
                SelectedDoctor = DoctorList.FirstOrDefault();
            }

            // Reset examination details
            RecordDate = DateTime.Today;
            Pulse = string.Empty;
            Respiration = string.Empty;
            Temperature = string.Empty;
            Weight = string.Empty;
            SystolicPressure = string.Empty;
            DiastolicPressure = string.Empty;
            Diagnosis = string.Empty;
            DoctorAdvice = string.Empty;
            TestResults = string.Empty;
            Prescription = string.Empty;
        }

        private void UpdateAppointmentStatus(Appointment appointment, string newStatus)
        {
            try
            {
                if (appointment == null) return;

                var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                    .FirstOrDefault(a => a.AppointmentId == appointment.AppointmentId);

                if (appointmentToUpdate != null)
                {
                    appointmentToUpdate.Status = newStatus;
                    DataProvider.Instance.Context.SaveChanges();

                    // Update our local reference to match the database
                    RelatedAppointment.Status = newStatus;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi cập nhật trạng thái lịch hẹn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void LoadAppointmentTypes()
        {
            try
            {
                var appointmentTypes = DataProvider.Instance.Context.AppointmentTypes
                    .Where(at => at.IsDeleted != true)
                    .OrderBy(at => at.TypeName)
                    .ToList();

                AppointmentTypeList = new ObservableCollection<AppointmentType>(appointmentTypes);

                // Set default appointment type if available
                if (AppointmentTypeList.Count > 0 && SelectedAppointmentType == null)
                {
                    SelectedAppointmentType = AppointmentTypeList.First();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải danh sách loại khám: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }
        #endregion

        #region Helper Methods
        private int? ParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out int result))
                return result;

            return null;
        }

        private decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (decimal.TryParse(value, out decimal result))
                return result;

            return null;
        }

        private string CombineBloodPressure(string systolic, string diastolic)
        {
            if (string.IsNullOrWhiteSpace(systolic) && string.IsNullOrWhiteSpace(diastolic))
                return null;

            return $"{systolic ?? "?"}/{diastolic ?? "?"} mmHg";
        }
        #endregion

        #region Validation
        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the field
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(Diagnosis):
                        if (string.IsNullOrWhiteSpace(Diagnosis))
                        {
                            error = "Chẩn đoán không được để trống";
                        }
                        break;

                    case nameof(SelectedDoctor):
                        if (SelectedDoctor == null)
                        {
                            error = "Vui lòng chọn bác sĩ";
                        }
                        break;

                    case nameof(Pulse):
                        if (!string.IsNullOrWhiteSpace(Pulse) && !int.TryParse(Pulse, out _))
                        {
                            error = "Mạch phải là một số";
                        }
                        break;

                    case nameof(Respiration):
                        if (!string.IsNullOrWhiteSpace(Respiration) && !int.TryParse(Respiration, out _))
                        {
                            error = "Nhịp thở phải là một số";
                        }
                        break;

                    case nameof(Temperature):
                        if (!string.IsNullOrWhiteSpace(Temperature) && !decimal.TryParse(Temperature, out _))
                        {
                            error = "Nhiệt độ phải là một số";
                        }
                        break;

                    case nameof(Weight):
                        if (!string.IsNullOrWhiteSpace(Weight) && !decimal.TryParse(Weight, out _))
                        {
                            error = "Cân nặng phải là một số";
                        }
                        break;

                    case nameof(SystolicPressure):
                        if (!string.IsNullOrWhiteSpace(SystolicPressure) && !int.TryParse(SystolicPressure, out _))
                        {
                            error = "Huyết áp tâm thu phải là một số";
                        }
                        break;

                    case nameof(DiastolicPressure):
                        if (!string.IsNullOrWhiteSpace(DiastolicPressure) && !int.TryParse(DiastolicPressure, out _))
                        {
                            error = "Huyết áp tâm trương phải là một số";
                        }
                        break;
                }

                return error;
            }
        }
        #endregion

        #region PDF
        private bool CanExportToPdf()
        {
            return SelectedPatient != null &&
                   SelectedDoctor != null &&
                   !string.IsNullOrWhiteSpace(Diagnosis);
        }

        private void ExporttoPDF()
        {
            try
            {
                // Perform the same validations as in SaveRecord method
                if (SelectedPatient == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn bệnh nhân trước khi xuất phiếu khám.", "Thiếu thông tin");
                    return;
                }

                if (SelectedDoctor == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn bác sĩ khám trước khi xuất phiếu khám.", "Thiếu thông tin");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Diagnosis))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập chẩn đoán trước khi xuất phiếu khám.", "Thiếu thông tin");
                    return;
                }
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có muốn xuất phiếu khám bệnh không? Hồ sơ sẽ được lưu trước khi xuất.",
                    "Xuất phiếu khám"
                );
                if (!result)
                {
                    return; // User cancelled the export
                }
                // Save the record first if needed (uses the same logic as SaveRecord method)
                MedicalRecord medicalRecord = null;
                bool isNewRecord = true;

                // Check if we already have a saved record for this patient and session
                if (RelatedAppointment != null)
                {
                    medicalRecord = DataProvider.Instance.Context.MedicalRecords
                        .FirstOrDefault(m => m.PatientId == SelectedPatient.PatientId &&
                                           m.DoctorId == SelectedDoctor.DoctorId &&
                                           m.RecordDate == RecordDate.Date &&
                                           m.IsDeleted != true);

                    if (medicalRecord != null)
                        isNewRecord = false;
                }

                if (isNewRecord)
                {
                    // Format vital signs for test results
                    string formattedVitalSigns = FormatVitalSigns();

                    // Combine vital signs with any existing test results
                    string combinedTestResults = formattedVitalSigns;
                    if (!string.IsNullOrWhiteSpace(TestResults))
                    {
                        combinedTestResults += formattedVitalSigns.Length > 0 ? " " + TestResults : TestResults;
                    }

                    // Create new medical record
                    medicalRecord = new MedicalRecord
                    {
                        PatientId = SelectedPatient.PatientId,
                        DoctorId = SelectedDoctor.DoctorId,
                        RecordDate = RecordDate,
                        Diagnosis = Diagnosis,
                        DoctorAdvice = DoctorAdvice,
                        TestResults = combinedTestResults,
                        Prescription = Prescription,
                        IsDeleted = false
                    };

                    // Save to database
                    DataProvider.Instance.Context.MedicalRecords.Add(medicalRecord);
                    DataProvider.Instance.Context.SaveChanges();

                    // Create invoice for examination
                    var invoice = new Invoice
                    {
                        PatientId = SelectedPatient.PatientId,
                        MedicalRecordId = medicalRecord.RecordId,
                        InvoiceDate = DateTime.Now,
                        Status = "Chưa thanh toán",
                        InvoiceType = "Khám bệnh",
                        TotalAmount = ExaminationFee,
                        Discount = SelectedPatient.PatientType?.Discount ?? 0,
                        Tax = 0
                    };
                    DataProvider.Instance.Context.Invoices.Add(invoice);
                    DataProvider.Instance.Context.SaveChanges();

                    // Add invoice detail for examination fee
                    var invoiceDetail = new InvoiceDetail
                    {
                        InvoiceId = invoice.InvoiceId,
                        ServiceName = SelectedAppointmentType?.TypeName ?? "Khám bệnh",
                        SalePrice = ExaminationFee,
                        Quantity = 1
                    };
                    DataProvider.Instance.Context.InvoiceDetails.Add(invoiceDetail);
                    DataProvider.Instance.Context.SaveChanges();

                    // Update related appointment if exists
                    if (RelatedAppointment != null)
                    {
                        UpdateAppointmentStatus(RelatedAppointment, "Đã khám");
                    }

                    MessageBoxService.ShowSuccess("Đã lưu hồ sơ và hóa đơn khám bệnh thành công!", "Thành công");
                }

                // Now that we have a valid medical record, export it to PDF
                QuestPDF.Settings.License = LicenseType.Community;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"PhieuKhamBenh_{SelectedPatient.FullName}_{DateTime.Now:yyyyMMdd}.pdf",
                    Title = "Lưu phiếu khám bệnh"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Generate PDF in background thread with progress reporting
                    Task.Run(() =>
                    {
                        try
                        {
                            // Report progress: 10% - Starting
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));
                            Thread.Sleep(100); // Small delay for visibility

                            // Report progress: 30% - Preparing data
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));
                            Thread.Sleep(100); // Small delay for visibility

                            // Report progress: 50% - Generating document
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                            // Create the PDF document
                            GenerateMedicalExaminationPdf(filePath, medicalRecord);

                            // Report progress: 90% - Saving file
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));
                            Thread.Sleep(100); // Small delay for visibility

                            // Report progress: 100% - Complete
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                            Thread.Sleep(300); // Show 100% briefly

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Close progress dialog
                                progressDialog.Close();

                                // Show success message
                                MessageBoxService.ShowSuccess(
                                    $"Đã xuất phiếu khám bệnh thành công!\nĐường dẫn: {filePath}",
                                    "Xuất phiếu khám"
                                );

                                // Ask if user wants to open the PDF
                                if (MessageBoxService.ShowQuestion("Bạn có muốn mở file PDF không?", "Mở file"))
                                {
                                    try
                                    {
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = filePath,
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
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError(
                                    $"Đã xảy ra lỗi khi xuất phiếu khám: {ex.Message}",
                                    "Lỗi"
                                );
                            });
                        }
                    });

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xuất phiếu khám: {ex.Message}",
                    "Lỗi"
                );
            }
        }



        private void GenerateMedicalExaminationPdf(string filePath, MedicalRecord medicalRecord)
        {
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(column =>
                    {
                        // HEADER
                        column.Item().Row(row =>
                        {
                            // Clinic information
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("PHÒNG KHÁM CLINIC MANAGEMENT")
                                    .FontSize(18).Bold();
                                col.Item().Text("Địa chỉ: 123 Đường Khám Bệnh, Q1, TP.HCM")
                                    .FontSize(10);
                                col.Item().Text("SĐT: 028.1234.5678 | Email: info@clinicmanagement.com")
                                    .FontSize(10);
                            });

                            // Date information
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().AlignRight().Text($"Ngày khám: {RecordDate:dd/MM/yyyy}")
                                    .FontSize(10);
                            });
                        });

                        // TITLE
                        column.Item().PaddingVertical(20)
                            .Text("PHIẾU KHÁM BỆNH")
                            .FontSize(16).Bold()
                            .AlignCenter();

                        // PATIENT INFORMATION
                        column.Item().PaddingTop(10)
                            .Column(patientCol =>
                            {
                                patientCol.Item().Text("THÔNG TIN BỆNH NHÂN").Bold();
                                patientCol.Item().PaddingTop(5).Text($"Họ tên: {SelectedPatient.FullName}");
                                patientCol.Item().Text($"Ngày sinh: {(SelectedPatient.DateOfBirth.HasValue ? SelectedPatient.DateOfBirth.Value.ToString("dd/MM/yyyy") : "Không có")}");
                                patientCol.Item().Text($"Giới tính: {SelectedPatient.Gender ?? "Không có"}");
                                patientCol.Item().Text($"Số điện thoại: {SelectedPatient.Phone ?? "Không có"}");

                                if (!string.IsNullOrEmpty(SelectedPatient.InsuranceCode))
                                    patientCol.Item().Text($"Mã BHYT: {SelectedPatient.InsuranceCode}");

                                if (SelectedPatient.PatientType != null)
                                    patientCol.Item().Text($"Loại khách hàng: {SelectedPatient.PatientType.TypeName}");
                            });

                        // VITAL SIGNS
                        if (!string.IsNullOrWhiteSpace(Pulse) ||
                            !string.IsNullOrWhiteSpace(Respiration) ||
                            !string.IsNullOrWhiteSpace(Temperature) ||
                            !string.IsNullOrWhiteSpace(Weight) ||
                            !string.IsNullOrWhiteSpace(SystolicPressure) ||
                            !string.IsNullOrWhiteSpace(DiastolicPressure))
                        {
                            column.Item().PaddingTop(20)
                                .Column(vitalCol =>
                                {
                                    vitalCol.Item().Text("DẤU HIỆU SINH TỒN").Bold();

                                    if (!string.IsNullOrWhiteSpace(Pulse) && int.TryParse(Pulse, out _))
                                        vitalCol.Item().Text($"Mạch: {Pulse} lần/ph");

                                    if (!string.IsNullOrWhiteSpace(Respiration) && int.TryParse(Respiration, out _))
                                        vitalCol.Item().Text($"Nhịp thở: {Respiration} lần/ph");

                                    if (!string.IsNullOrWhiteSpace(Temperature) && decimal.TryParse(Temperature, out _))
                                        vitalCol.Item().Text($"Nhiệt độ: {Temperature}°C");

                                    if (!string.IsNullOrWhiteSpace(Weight) && decimal.TryParse(Weight, out _))
                                        vitalCol.Item().Text($"Cân nặng: {Weight}kg");

                                    if ((!string.IsNullOrWhiteSpace(SystolicPressure) || !string.IsNullOrWhiteSpace(DiastolicPressure)))
                                    {
                                        string bloodPressure = CombineBloodPressure(SystolicPressure, DiastolicPressure);
                                        vitalCol.Item().Text($"Huyết áp: {bloodPressure}");
                                    }
                                });
                        }

                        // TEST RESULTS
                        if (!string.IsNullOrWhiteSpace(TestResults))
                        {
                            column.Item().PaddingTop(20)
                                .Column(testCol =>
                                {
                                    testCol.Item().Text("KẾT QUẢ XÉT NGHIỆM").Bold();
                                    testCol.Item().Text(TestResults);
                                });
                        }

                        // DIAGNOSIS
                        column.Item().PaddingTop(20)
                            .Column(diagCol =>
                            {
                                diagCol.Item().Text("CHẨN ĐOÁN").Bold();
                                diagCol.Item().Text(Diagnosis);
                            });

                        // PRESCRIPTION
                        if (!string.IsNullOrWhiteSpace(Prescription))
                        {
                            column.Item().PaddingTop(20)
                                .Column(presCol =>
                                {
                                    presCol.Item().Text("ĐƠN THUỐC").Bold();
                                    presCol.Item().Text(Prescription);
                                });
                        }

                        // DOCTOR ADVICE
                        if (!string.IsNullOrWhiteSpace(DoctorAdvice))
                        {
                            column.Item().PaddingTop(20)
                                .Column(adviceCol =>
                                {
                                    adviceCol.Item().Text("LỜI DẶN CỦA BÁC SĨ").Bold();
                                    adviceCol.Item().Text(DoctorAdvice);
                                });
                        }

                        // DOCTOR SIGNATURE
                        column.Item().PaddingTop(40)
                            .Row(row =>
                            {
                                row.RelativeItem(2);
                                row.RelativeItem(3).Column(col =>
                                {
                                    col.Item().AlignCenter().Text("Bác sĩ khám bệnh").Bold();
                                    col.Item().AlignCenter().Text("(Ký, họ tên)").Italic().FontSize(9);
                                    col.Item().PaddingTop(70).AlignCenter().Text(SelectedDoctor.FullName).Bold();
                                });
                            });

                        // FOOTER
                        column.Item().PaddingTop(30)
                            .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                            .PaddingTop(10)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(footerCol =>
                                {
                                    footerCol.Item().Text("Xin cám ơn Quý khách đã sử dụng dịch vụ của phòng khám chúng tôi!")
                                        .FontSize(9).Italic();
                                });

                                row.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span("Trang ").FontSize(9);
                                    text.CurrentPageNumber().FontSize(9);
                                    text.Span(" / ").FontSize(9);
                                    text.TotalPages().FontSize(9);
                                });
                            });
                    });
                });
            })
            .GeneratePdf(filePath);
        }
        #endregion
    }
}
