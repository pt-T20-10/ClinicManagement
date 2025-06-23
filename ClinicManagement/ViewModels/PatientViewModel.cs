using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;
using ClinicManagement.Services;

namespace ClinicManagement.ViewModels
{
    public class PatientViewModel : BaseViewModel
    {
        #region Properties  
        #region DisplayProperties
        private PatientType? _SelectedType;
        public PatientType? SelectedType
        {
            get => _SelectedType;
            set
            {
                _SelectedType = value;
                OnPropertyChanged(nameof(SelectedType));

                if (SelectedType != null)
                {
                    PatientTypeId = SelectedType.PatientTypeId;
                    TypeName = SelectedType.TypeName;
                    Discount = SelectedType.Discount.ToString();
                }
            }
        }

        private int _PatientTypeId;
        public int PatientTypeId
        {
            get => _PatientTypeId;
            set
            {
                _PatientTypeId = value; OnPropertyChanged();
            }
        }

        private string _TypeName;
        public string TypeName
        {
            get => _TypeName;
            set
            {
                _TypeName = value; OnPropertyChanged();
            }
        }

        private string _Discount;
        public string Discount
        {
            get => _Discount;
            set
            {
                _Discount = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<Patient> _PatientList;
        public ObservableCollection<Patient> PatientList
        {
            get => _PatientList;
            set
            {
                _PatientList = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<PatientType> _PatientTypeList;
        public ObservableCollection<PatientType> PatientTypeList
        {
            get => _PatientTypeList;
            set
            {
                _PatientTypeList = value; OnPropertyChanged();
            }
        }
        #endregion

        #region Filter Properties

        private ObservableCollection<Patient> _AllPatients;


        private DateTime? _SelectedDate;
        public DateTime? SelectedDate
        {
            get => _SelectedDate;
            set
            {
                _SelectedDate = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi ngày
                ExecuteAutoFilter();
            }
        }

        private string _SearchText;
        public string SearchText
        {
            get => _SearchText;
            set
            {
                _SearchText = value;
                OnPropertyChanged();
                ExecuteAutoFilter();
            }
        }

        private int? _FilterPatientTypeId;
        public int? FilterPatientTypeId
        {
            get => _FilterPatientTypeId;
            set
            {
                _FilterPatientTypeId = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi loại bệnh nhân
                ExecuteAutoFilter();
            }
        }
        private bool _IsVIPSelected;
        public bool IsVIPSelected
        {
            get => _IsVIPSelected;
            set
            {
                _IsVIPSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("VIP");
                }
            }
        }

        private bool _IsAllSelected;
        public bool IsAllSelected
        {
            get => _IsAllSelected;
            set
            {
                _IsAllSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    IsVIPSelected = false;
                    FilterPatientTypeId = null; // ← Thêm dòng này để xóa filter theo loại
                    ExecuteAutoFilter();
                }
            }
        }


        private bool _IsInsuranceSelected;
        public bool IsInsuranceSelected
        {
            get => _IsInsuranceSelected;
            set
            {
                _IsInsuranceSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Bảo hiểm y tế");
                }
            }
        }

        private bool _IsNormalSelected;
        public bool IsNormalSelected
        {
            get => _IsNormalSelected;
            set
            {
                _IsNormalSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsInsuranceSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Thường");
                }
            }
        }
        #endregion
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(); }
        }

        #endregion

        #region Command

        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand AddPatientCommand { get; set; }
        public ICommand OpenPatientDetailsCommand { get; set; }

        // Filter Commands
        public ICommand SearchCommand { get; set; }
        public ICommand VIPSelectedCommand { get; set; }
        public ICommand InsuranceSelectedCommand { get; set; }
        public ICommand NormalSelectedCommand { get; set; }
        public ICommand AllSelectedCommand { get; set; }
        public ICommand ResetFiltersCommand { get; set; }

        public ICommand ExportExcelCommand { get; private set; }
        #endregion

        public PatientViewModel()
        {


            LoadData();
            InitializTypeCommands();
            InitializeFilterCommands();
            IsAllSelected = true;

            

        }
        #region Type Commands

        private void InitializTypeCommands()
        {
            AddCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                         bool  result = MessageBoxService.ShowQuestion(
                             $"Bạn có chắc muốn thêm loại bệnh nhân '{TypeName}' không?",
                             "Xác Nhận Thêm"
                              
                               );

                        if ( !result)
                            return;

                        bool isExist = DataProvider.Instance.Context.PatientTypes
                              .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() && pt.IsDeleted == false);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Loại bệnh nhân này đã tồn tại.");
                        }
                        else
                        {
                            decimal parsedDiscount = 0;
                            if (decimal.TryParse(Discount, out parsedDiscount))
                            {
                                var objectt = new PatientType()
                                {
                                    TypeName = TypeName,
                                    Discount = parsedDiscount
                                };

                                DataProvider.Instance.Context.PatientTypes.Add(objectt);
                                DataProvider.Instance.Context.SaveChanges();

                                // Refresh data after add
                                LoadData();

                                // Hiển thị thông báo thành công
                                MessageBoxService.ShowSuccess(
                                     "Đã thêm loại bệnh nhân thành công!",
                                     "Thành Công"
                                      
                                       );
                            }
                            else
                            {
                                MessageBoxService.ShowSuccess("Vui lòng nhập giảm giá hợp lệ (số thực).");
                            }
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        // Xử lý lỗi liên quan đến cơ sở dữ liệu
                        MessageBoxService.ShowError(
                             $"Không thể thêm loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
                             "Lỗi Cơ Sở Dữ Liệu"
                              
                              );
                    }
                    catch (Exception ex)
                    {
                        // Xử lý lỗi chung
                        MessageBoxService.ShowError(
                             $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                             "Lỗi"
                              
                              );
                    }
                },
                (p) => true
            );

            EditCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                         bool  result = MessageBoxService.ShowQuestion(
                             $"Bạn có chắc muốn sửa loại bệnh nhân '{TypeName}' không?",
                             "Xác Nhận Sửa"
                              
                               );

                        if ( !result)
                            return;

                        // Tìm loại bệnh nhân cần sửa
                        var patientTypeToUpdate = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại bệnh nhân cần sửa.");
                            return;
                        }

                        // Kiểm tra TypeName mới có trùng với loại khác không (ngoại trừ chính nó)
                        bool isExist = DataProvider.Instance.Context.PatientTypes
                            .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() &&
                                  pt.PatientTypeId != SelectedType.PatientTypeId);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Loại bệnh nhân này đã tồn tại.");
                            return;
                        }

                        // Kiểm tra Discount
                        if (decimal.TryParse(Discount, out decimal parsedDiscount))
                        {
                            patientTypeToUpdate.TypeName = TypeName;
                            patientTypeToUpdate.Discount = parsedDiscount;

                            DataProvider.Instance.Context.SaveChanges();

                            MessageBoxService.ShowSuccess(
                                 "Đã cập nhật loại bệnh nhân thành công!",
                                 "Thành Công"
                                  
                                   );

                            // Refresh data after edit
                            LoadData();
                        }
                        else
                        {
                            MessageBoxService.ShowWarning("Vui lòng nhập giảm giá hợp lệ (số thực).");
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        MessageBoxService.ShowError(
                             $"Không thể sửa loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
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
                },
                (p) => SelectedType != null
            );

            DeleteCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                         bool  result = MessageBoxService.ShowQuestion(
                            $"Bạn có chắc muốn xóa loại bệnh nhân '{SelectedType?.TypeName}' không?",
                            "Xác Nhận Xóa"
                             
                              );

                        if ( !result)
                            return;

                        // Tìm đối tượng cần xóa
                        var patientTypeToDelete = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại bệnh nhân để xóa.");
                            return;
                        }

                        // Đánh dấu IsDeleted = true
                        patientTypeToDelete.IsDeleted = true;

                        DataProvider.Instance.Context.SaveChanges();

                        MessageBoxService.ShowSuccess(
                            "Đã xóa (ẩn) loại bệnh nhân thành công.",
                            "Xóa Thành Công"
                             
                              );

                        // Refresh data after delete
                        LoadData();
                        SelectedType = null;
                    }
                    catch (Exception ex)
                    {
                        MessageBoxService.ShowError(
                            $"Đã xảy ra lỗi khi xóa: {ex.Message}",
                            "Lỗi"
                             
                             );
                    }
                },
                (p) => SelectedType != null
            );
            AddPatientCommand = new RelayCommand<object>(
                   (p) =>
                   {
                       // Mở cửa sổ thêm bệnh nhân mới
                       AddPatientWindow addPatientWindow = new AddPatientWindow();
                       addPatientWindow.ShowDialog();
                       // Refresh data after adding a new patient
                       LoadData();
                   },
                   (p) => true
               );

            // In the constructor, initialize the command:
            ExportExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => PatientList != null && PatientList.Count > 0
            );
        }

        #endregion

        #region Data Loading Methods

        public void LoadData()
        {
            _AllPatients = new ObservableCollection<Patient>(
                DataProvider.Instance.Context.Patients
                .Include(p => p.PatientType)
                .Where(p => p.IsDeleted != true)
                .ToList()
            );

            PatientList = new ObservableCollection<Patient>(_AllPatients);

            PatientTypeList = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => (bool)!pt.IsDeleted)
                .ToList()
            );

            OpenPatientDetailsCommand = new RelayCommand<Patient>(
                (p) => OpenPatientDetails(p),
                (p) => p != null
                );
                }

        private void OpenPatientDetails(Patient patient)
        {
            if (patient == null) return;

            var viewModel = new PatientDetailsWindowViewModel();
            viewModel.Patient = patient;  // This will now properly set all derived properties
            
            var detailsWindow = new PatientDetailsWindow
            {
                DataContext = viewModel
            };
            detailsWindow.ShowDialog();
            LoadData();  // Refresh data after the details window is closed
        }

        #endregion

        #region Filter Methods

        private void InitializeFilterCommands()
        {
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );
            AllSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteAllFilter(),
                (p) => true
            );

            VIPSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteVIPFilter(),
                (p) => true
            );

            InsuranceSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteInsuranceFilter(),
                (p) => true
            );

            NormalSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteNormalFilter(),
                (p) => true
            );

            ResetFiltersCommand = new RelayCommand<object>(
                (p) => ExecuteResetFilters(),
                (p) => true
            );
        }

        private void ExecuteSearch()
        {
            ApplyFilters();
        }
        private void ExecuteAllFilter()
        {
            ApplyFilters();
        }

        private void ExecuteVIPFilter()
        {
            IsVIPSelected = true;
        }

        private void ExecuteInsuranceFilter()
        {
            IsInsuranceSelected = true;
        }

        private void ExecuteNormalFilter()
        {
            IsNormalSelected = true;
        }

        private void ExecuteAutoFilter()
        {
            // Lọc tự động khi thay đổi ngày hoặc loại bệnh nhân
            ApplyFilters();
        }

        private void ExecuteResetFilters()
        {
            SelectedDate = null;
            SearchText = string.Empty;
            FilterPatientTypeId = null;
            IsVIPSelected = false;
            IsInsuranceSelected = false;
            IsNormalSelected = false;

            PatientList = new ObservableCollection<Patient>(_AllPatients);
        }

        private void ApplyFilters()
        {
            if (_AllPatients == null || _AllPatients.Count == 0)
            {
                PatientList = new ObservableCollection<Patient>();
                return;
            }

            var filteredList = _AllPatients.AsEnumerable();

            // Lọc theo ngày tạo
            if (SelectedDate.HasValue)
            {
                filteredList = filteredList.Where(p => p.CreatedAt?.Date == SelectedDate.Value.Date);
            }

            // Lọc theo loại bệnh nhân
            if (FilterPatientTypeId.HasValue)
            {
                filteredList = filteredList.Where(p => p.PatientTypeId == FilterPatientTypeId.Value);
            }

            // Lọc theo tên hoặc mã bảo hiểm
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = filteredList.Where(p =>
                    (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(p.InsuranceCode) && p.InsuranceCode.ToLower().Contains(searchTerm))
                );
            }

            PatientList = new ObservableCollection<Patient>(filteredList.ToList());
        }

        private int? GetPatientTypeIdByName(string typeName)
        {
            var patientType = PatientTypeList?.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals(typeName.Trim(), StringComparison.OrdinalIgnoreCase) == true);
            return patientType?.PatientTypeId;
        }

        #endregion

        private void ExportToExcel()
        {
            try
            {
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"DanhSachBenhNhan_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                var worksheet = workbook.Worksheets.Add("Danh sách bệnh nhân");

                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Add title (merged cells)
                                worksheet.Cell(1, 1).Value = "DANH SÁCH BỆNH NHÂN";
                                var titleRange = worksheet.Range(1, 1, 1, 9);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 9);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added title
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Add headers (with spacing of 4 cells from title)
                                int headerRow = 6; // Row 6 (leaving 3 blank rows after title)
                                worksheet.Cell(headerRow, 1).Value = "Mã BN";
                                worksheet.Cell(headerRow, 2).Value = "Bảo hiểm y tế";
                                worksheet.Cell(headerRow, 3).Value = "Họ và tên";
                                worksheet.Cell(headerRow, 4).Value = "Ngày sinh";
                                worksheet.Cell(headerRow, 5).Value = "Giới tính";
                                worksheet.Cell(headerRow, 6).Value = "Loại khách hàng";
                                worksheet.Cell(headerRow, 7).Value = "Điện thoại";
                                worksheet.Cell(headerRow, 8).Value = "Địa chỉ";
                                worksheet.Cell(headerRow, 9).Value = "Ngày đăng kí";

                                // Style header row
                                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
                                headerRange.Style.Font.Bold = true;
                                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add borders to header
                                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                // Report progress: 20% - Headers added
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(20));

                                // Add data
                                int row = headerRow + 1; // Start data from next row after header
                                int totalPatients = PatientList.Count;

                                // Create data range (to apply borders later)
                                var dataStartRow = row;

                                for (int i = 0; i < totalPatients; i++)
                                {
                                    var patient = PatientList[i];

                                    worksheet.Cell(row, 1).Value = patient.PatientId;
                                    worksheet.Cell(row, 2).Value = patient.InsuranceCode ?? "";
                                    worksheet.Cell(row, 3).Value = patient.FullName ?? "";

                                    if (patient.DateOfBirth.HasValue)
                                        worksheet.Cell(row, 4).Value = patient.DateOfBirth.Value.ToString("dd/MM/yyyy");
                                    else
                                        worksheet.Cell(row, 4).Value = "";

                                    worksheet.Cell(row, 5).Value = patient.Gender ?? "";
                                    worksheet.Cell(row, 6).Value = patient.PatientType?.TypeName ?? "";
                                    worksheet.Cell(row, 7).Value = patient.Phone ?? "";
                                    worksheet.Cell(row, 8).Value = patient.Address ?? "";

                                    if (patient.CreatedAt.HasValue)
                                        worksheet.Cell(row, 9).Value = patient.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm");
                                    else
                                        worksheet.Cell(row, 9).Value = "";

                                    row++;

                                    // Update progress based on percentage of patients processed
                                    int progressValue = 20 + (i * 60 / totalPatients);
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));

                                    // Add a small delay to make the progress visible
                                    Thread.Sleep(30);
                                }

                                // Report progress: 80% - Data added
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Apply borders to the data range
                                if (totalPatients > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, 1, row - 1, 9);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                    // Center-align ID, Date, Gender columns
                                    worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    worksheet.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                }

                                // Add total row
                                worksheet.Cell(row + 1, 2).Value = "Tổng số:";
                                worksheet.Cell(row + 1, 3).Value = totalPatients;
                                worksheet.Cell(row + 1, 3).Style.Font.Bold = true;

                                // Auto-fit columns
                                worksheet.Columns().AdjustToContents();

                                // Set minimum widths for better readability
                                worksheet.Column(1).Width = 10; // ID
                                worksheet.Column(3).Width = 25; // Name
                                worksheet.Column(6).Width = 15; // Patient Type
                                worksheet.Column(8).Width = 50; // Address

                                // Report progress: 90% - Formatting complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - File saved
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));

                                // Small delay to show 100%
                                Thread.Sleep(300);

                                // Close progress dialog
                                Application.Current.Dispatcher.Invoke(() => progressDialog.Close());

                                // Show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất danh sách bệnh nhân thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                         
                                          );
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Close progress dialog on error
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();

                                MessageBoxService.ShowError(
                                    $"Lỗi khi xuất Excel: {ex.Message}",
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
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }




    }

}
