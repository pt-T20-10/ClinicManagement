using ClinicManagement.Models;
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_fullName))
                        _touchedFields.Add(nameof(FullName));

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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_phone))
                        _touchedFields.Add(nameof(Phone));

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
                _email = value;
                OnPropertyChanged();
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_schedule))
                        _touchedFields.Add(nameof(Schedule));

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

        // Account Information
        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
         
                ValidatePasswords();
            }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ValidatePasswords();
            }
        }
        private bool _showPasswordMismatchError;
        public bool ShowPasswordMismatchError
        {
            get => _showPasswordMismatchError;
            set
            {
                _showPasswordMismatchError = value;
                OnPropertyChanged();
            }
        }

        private string _selectedRole;
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _roleList;
        public ObservableCollection<string> RoleList
        {
            get => _roleList;
            set
            {
                _roleList = value;
                OnPropertyChanged();
            }
        }

        private bool _passwordsMatch = true;
        public bool PasswordsMatch
        {
            get => _passwordsMatch;
            set
            {
                _passwordsMatch = value;
                OnPropertyChanged();
            }
        }

        // Added schedule format example for user reference
        public string ScheduleFormatExample => "Ví dụ: T2, T3, T4: 7h-13h";

        public ICommand LoadedWindowCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        public ICommand ConfirmPasswordChangedCommand { get; private set; }
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

                    case nameof(Password):
                        if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 6)
                        {
                            error = "Mật khẩu phải có ít nhất 6 ký tự";
                        }
                        break;

                    case nameof(ConfirmPassword):
                        // Only validate confirm password if password has been entered
                        if (!string.IsNullOrWhiteSpace(Password) &&
                            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                            Password != ConfirmPassword)
                        {
                            error = "Mật khẩu xác nhận không khớp";
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
                       !string.IsNullOrEmpty(this[nameof(UserName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Password)]) ||
                       !string.IsNullOrEmpty(this[nameof(ConfirmPassword)]) ||
                       !PasswordsMatch;
            }
        }

        private void ValidatePasswords()
        {
            // Only show error when both fields have values AND they don't match
            if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(ConfirmPassword))
            {
                PasswordsMatch = Password == ConfirmPassword;
                ShowPasswordMismatchError = !PasswordsMatch;
            }
            else
            {
                // Don't show error if either field is empty
                PasswordsMatch = true;
                ShowPasswordMismatchError = false;
            }
        }

        /// <summary>
        /// Validates that the schedule follows the format: "T2, T3, T4: 7h-13h"
        /// </summary>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Empty schedule is valid (not required)

            // Pattern: Days (T2, T3, etc.) followed by colon, then hours (7h-13h)
            string pattern = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h-\d{1,2}h$";

            // Check if basic format matches
            if (!Regex.IsMatch(schedule, pattern))
                return false;

            // Additional validation for time range can be added here if needed
            // For example, checking that start time is before end time

            return true;
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

            PasswordChangedCommand = new RelayCommand<PasswordBox>(
                (p) =>
                {
                    Password = p.Password;
                    ValidatePasswords();
                },
                (p) => true
            );

            ConfirmPasswordChangedCommand = new RelayCommand<PasswordBox>(
                (p) =>
                {
                    ConfirmPassword = p.Password;
                    ValidatePasswords();
                },
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

            // Initialize role list
            RoleList = new ObservableCollection<string>
            {
                "Bác sĩ",
                "Admin",
                "Nhân viên",
                "Dược sĩ"
            };
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedSpecialty != null;
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
                    MessageBox.Show(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm bác sĩ.",
                        "Lỗi Validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Check if phone number already exists
                bool phoneExists = DataProvider.Instance.Context.Doctors
                    .Any(d => d.Phone == Phone.Trim() && (bool)!d.IsDeleted);

                if (phoneExists)
                {
                    MessageBox.Show(
                        "Số điện thoại này đã được sử dụng bởi một bác sĩ khác.",
                        "Lỗi Dữ Liệu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create and save Doctor object
                var newDoctor = new Doctor
                {
                    FullName = FullName.Trim(),
                    SpecialtyId = SelectedSpecialty?.SpecialtyId,
                    CertificateLink = CertificateLink?.Trim(),
                    Schedule = Schedule?.Trim(),
                    Phone = Phone.Trim(),
                    Address = Address?.Trim(),
                    IsDeleted = false
                };

                DataProvider.Instance.Context.Doctors.Add(newDoctor);
                DataProvider.Instance.Context.SaveChanges();

                // Create account if username and password provided
                if (!string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password))
                {
                    // Check if username already exists
                    bool usernameExists = DataProvider.Instance.Context.Accounts
                        .Any(a => a.Username == UserName.Trim() && (bool)!a.IsDeleted);

                    if (usernameExists)
                    {
                        MessageBox.Show(
                            "Tên đăng nhập đã tồn tại. Tài khoản không được tạo nhưng thông tin bác sĩ đã được lưu.",
                            "Cảnh Báo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Create account
                        var newAccount = new Account
                        {
                            Username = UserName.Trim(),
                            Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(Password)), // Hash password
                            DoctorId = newDoctor.DoctorId,
                            Role = SelectedRole,
                            IsLogined = false,
                            IsDeleted = false
                        };

                        DataProvider.Instance.Context.Accounts.Add(newAccount);
                        DataProvider.Instance.Context.SaveChanges();
                    }
                }

                MessageBox.Show(
                    "Đã thêm bác sĩ thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Close the window
                _window?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi thêm bác sĩ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            SelectedRole = "Doctor";

            // Set default value for specialty if available
            if (SpecialtyList != null && SpecialtyList.Count > 0)
            {
                SelectedSpecialty = SpecialtyList.FirstOrDefault();
            }

            // Reset validation state
            _touchedFields.Clear();
            _isValidating = false;
            PasswordsMatch = true;
        }
        #endregion
    }
}

