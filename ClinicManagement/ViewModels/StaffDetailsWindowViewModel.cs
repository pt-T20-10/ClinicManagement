using ClinicManagement.Models;
using ClinicManagement.Services;
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
    public class StaffDetailsWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                UpdatePermissions(); // Update UI permissions when account changes
            }
        }

        private bool _canEditStaff = false;
        public bool CanEditStaff
        {
            get => _canEditStaff;
            set
            {
                _canEditStaff = value;
                OnPropertyChanged();
            }
        }

        // Add this method to check permissions
        private void UpdatePermissions()
        {
            // Default to no permissions
            CanEditStaff = false;

            // Check if the current account exists
            if (CurrentAccount == null)
                return;

            // Check role-based permissions
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Admin and Manager have full permissions
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                CanEditStaff = true;
            }

            // Force command CanExecute to be reevaluated
            CommandManager.InvalidateRequerySuggested();
        }

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
        private Staff? _doctor;
        public Staff? Doctor
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
                    else
                        _touchedFields.Remove(nameof(FullName)); // Remove if empty

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
                    else
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
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_schedule))
                        _touchedFields.Add(nameof(Schedule));
                    else
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
                if (_address != value)
                {
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_address))
                        _touchedFields.Add(nameof(Address));
                    else
                        _touchedFields.Remove(nameof(Address));

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
                    else
                        _touchedFields.Remove(nameof(Email));

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
                    else
                        _touchedFields.Remove(nameof(CertificateLink));

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
                    else
                        _touchedFields.Remove(nameof(NewUsername));

                    _newUsername = value;
                    OnPropertyChanged();
                    // Refresh AddDoctorAccountCommand can-execute when username changes
                    CommandManager.InvalidateRequerySuggested();
                }
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

        public StaffDetailsWindowViewModel()
        {
            InitializeCommands();
            InitializeData();

            // Get current account from MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
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

            // Initialize collections to prevent null references
            DoctorAppointments = new ObservableCollection<Appointment>();
            DoctorAppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
        }

        private void LoadRelatedData()
        {
            if (Doctor == null) return;

            // Load doctor information
            StaffId = Doctor.StaffId;
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

            // Get staff's role name from the database
            var staffRole = DataProvider.Instance.Context.Roles
                .FirstOrDefault(r => r.RoleId == Doctor.RoleId && r.IsDeleted != true);

            Role = staffRole?.RoleName ?? string.Empty;

            // Check if staff has an account
            var account = DataProvider.Instance.Context.Accounts
                .FirstOrDefault(a => a.StaffId == Doctor.StaffId && a.IsDeleted != true);

            if (account != null)
            {
                UserName = account.Username;
                NewUsername = account.Username; // Sync with current username
                HasAccount = true;  // Set HasAccount to true when there's an account
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                UserName = string.Empty;
                HasAccount = false;  // Set HasAccount to false when there's no account
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
            return CanEditStaff &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedSpecialty != null;
        }

        private void ExecuteUpdateDoctorInfo()
        {
            try
            {
                if (Doctor == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!", "Lỗi");
                    return;
                }

                // Bật xác thực cho tất cả các trường
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Email));
                _touchedFields.Add(nameof(Schedule));
                _touchedFields.Add(nameof(CertificateLink));

                // Kích hoạt xác thực cho các trường bắt buộc
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Schedule));
                OnPropertyChanged(nameof(CertificateLink));

                // Kiểm tra lỗi xác thực
                if (HasErrors)
                {
                    MessageBoxService.ShowError(
                        "Vui lòng sửa các lỗi nhập liệu trước khi cập nhật thông tin bác sĩ.",
                        "Lỗi thông tin");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        bool emailExits = DataProvider.Instance.Context.Staffs
                                        .Any(d => d.Email == Email.Trim() && d.StaffId != Doctor.StaffId && d.IsDeleted == false);
                        if (emailExits)
                        {
                            MessageBoxService.ShowError(
                                        "Email đã tồn tại. Vui lòng sử dụng email khác.",
                                        "Lỗi Dữ Liệu");
                            return;
                        }
                        // Kiểm tra số điện thoại đã tồn tại chưa (ngoại trừ bác sĩ hiện tại)
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && d.StaffId != Doctor.StaffId && d.IsDeleted == false);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowError(
                                "Số điện thoại này đã được sử dụng bởi một bác sĩ khác.",
                                "Lỗi Dữ Liệu");
                            return;
                        }

                        // Lấy thông tin bác sĩ cần cập nhật
                        var doctorToUpdate = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == Doctor.StaffId);

                        if (doctorToUpdate != null)
                        {
                            // Cập nhật thông tin
                            doctorToUpdate.FullName = FullName.Trim();
                            doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                            doctorToUpdate.CertificateLink = CertificateLink.Trim();
                            doctorToUpdate.Email = Email.Trim();
                            doctorToUpdate.Schedule = Schedule.Trim();
                            doctorToUpdate.Phone = Phone.Trim();
                            doctorToUpdate.Address = Address.Trim();

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Hoàn tất transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowMessage(
                                "Đã cập nhật thông tin bác sĩ thành công!",
                                "Thành Công");
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy thông tin bác sĩ!",
                                "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi cập nhật thông tin bác sĩ: {ex.Message}",
                    "Lỗi");
            }
        }

        private void ExecuteDeleteDoctor()
        {
            try
            {
                if (Doctor == null) return;

                // Kiểm tra xem bác sĩ có lịch hẹn đang chờ hoặc đang khám không
                bool hasActiveAppointments = DataProvider.Instance.Context.Appointments
                    .Any(a => a.StaffId == Doctor.StaffId &&
                             (a.Status == "Đang chờ" || a.Status == "Đang khám") &&
                             a.IsDeleted != true);

                if (hasActiveAppointments)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa bác sĩ này vì còn lịch hẹn đang chờ hoặc đang khám.\n" +
                        "Vui lòng giải quyết các lịch hẹn hiện tại trước khi xóa.",
                        "Cảnh Báo");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowSuccess(
                    $"Bạn có chắc muốn xóa bác sĩ {FullName} không?\n" +
                    "Lưu ý: Tài khoản liên kết với bác sĩ này cũng sẽ bị xóa.",
                    "Xác Nhận Xóa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm và đánh dấu xóa bác sĩ
                        var doctorToDelete = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == Doctor.StaffId);

                        if (doctorToDelete != null)
                        {
                            // Đánh dấu xóa mềm bác sĩ
                            doctorToDelete.IsDeleted = true;

                            // Đồng thời xóa mềm tài khoản liên kết
                            var accountToDelete = DataProvider.Instance.Context.Accounts
                                .FirstOrDefault(a => a.StaffId == Doctor.StaffId);

                            if (accountToDelete != null)
                            {
                                accountToDelete.IsDeleted = true;
                            }

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Hoàn tất transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                "Đã xóa bác sĩ thành công!",
                                "Thành Công");

                            // Đóng cửa sổ
                            _window?.Close();
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy thông tin bác sĩ!",
                                "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa bác sĩ: {ex.Message}",
                    "Lỗi");
            }
        }


        private void ExecuteResetPassword()
        {
            try
            {
                if (Doctor == null || !HasAccount)
                {
                    MessageBoxService.ShowError(
                        "Không thể đặt lại mật khẩu. Bác sĩ này chưa có tài khoản!",
                        "Lỗi");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc muốn đặt lại mật khẩu cho tài khoản này không?\n" +
                    "Mật khẩu mới sẽ là: 1111",
                    "Xác Nhận Đặt Lại Mật Khẩu");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm tài khoản liên kết với bác sĩ
                        var account = DataProvider.Instance.Context.Accounts
                            .FirstOrDefault(a => a.StaffId == Doctor.StaffId && a.IsDeleted != true);

                        if (account != null)
                        {
                            // Đặt lại mật khẩu thành "1111"
                            string hashedPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode("1111"));
                            account.Password = hashedPassword;

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                "Đã đặt lại mật khẩu thành công!\n" +
                                "Mật khẩu mới: 1111",
                                "Thành Công");
                        }
                        else
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy tài khoản liên kết với bác sĩ này!",
                                "Lỗi");

                            // Cập nhật lại trạng thái HasAccount
                            HasAccount = false;
                            CommandManager.InvalidateRequerySuggested();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi đặt lại mật khẩu: {ex.Message}",
                    "Lỗi");
            }
        }


        private void LoadAppointments()
        {
            try
            {
                if (Doctor == null) return;

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.StaffId == Doctor.StaffId &&
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
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}",
                    "Lỗi"
                     
                      );
            }
        }

        private void ViewAppointmentDetails(AppointmentDisplayInfo appointmentInfo)
        {
            // Show appointment details with date and time from the DateTime field
            MessageBoxService.ShowInfo($"Chi tiết lịch hẹn của bệnh nhân {appointmentInfo.PatientName}\n" +
                           $"Ngày: {appointmentInfo.AppointmentDate.ToString("dd/MM/yyyy")}\n" +
                           $"Giờ: {appointmentInfo.AppointmentTimeString}\n" +
                           $"Trạng thái: {appointmentInfo.Status}\n" +
                           $"Lý do khám: {appointmentInfo.Reason}",
                           "Chi tiết lịch hẹn"
                            
                            );
        }

        #region Account Management
        private bool CanAddDoctorAccount()
        {
            if (HasAccount)
                return false;
            // Only allow adding when there's no account and the username is valid
            bool isValidUsername = !string.IsNullOrWhiteSpace(NewUsername) && NewUsername.Trim().Length >= 4 && !HasUsernameErrors;

            return Doctor != null && !HasAccount && isValidUsername && !string.IsNullOrEmpty(Role);
        }

        private void ExecuteAddDoctorAccount()
        {
            try
            {
                if (Doctor == null) return;

                // Bật xác thực cho trường tên đăng nhập
                _isValidating = true;
                _touchedFields.Add(nameof(NewUsername));

                // Kích hoạt xác thực
                OnPropertyChanged(nameof(NewUsername));

                // Kiểm tra lỗi xác thực
                if (HasUsernameErrors)
                {
                    MessageBoxService.ShowError(
                        "Tên đăng nhập không hợp lệ. Vui lòng nhập tên đăng nhập có ít nhất 4 ký tự.",
                        "Lỗi thông tin");
                    return;
                }

                // Kiểm tra tên đăng nhập đã tồn tại chưa
                bool usernameExists = DataProvider.Instance.Context.Accounts
                    .Any(a => a.Username == NewUsername.Trim() && a.IsDeleted != true);

                if (usernameExists)
                {
                    MessageBoxService.ShowError(
                        "Tên đăng nhập đã tồn tại. Vui lòng chọn tên đăng nhập khác.",
                        "Lỗi Dữ Liệu");
                    return;
                }

                // Yêu cầu xác nhận từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn tạo tài khoản cho {FullName} không?\n" +
                    $"Tên đăng nhập: {NewUsername.Trim()}\n" +
                    $"Mật khẩu mặc định: 1111\n" +
                    $"Vai trò: {Role}",
                    "Xác Nhận Tạo Tài Khoản");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tạo tài khoản mới với mật khẩu mặc định "1111"
                        var defaultPassword = "1111";
                        var newAccount = new Account
                        {
                            Username = NewUsername.Trim(),
                            Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(defaultPassword)),
                            StaffId = Doctor.StaffId,
                            Role = Role, // Sử dụng vai trò của nhân viên
                            IsLogined = false,
                            IsDeleted = false
                        };

                        // Thêm tài khoản vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Accounts.Add(newAccount);
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi mọi thứ thành công
                        transaction.Commit();

                        MessageBoxService.ShowSuccess(
                            "Đã tạo tài khoản thành công với mật khẩu mặc định là \"1111\".",
                            "Thành Công");

                        // Cập nhật giao diện để phản ánh tài khoản mới
                        UserName = NewUsername.Trim();
                        HasAccount = true;  // Cập nhật trạng thái tài khoản

                        // Làm mới giao diện
                        OnPropertyChanged(nameof(HasAccount));
                        CommandManager.InvalidateRequerySuggested();
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tạo tài khoản: {ex.Message}",
                    "Lỗi");
            }
        }
        #endregion

        #endregion
    }
}
