using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.UserControlToUse;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{

    public class AppointmentDetailsViewModel : BaseViewModel, IDataErrorInfo
    {
        // Tham chiếu đến cửa sổ hiện tại
        private Window _window;

        #region Properties 

        #region Authentication

        // Quyền tiếp nhận lịch hẹn - áp dụng cho bác sĩ và quản lý
        private bool _canAcceptAppointment = false;
        public bool CanAcceptAppointment
        {
            get => _canAcceptAppointment;
            set
            {
                _canAcceptAppointment = value;
                OnPropertyChanged();
            }
        }

        // Kiểm tra xem bác sĩ hiện tại có thể tiến hành khám bệnh này không
        // Phụ thuộc vào việc lịch hẹn đã được chỉ định cho bác sĩ nào chưa
        private bool _canCurrentDoctorAccept = false;
        public bool CanCurrentDoctorAccept
        {
            get => _canCurrentDoctorAccept;
            set
            {
                _canCurrentDoctorAccept = value;
                OnPropertyChanged();
            }
        }

        // Tài khoản người dùng hiện tại để kiểm tra quyền
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                UpdatePermissions(); // Cập nhật quyền khi tài khoản thay đổi
            }
        }

        #endregion

        #region Thông tin lịch hẹn

        // Lịch hẹn gốc từ database
        private Appointment _originalAppointment;
        public Appointment OriginalAppointment
        {
            get => _originalAppointment;
            set
            {
                _originalAppointment = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadAppointmentData(); // Tải dữ liệu liên quan khi có lịch hẹn
                }
            }
        }

        // Ngày hẹn - có thể chỉnh sửa
        private DateTime? _appointmentDate;
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                _appointmentDate = value;
                OnPropertyChanged();
                if (value.HasValue && SelectedAppointmentTime.HasValue)
                {
                    // Khi ngày thay đổi, validate lại thời gian
                    ValidateDateTimeSelection();
                }
            }
        }

        // Giờ hẹn - có thể chỉnh sửa
        private DateTime? _selectedAppointmentTime;
        public DateTime? SelectedAppointmentTime
        {
            get => _selectedAppointmentTime;
            set
            {
                _selectedAppointmentTime = value;
                OnPropertyChanged();
                if (value.HasValue && AppointmentDate.HasValue)
                {
                    // Khi giờ thay đổi, validate lại thời gian
                    ValidateDateTimeSelection();
                }
            }
        }

        // Bác sĩ được chọn - có thể null (chưa chỉ định bác sĩ)
        private Staff _selectedDoctor;
        public Staff SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
                if (value != null && AppointmentDate.HasValue && SelectedAppointmentTime.HasValue)
                {
                    // Khi bác sĩ thay đổi, validate lại thời gian
                    ValidateDateTimeSelection();
                }
                // Cập nhật lại quyền khi thay đổi bác sĩ
                UpdatePermissions();
            }
        }

        // Danh sách bác sĩ để chọn (bao gồm option "Không chỉ định bác sĩ")
        private ObservableCollection<Staff> _doctorList;
        public ObservableCollection<Staff> DoctorList
        {
            get => _doctorList;
            set
            {
                _doctorList = value;
                OnPropertyChanged();
            }
        }

        // Loại lịch hẹn được chọn
        private AppointmentType _selectedAppointmentType;
        public AppointmentType SelectedAppointmentType
        {
            get => _selectedAppointmentType;
            set
            {
                _selectedAppointmentType = value;
                OnPropertyChanged();
            }
        }

        // Danh sách loại lịch hẹn
        private ObservableCollection<AppointmentType> _appointmentTypes;
        public ObservableCollection<AppointmentType> AppointmentTypes
        {
            get => _appointmentTypes;
            set
            {
                _appointmentTypes = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Validation thời gian

        // Trạng thái validation thời gian hợp lệ
        private bool _isTimeSlotValid = true;
        public bool IsTimeSlotValid
        {
            get => _isTimeSlotValid;
            set
            {
                _isTimeSlotValid = value;
                OnPropertyChanged();
            }
        }

        // Thông báo lỗi validation thời gian
        private string _timeSlotError = "";
        public string TimeSlotError
        {
            get => _timeSlotError;
            set
            {
                _timeSlotError = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region IDataErrorInfo

        // Thuộc tính bắt buộc của IDataErrorInfo
        public string Error => null;
        
        // Theo dõi các trường người dùng đã tương tác
        private HashSet<string> _touchedFields = new HashSet<string>();
        
        // Cờ để bật validation khi cần thiết
        private bool _isValidating = false;

        #endregion

        #endregion

        #region Commands - Các lệnh xử lý sự kiện

        public ICommand CancelAppointmentCommand { get; set; }         // Lệnh hủy lịch hẹn
        public ICommand EditAppointmentCommand { get; set; }           // Lệnh chỉnh sửa lịch hẹn
        public ICommand AcceptAppointmentCommand { get; set; }         // Lệnh tiếp nhận lịch hẹn
        public ICommand ConfirmTimeSelectionCommand { get; set; }      // Lệnh xác nhận chọn thời gian
        public ICommand CancelTimeSelectionCommand { get; set; }       // Lệnh hủy chọn thời gian
        public ICommand LoadedWindowCommand { get; set; }              // Lệnh khi cửa sổ được tải

        #endregion

        #region Constructors - Các hàm khởi tạo

        /// <summary>
        /// Constructor mặc định
        /// </summary>
        public AppointmentDetailsViewModel()
        {
            InitializeCommands();
            LoadStaffs();
            LoadAppointmentTypes();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
        }

        /// <summary>
        /// Constructor nhận đối tượng Appointment để khởi tạo ViewModel
        /// </summary>
        /// <param name="appointment">Lịch hẹn cần xem hoặc chỉnh sửa</param>
        public AppointmentDetailsViewModel(Appointment appointment)
        {
            // Tải tất cả dữ liệu cần thiết trước
            InitializeCommands();
            LoadStaffs();
            LoadAppointmentTypes();

            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

            // Thiết lập lịch hẹn sau khi tải dependencies
            OriginalAppointment = appointment;
        }

        #endregion

        #region Initialization Methods - Các phương thức khởi tạo

        /// <summary>
        /// Khởi tạo tất cả các command cho ViewModel
        /// </summary>
        private void InitializeCommands()
        {
            // Command khi cửa sổ được tải
            LoadedWindowCommand = new RelayCommand<Window>(
             (w) => {
                 _window = w;

                 // Làm mới tài khoản hiện tại
                 var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                 if (mainVM != null)
                 {
                     CurrentAccount = mainVM.CurrentAccount;
                 }
             },
             (w) => true
         );

            // Command tiếp nhận lịch hẹn
            AcceptAppointmentCommand = new RelayCommand<object>(
             (p) => AcceptAppointment(),
             // Đảm bảo CanExecute handler khớp với trạng thái hiển thị
             (p) => CanAcceptAppointment &&
                    CanCurrentDoctorAccept &&
                    OriginalAppointment != null &&
                    OriginalAppointment.Status == "Đang chờ" &&
                    OriginalAppointment.Status != "Đã hủy"
         );

            // Command chỉnh sửa lịch hẹn
            EditAppointmentCommand = new RelayCommand<object>(
                (p) => EditAppointment(),
                (p) => CanEditAppointment()
            );

            // Command hủy lịch hẹn
            CancelAppointmentCommand = new RelayCommand<object>(
      (p) => CancelAppointment(),
      (p) =>
          CanAcceptAppointment &&
          CanCurrentDoctorAccept &&
          OriginalAppointment != null &&
          OriginalAppointment.Status == "Đang chờ" &&
          OriginalAppointment.Status != "Đã hủy"
  );


            // Command xác nhận chọn thời gian
            ConfirmTimeSelectionCommand = new RelayCommand<DateTime?>(
                (time) => {
                    if (time.HasValue)
                    {
                        SelectedAppointmentTime = time;
                    }
                },
                (time) => time.HasValue
            );

            // Command hủy chọn thời gian
            CancelTimeSelectionCommand = new RelayCommand<object>(
                (p) => { },
                (p) => true
            );
        }

        /// <summary>
        /// Tải danh sách nhân viên (bác sĩ) từ database
        /// </summary>
        private void LoadStaffs()
        {
            try
            {
                // Tải tất cả nhân viên chưa bị xóa, sắp xếp theo tên
                var staffs = DataProvider.Instance.Context.Staffs
                    .Where(d => d.IsDeleted != true && d.RoleId == 1) // RoleId == 1 là bác sĩ
                    .OrderBy(d => d.FullName)
                    .ToList();

                // Tạo danh sách mới với option "Không chỉ định bác sĩ" ở đầu
                var doctorListWithNull = new List<Staff>();
                doctorListWithNull.Add(null); // Thêm null làm item đầu tiên
                doctorListWithNull.AddRange(staffs);

                DoctorList = new ObservableCollection<Staff>(doctorListWithNull);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải dữ liệu chi tiết của lịch hẹn từ database
        /// </summary>
        private void LoadAppointmentData()
        {
            if (OriginalAppointment == null) return;

            try
            {
                // Đảm bảo có tất cả dữ liệu liên quan của lịch hẹn
                _originalAppointment = DataProvider.Instance.Context.Appointments
              .Include(a => a.Patient)
              .Include(a => a.Staff)
              .Include(a => a.AppointmentType)
              .FirstOrDefault(a => a.AppointmentId == _originalAppointment.AppointmentId);
                OnPropertyChanged(nameof(OriginalAppointment));

                if (OriginalAppointment == null) return;

                // Thiết lập ngày và giờ hẹn
                AppointmentDate = OriginalAppointment.AppointmentDate.Date;
                SelectedAppointmentTime = new DateTime(
                    OriginalAppointment.AppointmentDate.Year,
                    OriginalAppointment.AppointmentDate.Month,
                    OriginalAppointment.AppointmentDate.Day,
                    OriginalAppointment.AppointmentDate.Hour,
                    OriginalAppointment.AppointmentDate.Minute,
                    0);

                // Thiết lập bác sĩ (có thể null)
                SelectedDoctor = OriginalAppointment.Staff;

                // Thiết lập loại lịch hẹn
                SelectedAppointmentType = OriginalAppointment.AppointmentType;

                // Validate thời gian đã chọn
                ValidateDateTimeSelection();

                // Cập nhật quyền dựa trên thông tin lịch hẹn đã tải
                UpdatePermissions();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải danh sách loại lịch hẹn từ database
        /// </summary>
        private void LoadAppointmentTypes()
        {
            try
            {
                var types = DataProvider.Instance.Context.AppointmentTypes
                    .Where(at => at.IsDeleted != true)
                    .OrderBy(at => at.TypeName)
                    .ToList();

                AppointmentTypes = new ObservableCollection<AppointmentType>(types);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi tải danh sách loại lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        #endregion

        #region Command Methods - Các phương thức xử lý lệnh

        /// <summary>
        /// Kiểm tra điều kiện có thể chỉnh sửa lịch hẹn
        /// Dựa trên vai trò người dùng và trạng thái lịch hẹn
        /// </summary>
        /// <returns>True nếu có thể chỉnh sửa, False nếu không được phép</returns>
        private bool CanEditAppointment()
        {
            // Các điều kiện cơ bản phải đúng để có thể chỉnh sửa
            if (OriginalAppointment == null ||
                OriginalAppointment.Status == "Đã hủy" ||
                OriginalAppointment.Status == "Đã khám" ||
                !AppointmentDate.HasValue ||
                !SelectedAppointmentTime.HasValue ||
                SelectedAppointmentType == null)
            {
                return false;
            }

            // Lấy vai trò người dùng
            string role = CurrentAccount?.Role?.Trim() ?? string.Empty;

            // Quản lí và Thu ngân có thể chỉnh sửa bất kỳ lịch hẹn nào ở trạng thái "Đang chờ"
            if (role.Equals("Quản lí", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Chỉ cho phép sửa lịch hẹn "Đang chờ"
                if (OriginalAppointment.Status == "Đang chờ")
                {
                    return true; // Không validate thời gian tại thời điểm này
                }
            }
            // Nếu người dùng là bác sĩ
            else if (role.Contains("Bác sĩ"))
            {
                // Nếu lịch hẹn đã có bác sĩ được chỉ định và người dùng không phải bác sĩ đó
                if (OriginalAppointment.StaffId.HasValue && OriginalAppointment.StaffId != CurrentAccount.StaffId)
                {
                    return false; // Bác sĩ khác không được chỉnh sửa
                }

                // Bác sĩ chỉ có thể sửa lịch hẹn ở trạng thái "Đang chờ"
                if (OriginalAppointment.Status == "Đang chờ")
                {
                    return true; // Không validate thời gian tại thời điểm này
                }
            }

            // Mặc định không cho phép chỉnh sửa với các vai trò khác
            return false;
        }

        /// <summary>
        /// Thực hiện chỉnh sửa lịch hẹn
        /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
        /// </summary>
        private void EditAppointment()
        {
            try
            {
                // Kiểm tra xem lịch hẹn có tồn tại không
                if (OriginalAppointment == null) return;

                // Xác thực thời gian đã chọn
                if (!ValidateDateTimeSelection())
                {
                    MessageBoxService.ShowError(TimeSlotError, "Lỗi - Thời gian không hợp lệ");
                    return;
                }

                // Kiểm tra loại lịch hẹn đã được chọn chưa
                if (SelectedAppointmentType == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn loại lịch hẹn", "Thiếu thông tin");
                    return;
                }

                // Kiểm tra quyền chỉnh sửa bác sĩ nếu người dùng là bác sĩ
                if (CurrentAccount?.Role?.Contains("Bác sĩ") == true &&
                    OriginalAppointment.StaffId.HasValue &&
                    OriginalAppointment.StaffId != CurrentAccount.StaffId)
                {
                    MessageBoxService.ShowWarning(
                        "Bạn không có quyền chỉnh sửa lịch hẹn đã được chỉ định cho bác sĩ khác",
                        "Không đủ quyền");
                    return;
                }

                // Kết hợp ngày và giờ thành thời gian lịch hẹn thực tế
                DateTime appointmentDateTime = new DateTime(
                    AppointmentDate.Value.Year,
                    AppointmentDate.Value.Month,
                    AppointmentDate.Value.Day,
                    SelectedAppointmentTime.Value.Hour,
                    SelectedAppointmentTime.Value.Minute,
                    0);

                // Yêu cầu xác nhận từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có muốn cập nhật lịch hẹn với thông tin sau không?\n" +
                    $"- Bác sĩ: {(SelectedDoctor?.FullName ?? "Không có bác sĩ")}\n" +
                    $"- Loại lịch hẹn: {SelectedAppointmentType.TypeName}\n" +
                    $"- Ngày: {AppointmentDate?.ToString("dd/MM/yyyy")}\n" +
                    $"- Giờ: {SelectedAppointmentTime?.ToString("HH:mm")}",
                    "Xác nhận cập nhật");

                if (!result)
                    return;

                // Sử dụng giao dịch để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm lịch hẹn trong cơ sở dữ liệu
                        var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                            .FirstOrDefault(a => a.AppointmentId == OriginalAppointment.AppointmentId);

                        if (appointmentToUpdate != null)
                        {
                            // Cập nhật thông tin lịch hẹn
                            appointmentToUpdate.StaffId = SelectedDoctor?.StaffId; // Có thể null nếu không chọn bác sĩ
                            appointmentToUpdate.AppointmentDate = appointmentDateTime;
                            appointmentToUpdate.AppointmentTypeId = SelectedAppointmentType.AppointmentTypeId;

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi thành công
                            transaction.Commit();

                            // Làm mới dữ liệu lịch hẹn từ cơ sở dữ liệu
                            OriginalAppointment = DataProvider.Instance.Context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Staff)
                                .Include(a => a.AppointmentType)
                                .FirstOrDefault(a => a.AppointmentId == OriginalAppointment.AppointmentId);

                            // Cập nhật lại quyền tiếp nhận sau khi cập nhật lịch hẹn
                            UpdatePermissions();

                            MessageBoxService.ShowSuccess("Lịch hẹn đã được cập nhật thành công!", "Thành công");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu xảy ra lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để được xử lý bởi khối catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi cập nhật lịch hẹn: {ex.Message}", "Lỗi");
            }
        }
        // Thêm hàm kiểm tra quyền hủy lịch hẹn
        private bool CanCancelAppointment(Appointment appointment)
        {
            if (appointment == null ||
                appointment.Status == "Đã hủy" ||
                appointment.Status == "Đã khám" ||
                appointment.Status == "Đang khám")
                return false;

            // Nếu lịch hẹn đã có bác sĩ chỉ định
            if (appointment.StaffId.HasValue)
            {
                var role = CurrentAccount?.Role?.Trim() ?? string.Empty;
                var isDoctor = role.Contains("Bác sĩ");
                var isCashier = role.Equals("Thu ngân", StringComparison.OrdinalIgnoreCase);
                var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                var isManager = role.Equals("Quản lí", StringComparison.OrdinalIgnoreCase);

                // Chỉ bác sĩ được chỉ định, thu ngân, admin, manager mới được hủy
                if (isDoctor && CurrentAccount?.StaffId == appointment.StaffId)
                    return true;
                if (isCashier || isAdmin || isManager)
                    return true;
                return false;
            }

            // Nếu chưa có bác sĩ chỉ định thì ai cũng có thể hủy (giữ nguyên logic cũ)
            return true;
        }
        /// <summary>
        /// Thực hiện hủy lịch hẹn
        /// Yêu cầu lý do hủy và sử dụng transaction
        /// </summary>
        /// 
        private void CancelAppointment()
        {
            try
            {
                // Kiểm tra xem lịch hẹn có tồn tại không
                if (OriginalAppointment == null) return;

                // Kiểm tra xem có lý do hủy lịch hẹn không
                if (string.IsNullOrWhiteSpace(OriginalAppointment.Notes))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập lý do hủy lịch hẹn.", "Thiếu thông tin");
                    return;
                }

                // Yêu cầu xác nhận từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy");

                if (!result)
                    return;

                // Sử dụng giao dịch để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm lịch hẹn trong cơ sở dữ liệu
                        var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                            .FirstOrDefault(a => a.AppointmentId == OriginalAppointment.AppointmentId);

                        if (appointmentToUpdate != null)
                        {
                            // Cập nhật trạng thái lịch hẹn thành "Đã hủy"
                            appointmentToUpdate.Status = "Đã hủy";
                            // Lý do hủy đã được cập nhật thông qua binding với OriginalAppointment.Notes

                            // Lưu thay đổi vào cơ sở dữ liệu
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi thành công
                            transaction.Commit();

                            // Làm mới dữ liệu lịch hẹn từ cơ sở dữ liệu
                            OriginalAppointment = DataProvider.Instance.Context.Appointments
                                .Include(a => a.Patient)
                                .Include(a => a.Staff)
                                .Include(a => a.AppointmentType)
                                .FirstOrDefault(a => a.AppointmentId == OriginalAppointment.AppointmentId);

                            MessageBoxService.ShowSuccess("Lịch hẹn đã được hủy thành công!", "Thành công");

                            // Đóng cửa sổ sau khi hủy
                            _window?.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu xảy ra lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để được xử lý bởi khối catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi hủy lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Thực hiện tiếp nhận lịch hẹn và chuyển sang tab khám bệnh
        /// Cập nhật trạng thái thành "Đang khám" và mở ExamineViewModel
        /// </summary>
        private void AcceptAppointment()
        {
            bool updateSuccessful = false;

            try
            {
                // Kiểm tra xem lịch hẹn có tồn tại không
                if (OriginalAppointment == null)
                    return;

            

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Cập nhật trạng thái lịch hẹn thành "Đang khám"
                        var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                            .FirstOrDefault(a => a.AppointmentId == OriginalAppointment.AppointmentId);

                        if (appointmentToUpdate != null)
                        {
                            // Cập nhật trạng thái thành "Đang khám"
                            appointmentToUpdate.Status = "Đang khám";

                            // Kiểm tra nếu CurrentAccount là bác sĩ, thì cập nhật StaffId cho lịch hẹn
                            if (CurrentAccount != null && CurrentAccount.Role?.Contains("Bác sĩ") == true)
                            {
                                // Lấy thông tin bác sĩ từ tài khoản hiện tại
                                var doctorInfo = DataProvider.Instance.Context.Staffs
                                    .FirstOrDefault(s => s.StaffId == CurrentAccount.StaffId && s.IsDeleted != true);

                                if (doctorInfo != null)
                                {
                                    // Cập nhật thông tin bác sĩ đang khám
                                    appointmentToUpdate.StaffId = doctorInfo.StaffId;

                                    // Cập nhật lại thông tin OriginalAppointment để hiển thị đúng
                                    OriginalAppointment.StaffId = doctorInfo.StaffId;
                                    OriginalAppointment.Staff = doctorInfo;
                                }
                            }

                            DataProvider.Instance.Context.SaveChanges();
                        }

                        // Commit transaction trước khi chuyển đến tab khám bệnh
                        transaction.Commit();
                        updateSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để được xử lý bởi khối catch bên ngoài
                    }
                }

                // Chỉ tiến hành các thao tác UI nếu cập nhật database thành công
                if (updateSuccessful)
                {
                    try
                    {
                        // Lấy MainWindow
                        var mainWindow = Application.Current.MainWindow;
                        if (mainWindow == null)
                        {
                            throw new Exception("Không thể tìm thấy cửa sổ chính của ứng dụng.");
                        }

                        // Tìm TabControl trong MainWindow
                        var mainTabControl = mainWindow.FindName("MainTabControl") as TabControl;
                        if (mainTabControl == null)
                        {
                            throw new Exception("Không thể tìm thấy TabControl trong cửa sổ chính.");
                        }

                        // Chọn tab ExamineUC (index 2)
                        mainTabControl.SelectedIndex = 2;

                        // Tìm ExamineUC user control
                        var examineUC = mainTabControl.SelectedContent as ExamineUC;
                        if (examineUC == null)
                        {
                            throw new Exception("Không thể tìm thấy giao diện khám bệnh.");
                        }

                        // Tạo ExamineViewModel mới với thông tin Patient và Appointment
                        var examineViewModel = new ExamineViewModel(OriginalAppointment.Patient, OriginalAppointment);

                        // Đặt DataContext cho ExamineUC
                        examineUC.DataContext = examineViewModel;

                        MessageBoxService.ShowSuccess($"Đã chuyển đến phần khám bệnh cho bệnh nhân {OriginalAppointment.Patient.FullName}.", "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Chỉ hiển thị lỗi chuyển tab, không ảnh hưởng đến transaction đã commit
                        MessageBoxService.ShowError($"Đã cập nhật trạng thái lịch hẹn thành công nhưng không thể chuyển tab: {ex.Message}", "Lỗi");
                    }
                    finally
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            _window?.Close();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi tiến hành khám: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật quyền dựa trên vai trò người dùng và trạng thái lịch hẹn
        /// </summary>
        private void UpdatePermissions()
        {
            // Mặc định không ai có quyền chỉnh sửa
            CanAcceptAppointment = false;
            CanCurrentDoctorAccept = false;

            // Kiểm tra xem tài khoản và lịch hẹn có tồn tại không
            if (CurrentAccount == null || OriginalAppointment == null)
                return;

            // Lấy vai trò người dùng
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Nếu lịch hẹn ở trạng thái "Đang chờ" thì mới kiểm tra quyền chỉnh sửa
            if (OriginalAppointment.Status == "Đang chờ")
            {
                // Kiểm tra quyền tiếp nhận lịch hẹn dựa trên vai trò và tình trạng lịch hẹn
                if (role.Contains("Bác sĩ"))
                {
                    // Người dùng là bác sĩ
                    CanAcceptAppointment = true;

                    // Kiểm tra xem lịch hẹn có được chỉ định cho bác sĩ nào không
                    if (OriginalAppointment.StaffId.HasValue)
                    {
                        // Lịch hẹn đã có bác sĩ - chỉ bác sĩ đó mới có thể tiếp nhận
                        CanCurrentDoctorAccept = OriginalAppointment.StaffId == CurrentAccount.StaffId;
                    }
                    else
                    {
                        // Lịch hẹn chưa có bác sĩ - bất kỳ bác sĩ nào cũng có thể tiếp nhận
                        CanCurrentDoctorAccept = true;
                    }
                }
                else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                        role.Equals("Quản lí", StringComparison.OrdinalIgnoreCase))
                {
                    // Quản trị viên hoặc quản lý luôn có quyền tiếp nhận
                    CanAcceptAppointment = true;
                    CanCurrentDoctorAccept = true;
                }
            }

            // Đảm bảo trạng thái các nút được cập nhật
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Validation Methods - Các phương thức xác thực

        /// <summary>
        /// Indexer cho IDataErrorInfo - xác thực từng trường dữ liệu
        /// </summary>
        /// <param name="columnName">Tên trường cần xác thực</param>
        /// <returns>Thông báo lỗi hoặc null nếu hợp lệ</returns>
        public string this[string columnName]
        {
            get
            {
                // Chỉ validate khi người dùng đã tương tác với form
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(SelectedDoctor):
                        // Không cần validation - bác sĩ là tùy chọn
                        break;

                    case nameof(AppointmentDate):
                        if (!AppointmentDate.HasValue)
                        {
                            error = "Vui lòng chọn ngày hẹn";
                        }
                        else if (AppointmentDate.Value.Date < DateTime.Today)
                        {
                            error = "Ngày hẹn không thể trong quá khứ";
                        }
                        break;

                    case nameof(SelectedAppointmentTime):
                        if (!SelectedAppointmentTime.HasValue)
                        {
                            error = "Vui lòng chọn giờ hẹn";
                        }
                        else if (!IsTimeSlotValid)
                        {
                            error = TimeSlotError;
                        }
                        break;

                    case nameof(SelectedAppointmentType):
                        if (SelectedAppointmentType == null)
                        {
                            error = "Vui lòng chọn loại lịch hẹn";
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Xác thực lựa chọn ngày và giờ hẹn
        /// Kiểm tra tính hợp lệ, trùng lặp và lịch làm việc của bác sĩ
        /// </summary>
        /// <returns>True nếu hợp lệ, False nếu có lỗi</returns>
        public bool ValidateDateTimeSelection()
        {
            // Kiểm tra đã chọn đầy đủ ngày và giờ chưa
            if (!AppointmentDate.HasValue || !SelectedAppointmentTime.HasValue)
            {
                IsTimeSlotValid = false;
                TimeSlotError = "Vui lòng chọn đầy đủ ngày và giờ hẹn";
                return false;
            }

            // Kết hợp ngày và giờ
            DateTime appointmentDateTime = new DateTime(
                AppointmentDate.Value.Year,
                AppointmentDate.Value.Month,
                AppointmentDate.Value.Day,
                SelectedAppointmentTime.Value.Hour,
                SelectedAppointmentTime.Value.Minute,
                0);

            // Kiểm tra lịch hẹn có ở quá khứ không
            if (appointmentDateTime < DateTime.Now)
            {
                IsTimeSlotValid = false;
                TimeSlotError = "Thời gian hẹn đã qua, vui lòng chọn thời gian trong tương lai";
                return false;
            }

            // Kiểm tra giờ hẹn có trong khung giờ làm việc không (7:00 đến 17:00)
            TimeSpan minTime = new TimeSpan(7, 0, 0);  // 07:00
            TimeSpan maxTime = new TimeSpan(17, 0, 0); // 17:00
            TimeSpan selectedTime = appointmentDateTime.TimeOfDay;

            if (selectedTime < minTime || selectedTime > maxTime)
            {
                IsTimeSlotValid = false;
                TimeSlotError = "Giờ hẹn chỉ được phép trong khoảng từ 07:00 đến 17:00";
                return false;
            }

            try
            {
                // Kiểm tra lịch hẹn của bệnh nhân - xem có trùng ngày/giờ không
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == OriginalAppointment.PatientId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date &&
                        a.AppointmentId != OriginalAppointment.AppointmentId) // Loại trừ lịch hẹn hiện tại
                    .ToList();

                // Kiểm tra trùng chính xác
                if (patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                {
                    IsTimeSlotValid = false;
                    TimeSlotError = $"Bệnh nhân {OriginalAppointment.Patient.FullName} đã có lịch hẹn khác vào cùng thời điểm";
                    return false;
                }

                // Kiểm tra lịch hẹn gần trong vòng 30 phút
                foreach (var existingAppointment in patientAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30)
                    {
                        IsTimeSlotValid = false;
                        TimeSlotError = $"Bệnh nhân {OriginalAppointment.Patient.FullName} đã có lịch hẹn vào lúc {existingAppointment.AppointmentDate.ToString("HH:mm")}";
                        return false;
                    }
                }

                // Chỉ kiểm tra lịch bác sĩ nếu có chọn bác sĩ
                if (SelectedDoctor != null)
                {
                    // Chỉ kiểm tra lịch làm việc nếu bác sĩ có định nghĩa
                    if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
                    {
                        // Lấy thứ trong tuần của lịch hẹn
                        DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                        string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                        // Phân tích giờ làm việc
                        var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                        // Kiểm tra bác sĩ có làm việc vào ngày này không
                        if (!workingDays.Contains(dayCode))
                        {
                            IsTimeSlotValid = false;
                            TimeSlotError = $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)})";
                            return false;
                        }

                        // Kiểm tra thời gian có trong giờ làm việc không
                        TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                        if (appointmentTime < startTime || appointmentTime > endTime)
                        {
                            IsTimeSlotValid = false;
                            TimeSlotError = $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName} ({startTime.ToString(@"hh\:mm")} - {endTime.ToString(@"hh\:mm")})";
                            return false;
                        }
                    }

                    // Lấy lịch hẹn của bác sĩ trong ngày đó (loại trừ lịch hiện tại)
                    var doctorAppointments = DataProvider.Instance.Context.Appointments
                        .Where(a =>
                            a.StaffId == SelectedDoctor.StaffId &&
                            a.IsDeleted != true &&
                            a.Status != "Đã hủy" &&
                            a.AppointmentDate.Date == appointmentDateTime.Date &&
                            a.AppointmentId != OriginalAppointment.AppointmentId) // Loại trừ lịch hẹn hiện tại
                        .ToList();

                    // Kiểm tra trùng chính xác trước (cùng giờ và phút)
                    if (doctorAppointments.Any(a =>
                        a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                        a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    {
                        IsTimeSlotValid = false;
                        TimeSlotError = $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc {appointmentDateTime.ToString("HH:mm")}";
                        return false;
                    }

                    // Kiểm tra lịch hẹn chồng chéo trong vòng 30 phút
                    var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
                    foreach (var existingAppointment in doctorAppointments)
                    {
                        double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                        double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                        if (timeDifference < 30 && timeDifference % 30 != 0)
                        {
                            IsTimeSlotValid = false;
                            TimeSlotError = $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc {existingAppointment.AppointmentDate.ToString("HH:mm")}, vui lòng chọn thời gian cách ít nhất 30 phút";
                            return false;
                        }
                    }
                }

                // Tất cả validation đã passed
                IsTimeSlotValid = true;
                TimeSlotError = "";
                return true;
            }
            catch (Exception ex)
            {
                IsTimeSlotValid = false;
                TimeSlotError = $"Lỗi kiểm tra thời gian: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region Helper Methods - Các phương thức hỗ trợ

        /// <summary>
        /// Chuyển đổi DayOfWeek thành mã ngày tiếng Việt
        /// </summary>
        /// <param name="dayOfWeek">Ngày trong tuần</param>
        /// <returns>Mã ngày tiếng Việt (T2, T3, ...)</returns>
        private string ConvertDayOfWeekToVietnameseCode(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                DayOfWeek.Sunday => "CN",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Lấy tên ngày tiếng Việt
        /// </summary>
        /// <param name="dayOfWeek">Ngày trong tuần</param>
        /// <returns>Tên ngày tiếng Việt</returns>
        private string GetVietnameseDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ hai",
                DayOfWeek.Tuesday => "Thứ ba",
                DayOfWeek.Wednesday => "Thứ tư",
                DayOfWeek.Thursday => "Thứ năm",
                DayOfWeek.Friday => "Thứ sáu",
                DayOfWeek.Saturday => "Thứ bảy",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Phân tích chuỗi lịch làm việc của bác sĩ
        /// Hỗ trợ các định dạng: "T2-T6: 8h-17h", "T2, T3, T4: 7h-13h", "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
        /// </summary>
        /// <param name="schedule">Chuỗi lịch làm việc</param>
        /// <returns>Tuple chứa danh sách ngày làm việc, giờ bắt đầu và giờ kết thúc</returns>
        private (List<string> WorkingDays, TimeSpan StartTime, TimeSpan EndTime) ParseWorkingSchedule(string schedule)
        {
            List<string> workingDays = new List<string>();
            TimeSpan startTime = TimeSpan.Zero;
            TimeSpan endTime = TimeSpan.Zero;
            try
            {
                // Ví dụ định dạng: "T2-T6: 8h-17h" hoặc "T2, T3, T4: 7h-13h" hoặc "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
                string[] parts = schedule.Split(':');
                if (parts.Length < 2)
                    return (workingDays, startTime, endTime);

                // Phân tích ngày
                string daysSection = parts[0].Trim();
                if (daysSection.Contains('-'))
                {
                    // Định dạng khoảng: "T2-T6"
                    string[] dayRange = daysSection.Split('-');
                    if (dayRange.Length == 2)
                    {
                        string startDay = dayRange[0].Trim();
                        string endDay = dayRange[1].Trim();
                        // Chuyển đổi thành số thứ tự ngày
                        int startDayNum = ConvertVietNameseCodeToDayNumber(startDay);
                        int endDayNum = ConvertVietNameseCodeToDayNumber(endDay);
                        for (int i = startDayNum; i <= endDayNum; i++)
                        {
                            workingDays.Add(ConvertDayNumberToVietnameseCode(i));
                        }
                    }
                }
                else if (daysSection.Contains(','))
                {
                    // Định dạng danh sách: "T2, T3, T4"
                    string[] daysList = daysSection.Split(',');
                    foreach (string day in daysList)
                    {
                        workingDays.Add(day.Trim());
                    }
                }
                else
                {
                    // Định dạng một ngày: "T2"
                    workingDays.Add(daysSection);
                }

                // Phân tích phần thời gian - nối tất cả phần sau dấu ':' đầu tiên
                string timeSection = string.Join(":", parts.Skip(1)).Trim();

                // Xử lý nhiều khoảng thời gian (ví dụ: "8h-12h, 13h30-17h")
                // Hiện tại, ta sẽ lấy thời gian đầu và cuối để có được khoảng thời gian làm việc tổng thể
                var timeRanges = timeSection.Split(',');
                if (timeRanges.Length > 0)
                {
                    // Lấy khoảng thời gian đầu tiên cho thời gian bắt đầu
                    var firstRange = timeRanges[0].Trim();
                    var firstRangeParts = firstRange.Split('-');
                    if (firstRangeParts.Length >= 2)
                    {
                        startTime = ParseTimeString(firstRangeParts[0].Trim());
                    }

                    // Lấy khoảng thời gian cuối cùng cho thời gian kết thúc
                    var lastRange = timeRanges[timeRanges.Length - 1].Trim();
                    var lastRangeParts = lastRange.Split('-');
                    if (lastRangeParts.Length >= 2)
                    {
                        endTime = ParseTimeString(lastRangeParts[lastRangeParts.Length - 1].Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để debug
                System.Diagnostics.Debug.WriteLine($"Lỗi phân tích lịch làm việc '{schedule}': {ex.Message}");
                // Trong trường hợp lỗi phân tích, trả về kết quả rỗng
                workingDays.Clear();
            }
            return (workingDays, startTime, endTime);
        }

        /// <summary>
        /// Phân tích chuỗi thời gian thành TimeSpan
        /// Hỗ trợ các định dạng: "8h", "8h30", "13h", "13h30", "8:30", "13:30"
        /// </summary>
        /// <param name="timeStr">Chuỗi thời gian</param>
        /// <returns>TimeSpan tương ứng hoặc TimeSpan.Zero nếu không hợp lệ</returns>
        private TimeSpan ParseTimeString(string timeStr)
        {
            // Loại bỏ hậu tố 'h' nếu có
            timeStr = timeStr.Replace("h", "").Trim();

            // Thử phân tích định dạng giờ:phút trước (ví dụ: "8:30", "13:30")
            timeStr = timeStr.Replace('.', ':'); // Thay dấu chấm bằng dấu hai châm để nhất quán
            if (timeStr.Contains(':'))
            {
                string[] parts = timeStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int hrs) && int.TryParse(parts[1], out int mins))
                {
                    if (hrs >= 0 && hrs <= 23 && mins >= 0 && mins <= 59)
                        return new TimeSpan(hrs, mins, 0);
                }
            }

            // Thử phân tích chỉ có giờ (ví dụ: "8" -> 08:00, "17" -> 17:00)
            if (int.TryParse(timeStr, out int hours))
            {
                if (hours >= 0 && hours <= 23)
                    return new TimeSpan(hours, 0, 0);
            }

            // Thử phân tích định dạng thời gian chuẩn (ví dụ: "08:00:00")
            if (TimeSpan.TryParse(timeStr + ":00", out TimeSpan result))
                return result;

            return TimeSpan.Zero; // Mặc định nếu phân tích thất bại
        }

        /// <summary>
        /// Chuyển đổi mã ngày tiếng Việt thành số thứ tự
        /// </summary>
        /// <param name="code">Mã ngày (T2, T3, ...)</param>
        /// <returns>Số thứ tự ngày</returns>
        private int ConvertVietNameseCodeToDayNumber(string code)
        {
            return code switch
            {
                "T2" => 2,
                "T3" => 3,
                "T4" => 4,
                "T5" => 5,
                "T6" => 6,
                "T7" => 7,
                "CN" => 8, // Coi Chủ nhật là ngày thứ 8 để sắp xếp thứ tự
                _ => 0
            };
        }

        /// <summary>
        /// Chuyển đổi số thứ tự ngày thành mã ngày tiếng Việt
        /// </summary>
        /// <param name="dayNumber">Số thứ tự ngày</param>
        /// <returns>Mã ngày tiếng Việt</returns>
        private string ConvertDayNumberToVietnameseCode(int dayNumber)
        {
            return dayNumber switch
            {
                2 => "T2",
                3 => "T3",
                4 => "T4",
                5 => "T5",
                6 => "T6",
                7 => "T7",
                8 => "CN",
                _ => string.Empty
            };
        }

        #endregion


    }

    /// <summary>
    /// Lớp hỗ trợ hiển thị thông tin lịch hẹn trong DataGrid
    /// Chứa thông tin đã được format để hiển thị thân thiện với người dùng
    /// </summary>
    public class AppointmentDisplayInfo
    {
        public int AppointmentId { get; set; }                    // ID lịch hẹn
        public string PatientName { get; set; }                  // Tên bệnh nhân
        public DateTime AppointmentDate { get; set; }             // Ngày hẹn
        public string AppointmentTimeString { get; set; }         // Giờ hẹn (dạng string)
        public string Status { get; set; }                       // Trạng thái
        public string Reason { get; set; }                       // Lý do khám
        public Appointment OriginalAppointment { get; set; }      // Tham chiếu đến lịch hẹn gốc
    }
}
