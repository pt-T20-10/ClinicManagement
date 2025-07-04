using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class AddDoctorWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        private Window _window;

        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        // Doctor Information
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

        private ObservableCollection<DoctorSpecialty> _specialtyList;
        public ObservableCollection<DoctorSpecialty> SpecialtyList
        {
            get => _specialtyList;
            set
            {
                _specialtyList = value;
                OnPropertyChanged();
            }
        }

        private string _phone;
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

      private string _email;
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

        private string _schedule;
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

        private string _certificateLink;
        public string CertificateLink
        {
            get => _certificateLink;
            set
            {
                _certificateLink = value;
                OnPropertyChanged();
            }
        }


        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_userName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(UserName));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(UserName));

                    _userName = value;
                    OnPropertyChanged();
                }
            }
        }

        // Role properties
        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();

                // Show/hide specialty selection based on role
                if (value != null)
                {
                    // If role is Doctor, show specialty selection
                    IsSpecialtyVisible = value.RoleName.Contains("Bác sĩ");
                }
                else
                {
                    IsSpecialtyVisible = false;
                }
            }
        }

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

        // Add property to control Specialty ComboBox visibility
        private bool _isSpecialtyVisible = true; // Default to true for backward compatibility
        public bool IsSpecialtyVisible
        {
            get => _isSpecialtyVisible;
            set
            {
                _isSpecialtyVisible = value;
                OnPropertyChanged();
            }
        }

        // Added schedule format example for user reference
        public string ScheduleFormatExample => "Ví dụ: T2, T3, T4: 7h-13h";

        public ICommand LoadedWindowCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        #endregion

        public AddDoctorWindowViewModel()
        {
            InitializeCommands();
            LoadData();
            ResetForm();
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

        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(FullName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Phone)]) ||
                       !string.IsNullOrEmpty(this[nameof(Email)]) ||
                       !string.IsNullOrEmpty(this[nameof(Schedule)]) ||
                       !string.IsNullOrEmpty(this[nameof(UserName)]);
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

        #region Commands
        private void InitializeCommands()
        {
            LoadedWindowCommand = new RelayCommand<Window>((w) => _window = w, (w) => true);

            SaveCommand = new RelayCommand<object>(
                (p) => ExecuteSave(),
                (p) => CanSave()
            );

            CancelCommand = new RelayCommand<object>(
                (p) => ExecuteCancel(),
                (p) => true
            );
        }
        #endregion

        #region Methods
        private void LoadData()
        {
            // Load specialties
            SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                DataProvider.Instance.Context.DoctorSpecialties
                .Where(s => s.IsDeleted != true)
                .ToList()
            );

            // Load roles
            var roles = DataProvider.Instance.Context.Roles
                .Where(r => r.IsDeleted != true)
                .ToList();

            // Check if there's already an admin role assigned to any staff
            bool hasAdmin = DataProvider.Instance.Context.Staffs
                .Include(s => s.Role)
                .Any(s => s.Role != null && s.Role.RoleName == "Quản lí" && s.IsDeleted != true);

            // If there's already an admin, remove it from the available roles
            if (hasAdmin)
            {
                roles = roles.Where(r => r.RoleName != "Quản lí").ToList();
            }

            RoleList = new ObservableCollection<Role>(roles);

            // Default to Doctor role
            SelectedRole = RoleList.FirstOrDefault(r => r.RoleName == "Bác sĩ");
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedRole != null &&
                   (!IsSpecialtyVisible || SelectedSpecialty != null);
        }

        private void ExecuteSave()
        {
            try
            {
                // Enable validation for all fields
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Schedule));

                // Trigger validation for required fields
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Schedule));

                // Check for validation errors
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm nhân viên.",
                        "Lỗi thông tin");
                    return;
                }

                // Start a database transaction to ensure data consistency
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Check if phone number already exists
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && (bool)!d.IsDeleted);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowWarning(
                                "Số điện thoại này đã được sử dụng bởi một nhân viên khác.",
                                "Lỗi Dữ Liệu");
                            return;
                        }

                        // Check if email already exists (if provided)
                        if (!string.IsNullOrWhiteSpace(Email))
                        {
                            bool emailExists = DataProvider.Instance.Context.Staffs
                                .Any(d => d.Email == Email.Trim() && (bool)!d.IsDeleted);

                            if (emailExists)
                            {
                                MessageBoxService.ShowWarning(
                                    "Email này đã được sử dụng bởi một nhân viên khác.",
                                    "Lỗi Dữ Liệu");
                                return;
                            }
                        }

                        // Create and save Staff object
                        var newStaff = new Staff
                        {
                            FullName = FullName.Trim(),
                            RoleId = SelectedRole.RoleId,
                            SpecialtyId = IsSpecialtyVisible ? SelectedSpecialty?.SpecialtyId : null,
                            CertificateLink = CertificateLink?.Trim(),
                            Schedule = Schedule?.Trim(),
                            Phone = Phone.Trim(),
                            Email = Email?.Trim(),
                            Address = Address?.Trim(),
                            IsDeleted = false
                        };

                        DataProvider.Instance.Context.Staffs.Add(newStaff);
                        DataProvider.Instance.Context.SaveChanges();

                        // Variable to track if we need to create an account
                        bool createAccount = false;
                        string accountMessage = "";

                        // Create account if username is provided
                        if (!string.IsNullOrWhiteSpace(UserName))
                        {
                            // Check if username already exists
                            bool usernameExists = DataProvider.Instance.Context.Accounts
                                .Any(a => a.Username == UserName.Trim() && (bool)!a.IsDeleted);

                            if (usernameExists)
                            {
                                accountMessage = "Tên đăng nhập đã tồn tại. Tài khoản không được tạo nhưng thông tin nhân viên đã được lưu.";
                            }
                            else
                            {
                                // Create account with default password "1111"
                                var defaultPassword = "1111";
                                var newAccount = new Account
                                {
                                    Username = UserName.Trim(),
                                    Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(defaultPassword)),
                                    StaffId = newStaff.StaffId,
                                    Role = SelectedRole.RoleName, // Use the selected role name
                                    IsLogined = false,
                                    IsDeleted = false
                                };

                                DataProvider.Instance.Context.Accounts.Add(newAccount);
                                DataProvider.Instance.Context.SaveChanges();
                                createAccount = true;
                                accountMessage = "Tài khoản được tạo thành công với mật khẩu mặc định là \"1111\".";
                            }
                        }

                        // Commit the transaction
                        transaction.Commit();

                        // Show success messages
                        if (!string.IsNullOrEmpty(accountMessage))
                        {
                            // Only show account message if we tried to create one
                            if (createAccount)
                            {
                                MessageBoxService.ShowSuccess(accountMessage, "Thông Báo");
                            }
                            else
                            {
                                MessageBoxService.ShowWarning(accountMessage, "Cảnh Báo");
                            }
                        }

                        MessageBoxService.ShowSuccess(
                            $"Đã thêm {(SelectedRole.RoleName == "Bác sĩ" ? "bác sĩ" : "nhân viên")} thành công!",
                            "Thành Công");

                        // Close the window
                        _window?.Close();
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction on error
                        transaction.Rollback();
                        throw; // Rethrow to be caught by outer catch block
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi thêm nhân viên: {ex.Message}",
                    "Lỗi");
            }
        }


        private void ExecuteCancel()
        {
            // Reset form
            ResetForm();

            // Close window
            _window?.Close();
        }

        private void ResetForm()
        {
            FullName = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Schedule = string.Empty;
            Address = string.Empty;
            CertificateLink = string.Empty;
            UserName = string.Empty;

            // Set default values for Role and Specialty if available
            if (RoleList != null && RoleList.Count > 0)
            {
                SelectedRole = RoleList.FirstOrDefault(r => r.RoleName == "Bác sĩ") ?? RoleList.FirstOrDefault();
            }

            if (SpecialtyList != null && SpecialtyList.Count > 0 && IsSpecialtyVisible)
            {
                SelectedSpecialty = SpecialtyList.FirstOrDefault();
            }

            // Reset validation state
            _touchedFields.Clear();
            _isValidating = false;
        }
        #endregion
    }
}
    