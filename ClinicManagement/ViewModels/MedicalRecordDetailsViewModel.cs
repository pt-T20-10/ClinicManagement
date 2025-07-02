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
    /// <summary>
    /// ViewModel quản lý chi tiết hồ sơ bệnh án
    /// </summary>
    public class MedicalRecordDetailsViewModel : BaseViewModel
    {
        #region Properties
        // Tài khoản người dùng hiện tại
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                CheckEditPermission(); // Kiểm tra quyền chỉnh sửa khi thay đổi tài khoản
            }
        }

        // Hồ sơ bệnh án đang xem/chỉnh sửa
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

        // Thông tin bệnh nhân
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

        // Thông tin bác sĩ
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

        // Ngày tạo hồ sơ
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

        // Quyền chỉnh sửa
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

        // Các trường thông tin hồ sơ bệnh án
        // Chẩn đoán
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

        // Đơn thuốc
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

        // Kết quả xét nghiệm
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

        // Lời dặn của bác sĩ
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

        // Các chỉ số sinh hiệu
        // Mạch
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

        // Nhịp thở
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

        // Nhiệt độ
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

        // Cân nặng
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

        // Huyết áp tâm thu
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

        // Huyết áp tâm trương
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

        // Tham chiếu đến cửa sổ
        private Window _window;
        #endregion

        #region Commands
        // Lệnh lưu hồ sơ
        public ICommand SaveRecordCommand { get; set; }
        // Lệnh đặt lại hồ sơ
        public ICommand ResetRecordCommand { get; set; }
        // Lệnh khi cửa sổ được tải
        public ICommand LoadedWindowCommand { get; set; }
        // Lệnh xuất PDF
        public ICommand ExportPDFCommand { get; set; }
        #endregion

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public MedicalRecordDetailsViewModel()
        {
            InitializeCommands();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null && mainVM.CurrentAccount != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

        }

        /// <summary>
        /// Constructor với hồ sơ bệnh án
        /// </summary>
        /// <param name="record">Hồ sơ bệnh án cần hiển thị</param>
        public MedicalRecordDetailsViewModel(MedicalRecord record)
        {
            InitializeCommands();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null && mainVM.CurrentAccount != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

            MedicalRecord = record;
            LoadMedicalRecordData(); // Tải dữ liệu hồ sơ bệnh án
        }

        /// <summary>
        /// Khởi tạo các lệnh
        /// </summary>
        private void InitializeCommands()
        {
            // Lệnh khi cửa sổ được tải
            LoadedWindowCommand = new RelayCommand<Window>(
                p =>
                {
                    _window = p;
                    if (MedicalRecord != null)
                    {
                        LoadMedicalRecordData(); // Tải dữ liệu khi cửa sổ được mở
                    }
                },
                p => true
            );

            // Lệnh lưu hồ sơ
            SaveRecordCommand = new RelayCommand<object>(
                p => SaveMedicalRecord(),
                p => CanEdit && MedicalRecord != null && !string.IsNullOrWhiteSpace(Diagnosis)
            );

            // Lệnh đặt lại hồ sơ
            ResetRecordCommand = new RelayCommand<object>(
                p => ResetFields(),
                p => CanEdit && MedicalRecord != null
            );

            // Lệnh xuất PDF
            ExportPDFCommand = new RelayCommand<object>(
                  p => ExportToPDF(),
                  p => MedicalRecord != null
              );
        }

        /// <summary>
        /// Tải dữ liệu hồ sơ bệnh án
        /// </summary>
        private void LoadMedicalRecordData()
        {
            if (MedicalRecord == null) return;

            try
            {
                // Đảm bảo có đầy đủ thông tin bệnh án và các đối tượng liên quan
                MedicalRecord = DataProvider.Instance.Context.MedicalRecords
                    .Include(m => m.Doctor)
                    .Include(m => m.Patient)
                    .FirstOrDefault(m => m.RecordId == MedicalRecord.RecordId);

                if (MedicalRecord == null) return;

                // Tải thông tin bệnh nhân
                SelectedPatient = MedicalRecord.Patient;

                // Tải thông tin bác sĩ
                Doctor = MedicalRecord.Doctor;

                // Tải chi tiết hồ sơ bệnh án
                RecordDate = MedicalRecord.RecordDate;
                Diagnosis = MedicalRecord.Diagnosis ?? "";
                Prescription = MedicalRecord.Prescription ?? "";
                TestResults = MedicalRecord.TestResults ?? "";
                DoctorAdvice = MedicalRecord.DoctorAdvice ?? "";

                // Phân tích các chỉ số sinh hiệu từ dữ liệu kết quả xét nghiệm
                ParseVitalSigns();

                // Kiểm tra quyền chỉnh sửa của người dùng hiện tại
                CheckEditPermission();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu hồ sơ bệnh án: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Kiểm tra quyền chỉnh sửa hồ sơ bệnh án
        /// </summary>
        private void CheckEditPermission()
        {
            // Mặc định không ai có quyền chỉnh sửa
            CanEdit = false;

            // Kiểm tra xem tài khoản và hồ sơ bệnh án có tồn tại không
            if (CurrentAccount == null || MedicalRecord == null) return;

            // Kiểm tra xem người dùng hiện tại có phải là bác sĩ tạo hồ sơ này không
            if (CurrentAccount.StaffId.HasValue &&
                MedicalRecord.StaffId == CurrentAccount.StaffId.Value)
            {
                CanEdit = true;
            }
            // Admin cũng có thể chỉnh sửa
            else if (CurrentAccount.Role != null &&
                     CurrentAccount.Role.ToLower().Contains("admin"))
            {
                CanEdit = true;
            }
            // Nếu người dùng có vai trò bác sĩ và là bác sĩ trong hồ sơ
            else if (CurrentAccount.StaffId.HasValue &&
                     CurrentAccount.Role != null &&
                     CurrentAccount.Role.ToLower().Contains("bác sĩ") &&
                     MedicalRecord.Doctor != null &&
                     MedicalRecord.Doctor.StaffId == CurrentAccount.StaffId.Value)
            {
                CanEdit = true;
            }

            // Cập nhật trạng thái của các lệnh
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Đặt lại các trường dữ liệu về trạng thái ban đầu
        /// </summary>
        private void ResetFields()
        {
            try
            {
                // Tải lại dữ liệu từ cơ sở dữ liệu để đặt lại các thay đổi
                LoadMedicalRecordData();

                MessageBoxService.ShowSuccess("Đã làm mới dữ liệu hồ sơ bệnh án.",
                    "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi làm mới hồ sơ bệnh án: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Thiết lập hồ sơ bệnh án để hiển thị
        /// </summary>
        /// <param name="record">Hồ sơ bệnh án cần hiển thị</param>
        public void SetRecord(MedicalRecord record)
        {
            MedicalRecord = record;
            LoadMedicalRecordData();
        }

        /// <summary>
        /// Phân tích các chỉ số sinh hiệu từ chuỗi kết quả xét nghiệm
        /// </summary>
        private void ParseVitalSigns()
        {
            if (MedicalRecord == null || string.IsNullOrWhiteSpace(MedicalRecord.TestResults))
            {
                // Đặt giá trị mặc định cho các chỉ số sinh hiệu
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
                // Phân tích các chỉ số sinh hiệu từ chuỗi kết quả xét nghiệm
                string testResults = MedicalRecord.TestResults;

                // Phân tích mạch
                Pulse = ExtractValue(testResults, "Mạch: (\\d+) lần/ph");

                // Phân tích nhịp thở
                Respiration = ExtractValue(testResults, "Nhịp thở: (\\d+) lần/ph");

                // Phân tích nhiệt độ
                Temperature = ExtractValue(testResults, "Nhiệt độ: ([\\d.]+)°C");

                // Phân tích cân nặng
                Weight = ExtractValue(testResults, "Cân nặng: ([\\d.]+)kg");

                // Phân tích huyết áp
                // Cải thiện phương thức để xử lý huyết áp
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
                MessageBoxService.ShowError($"Lỗi khi phân tích thông số sinh hiệu: {ex.Message}", "Lỗi");
                Pulse = "";
                Respiration = "";
                Temperature = "";
                Weight = "";
                SystolicPressure = "";
                DiastolicPressure = "";
            }
        }

        /// <summary>
        /// Trích xuất giá trị từ chuỗi sử dụng regex
        /// </summary>
        /// <param name="input">Chuỗi đầu vào</param>
        /// <param name="pattern">Mẫu regex</param>
        /// <returns>Giá trị được trích xuất</returns>
        private string ExtractValue(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            return "";
        }

        /// <summary>
        /// Định dạng các thông số sinh hiệu thành chuỗi
        /// </summary>
        /// <returns>Chuỗi các thông số sinh hiệu đã định dạng</returns>
        private string FormatVitalSigns()
        {
            List<string> vitalSigns = new List<string>();

            // Thêm mạch nếu có
            if (!string.IsNullOrWhiteSpace(Pulse) && int.TryParse(Pulse, out _))
            {
                vitalSigns.Add($"Mạch: {Pulse} lần/ph");
            }

            // Thêm nhịp thở nếu có
            if (!string.IsNullOrWhiteSpace(Respiration) && int.TryParse(Respiration, out _))
            {
                vitalSigns.Add($"Nhịp thở: {Respiration} lần/ph");
            }

            // Thêm nhiệt độ nếu có
            if (!string.IsNullOrWhiteSpace(Temperature) && decimal.TryParse(Temperature, out _))
            {
                vitalSigns.Add($"Nhiệt độ: {Temperature}°C");
            }

            // Thêm cân nặng nếu có
            if (!string.IsNullOrWhiteSpace(Weight) && decimal.TryParse(Weight, out _))
            {
                vitalSigns.Add($"Cân nặng: {Weight}kg");
            }

            // Thêm huyết áp nếu có
            if (!string.IsNullOrWhiteSpace(SystolicPressure) || !string.IsNullOrWhiteSpace(DiastolicPressure))
            {
                string bloodPressure = CombineBloodPressure(SystolicPressure, DiastolicPressure);
                if (!string.IsNullOrWhiteSpace(bloodPressure))
                {
                    vitalSigns.Add($"Huyết áp: {bloodPressure}");
                }
            }

            // Nối tất cả các chỉ số sinh hiệu bằng dấu phẩy
            return vitalSigns.Count > 0 ? string.Join(", ", vitalSigns) + "." : "";
        }

        /// <summary>
        /// Kết hợp huyết áp tâm thu và tâm trương thành một chuỗi
        /// </summary>
        /// <param name="systolic">Huyết áp tâm thu</param>
        /// <param name="diastolic">Huyết áp tâm trương</param>
        /// <returns>Chuỗi huyết áp đã định dạng</returns>
        private string CombineBloodPressure(string systolic, string diastolic)
        {
            if (string.IsNullOrWhiteSpace(systolic) && string.IsNullOrWhiteSpace(diastolic))
                return null;

            return $"{systolic ?? "?"}/{diastolic ?? "?"} mmHg";
        }

        /// <summary>
        /// Lưu thông tin hồ sơ bệnh án vào cơ sở dữ liệu
        /// </summary>
        private void SaveMedicalRecord()
        {
            try
            {
                if (MedicalRecord == null) return;

                // Kiểm tra quyền chỉnh sửa
                if (!CanEdit)
                {
                    MessageBoxService.ShowWarning("Bạn không có quyền chỉnh sửa hồ sơ bệnh án này!",
                        "Quyền truy cập bị từ chối");
                    return;
                }

                // Tìm hồ sơ bệnh án trong cơ sở dữ liệu
                var recordToUpdate = DataProvider.Instance.Context.MedicalRecords
                    .FirstOrDefault(m => m.RecordId == MedicalRecord.RecordId);

                if (recordToUpdate == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy hồ sơ bệnh án trong cơ sở dữ liệu!",
                        "Lỗi");
                    return;
                }

                // Định dạng các thông số sinh hiệu
                string formattedVitalSigns = FormatVitalSigns();

                // Trích xuất kết quả xét nghiệm không phải thông số sinh hiệu
                string existingTestResults = ExtractNonVitalSignResults(recordToUpdate.TestResults);

                // Kết hợp các thông số sinh hiệu với kết quả xét nghiệm hiện có
                string combinedTestResults = formattedVitalSigns;
                if (!string.IsNullOrWhiteSpace(existingTestResults))
                {
                    combinedTestResults += formattedVitalSigns.Length > 0 ? "\n\n" + existingTestResults : existingTestResults;
                }

                // Cập nhật các trường của hồ sơ bệnh án
                recordToUpdate.Diagnosis = Diagnosis?.Trim();
                recordToUpdate.Prescription = Prescription?.Trim();
                recordToUpdate.TestResults = combinedTestResults;  // Sử dụng kết quả đã kết hợp
                recordToUpdate.DoctorAdvice = DoctorAdvice?.Trim();

                // Lưu thay đổi vào cơ sở dữ liệu
                DataProvider.Instance.Context.SaveChanges();

                MessageBoxService.ShowSuccess("Đã lưu thông tin hồ sơ bệnh án thành công!");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lưu hồ sơ bệnh án: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Trích xuất phần kết quả xét nghiệm không phải thông số sinh hiệu
        /// </summary>
        /// <param name="testResults">Chuỗi kết quả xét nghiệm gốc</param>
        /// <returns>Phần kết quả xét nghiệm không bao gồm thông số sinh hiệu</returns>
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
        /// <summary>
        /// Xuất hồ sơ bệnh án ra file PDF
        /// </summary>
        private void ExportToPDF()
        {
            try
            {
                // Xác nhận với người dùng trước khi tiếp tục
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có muốn xuất phiếu khám bệnh này thành PDF không?",
                    "Xuất phiếu khám bệnh"
                );

                if (!result)
                    return; // Người dùng hủy thao tác

                // Cấu hình giấy phép QuestPDF
                QuestPDF.Settings.License = LicenseType.Community;

                // Tạo hộp thoại lưu file để cho phép người dùng chọn nơi lưu PDF
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

                    // Tạo và hiển thị hộp thoại tiến trình
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Tạo PDF trong luồng nền với báo cáo tiến trình
                    Task.Run(() =>
                    {
                        try
                        {
                            // Báo cáo tiến trình: 10% - Bắt đầu
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));
                            Thread.Sleep(100); // Độ trễ nhỏ để hiển thị

                            // Báo cáo tiến trình: 30% - Chuẩn bị dữ liệu
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));
                            Thread.Sleep(100); // Độ trễ nhỏ để hiển thị

                            // Báo cáo tiến trình: 50% - Tạo tài liệu
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                            // Tạo tài liệu PDF
                            GenerateMedicalRecordPdf(filePath);

                            // Báo cáo tiến trình: 90% - Lưu file
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));
                            Thread.Sleep(100); // Độ trễ nhỏ để hiển thị

                            // Báo cáo tiến trình: 100% - Hoàn thành
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                            Thread.Sleep(300); // Hiển thị 100% trong thời gian ngắn

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Đóng hộp thoại tiến trình
                                progressDialog.Close();

                                // Hiển thị thông báo thành công
                                MessageBoxService.ShowSuccess(
                                    $"Đã xuất phiếu khám bệnh thành công!\nĐường dẫn: {filePath}",
                                    "Xuất phiếu khám"
                                );

                                // Hỏi người dùng có muốn mở file PDF không
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

                    // Hiển thị hộp thoại - sẽ chặn cho đến khi hộp thoại đóng
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

        /// <summary>
        /// Tạo file PDF cho hồ sơ bệnh án
        /// </summary>
        /// <param name="filePath">Đường dẫn lưu file PDF</param>
        private void GenerateMedicalRecordPdf(string filePath)
        {
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    // Thiết lập kích thước và lề trang
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(column =>
                    {
                        // PHẦN ĐẦU TRANG
                        column.Item().Row(row =>
                        {
                            // Thông tin phòng khám
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("PHÒNG KHÁM ABC")
                                    .FontSize(18).Bold();
                                col.Item().Text("Địa chỉ: 123 Đường 456, Quận 789, TP.XYZ")
                                    .FontSize(10);
                                col.Item().Text("SĐT: 028.1234.5678 | Email: email@gmail.com")
                                    .FontSize(10);
                            });

                            // Thông tin ngày khám
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().AlignRight().Text($"Ngày khám: {MedicalRecord.RecordDate?.ToString("dd/MM/yyyy") ?? "N/A"}")
                                    .FontSize(10);
                            });
                        });

                        // TIÊU ĐỀ
                        column.Item().PaddingVertical(20)
                            .Text("PHIẾU KHÁM BỆNH")
                            .FontSize(16).Bold()
                            .AlignCenter();

                        // THÔNG TIN BỆNH NHÂN
                        column.Item().PaddingTop(10)
                            .Row(patientRow =>
                            {
                                // Cột trái
                                patientRow.RelativeItem().Column(leftCol =>
                                {
                                    leftCol.Item().Text("THÔNG TIN BỆNH NHÂN").Bold().FontSize(12);
                                    leftCol.Item().PaddingTop(5).Text($"Họ tên: {MedicalRecord.Patient?.FullName ?? "N/A"}");
                                    leftCol.Item().Text($"Ngày sinh: {(MedicalRecord.Patient?.DateOfBirth?.ToString("dd/MM/yyyy") ?? "Không có")}");
                                    leftCol.Item().Text($"Giới tính: {MedicalRecord.Patient?.Gender ?? "Không có"}");
                                });

                                // Cột phải
                                patientRow.RelativeItem().Column(rightCol =>
                                {
                                    rightCol.Item().Text(" ").Bold(); // Placeholder trống để căn chỉnh với tiêu đề bên trái
                                    rightCol.Item().PaddingTop(5).Text($"Số điện thoại: {MedicalRecord.Patient?.Phone ?? "Không có"}");

                                    if (!string.IsNullOrEmpty(MedicalRecord.Patient?.InsuranceCode))
                                        rightCol.Item().Text($"Mã BHYT: {MedicalRecord.Patient.InsuranceCode}");
                                    else
                                        rightCol.Item().Text("Mã BHYT: Không có");

                                    if (MedicalRecord.Patient?.PatientType != null)
                                        rightCol.Item().Text($"Loại khách hàng: {MedicalRecord.Patient.PatientType.TypeName}");
                                    else
                                        rightCol.Item().Text("Loại khách hàng: Không có");
                                });
                            });

                        // DẤU HIỆU SINH TỒN
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

                        // Trích xuất kết quả xét nghiệm không phải dấu hiệu sinh tồn
                        string extraTestResults = ExtractNonVitalSignResults(MedicalRecord.TestResults);

                        // KẾT QUẢ XÉT NGHIỆM
                        if (!string.IsNullOrWhiteSpace(extraTestResults))
                        {
                            column.Item().PaddingTop(20)
                                .Column(testCol =>
                                {
                                    testCol.Item().Text("KẾT QUẢ XÉT NGHIỆM").Bold();
                                    testCol.Item().Text(extraTestResults);
                                });
                        }

                        // CHẨN ĐOÁN
                        column.Item().PaddingTop(20)
                            .Column(diagCol =>
                            {
                                diagCol.Item().Text("CHẨN ĐOÁN").Bold();
                                diagCol.Item().Text(Diagnosis ?? "");
                            });

                        // ĐƠN THUỐC
                        if (!string.IsNullOrWhiteSpace(Prescription))
                        {
                            column.Item().PaddingTop(20)
                                .Column(presCol =>
                                {
                                    presCol.Item().Text("ĐƠN THUỐC").Bold();
                                    presCol.Item().Text(Prescription);
                                });
                        }

                        // LỜI DẶN CỦA BÁC SĨ
                        if (!string.IsNullOrWhiteSpace(DoctorAdvice))
                        {
                            column.Item().PaddingTop(20)
                                .Column(adviceCol =>
                                {
                                    adviceCol.Item().Text("LỜI DẶN CỦA BÁC SĨ").Bold();
                                    adviceCol.Item().Text(DoctorAdvice);
                                });
                        }

                        // CHỮ KÝ BÁC SĨ
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
