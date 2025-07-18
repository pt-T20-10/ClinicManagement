using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho cửa sổ thêm bác sĩ/nhân viên mới
    /// Kế thừa từ BaseViewModel để có sẵn các chức năng INotifyPropertyChanged và RelayCommand
    /// Triển khai IDataErrorInfo để hỗ trợ validation cho giao diện
    /// </summary>
    public class AddDoctorWindowViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties - Các thuộc tính của ViewModel

        // Tham chiếu đến cửa sổ hiện tại
        private Window _window;

        // Thuộc tính bắt buộc của IDataErrorInfo - trả về null vì ta không sử dụng validation cấp object
        public string Error => null;

        // Theo dõi các trường người dùng đã tương tác để chỉ validate khi cần thiết
        private HashSet<string> _touchedFields = new HashSet<string>();
        
        // Cờ để bật validation khi người dùng nhấn Save
        private bool _isValidating = false;

        #region Thông tin bác sĩ/nhân viên

        // Họ và tên - trường bắt buộc
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
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

        // Chuyên khoa đã chọn - chỉ hiển thị khi vai trò là bác sĩ
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

        // Danh sách chuyên khoa để hiển thị trong ComboBox
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

        // Số điện thoại - trường bắt buộc và phải unique
        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
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

        // Email - trường tùy chọn nhưng phải đúng định dạng nếu có nhập
        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
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

        // Lịch làm việc - trường tùy chọn nhưng phải đúng định dạng nếu có nhập
        private string _schedule;
        public string Schedule
        {
            get => _schedule;
            set
            {
                if (_schedule != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
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

        // Địa chỉ - trường tùy chọn
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

        // Link chứng chỉ - trường tùy chọn
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

        #endregion

        #region Thông tin tài khoản

        // Tên đăng nhập - trường tùy chọn để tạo tài khoản
        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
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

        #endregion

        #region Vai trò (Role)

        // Vai trò đã chọn - trường bắt buộc
        private Role _selectedRole;
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();

                // Hiển thị/ẩn phần chọn chuyên khoa dựa trên vai trò
                if (value != null)
                {
                    // Nếu vai trò là bác sĩ thì hiển thị combobox chuyên khoa
                    IsSpecialtyVisible = value.RoleName.Contains("Bác sĩ");
                }
                else
                {
                    IsSpecialtyVisible = false;
                }
            }
        }

        // Danh sách vai trò để hiển thị trong ComboBox
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

        // Thuộc tính điều khiển việc hiển thị ComboBox chuyên khoa
        private bool _isSpecialtyVisible = true; // Mặc định true để tương thích với code cũ
        public bool IsSpecialtyVisible
        {
            get => _isSpecialtyVisible;
            set
            {
                _isSpecialtyVisible = value;
                OnPropertyChanged();
            }
        }

        #endregion

        // Ví dụ định dạng lịch làm việc để hướng dẫn người dùng
        public string ScheduleFormatExample => "Ví dụ: T2, T3, T4: 7h-13h";

        #region Commands - Các lệnh xử lý sự kiện

        public ICommand LoadedWindowCommand { get; private set; }  // Lệnh khi cửa sổ được tải
        public ICommand SaveCommand { get; private set; }          // Lệnh lưu thông tin
        public ICommand CancelCommand { get; private set; }        // Lệnh hủy và đóng cửa sổ

        #endregion

        #endregion

        /// <summary>
        /// Constructor khởi tạo ViewModel
        /// </summary>
        public AddDoctorWindowViewModel()
        {
            InitializeCommands();   // Khởi tạo các command
            LoadData();            // Tải dữ liệu từ database
            ResetForm();           // Đặt lại form về trạng thái ban đầu
        }

        #region Validation - Xác thực dữ liệu

        /// <summary>
        /// Indexer cho IDataErrorInfo - xác thực từng trường dữ liệu
        /// </summary>
        /// <param name="columnName">Tên trường cần xác thực</param>
        /// <returns>Thông báo lỗi hoặc null nếu hợp lệ</returns>
        public string this[string columnName]
        {
            get
            {
                // Chỉ xác thực khi người dùng đã tương tác với trường hoặc khi đang submit form
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(FullName):
                        // Kiểm tra họ tên không được để trống
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ và tên không được để trống";
                        }
                        // Kiểm tra độ dài tối thiểu
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ và tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        // Kiểm tra số điện thoại không được để trống
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Phone))
                        {
                            error = "Số điện thoại không được để trống";
                        }
                        // Kiểm tra định dạng số điện thoại Việt Nam
                        else if (!string.IsNullOrWhiteSpace(Phone) &&
                                !Regex.IsMatch(Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(Email):
                        // Kiểm tra định dạng email nếu có nhập
                        if (!string.IsNullOrWhiteSpace(Email) &&
                            !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(Schedule):
                        // Kiểm tra định dạng lịch làm việc nếu có nhập
                        if (!string.IsNullOrWhiteSpace(Schedule) && !IsValidScheduleFormat(Schedule))
                        {
                            error = "Lịch làm việc không đúng định dạng. Vui lòng nhập theo mẫu: T2, T3, T4: 7h-13h";
                        }
                        break;

                    case nameof(UserName):
                        // Kiểm tra độ dài tối thiểu của tên đăng nhập
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
        /// Kiểm tra xem có lỗi validation nào không
        /// </summary>
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
        /// Xác thực định dạng lịch làm việc theo các mẫu được phép
        /// Hỗ trợ các định dạng: "T2, T3, T4: 7h-13h", "T2-T7: 7h-11h", v.v.
        /// </summary>
        /// <param name="schedule">Chuỗi lịch làm việc cần kiểm tra</param>
        /// <returns>True nếu hợp lệ, False nếu không hợp lệ</returns>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Lịch làm việc trống là hợp lệ (không bắt buộc)

            // Các pattern regex để kiểm tra định dạng lịch làm việc
            // Pattern 1: "T2, T3, T4: 7h-13h" (danh sách các ngày)
            string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            
            // Pattern 2: "T2-T7: 7h-11h" (khoảng ngày)
            string pattern2 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            
            // Pattern 3: "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h" (nhiều ca làm việc)
            string pattern3 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            
            // Pattern 4: Khoảng ngày với nhiều ca
            string pattern4 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            
            // Pattern 5: Định dạng có phút cụ thể
            string pattern5 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h\d{2}-\d{1,2}h\d{2}(, \d{1,2}h\d{2}-\d{1,2}h\d{2})*$";

            // Kiểm tra xem có pattern nào khớp không
            if (Regex.IsMatch(schedule, pattern1) ||
                Regex.IsMatch(schedule, pattern2) ||
                Regex.IsMatch(schedule, pattern3) ||
                Regex.IsMatch(schedule, pattern4) ||
                Regex.IsMatch(schedule, pattern5))
            {
                try
                {
                    // Phân tích và kiểm tra tính logic của các khoảng thời gian
                    string[] parts = schedule.Split(':');
                    if (parts.Length < 2)
                        return false;

                    // Lấy phần thời gian sau dấu ':')
                    string timeSection = string.Join(":", parts.Skip(1)).Trim();
                    var timeRanges = timeSection.Split(',');

                    // Kiểm tra từng khoảng thời gian
                    foreach (var range in timeRanges)
                    {
                        var times = range.Trim().Split('-');
                        if (times.Length == 2)
                        {
                            var start = ParseTimeString(times[0].Trim());
                            var end = ParseTimeString(times[1].Trim());
                            
                            // Kiểm tra định dạng thời gian hợp lệ
                            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                                return false; // Định dạng thời gian không hợp lệ
                                
                            // Kiểm tra thời gian bắt đầu phải nhỏ hơn thời gian kết thúc
                            if (start >= end)
                                return false;
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

        /// <summary>
        /// Phân tích chuỗi thời gian thành TimeSpan
        /// Hỗ trợ các định dạng: "8h", "8h30", "13h", "13h30", v.v.
        /// </summary>
        /// <param name="timeStr">Chuỗi thời gian cần phân tích</param>
        /// <returns>TimeSpan tương ứng hoặc TimeSpan.Zero nếu không hợp lệ</returns>
        private TimeSpan ParseTimeString(string timeStr)
        {
            // Chuyển đổi định dạng "8h30" thành "8:30"
            timeStr = timeStr.Replace("h", ":").Replace(" ", "");
            if (timeStr.EndsWith(":")) timeStr += "00";
            
            var parts = timeStr.Split(':');
            
            // Xử lý định dạng "giờ:phút"
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return new TimeSpan(h, m, 0);
                
            // Xử lý định dạng chỉ có giờ
            if (parts.Length == 1 && int.TryParse(parts[0], out h))
                return new TimeSpan(h, 0, 0);
                
            return TimeSpan.Zero;
        }

        #endregion

        #region Commands - Khởi tạo các lệnh

        /// <summary>
        /// Khởi tạo tất cả các command cho ViewModel
        /// </summary>
        private void InitializeCommands()
        {
            // Command khi cửa sổ được tải - lưu tham chiếu đến window
            LoadedWindowCommand = new RelayCommand<Window>((w) => _window = w, (w) => true);

            // Command lưu thông tin - kiểm tra điều kiện trước khi thực thi
            SaveCommand = new RelayCommand<object>(
                (p) => ExecuteSave(),
                (p) => CanSave()
            );

            // Command hủy và đóng cửa sổ
            CancelCommand = new RelayCommand<object>(
                (p) => ExecuteCancel(),
                (p) => true
            );
        }

        #endregion

        #region Methods - Các phương thức xử lý

        /// <summary>
        /// Tải dữ liệu từ database (chuyên khoa, vai trò)
        /// </summary>
        private void LoadData()
        {
            // Tải danh sách chuyên khoa (chỉ những chuyên khoa chưa bị xóa)
            SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                DataProvider.Instance.Context.DoctorSpecialties
                .Where(s => s.IsDeleted != true)
                .ToList()
            );

            // Tải danh sách vai trò
            var roles = DataProvider.Instance.Context.Roles
                .Where(r => r.IsDeleted != true)
                .ToList();

            // Kiểm tra xem đã có ai có vai trò Quản lý chưa
            bool hasAdmin = DataProvider.Instance.Context.Staffs
                .Include(s => s.Role)
                .Any(s => s.Role != null && s.Role.RoleName == "Quản lí" && s.IsDeleted != true);

            // Nếu đã có Quản lý rồi thì loại bỏ khỏi danh sách có thể chọn
            if (hasAdmin)
            {
                roles = roles.Where(r => r.RoleName != "Quản lí").ToList();
            }

            RoleList = new ObservableCollection<Role>(roles);

            // Mặc định chọn vai trò Bác sĩ
            SelectedRole = RoleList.FirstOrDefault(r => r.RoleName == "Bác sĩ");
        }

        /// <summary>
        /// Kiểm tra điều kiện để có thể lưu
        /// </summary>
        /// <returns>True nếu có thể lưu, False nếu chưa đủ điều kiện</returns>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedRole != null &&
                   (!IsSpecialtyVisible || SelectedSpecialty != null); // Chỉ yêu cầu chuyên khoa nếu đang hiển thị
        }

        /// <summary>
        /// Thực hiện lưu thông tin nhân viên mới
        /// </summary>
        private void ExecuteSave()
        {
            try
            {
                // Bật validation cho tất cả các trường
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Schedule));

                // Kích hoạt validation cho các trường bắt buộc
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Schedule));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm nhân viên.",
                        "Lỗi thông tin");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra số điện thoại đã tồn tại chưa
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && (bool)!d.IsDeleted);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowWarning(
                                "Số điện thoại này đã được sử dụng bởi một nhân viên khác.",
                                "Lỗi Dữ Liệu");
                            return;
                        }

                        // Kiểm tra email đã tồn tại chưa (nếu có nhập)
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

                        // Tạo và lưu đối tượng Staff mới
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

                        // Biến theo dõi việc tạo tài khoản
                        bool createAccount = false;
                        string accountMessage = "";

                        // Tạo tài khoản nếu có nhập tên đăng nhập
                        if (!string.IsNullOrWhiteSpace(UserName))
                        {
                            // Kiểm tra tên đăng nhập đã tồn tại chưa
                            bool usernameExists = DataProvider.Instance.Context.Accounts
                                .Any(a => a.Username == UserName.Trim() && (bool)!a.IsDeleted);

                            if (usernameExists)
                            {
                                accountMessage = "Tên đăng nhập đã tồn tại. Tài khoản không được tạo nhưng thông tin nhân viên đã được lưu.";
                            }
                            else
                            {
                                // Tạo tài khoản với mật khẩu mặc định "1111"
                                var defaultPassword = "1111";
                                var newAccount = new Account
                                {
                                    Username = UserName.Trim(),
                                    Password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(defaultPassword)),
                                    StaffId = newStaff.StaffId,
                                    Role = SelectedRole.RoleName, // Sử dụng tên vai trò đã chọn
                                    IsLogined = false,
                                    IsDeleted = false
                                };

                                DataProvider.Instance.Context.Accounts.Add(newAccount);
                                DataProvider.Instance.Context.SaveChanges();
                                createAccount = true;
                                accountMessage = "Tài khoản được tạo thành công với mật khẩu mặc định là \"1111\".";
                            }
                        }

                        // Commit transaction
                        transaction.Commit();

                        // Hiển thị thông báo thành công
                        if (!string.IsNullOrEmpty(accountMessage))
                        {
                            // Chỉ hiển thị thông báo tài khoản nếu có cố gắng tạo
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

                        // Đóng cửa sổ
                        _window?.Close();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại exception để xử lý ở catch bên ngoài
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

        /// <summary>
        /// Thực hiện hủy và đóng cửa sổ
        /// </summary>
        private void ExecuteCancel()
        {
            // Đặt lại form về trạng thái ban đầu
            ResetForm();

            // Đóng cửa sổ
            _window?.Close();
        }

        /// <summary>
        /// Đặt lại form về trạng thái ban đầu
        /// </summary>
        private void ResetForm()
        {
            // Xóa tất cả dữ liệu nhập
            FullName = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Schedule = string.Empty;
            Address = string.Empty;
            CertificateLink = string.Empty;
            UserName = string.Empty;

            // Đặt lại giá trị mặc định cho Role và Specialty
            if (RoleList != null && RoleList.Count > 0)
            {
                SelectedRole = RoleList.FirstOrDefault(r => r.RoleName == "Bác sĩ") ?? RoleList.FirstOrDefault();
            }

            if (SpecialtyList != null && SpecialtyList.Count > 0 && IsSpecialtyVisible)
            {
                SelectedSpecialty = SpecialtyList.FirstOrDefault();
            }

            // Đặt lại trạng thái validation
            _touchedFields.Clear();
            _isValidating = false;
        }

        #endregion
    }
}