using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;
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
        private Staff _doctor;
        public Staff Doctor
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
        public ICommand ResetRecordCommand { get; set; }
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand ExportPDFCommand { get; set; }
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

          

            ResetRecordCommand = new RelayCommand<object>(
                p => ResetFields(),
                p => CanEdit && MedicalRecord != null
            );
            ExportPDFCommand = new RelayCommand<object>(
                  p => ExportToPDF(),
                  p => MedicalRecord != null
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
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu hồ sơ bệnh án: {ex.Message}",
                    "Lỗi"    );
            }
        }

        private void CheckEditPermission()
        {
            // By default, nobody can edit
            CanEdit = false;

            // Check if both account and medical record are available
            if (CurrentAccount == null || MedicalRecord == null) return;

            // Check if current user is the doctor who created this record
            if (CurrentAccount.StaffId.HasValue &&
                MedicalRecord.StaffId == CurrentAccount.StaffId.Value)
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

        private void ResetFields()
        {
            try
            {
                // Reload data from database to reset any changes
                LoadMedicalRecordData();

                MessageBoxService.ShowSuccess("Đã làm mới dữ liệu hồ sơ bệnh án.",
                    "Thông báo"     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi làm mới hồ sơ bệnh án: {ex.Message}",
                    "Lỗi"    );
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
                // Parse blood pressure (Huyết áp) - Cải thiện phương thức để xử lý huyết áp
                var match = Regex.Match(testResults, "Huyết áp: (\\d+)/(\\d+) mmHg");
                if (match.Success && match.Groups.Count > 2)
                {
                    SystolicPressure = match.Groups[1].Value;
                    DiastolicPressure = match.Groups[2].Value;

                    // Đảm bảo triggering PropertyChanged để UI cập nhật
                    OnPropertyChanged(nameof(SystolicPressure));
                    OnPropertyChanged(nameof(DiastolicPressure));
                }
                else
                {
                    // Thử mẫu regex khác nếu mẫu đầu tiên không khớp
                    match = Regex.Match(testResults, "Huyết áp: (\\d+)/(\\d+)");
                    if (match.Success && match.Groups.Count > 2)
                    {
                        SystolicPressure = match.Groups[1].Value;
                        DiastolicPressure = match.Groups[2].Value;

                        // Đảm bảo triggering PropertyChanged để UI cập nhật
                        OnPropertyChanged(nameof(SystolicPressure));
                        OnPropertyChanged(nameof(DiastolicPressure));
                    }
                }
            
                }
            catch (Exception ex)
            {
                // Nếu có lỗi khi parse, sử dụng giá trị mặc định
                MessageBoxService.ShowError($"Lỗi khi phân tích thông số sinh hiệu: {ex.Message}", "Lỗi"     );
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
                    MessageBoxService.ShowWarning("Bạn không có quyền chỉnh sửa hồ sơ bệnh án này!",
                        "Quyền truy cập bị từ chối");
                    return;
                }

                // Find the medical record in database
                var recordToUpdate = DataProvider.Instance.Context.MedicalRecords
                    .FirstOrDefault(m => m.RecordId == MedicalRecord.RecordId);

                if (recordToUpdate == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy hồ sơ bệnh án trong cơ sở dữ liệu!",
                        "Lỗi"    );
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

                MessageBoxService.ShowSuccess("Đã lưu thông tin hồ sơ bệnh án thành công!"
                       );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lưu hồ sơ bệnh án: {ex.Message}",
                    "Lỗi"    );
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

        #region PDF
        private void ExportToPDF()
        {
            try
            {
                // Confirm with the user before proceeding
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có muốn xuất phiếu khám bệnh này thành PDF không?",
                    "Xuất phiếu khám bệnh"
                );

                if (!result)
                    return; // User cancelled the operation

                QuestPDF.Settings.License = LicenseType.Community;

                // Create save file dialog to let the user choose where to save the PDF
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"PhieuKhamBenh_{MedicalRecord.Patient?.FullName ?? "Unknown"}_{MedicalRecord.RecordDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd")}.pdf",
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
                            GenerateMedicalRecordPdf(filePath);

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

        private void GenerateMedicalRecordPdf(string filePath)
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
                                col.Item().AlignRight().Text($"Ngày khám: {MedicalRecord.RecordDate?.ToString("dd/MM/yyyy") ?? "N/A"}")
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
                                patientCol.Item().PaddingTop(5).Text($"Họ tên: {MedicalRecord.Patient?.FullName ?? "N/A"}");
                                patientCol.Item().Text($"Ngày sinh: {(MedicalRecord.Patient?.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không có")}");
                                patientCol.Item().Text($"Giới tính: {MedicalRecord.Patient?.Gender ?? "Không có"}");
                                patientCol.Item().Text($"Số điện thoại: {MedicalRecord.Patient?.Phone ?? "Không có"}");

                                if (!string.IsNullOrEmpty(MedicalRecord.Patient?.InsuranceCode))
                                    patientCol.Item().Text($"Mã BHYT: {MedicalRecord.Patient.InsuranceCode}");

                                if (MedicalRecord.Patient?.PatientType != null)
                                    patientCol.Item().Text($"Loại khách hàng: {MedicalRecord.Patient.PatientType.TypeName}");
                            });

                        // VITAL SIGNS - Parse from formatted vital signs
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

                        // Extract non-vital sign test results for display
                        string extraTestResults = ExtractNonVitalSignResults(MedicalRecord.TestResults);

                        // TEST RESULTS
                        if (!string.IsNullOrWhiteSpace(extraTestResults))
                        {
                            column.Item().PaddingTop(20)
                                .Column(testCol =>
                                {
                                    testCol.Item().Text("KẾT QUẢ XÉT NGHIỆM").Bold();
                                    testCol.Item().Text(extraTestResults);
                                });
                        }

                        // DIAGNOSIS
                        column.Item().PaddingTop(20)
                            .Column(diagCol =>
                            {
                                diagCol.Item().Text("CHẨN ĐOÁN").Bold();
                                diagCol.Item().Text(Diagnosis ?? "");
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
                                    col.Item().PaddingTop(70).AlignCenter().Text(Doctor?.FullName ?? "Không xác định").Bold();
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
    

