using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class MedicalRecordDetailsViewModel : BaseViewModel
    {
        #region Properties
        // Current logged in account
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                CheckEditPermission();
            }
        }

        // The medical record being viewed/edited
        private MedicalRecord _medicalRecord;
        public MedicalRecord MedicalRecord
        {
            get => _medicalRecord;
            set
            {
                _medicalRecord = value;
                OnPropertyChanged();
              
            }
        }

        // Patient info
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

        // Doctor info
        private Doctor _doctor;
        public Doctor Doctor
        {
            get => _doctor;
            set
            {
                _doctor = value;
                OnPropertyChanged();
            }
        }

        // Record date
        private DateTime? _recordDate;
        public DateTime? RecordDate
        {
            get => _recordDate;
            set
            {
                _recordDate = value;
                OnPropertyChanged();
            }
        }

        // Permission to edit
        private bool _canEdit;
        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                _canEdit = value;
                OnPropertyChanged();
            }
        }

        // Medical record fields
        private string _diagnosis;
        public string Diagnosis
        {
            get => _diagnosis;
            set
            {
                _diagnosis = value;
                OnPropertyChanged();
                if (MedicalRecord != null) MedicalRecord.Diagnosis = value;
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
                if (MedicalRecord != null) MedicalRecord.Prescription = value;
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
                if (MedicalRecord != null) MedicalRecord.TestResults = value;
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
                if (MedicalRecord != null) MedicalRecord.DoctorAdvice = value;
            }
        }

        // Vital signs
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

        // Window reference
        private Window _window;
        #endregion

        #region Commands
        public ICommand SaveRecordCommand { get; set; }
        public ICommand PrintRecordCommand { get; set; }
        public ICommand ResetRecordCommand { get; set; }
        public ICommand LoadedWindowCommand { get; set; }
        #endregion

        public MedicalRecordDetailsViewModel()
        {
            InitializeCommands();

            // Get current account from MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null && mainVM.CurrentAccount != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
       
        }

        // 1. Sửa constructor để gọi LoadMedicalRecordData sau khi set record
        public MedicalRecordDetailsViewModel(MedicalRecord record)
        {
            InitializeCommands();

            // Get current account from MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null && mainVM.CurrentAccount != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

            MedicalRecord = record;
            LoadMedicalRecordData(); // Thêm dòng này
        }

        private void InitializeCommands()
        {
            LoadedWindowCommand = new RelayCommand<Window>(
                p =>
                {
                    _window = p;
                    if (MedicalRecord != null)
                    {
                        LoadMedicalRecordData(); // Load dữ liệu khi window được mở
                    }
                },
                p => true
            );

            SaveRecordCommand = new RelayCommand<object>(
                p => SaveMedicalRecord(),
                p => CanEdit && MedicalRecord != null && !string.IsNullOrWhiteSpace(Diagnosis)
            );

            PrintRecordCommand = new RelayCommand<object>(
                p => PrintMedicalRecord(),
                p => MedicalRecord != null
            );

            ResetRecordCommand = new RelayCommand<object>(
                p => ResetFields(),
                p => CanEdit && MedicalRecord != null
            );
        }

        private void LoadMedicalRecordData()
        {
            if (MedicalRecord == null) return;
           
            try
            {
                // Ensure we have the complete record with related entities
                MedicalRecord = DataProvider.Instance.Context.MedicalRecords
                    .Include(m => m.Doctor)
                    .Include(m => m.Patient)
                    .FirstOrDefault(m => m.RecordId == MedicalRecord.RecordId);

                if (MedicalRecord == null) return;

                // Load patient information
                SelectedPatient = MedicalRecord.Patient;

                // Load doctor information
                Doctor = MedicalRecord.Doctor;

                // Load medical record details
                RecordDate = MedicalRecord.RecordDate;
                Diagnosis = MedicalRecord.Diagnosis ?? "";
                Prescription = MedicalRecord.Prescription ?? "";
                TestResults = MedicalRecord.TestResults ?? "";
                DoctorAdvice = MedicalRecord.DoctorAdvice ?? "";

          
                ParseVitalSigns();

                // Check if the current user has permission to edit
                CheckEditPermission();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu hồ sơ bệnh án: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckEditPermission()
        {
            // By default, nobody can edit
            CanEdit = false;

            // Check if both account and medical record are available
            if (CurrentAccount == null || MedicalRecord == null) return;

            // Check if current user is the doctor who created this record
            if (CurrentAccount.DoctorId.HasValue &&
                MedicalRecord.DoctorId == CurrentAccount.DoctorId.Value)
            {
                CanEdit = true;
            }

            // Admin can also edit
            if (CurrentAccount.Role != null &&
                CurrentAccount.Role.ToLower().Contains("admin"))
            {
                CanEdit = true;
            }

            // Update command can-execute status
            CommandManager.InvalidateRequerySuggested();
        }
        private void PrintMedicalRecord()
        {
            try
            {
                // Implement printing functionality
                MessageBox.Show("Chức năng in hồ sơ bệnh án đang được phát triển.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in hồ sơ bệnh án: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ResetFields()
        {
            try
            {
                // Reload data from database to reset any changes
                LoadMedicalRecordData();

                MessageBox.Show("Đã làm mới dữ liệu hồ sơ bệnh án.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi làm mới hồ sơ bệnh án: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void SetRecord(MedicalRecord record)
        {
            MedicalRecord = record;
            LoadMedicalRecordData();
        }
        private void ParseVitalSigns()
        {
            if (MedicalRecord == null || string.IsNullOrWhiteSpace(MedicalRecord.TestResults))
            {
                // Đặt giá trị mặc định
                Pulse = "";
                Respiration = "";
                Temperature = "";
                Weight = "";
                SystolicPressure = "";
                DiastolicPressure = "";
                return;
            }

            try
            {
                // Parse vital signs from TestResults
                string testResults = MedicalRecord.TestResults;

                // Parse pulse (Mạch)
                Pulse = ExtractValue(testResults, "Mạch: (\\d+) lần/ph");

                // Parse respiration (Nhịp thở)
                Respiration = ExtractValue(testResults, "Nhịp thở: (\\d+) lần/ph");

                // Parse temperature (Nhiệt độ)
                Temperature = ExtractValue(testResults, "Nhiệt độ: ([\\d.]+)°C");

                // Parse weight (Cân nặng)
                Weight = ExtractValue(testResults, "Cân nặng: ([\\d.]+)kg");

                // Parse blood pressure (Huyết áp)
                var bloodPressure = ExtractValue(testResults, "Huyết áp: (\\d+)/(\\d+) mmHg");
                if (!string.IsNullOrEmpty(bloodPressure) && bloodPressure.Contains("/"))
                {
                    string[] parts = bloodPressure.Split('/');
                    if (parts.Length == 2)
                    {
                        SystolicPressure = parts[0].Trim();
                        DiastolicPressure = parts[1].Trim().Replace(" mmHg", "");
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu có lỗi khi parse, sử dụng giá trị mặc định
                MessageBox.Show($"Lỗi khi phân tích thông số sinh hiệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                Pulse = "";
                Respiration = "";
                Temperature = "";
                Weight = "";
                SystolicPressure = "";
                DiastolicPressure = "";
            }
        }

        // Phương thức trích xuất giá trị từ chuỗi sử dụng regex
        private string ExtractValue(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return "";
        }

        // Phương thức định dạng thông số sinh hiệu
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

        private string CombineBloodPressure(string systolic, string diastolic)
        {
            if (string.IsNullOrWhiteSpace(systolic) && string.IsNullOrWhiteSpace(diastolic))
                return null;

            return $"{systolic ?? "?"}/{diastolic ?? "?"} mmHg";
        }

        // Sửa lại phương thức SaveMedicalRecord để kết hợp thông số sinh hiệu
        private void SaveMedicalRecord()
        {
            try
            {
                if (MedicalRecord == null) return;

                // Check if user has permission to edit
                if (!CanEdit)
                {
                    MessageBox.Show("Bạn không có quyền chỉnh sửa hồ sơ bệnh án này!",
                        "Quyền truy cập bị từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Find the medical record in database
                var recordToUpdate = DataProvider.Instance.Context.MedicalRecords
                    .FirstOrDefault(m => m.RecordId == MedicalRecord.RecordId);

                if (recordToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy hồ sơ bệnh án trong cơ sở dữ liệu!",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Format vital signs
                string formattedVitalSigns = FormatVitalSigns();

                // Extract existing test results without vital signs
                string existingTestResults = ExtractNonVitalSignResults(recordToUpdate.TestResults);

                // Combine formatted vital signs with existing test results
                string combinedTestResults = formattedVitalSigns;
                if (!string.IsNullOrWhiteSpace(existingTestResults))
                {
                    combinedTestResults += formattedVitalSigns.Length > 0 ? "\n\n" + existingTestResults : existingTestResults;
                }

                // Update record fields
                recordToUpdate.Diagnosis = Diagnosis?.Trim();
                recordToUpdate.Prescription = Prescription?.Trim();
                recordToUpdate.TestResults = combinedTestResults;  // Use combined results
                recordToUpdate.DoctorAdvice = DoctorAdvice?.Trim();

                // Save changes to database
                DataProvider.Instance.Context.SaveChanges();

                MessageBox.Show("Đã lưu thông tin hồ sơ bệnh án thành công!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hồ sơ bệnh án: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Phương thức trích xuất phần kết quả xét nghiệm không phải thông số sinh hiệu
        private string ExtractNonVitalSignResults(string testResults)
        {
            if (string.IsNullOrWhiteSpace(testResults))
                return "";

            // Xóa các thông số sinh hiệu đã biết khỏi chuỗi kết quả
            string result = testResults;

            // Các mẫu regex cho các thông số sinh hiệu
            string[] patterns = {
                "Mạch: \\d+ lần/ph",
                "Nhịp thở: \\d+ lần/ph",
                "Nhiệt độ: [\\d.]+°C",
                "Cân nặng: [\\d.]+kg",
                "Huyết áp: \\d+/\\d+ mmHg"
            };

            // Xóa các thông số sinh hiệu và dấu phẩy/dấu chấm thừa
            foreach (var pattern in patterns)
            {
                result = Regex.Replace(result, pattern + "(, |\\.| |$)", "");
            }

            // Xóa dấu phẩy hoặc dấu chấm ở đầu chuỗi
            result = Regex.Replace(result, "^[,. ]+", "");

            return result.Trim();
        }
    }
}
    

