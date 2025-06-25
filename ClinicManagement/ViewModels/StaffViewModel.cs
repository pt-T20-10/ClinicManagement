using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Threading;
using ClinicManagement.Services;

namespace ClinicManagement.ViewModels
{
    public class StaffViewModel : BaseViewModel
    {
        #region Properties

        #region DisplayProperties
        private ObservableCollection<Staff> _DoctorList;
        public ObservableCollection<Staff> DoctorList
        {
            get => _DoctorList;
            set
            {
                _DoctorList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DoctorSpecialty> _ListSpecialty;
        public ObservableCollection<DoctorSpecialty> ListSpecialty
        {
            get => _ListSpecialty;
            set
            {
                _ListSpecialty = value;
                OnPropertyChanged();
            }
        }

        private DoctorSpecialty _SelectedItem;
        public DoctorSpecialty SelectedItem
        {
            get => _SelectedItem;
            set
            {
                _SelectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));

                if (SelectedItem != null)
                {
                    SpecialtyName = SelectedItem.SpecialtyName;
                    Description = SelectedItem.Description;
                }
            }
        }
        private DoctorSpecialty _SelectedSpecialty;
        public DoctorSpecialty SelectedSpecialty
        {
            get => _SelectedSpecialty;
            set
            {
                _SelectedSpecialty = value;
                OnPropertyChanged();

                if (value != null)
                {
                    // Handle the "All" option (SpecialtyId = -1)
                    if (value.SpecialtyId == -1)
                    {
                        SelectedSpecialtyId = null; // Set to null to indicate no filtering by specialty
                    }
                    else
                    {
                        // Normal case: Set the selected specialty ID
                        SelectedSpecialtyId = value.SpecialtyId;
                    }
                }
                else
                {
                    SelectedSpecialtyId = null;
                }
            }
        }
        private int? _SelectedSpecialtyId;
        public int? SelectedSpecialtyId
        {
            get => _SelectedSpecialtyId;
            set
            {
                _SelectedSpecialtyId = value;
                OnPropertyChanged();

                ExecuteSearch();
            }
        }
        private Staff _selectedDoctor;
        public Staff SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
            }
        }

        private string _SpecialtyName;
        public string SpecialtyName
        {
            get => _SpecialtyName;
            set
            {
                _SpecialtyName = value;
                OnPropertyChanged();
            }
        }

        private string _Description;
        public string Description
        {
            get => _Description;
            set
            {
                _Description = value;
                OnPropertyChanged();
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
                ExecuteSearch();
            }
        }

        // Add to your properties region
        private ObservableCollection<Role> _roleList;
        public ObservableCollection<Role> RoleList
        {
            get => _roleList;
            set
            {
                _roleList = value;
                OnPropertyChanged();
            }
        }

        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();

                if (value != null)
                {
                    // Handle the "All" option (RoleId = -1)
                    if (value.RoleId == -1)
                    {
                        SelectedRoleId = null; // Set to null to indicate no filtering by role
                        IsSpecialtyVisible = false; // Hide specialty when "All" is selected
                    }
                    else
                    {
                        // Normal case: Update filtering by role
                        SelectedRoleId = value.RoleId;

                        // Check if the selected role is a doctor (assuming "Bác sĩ" is the role name for doctors)
                        IsSpecialtyVisible = value.RoleName.Contains("Bác sĩ");
                    }
                }
                else
                {
                    SelectedRoleId = null;
                    IsSpecialtyVisible = false;
                }
            }
        }

        private int? _selectedRoleId;
        public int? SelectedRoleId
        {
            get => _selectedRoleId;
            set
            {
                _selectedRoleId = value;
                OnPropertyChanged();
                ExecuteSearch();
            }
        }

        // Add property to control Specialty ComboBox visibility
        private bool _isSpecialtyVisible = false;
        public bool IsSpecialtyVisible
        {
            get => _isSpecialtyVisible;
            set
            {
                _isSpecialtyVisible = value;
                OnPropertyChanged();

                // If specialty shouldn't be visible, clear the selection
                if (!value)
                {
                    SelectedSpecialty = null;
                }
            }
        }
        #endregion

        private ObservableCollection<Staff> _allStaffs;
        #endregion

        #region Commands
     
        
        // Doctor Commands
        public ICommand AddStaffCommand { get; set; }
        public ICommand EditDoctorCommand { get; set; }
        public ICommand DeleteDoctorCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ResetFiltersCommand { get; set; }

        public ICommand OpenDoctorDetailsCommand { get; set; }
        public ICommand ExportExcelCommand { get;  set; }
        // Specialty Commands
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        #endregion

        public StaffViewModel()
        {
                    LoadData();
               

            InitializeCommands();
        }

        #region Methods
        private void InitializeCommands()
        {
            ResetFiltersCommand = new RelayCommand<object>(
               (p) => ExecuteResetFilters(),
               (p) => true
           );
            // Doctor Commands
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );

            AddStaffCommand = new RelayCommand<object>(
                (p) =>
                {
                    // Open window to add new doctor
                    var addDoctorWindow = new AddDoctorWindow();
                    addDoctorWindow.ShowDialog();
                    // Refresh data after adding new doctor
                    LoadData();
                },
                (p) => true
            );

            OpenDoctorDetailsCommand = new RelayCommand<Staff>(
                (p) => OpenDoctorDetails(p),
                (p) => p != null
            );
            // In the InitializeCommands method
            ExportExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => DoctorList != null && DoctorList.Count > 0
            );

            // Specialty Commands
            AddCommand = new RelayCommand<object>(
                (p) => AddSpecialty(),
                (p) => !string.IsNullOrEmpty(SpecialtyName)
            );

            EditCommand = new RelayCommand<object>(
                (p) => EditSpecialty(),
                (p) => SelectedItem != null && !string.IsNullOrEmpty(SpecialtyName)
            );

            DeleteCommand = new RelayCommand<object>(
                (p) => DeleteSpecialty(),
                (p) => SelectedItem != null
            );
        }
        private void OpenDoctorDetails(Staff doctor)
        {
            if (doctor == null) return;

            var detailsWindow = new DoctorDetailsWindow
            {
                DataContext = new DoctorDetailsWindowViewModel { Doctor = doctor }
            };
            detailsWindow.ShowDialog();
            LoadData(); // Refresh data after closing details window
        }
        // Modify the LoadData method to include roles
        public void LoadData()
        {
            try
            {
                // Use AsNoTracking for better performance when you're just reading data
                _allStaffs = new ObservableCollection<Staff>(
                    DataProvider.Instance.Context.Staffs
                        .AsNoTracking()
                        .Include(d => d.Specialty)
                        .Include(d => d.Role)
                        .Where(d => (bool)!d.IsDeleted)
                        .ToList()
                        .Select(d => {
                            // Handle any potential null string properties
                            d.FullName = d.FullName ?? "";
                            d.Phone = d.Phone ?? "";
                            d.Email = d.Email ?? "";
                            d.Schedule = d.Schedule ?? "";
                            d.Address = d.Address ?? "";
                            d.CertificateLink = d.CertificateLink ?? "";

                            return d;
                        })
                );

                DoctorList = new ObservableCollection<Staff>(_allStaffs);
                // Load specialties with "All" option
                var specialties = DataProvider.Instance.Context.DoctorSpecialties
                    .AsNoTracking()
                    .Where(s => (bool)!s.IsDeleted)
                    .ToList()
                    .Select(s => {
                        s.SpecialtyName = s.SpecialtyName ?? "";
                        s.Description = s.Description ?? "";
                        return s;
                    })
                    .ToList();

                // Add "All Specialties" option at the beginning of the list
                specialties.Insert(0, new DoctorSpecialty
                {
                    SpecialtyId = -1,
                    SpecialtyName = "-- Tất cả chuyên khoa --"
                });

                ListSpecialty = new ObservableCollection<DoctorSpecialty>(specialties);

                // Load roles with "All" option
                var roles = DataProvider.Instance.Context.Roles
                    .AsNoTracking()
                    .Where(r => (bool)!r.IsDeleted)
                    .ToList()
                    .Select(r => {
                        r.RoleName = r.RoleName ?? "";
                        r.Description = r.Description ?? "";
                        return r;
                    })
                    .ToList();

                // Add "All Roles" option at the beginning of the list
                roles.Insert(0, new Role
                {
                    RoleId = -1,
                    RoleName = "-- Tất cả vai trò --"
                });

                RoleList = new ObservableCollection<Role>(roles);
                
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi");

                // Initialize with empty collections to avoid null reference exceptions
                _allStaffs = new ObservableCollection<Staff>();
                DoctorList = new ObservableCollection<Staff>();
                ListSpecialty = new ObservableCollection<DoctorSpecialty>();
                RoleList = new ObservableCollection<Role>();
            }
        }
        private void ExecuteResetFilters()
        {
            SearchText = string.Empty;

            // Select "All" items in the ComboBoxes instead of setting to null
            SelectedRole = RoleList?.FirstOrDefault(r => r.RoleId == -1);
            SelectedSpecialty = IsSpecialtyVisible ?
                ListSpecialty?.FirstOrDefault(s => s.SpecialtyId == -1) : null;

            IsSpecialtyVisible = false;

            DoctorList = new ObservableCollection<Staff>(_allStaffs);
        }
        private void ExecuteSearch()
        {
            if (_allStaffs == null || _allStaffs.Count == 0)
            {
                DoctorList = new ObservableCollection<Staff>();
                return;
            }

            var filteredList = _allStaffs.AsEnumerable();

            // Filter by specialty if selected
            if (SelectedSpecialtyId.HasValue)
                filteredList = filteredList.Where(d => d.SpecialtyId == SelectedSpecialtyId && (bool)!d.IsDeleted);

            // Filter by role if selected
            if (SelectedRoleId.HasValue)
                filteredList = filteredList.Where(d => d.RoleId == SelectedRoleId && (bool)!d.IsDeleted);

            // Filter by search text if provided
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = filteredList.Where(d =>
                    (d.FullName != null && d.FullName.ToLower().Contains(searchTerm)) ||
                    (d.Phone != null && d.Phone.ToLower().Contains(searchTerm)) ||
                    (d.Email != null && d.Email.ToLower().Contains(searchTerm))
                );
            }

            DoctorList = new ObservableCollection<Staff>(filteredList);
        }

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
                    FileName = $"DanhSachBacSi_{DateTime.Now:dd-MM-yyyy}.xlsx"
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
                                var worksheet = workbook.Worksheets.Add("Danh sách bác sĩ");

                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Add title (merged cells)
                                worksheet.Cell(1, 1).Value = "DANH SÁCH BÁC SĨ";
                                var titleRange = worksheet.Range(1, 1, 1, 8);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 8);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added title
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Add headers (with spacing of 4 cells from title)
                                int headerRow = 6; // Row 6 (leaving 3 blank rows after title)
                                worksheet.Cell(headerRow, 1).Value = "ID";
                                worksheet.Cell(headerRow, 2).Value = "Họ và tên";
                                worksheet.Cell(headerRow, 3).Value = "Vai trò";  // Add role column
                                worksheet.Cell(headerRow, 4).Value = "Chuyên khoa";  // Shift other c
                                worksheet.Cell(headerRow, 5).Value = "Điện thoại";
                                worksheet.Cell(headerRow, 6).Value = "Email";
                                worksheet.Cell(headerRow, 7).Value = "Lịch làm việc";
                                worksheet.Cell(headerRow, 8).Value = "Địa chỉ";
                         

                                // Style header row
                                var headerRange = worksheet.Range(headerRow, 1, headerRow, 8);
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
                                int totalStaffs = DoctorList.Count;

                                // Create data range (to apply borders later)
                                var dataStartRow = row;

                                for (int i = 0; i < totalStaffs; i++)
                                {
                                    var doctor = DoctorList[i];

                                    worksheet.Cell(row, 1).Value = doctor.StaffId;
                                    worksheet.Cell(row, 2).Value = doctor.FullName ?? "";
                                    worksheet.Cell(row, 3).Value = doctor.Role?.RoleName ?? ""; 
                                    worksheet.Cell(row, 4).Value = doctor.Specialty?.SpecialtyName ?? "";
                                    worksheet.Cell(row, 5).Value = doctor.Phone ?? "";
                                    worksheet.Cell(row, 6).Value = doctor.Email ?? "";
                                    worksheet.Cell(row, 7).Value = doctor.Schedule ?? "";
                                    worksheet.Cell(row, 8).Value = doctor.Address ?? "";

                                    // Check if doctor has an account
                                    var account = DataProvider.Instance.Context.Accounts
                                        .FirstOrDefault(a => a.StaffId == doctor.StaffId && a.IsDeleted != true);

                                    worksheet.Cell(row, 8).Value = account != null ? account.Username : "Không có";

                                    row++;

                                    // Update progress based on percentage of Staffs processed
                                    int progressValue = 20 + (i * 60 / totalStaffs);
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));

                                    // Add a small delay to make the progress visible
                                    Thread.Sleep(30);
                                }

                                // Report progress: 80% - Data added
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Apply borders to the data range
                                if (totalStaffs > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, 1, row - 1, 8);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                    // Center-align certain columns
                                    worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // ID
                                    worksheet.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Specialty
                                    worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Phone
                                    worksheet.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Account
                                }

                                // Add total row
                                worksheet.Cell(row + 1, 1).Value = "Tổng số:";
                                worksheet.Cell(row + 1, 2).Value = totalStaffs;
                                worksheet.Cell(row + 1, 2).Style.Font.Bold = true;

                                // Auto-fit columns
                                worksheet.Columns().AdjustToContents();

                                // Set minimum widths for better readability
                                worksheet.Column(1).Width = 10; // ID
                                worksheet.Column(2).Width = 25; // Name
                                worksheet.Column(3).Width = 20; // Specialty
                                worksheet.Column(7).Width = 80; // Schedule
                                worksheet.Column(8).Width = 85; // Address

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
                                     $"Đã xuất danh sách bác sĩ thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                     "Thành công");
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
                                    "Lỗi");
                              
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
                                   "Lỗi");
            }
        }


        #region Specialty Methods
        private void AddSpecialty()
        {
            try
            {
                // Confirm dialog
                bool result = MessageBoxService.ShowQuestion(
                     $"Bạn có chắc muốn thêm chuyên khoa '{SpecialtyName}' không?",
                     "Xác Nhận Thêm");
                if (!result)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                    .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Chuyên khoa này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newSpecialty = new DoctorSpecialty
                {
                    SpecialtyName = SpecialtyName,
                    Description = Description ?? "",
                    IsDeleted = false
                };

                DataProvider.Instance.Context.DoctorSpecialties.Add(newSpecialty);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear fields
                SpecialtyName = "";
                Description = "";

                MessageBoxService.ShowSuccess(
                    "Đã thêm chuyên khoa thành công!",
                    "Thành Công");
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể thêm chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        private void EditSpecialty()
        {
            try
            {
                // Confirm dialog
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa chuyên khoa '{SelectedItem.SpecialtyName}' thành '{SpecialtyName}' không?",
                    "Xác Nhận Sửa");

                if (!result)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                    .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() && 
                              s.SpecialtyId != SelectedItem.SpecialtyId && 
                             (bool) !s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Tên chuyên khoa này đã tồn tại.");
                    return;
                }

                // Update specialty
                var specialtyToUpdate = DataProvider.Instance.Context.DoctorSpecialties
                    .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                if (specialtyToUpdate == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy chuyên khoa cần sửa.");
                    return;
                }

                specialtyToUpdate.SpecialtyName = SpecialtyName;
                specialtyToUpdate.Description = Description ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Update doctor list as specialty names may have changed
                LoadData();

                MessageBoxService.ShowSuccess(
                    "Đã cập nhật chuyên khoa thành công!",
                    "Thành Công");
                ;
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                  );
            }
        }

        private void DeleteSpecialty()
        {
            try
            {
                // Check if specialty is in use by any Staffs
                bool isInUse = DataProvider.Instance.Context.Staffs
                    .Any(d => d.SpecialtyId == SelectedItem.SpecialtyId && (bool)!d.IsDeleted);

                if (isInUse)
                {
                    MessageBoxService.ShowError(
                        "Không thể xóa chuyên khoa này vì đang được sử dụng bởi một hoặc nhiều bác sĩ.",
                        "Cảnh báo");
                    return;
                }

                // Confirm deletion
               bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa chuyên khoa '{SelectedItem.SpecialtyName}' không?",
                    "Xác Nhận Xóa");

                if (!result)
                    return;

                // Soft delete the specialty
                var specialtyToDelete = DataProvider.Instance.Context.DoctorSpecialties
                    .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                if (specialtyToDelete == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy chuyên khoa cần xóa.");
                    return;
                }

                specialtyToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear selection and fields
                SelectedItem = null;
                SpecialtyName = "";
                Description = "";

                MessageBoxService.ShowSuccess(
                    "Đã xóa chuyên khoa thành công.",
                    "Thành Công");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa chuyên khoa: {ex.Message}",
                    "Lỗi"
                    );
            }
        }

       
        #endregion
        #endregion
    }
}
