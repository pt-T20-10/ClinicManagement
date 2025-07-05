using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
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
using System.Windows.Media;

namespace ClinicManagement.ViewModels
{
    public class SettingViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Doctor Information Properties

        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;
        private int _StaffId;
        public int StaffId
        {
            get => _StaffId;
            set
            {
                _StaffId = value;
                OnPropertyChanged();
            }
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_fullName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(FullName));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(FullName));

                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        private DoctorSpecialty _selectedSpecialty;
        public DoctorSpecialty SelectedSpecialty
        {
            get => _selectedSpecialty;
            set
            {
                _selectedSpecialty = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DoctorSpecialty> _specialtyList = new();
        public ObservableCollection<DoctorSpecialty> SpecialtyList
        {
            get => _specialtyList;
            set
            {
                _specialtyList = value;
                OnPropertyChanged();
            }
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_phone);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Phone));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Phone));

                    _phone = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _schedule = string.Empty;
        public string Schedule
        {
            get => _schedule;
            set
            {
                if (_schedule != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_schedule);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Schedule));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Schedule));

                    _schedule = value;
                    OnPropertyChanged();
                }
            }
        }


        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        private string _certificateLink = string.Empty;
        public string CertificateLink
        {
            get => _certificateLink;
            set
            {
                if (_certificateLink != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_certificateLink);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(CertificateLink));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(CertificateLink));

                    _certificateLink = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _role = string.Empty;
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_email);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Email));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Email));

                    _email = value;
                    OnPropertyChanged();
                }
            }
        }
        private MainViewModel _mainViewModel;

        #endregion


        #region Commands

        public ICommand UpdateDoctorInfoCommand { get; set; }
        public ICommand ChangePasswordCommand { get; set; }
        public ICommand LoadedCommand { get; set; }
        public ICommand SignOutCommand { get; set; }
        public ICommand RefreshDataCommand { get; set; }

        #endregion

        private Account _currentAccount;
        private Staff _currentDoctor;

        public SettingViewModel()
        {
            _mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;
            // Initialize commands
            InitializeCommands();
            
            
            // Load doctor information when MainViewModel's CurrentAccount changes
            MainViewModel mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                mainVM.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(mainVM.CurrentAccount))
                    {
                        LoadDoctorInformation(mainVM.CurrentAccount);
                    }
                };
                
                // Load initial data if CurrentAccount is already set
                if (mainVM.CurrentAccount != null)
                {
                    LoadDoctorInformation(mainVM.CurrentAccount);
                }
            }
        }

        private void InitializeCommands()
        {
            UpdateDoctorInfoCommand = new RelayCommand<object>(
                p => UpdateDoctorInfo(),
                p => true
            );

            ChangePasswordCommand = new RelayCommand<object>(
                p => ChangePassword(),
                p => _currentAccount != null
            );

            LoadedCommand = new RelayCommand<object>(
                p => {
                    // Refresh data when control is loaded
                    MainViewModel mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                    if (mainVM != null && mainVM.CurrentAccount != null)
                    {
                        LoadDoctorInformation(mainVM.CurrentAccount);
                    }
                },
                p => true
            );
            // Tạo SignOutCommand mới từ RelayCommand để chuyển tiếp đến MainViewModel.SignOutCommand
            SignOutCommand = new RelayCommand<object>(
                p => ExecuteSignOut(),
                p => CanExecuteSignOut()
            );
            RefreshDataCommand = new RelayCommand<object>(
       p => RefreshDoctorInformation(),
       p => _currentAccount != null
   );
        }

        private bool CanExecuteSignOut()
        {
            return _mainViewModel != null && _mainViewModel.CurrentAccount != null;
        }

        private void ExecuteSignOut()
        {
            try
            {
                // Get the latest MainViewModel reference
                var mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;

                if (mainViewModel == null || mainViewModel.CurrentAccount == null)
                {
                    MessageBoxService.ShowWarning("Không thể đăng xuất vào lúc này.", "Thông báo");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Xác nhận đăng xuất");

                if (result)
                {
                    // Call MainViewModel's SignOut method instead of duplicating logic
                    mainViewModel.SignOut();

                    // Lấy MainWindow
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Ẩn MainWindow
                        mainWindow.Hide();

                        // Hiển thị màn hình đăng nhập
                        LoginWindow loginWindow = new LoginWindow();
                        loginWindow.ShowDialog();

                        // Get the latest MainViewModel reference again after login
                        mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;

                        // Kiểm tra kết quả đăng nhập
                        var loginVM = loginWindow.DataContext as LoginViewModel;

                        // Nếu đăng nhập thành công (IsLogin = true và CurrentAccount khác null), hiển thị lại MainWindow
                        if (loginVM != null && loginVM.IsLogin && mainViewModel != null && mainViewModel.CurrentAccount != null)
                        {
                            mainWindow.Show();
                            // No need for welcome message here - it's shown in LoginViewModel.Login
                        }
                        else
                        {
                            // Nếu đã ấn Cancel hoặc đóng cửa sổ đăng nhập mà không đăng nhập lại thành công
                            mainWindow.Close();
                            Application.Current.Shutdown();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi đăng xuất: {ex.Message}", "Lỗi");
            }
        }

        public void LoadDoctorInformation(Account account)
        {
            if (account == null) return;

            _currentAccount = account;

            try
            {
                // Load doctor information from account
                _currentDoctor = DataProvider.Instance.Context.Staffs
                    .FirstOrDefault(d => d.StaffId == account.StaffId && d.IsDeleted != true);

                if (_currentDoctor != null)
                {
                    StaffId = _currentDoctor.StaffId;
                    FullName = _currentDoctor.FullName;
                    Phone = _currentDoctor.Phone ?? string.Empty;
                    Email = _currentDoctor.Email ?? string.Empty; // Add this line to load Email
                    Schedule = _currentDoctor.Schedule ?? string.Empty;
                    Address = _currentDoctor.Address ?? string.Empty;
                    CertificateLink = _currentDoctor.CertificateLink ?? string.Empty;

                    // Load specialties
                    SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                        DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => s.IsDeleted != true)
                        .ToList()
                    );

                    // Set selected specialty
                    SelectedSpecialty = SpecialtyList.FirstOrDefault(s => s.SpecialtyId == _currentDoctor.SpecialtyId);
                }

                // Set account information
                UserName = account.Username;
                Role = account.Role ?? string.Empty;

                // Update command state
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin bác sĩ: {ex.Message}", "Lỗi");
            }
        }

     

        private void UpdateDoctorInfo()
        {
            try
            {
                if (_currentDoctor == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!", "Lỗi");
                    return;
                }

                // Enable validation for all fields
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Email));
                _touchedFields.Add(nameof(Schedule));
                _touchedFields.Add(nameof(CertificateLink));

                // Trigger validation for required fields
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Schedule));
                OnPropertyChanged(nameof(CertificateLink));

                // Check if there are validation errors
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi cập nhật thông tin.", "Lỗi dữ liệu");
                    return;
                }

                // Use transaction to ensure data consistency
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Check if email already exists (excluding current doctor)
                        if (!string.IsNullOrWhiteSpace(Email))
                        {
                            bool emailExists = DataProvider.Instance.Context.Staffs
                                .Any(d => d.Email == Email.Trim() && d.StaffId != _currentDoctor.StaffId && d.IsDeleted == false);

                            if (emailExists)
                            {
                                MessageBoxService.ShowError("Email này đã được sử dụng bởi một nhân viên khác.", "Lỗi dữ liệu");
                                return;
                            }
                        }

                        // Check if phone number already exists (excluding current doctor)
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && d.StaffId != _currentDoctor.StaffId && d.IsDeleted == false);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowError("Số điện thoại này đã được sử dụng bởi một nhân viên khác.", "Lỗi dữ liệu");
                            return;
                        }

                        // Get doctor record from database
                        var doctorToUpdate = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == _currentDoctor.StaffId);

                        if (doctorToUpdate != null)
                        {
                            // Update properties
                            doctorToUpdate.FullName = FullName.Trim();
                            doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                            doctorToUpdate.CertificateLink = CertificateLink?.Trim();
                            doctorToUpdate.Email = Email?.Trim();
                            doctorToUpdate.Schedule = Schedule?.Trim();
                            doctorToUpdate.Phone = Phone?.Trim();
                            doctorToUpdate.Address = Address?.Trim();

                            // Save changes
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction
                            transaction.Commit();

                            // Update local copy
                            _currentDoctor = doctorToUpdate;

                            MessageBoxService.ShowSuccess("Đã cập nhật thông tin thành công!", "Thành công");
                        }
                        else
                        {
                            MessageBoxService.ShowError("Không tìm thấy thông tin trong cơ sở dữ liệu!", "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction if error occurs
                        transaction.Rollback();
                        throw; // Re-throw to be caught by outer catch block
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi cập nhật thông tin: {ex.Message}", "Lỗi");
            }
        }

        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(FullName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Phone)]) ||
                       !string.IsNullOrEmpty(this[nameof(Email)]) ||
                       !string.IsNullOrEmpty(this[nameof(Schedule)]) ||
                       !string.IsNullOrEmpty(this[nameof(CertificateLink)]);
            }
        }
        // Trong SettingViewModel
        private void ChangePassword()
        {
            var viewModel = new ChangeDoctorPasswordViewModel();
            viewModel.SetAccount(_currentAccount);

            var changePasswordWindow = new ChangePasswordWindow();
            changePasswordWindow.DataContext = viewModel; // Gán DataContext trực tiếp

            changePasswordWindow.ShowDialog();
        }

        private void RefreshDoctorInformation()
        {
            try
            {
                if (_currentAccount != null)
                {
                    // Clear any touched fields to reset validation
                    _touchedFields.Clear();
                    _isValidating = false;

                    // Reload doctor information from the database
                    LoadDoctorInformation(_currentAccount);

                    // Display message to user
                    MessageBoxService.ShowSuccess(
                        "Thông tin đã được làm mới thành công.",
                        "Thành công"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi làm mới thông tin: {ex.Message}",
                    "Lỗi"
                );
            }
        }
        #region Validation
        public string Error
        {
            get
            {
                return null;
            }
        }
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
                    case nameof(FullName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ và tên không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ và tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Phone))
                        {
                            error = "Số điện thoại không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(Phone) &&
                                !Regex.IsMatch(Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(Email):
                        if (!string.IsNullOrWhiteSpace(Email) &&
                            !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(Schedule):
                        if (!string.IsNullOrWhiteSpace(Schedule) && !IsValidScheduleFormat(Schedule))
                        {
                            error = "Lịch làm việc không đúng định dạng. Vui lòng nhập theo mẫu: T2, T3, T4: 7h-13h";
                        }
                        break;

                    case nameof(UserName):
                        if (!string.IsNullOrWhiteSpace(UserName) && UserName.Trim().Length < 4)
                        {
                            error = "Tên đăng nhập phải có ít nhất 4 ký tự";
                        }
                        break;
                }

                return error;
            }
        }

   
        /// <summary>
        /// Validates that the schedule follows the format: "T2, T3, T4: 7h-13h"
        /// </summary>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Empty schedule is valid (not required)

            // Multiple pattern support
            string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            string pattern2 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            string pattern3 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            string pattern4 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            string pattern5 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h\d{2}-\d{1,2}h\d{2}(, \d{1,2}h\d{2}-\d{1,2}h\d{2})*$";

            if (Regex.IsMatch(schedule, pattern1) ||
                Regex.IsMatch(schedule, pattern2) ||
                Regex.IsMatch(schedule, pattern3) ||
                Regex.IsMatch(schedule, pattern4) ||
                Regex.IsMatch(schedule, pattern5))
            {
                try
                {
                    // Parse all time slots and check each slot's start < end
                    string[] parts = schedule.Split(':');
                    if (parts.Length < 2)
                        return false;

                    string timeSection = string.Join(":", parts.Skip(1)).Trim();
                    var timeRanges = timeSection.Split(',');

                    foreach (var range in timeRanges)
                    {
                        var times = range.Trim().Split('-');
                        if (times.Length == 2)
                        {
                            var start = ParseTimeString(times[0].Trim());
                            var end = ParseTimeString(times[1].Trim());
                            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                                return false; // Invalid time format
                            if (start >= end)
                                return false; // Start must be before end
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        // Helper: parses "8h", "8h30", "13h", "13h30" etc.
        private TimeSpan ParseTimeString(string timeStr)
        {
            timeStr = timeStr.Replace("h", ":").Replace(" ", "");
            if (timeStr.EndsWith(":")) timeStr += "00";
            var parts = timeStr.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return new TimeSpan(h, m, 0);
            if (parts.Length == 1 && int.TryParse(parts[0], out h))
                return new TimeSpan(h, 0, 0);
            return TimeSpan.Zero;
        }
        #endregion    
       
    }

    }

