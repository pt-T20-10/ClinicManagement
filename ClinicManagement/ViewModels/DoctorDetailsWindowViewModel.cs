﻿using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class DoctorDetailsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        private ObservableCollection<AppointmentDisplayInfo>? _doctorAppointmentsDisplay;
        public ObservableCollection<AppointmentDisplayInfo> DoctorAppointmentsDisplay
        {
            get => _doctorAppointmentsDisplay ??= new ObservableCollection<AppointmentDisplayInfo>();
            set
            {
                _doctorAppointmentsDisplay = value;
                OnPropertyChanged();
            }
        }

        private Window? _window;
        private Doctor? _doctor;
        public Doctor? Doctor
        {
            get => _doctor;
            set
            {
                _doctor = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadRelatedData();
                }
            }
        }

        public string? Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        // Doctor Information Properties
        private int _doctorID;
        public int DoctorID
        {
            get => _doctorID;
            set
            {
                _doctorID = value;
                OnPropertyChanged();
            }
        }

        private string _fullName = string.Empty;
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

        private DoctorSpecialty? _selectedSpecialty;
        public DoctorSpecialty? SelectedSpecialty
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_phone))
                        _touchedFields.Add(nameof(Phone));

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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_schedule))
                        _touchedFields.Add(nameof(Schedule));

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
                if (_address != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_address))
                        _touchedFields.Add(nameof(Address));

                    _address = value;
                    OnPropertyChanged();
                }
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_email))
                        _touchedFields.Add(nameof(Email));

                    _email = value;
                    OnPropertyChanged();
                }
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_certificateLink))
                        _touchedFields.Add(nameof(CertificateLink));

                    _certificateLink = value;
                    OnPropertyChanged();
                }
            }
        }

        
        // Account Information
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

        private bool _hasAccount;
        public bool HasAccount
        {
            get => _hasAccount;
            set
            {
                _hasAccount = value;
                OnPropertyChanged(nameof(_hasAccount));
                // Refresh command can-execute status
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _newUsername = string.Empty;
        public string NewUsername
        {
            get => _newUsername;
            set
            {
                if (_newUsername != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_newUsername))
                        _touchedFields.Add(nameof(NewUsername));

                    _newUsername = value;
                    OnPropertyChanged();
                    // Refresh AddDoctorAccountCommand can-execute when username changes
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _selectedRole = "Bác sĩ";
        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                // Refresh AddDoctorAccountCommand can-execute when role changes
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private ObservableCollection<string> _roleList = new();
        public ObservableCollection<string> RoleList
        {
            get => _roleList;
            set
            {
                _roleList = value;
                OnPropertyChanged();
            }
        }

        // Appointment Properties
        private DateTime _appointmentStartDate = DateTime.Today.AddMonths(-1);
        public DateTime AppointmentStartDate
        {
            get => _appointmentStartDate;
            set
            {
                _appointmentStartDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _appointmentEndDate = DateTime.Today;
        public DateTime AppointmentEndDate
        {
            get => _appointmentEndDate;
            set
            {
                _appointmentEndDate = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _appointmentStatusList = new();
        public ObservableCollection<string> AppointmentStatusList
        {
            get => _appointmentStatusList;
            set
            {
                _appointmentStatusList = value;
                OnPropertyChanged();
            }
        }

        private string _selectedAppointmentStatus = string.Empty;
        public string SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Appointment> _doctorAppointments = new();
        public ObservableCollection<Appointment> DoctorAppointments
        {
            get => _doctorAppointments;
            set
            {
                _doctorAppointments = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand UpdateDoctorInfoCommand { get; set; }
        public ICommand DeleteDoctorCommand { get; set; }
        public ICommand ChangePasswordCommand { get; set; }
        public ICommand SearchAppointmentsCommand { get; set; }
        public ICommand ViewAppointmentDetailsCommand { get; set; }
        public ICommand AddDoctorAccountCommand { get; set; }
        #endregion

        public DoctorDetailsWindowViewModel()
        {
            InitializeCommands();
            InitializeData();
        }

        #region Initialization
        private void InitializeCommands()
        {
            LoadedWindowCommand = new RelayCommand<Window>((w) => _window = w, (p) => true);

            UpdateDoctorInfoCommand = new RelayCommand<object>(
                (p) => ExecuteUpdateDoctorInfo(),
                (p) => CanUpdateDoctorInfo()
            );

            DeleteDoctorCommand = new RelayCommand<object>(
                (p) => ExecuteDeleteDoctor(),
                (p) => Doctor != null
            );

            SearchAppointmentsCommand = new RelayCommand<object>(
                (p) => LoadAppointments(),
                (p) => Doctor != null
            );

            ViewAppointmentDetailsCommand = new RelayCommand<AppointmentDisplayInfo>(
                (p) => ViewAppointmentDetails(p),
                (p) => p != null
            );

            ChangePasswordCommand = new RelayCommand<object>(
               (p) => ExecuteResetPassword(),
               (p) => Doctor != null && HasAccount  // Chỉ enable khi có tài khoản
           );

            AddDoctorAccountCommand = new RelayCommand<object>(
                (p) => ExecuteAddDoctorAccount(),
                (p) => CanAddDoctorAccount()

            );
        }

        private void InitializeData()
        {
            // Initialize appointment status list
            AppointmentStatusList = new ObservableCollection<string>
            {
                "Tất cả",
                "Đang chờ",
                "Đang khám",
                "Đã khám",
                "Đã hủy"
            };

            SelectedAppointmentStatus = "Tất cả";

            // Initialize role list for account creation
            RoleList = new ObservableCollection<string>
            {
                "Bác sĩ",
                "Dược sĩ"
            };

            SelectedRole = "Bác sĩ";

            // Initialize collections to prevent null references
            DoctorAppointments = new ObservableCollection<Appointment>();
            DoctorAppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
        }

        private void LoadRelatedData()
        {
            if (Doctor == null) return;

            // Load doctor information
            DoctorID = Doctor.DoctorId;
            FullName = Doctor.FullName;
            Phone = Doctor.Phone ?? string.Empty;
            Email = Doctor.Email ?? string.Empty;   
            Schedule = Doctor.Schedule ?? string.Empty;
            Address = Doctor.Address ?? string.Empty;
            CertificateLink = Doctor.CertificateLink ?? string.Empty;

            // Load specialties
            SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                DataProvider.Instance.Context.DoctorSpecialties
                .Where(s => s.IsDeleted != true)
                .ToList()
            );

            // Set selected specialty
            SelectedSpecialty = SpecialtyList.FirstOrDefault(s => s.SpecialtyId == Doctor.SpecialtyId);

            var account = DataProvider.Instance.Context.Accounts
         .FirstOrDefault(a => a.DoctorId == Doctor.DoctorId && a.IsDeleted != true);

            if (account != null)
            {
                UserName = account.Username;
                Role = account.Role ?? string.Empty;
                NewUsername = account.Username; // Đồng bộ với tên hiện tại
                HasAccount = true;  // Đặt HasAccount = true khi có tài khoản
                                    // Cập nhật lại UI
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                UserName = string.Empty;
                Role = string.Empty;
                HasAccount = false;  // Đặt HasAccount = false khi không có tài khoản
                                     // Generate a suggested username from doctor's name
               
                // Cập nhật lại UI
                CommandManager.InvalidateRequerySuggested();
            }

            // Load appointments
            LoadAppointments();

            // Reset validation state
            _touchedFields.Clear();
            _isValidating = false;
        }
        #endregion

        #region Validation
        public string? this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the form or when submitting
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string? error = null;

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

                    case nameof(Schedule):
                        if (!string.IsNullOrWhiteSpace(Schedule) && !IsValidScheduleFormat(Schedule))
                        {
                            error = "Lịch làm việc không đúng định dạng. Vui lòng nhập theo mẫu:\n" +
                                    "- T2, T3, T4: 7h-13h\n" +
                                    "- T2-T7: 7h-11h\n" +
                                    "- T2: 9h-11h\n" +
                                    "- T2, T3, T4: 8h-12h, 13h30-17h";
                        }
                        break;

                    case nameof(CertificateLink):
                        // Only validate if user has entered something
                        if (!string.IsNullOrWhiteSpace(CertificateLink) && !IsValidUrl(CertificateLink))
                        {
                            error = "Link chứng chỉ không đúng định dạng URL";
                        }
                        break;

                    case nameof(NewUsername):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(NewUsername))
                        {
                            error = "Tên đăng nhập không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(NewUsername) && NewUsername.Trim().Length < 4)
                        {
                            error = "Tên đăng nhập phải có ít nhất 4 ký tự";
                        }
                        // Có thể thêm validation kiểm tra ký tự đặc biệt nếu cần
                        break;
                    case nameof(Email):
      
                       if(_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Email))
                        {
                            error = "Email không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(Email) && !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
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
                       !string.IsNullOrEmpty(this[nameof(CertificateLink)]);
            }
        }

        public bool HasUsernameErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(NewUsername)]);
            }
        }

       
        /// Validates that the schedule follows various acceptable formats:
        /// - "T2, T3, T4: 7h-13h"
        /// - "T2, T3, T4, T5, T6: 8h-12h, 13h30-17h" 
        /// - "T2: 9h-11h"
        /// - "T2-T7: 7h-11h"
        /// </summary>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Empty schedule is valid (not required)

            // Multiple pattern support

            // Pattern 1: "T2, T3, T4: 7h-13h"
            string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

            // Pattern 2: "T2, T3, T4, T5, T6: 8h-12h, 13h30-17h"
            string pattern2 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)*$";

            // Pattern 3: "T2-T7: 7h-11h"
            string pattern3 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

            // Check if any pattern matches
            if (Regex.IsMatch(schedule, pattern1) ||
                Regex.IsMatch(schedule, pattern2) ||
                Regex.IsMatch(schedule, pattern3))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Validates that a string is a valid URL
        /// </summary>
        private bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Empty URL is valid (not required)

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
        #endregion

        #region Methods
        private bool CanUpdateDoctorInfo()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedSpecialty != null;
        }

        private void ExecuteUpdateDoctorInfo()
        {
            try
            {
                if (Doctor == null)
                {
                    MessageBox.Show("Không tìm thấy thông tin bác sĩ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Check for validation errors
                if (HasErrors)
                {
                    MessageBox.Show(
                        "Vui lòng sửa các lỗi nhập liệu trước khi cập nhật thông tin bác sĩ.",
                        "Lỗi Validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                bool emailExits = DataProvider.Instance.Context.Doctors
                                 .Any(d => d.Email == Email.Trim() && d.DoctorId != Doctor.DoctorId && d.IsDeleted == false);
                if(emailExits)
                {
                    MessageBox.Show(
                                "Email đã tồn tại. Vui lòng sử dụng email khác.",
                                "Lỗi Dữ Liệu",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                    return;
                }    
                // Check if phone number already exists (excluding current doctor)
                bool phoneExists = DataProvider.Instance.Context.Doctors
                    .Any(d => d.Phone == Phone.Trim() && d.DoctorId != Doctor.DoctorId && d.IsDeleted == false);

                if (phoneExists)
                {
                    MessageBox.Show(
                        "Số điện thoại này đã được sử dụng bởi một bác sĩ khác.",
                        "Lỗi Dữ Liệu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Get doctor record
                var doctorToUpdate = DataProvider.Instance.Context.Doctors
                    .FirstOrDefault(d => d.DoctorId == Doctor.DoctorId);

                if (doctorToUpdate != null)
                {
                    // Update properties
                    doctorToUpdate.FullName = FullName.Trim();
                    doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                    doctorToUpdate.CertificateLink = CertificateLink.Trim();
                    doctorToUpdate.Email = Email.Trim();
                    doctorToUpdate.Schedule = Schedule.Trim();
                    doctorToUpdate.Phone = Phone.Trim();
                    doctorToUpdate.Address = Address.Trim();

                    DataProvider.Instance.Context.SaveChanges();

                    MessageBox.Show(
                        "Đã cập nhật thông tin bác sĩ thành công!",
                        "Thành Công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy thông tin bác sĩ!",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi cập nhật thông tin bác sĩ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteDoctor()
        {
            try
            {
                if (Doctor == null) return;

                // Check if doctor has pending or in-progress appointments
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.DoctorId == Doctor.DoctorId &&
                             (a.Status == "Đang chờ" || a.Status == "Đang khám") &&
                             a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBox.Show(
                        "Không thể xóa bác sĩ này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng giải quyết các lịch hẹn hiện tại trước khi xóa.",
                        "Cảnh Báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa bác sĩ {FullName} không?\n" +
                    "Lưu ý: Tài khoản liên kết với bác sĩ này cũng sẽ bị xóa.",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Find and soft-delete the doctor
                var doctorToDelete = DataProvider.Instance.Context.Doctors
                    .FirstOrDefault(d => d.DoctorId == Doctor.DoctorId);

                if (doctorToDelete != null)
                {
                    // Soft-delete the doctor
                    doctorToDelete.IsDeleted = true;

                    // Also soft-delete the associated account
                    var accountToDelete = DataProvider.Instance.Context.Accounts
                        .FirstOrDefault(a => a.DoctorId == Doctor.DoctorId);

                    if (accountToDelete != null)
                    {
                        accountToDelete.IsDeleted = true;
                    }

                    DataProvider.Instance.Context.SaveChanges();

                    MessageBox.Show(
                        "Đã xóa bác sĩ thành công!",
                        "Thành Công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Close the window
                    _window?.Close();
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy thông tin bác sĩ!",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa bác sĩ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExecuteResetPassword()
        {
            try
            {
                if (Doctor == null || !HasAccount)
                {
                    MessageBox.Show(
                        "Không thể đặt lại mật khẩu. Bác sĩ này chưa có tài khoản!",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Ask for confirmation
                MessageBoxResult result = MessageBox.Show(
                    "Bạn có chắc muốn đặt lại mật khẩu cho tài khoản này không?\n" +
                    "Mật khẩu mới sẽ là: 1111",
                    "Xác Nhận Đặt Lại Mật Khẩu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Find the account
                var account = DataProvider.Instance.Context.Accounts
                    .FirstOrDefault(a => a.DoctorId == Doctor.DoctorId && a.IsDeleted != true);

                if (account != null)
                {
                    // Reset password to "1111"
                    string hashedPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode("1111"));
                    account.Password = hashedPassword;
                    DataProvider.Instance.Context.SaveChanges();

                    MessageBox.Show(
                        "Đã đặt lại mật khẩu thành công!\n" +
                        "Mật khẩu mới: 1111",
                        "Thành Công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy tài khoản liên kết với bác sĩ này!",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Cập nhật lại trạng thái HasAccount
                    HasAccount = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi đặt lại mật khẩu: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                if (Doctor == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == Doctor.DoctorId &&
                               a.IsDeleted != true &&
                               a.AppointmentDate.Date >= AppointmentStartDate.Date &&
                               a.AppointmentDate.Date <= AppointmentEndDate.Date);

                // Apply status filter if not "Tất cả"
                if (!string.IsNullOrEmpty(SelectedAppointmentStatus) && SelectedAppointmentStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus);
                }

                var appointments = query.OrderByDescending(a => a.AppointmentDate)
                                        .ThenBy(a => a.AppointmentDate.TimeOfDay)
                                        .ToList();

                // Store original appointments list for reference
                DoctorAppointments = new ObservableCollection<Appointment>(appointments);

                // Create display-friendly objects with formatted time and reason
                DoctorAppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>(
                    appointments.Select(a => new AppointmentDisplayInfo
                    {
                        AppointmentId = a.AppointmentId,
                        PatientName = a.Patient.FullName,
                        AppointmentDate = a.AppointmentDate.Date,
                        AppointmentTimeString = a.AppointmentDate.ToString("HH:mm"),
                        Status = a.Status,
                        Reason = a.Notes ?? string.Empty,
                        OriginalAppointment = a
                    })
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ViewAppointmentDetails(AppointmentDisplayInfo appointmentInfo)
        {
            // Show appointment details with date and time from the DateTime field
            MessageBox.Show($"Chi tiết lịch hẹn của bệnh nhân {appointmentInfo.PatientName}\n" +
                           $"Ngày: {appointmentInfo.AppointmentDate.ToString("dd/MM/yyyy")}\n" +
                           $"Giờ: {appointmentInfo.AppointmentTimeString}\n" +
                           $"Trạng thái: {appointmentInfo.Status}\n" +
                           $"Lý do khám: {appointmentInfo.Reason}",
                           "Chi tiết lịch hẹn",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }

        #region Account Management
        private bool CanAddDoctorAccount()
        {
            if (HasAccount)
                return false;
            // Chỉ cho phép thêm khi chưa có tài khoản và tên đăng nhập hợp lệ
            bool isValidUsername = !string.IsNullOrWhiteSpace(NewUsername) && NewUsername.Trim().Length >= 4 && !HasUsernameErrors;
            bool isValidRole = !string.IsNullOrEmpty(SelectedRole) && (SelectedRole == "Bác sĩ" || SelectedRole == "Dược sĩ");

            return Doctor != null && !HasAccount && isValidUsername && isValidRole;
        }

        private void ExecuteAddDoctorAccount()
        {
            try
            {
                if (Doctor == null) return;

                // Enable validation for username field
                _isValidating = true;
                _touchedFields.Add(nameof(NewUsername));

                // Trigger validation
                OnPropertyChanged(nameof(NewUsername));

                // Check for validation errors
                if (HasUsernameErrors)
                {
                    MessageBox.Show(
                        "Tên đăng nhập không hợp lệ. Vui lòng nhập tên đăng nhập có ít nhất 4 ký tự.",
                        "Lỗi Validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Check if username already exists
                bool usernameExists = DataProvider.Instance.Context.Accounts
                    .Any(a => a.Username == NewUsername.Trim() && a.IsDeleted != true);

                if (usernameExists)
                {
                    MessageBox.Show(
                        "Tên đăng nhập đã tồn tại. Vui lòng chọn tên đăng nhập khác.",
                        "Lỗi Dữ Liệu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn tạo tài khoản cho bác sĩ {FullName} không?\n" +
                    $"Tên đăng nhập: {NewUsername.Trim()}\n" +
                    $"Mật khẩu mặc định: 1111\n" +
                    $"Vai trò: {SelectedRole}",
                    "Xác Nhận Tạo Tài Khoản",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Create account with default password "1111"
                var defaultPassword = "1111";
                var newAccount = new Account
                {
                    Username = NewUsername.Trim(),
                    Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(defaultPassword)),
                    DoctorId = Doctor.DoctorId,
                    Role = SelectedRole,
                    IsLogined = false,
                    IsDeleted = false
                };

                DataProvider.Instance.Context.Accounts.Add(newAccount);
                DataProvider.Instance.Context.SaveChanges();

                MessageBox.Show(
                    "Đã tạo tài khoản thành công với mật khẩu mặc định là \"1111\".",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Update UI to reflect new account
                UserName = NewUsername.Trim();
                Role = SelectedRole;
                HasAccount = true;  // Cập nhật trạng thái đã có tài khoản

                // Refresh UI
                OnPropertyChanged(nameof(HasAccount));
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tạo tài khoản: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        #endregion

        #endregion
    }
}
