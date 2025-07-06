using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.UserControlToUse;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class AppointmentDetailsViewModel : BaseViewModel, IDataErrorInfo
    {
        private Window _window;

        #region Properties
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

        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                UpdatePermissions();
            }
        }

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
                    LoadAppointmentData();
                }
            }
        }

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
                    // When date changes, validate time slot again
                    ValidateDateTimeSelection();
                }
            }
        }

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
                    // When time changes, validate time slot again
                    ValidateDateTimeSelection();
                }
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
                if (value != null && AppointmentDate.HasValue && SelectedAppointmentTime.HasValue)
                {
                    // When doctor changes, validate time slot again
                    ValidateDateTimeSelection();
                }
            }
        }

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

        // Validation fields
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
        // Error info
        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;
        #endregion

        #region Commands
        public ICommand CancelAppointmentCommand { get; set; }
        public ICommand EditAppointmentCommand { get; set; }
        public ICommand AcceptAppointmentCommand { get; set; }
        public ICommand ConfirmTimeSelectionCommand { get; set; }
        public ICommand CancelTimeSelectionCommand { get; set; }
        public ICommand LoadedWindowCommand { get; set; }
        #endregion

        public AppointmentDetailsViewModel()
        {
            InitializeCommands();
            LoadStaffs();
            LoadAppointmentTypes();

            // Get current account from MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }
        }

        /// <summary>
        /// Constructor that accepts an Appointment object to initialize the view model
        /// </summary>
        /// <param name="appointment">The appointment to be viewed or edited</param>
        /// <summary>
        /// Constructor with an Appointment object to initialize the view model
        /// </summary>
        /// <param name="appointment">The appointment to be viewed or edited</param>
        public AppointmentDetailsViewModel(Appointment appointment)
        {
            // Load all necessary data first
            InitializeCommands();
            LoadStaffs();
            LoadAppointmentTypes();

            // Get current account from MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

            // Set the appointment after loading dependencies
            OriginalAppointment = appointment;

            // The LoadAppointmentData method will be called from the property setter
            // of OriginalAppointment, so we don't need to call it again here
        }


        #region Initialization Methods
        private void InitializeCommands()
        {
            LoadedWindowCommand = new RelayCommand<Window>(
     (w) => {
         _window = w;

         // Refresh current account
         var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
         if (mainVM != null)
         {
             CurrentAccount = mainVM.CurrentAccount;
         }
     },
     (w) => true
 );

            AcceptAppointmentCommand = new RelayCommand<object>(
       (p) => AcceptAppointment(),
       (p) => CanAcceptAppointment &&
              OriginalAppointment != null &&
              OriginalAppointment.Status == "Đang chờ" &&
              OriginalAppointment.Status != "Đã hủy"
   );

            EditAppointmentCommand = new RelayCommand<object>(
                (p) => EditAppointment(),
                (p) => CanEditAppointment() && OriginalAppointment.Status != "Đã hủy" &&
                     OriginalAppointment.Status != "Đã khám"
            );

            CancelAppointmentCommand = new RelayCommand<object>(
            (p) => CancelAppointment(),
            (p) => OriginalAppointment != null &&
                     OriginalAppointment.Status != "Đã hủy" &&
                     OriginalAppointment.Status != "Đã khám"
);

            ConfirmTimeSelectionCommand = new RelayCommand<DateTime?>(
                (time) => {
                    if (time.HasValue)
                    {
                        SelectedAppointmentTime = time;
                    }
                },
                (time) => time.HasValue
            );

            CancelTimeSelectionCommand = new RelayCommand<object>(
                (p) => { },
                (p) => true
            );
        }

        private void LoadStaffs()
        {
            try
            {
                var Staffs = DataProvider.Instance.Context.Staffs
                    .Where(d => d.IsDeleted != true)
                    .OrderBy(d => d.FullName)
                    .ToList();

                DoctorList = new ObservableCollection<Staff>(Staffs);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}", "Lỗi");
            }
        }

        private void LoadAppointmentData()
        {
            if (OriginalAppointment == null) return;

            try
            {
                // Ensure we have all related appointment data
                _originalAppointment = DataProvider.Instance.Context.Appointments
              .Include(a => a.Patient)
              .Include(a => a.Staff)
              .Include(a => a.AppointmentType)
              .FirstOrDefault(a => a.AppointmentId == _originalAppointment.AppointmentId);
                OnPropertyChanged(nameof(OriginalAppointment));

                if (OriginalAppointment == null) return;

                // Set appointment date and time
                AppointmentDate = OriginalAppointment.AppointmentDate.Date;
                SelectedAppointmentTime = new DateTime(
                    OriginalAppointment.AppointmentDate.Year,
                    OriginalAppointment.AppointmentDate.Month,
                    OriginalAppointment.AppointmentDate.Day,
                    OriginalAppointment.AppointmentDate.Hour,
                    OriginalAppointment.AppointmentDate.Minute,
                    0);

                // Set doctor (may be null)
                SelectedDoctor = OriginalAppointment.Staff;

                // Set appointment type
                SelectedAppointmentType = OriginalAppointment.AppointmentType;

                // Validate the time slot
                ValidateDateTimeSelection();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin lịch hẹn: {ex.Message}", "Lỗi");
            }
        }


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

        #region Command Methods
        private bool CanEditAppointment()
        {
            return OriginalAppointment != null &&
                   OriginalAppointment.Status != "Đã hủy" &&
                   OriginalAppointment.Status != "Đã khám" &&
                   AppointmentDate.HasValue &&
                   SelectedAppointmentTime.HasValue &&
                   IsTimeSlotValid;

       
        }

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

                // Kết hợp ngày và giờ thành thời gian lịch hẹn thực tế
                DateTime appointmentDateTime = new DateTime(
                    AppointmentDate.Value.Year,
                    AppointmentDate.Value.Month,
                    AppointmentDate.Value.Day,
                    SelectedAppointmentTime.Value.Hour,
                    SelectedAppointmentTime.Value.Minute,
                    0);

                // Tạo thông báo xác nhận lịch hẹn
                string doctorInfo = SelectedDoctor != null
                    ? $" với bác sĩ {SelectedDoctor.FullName}"
                    : " (không có bác sĩ)";

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

                // Only proceed to UI operations if database update was successful
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



        private void UpdatePermissions()
        {
            // Default to no permissions
            CanAcceptAppointment = false;

            // Check if the current account exists
            if (CurrentAccount == null)
                return;

            // Check role-based permissions
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

            // Only doctors and administrators/managers can accept appointments
            if (role.Equals("Bác sĩ", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Quản lí", StringComparison.OrdinalIgnoreCase))
            {
                CanAcceptAppointment = true;
            }

            // Force command CanExecute to be reevaluated
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Validation Methods
        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the form
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(SelectedDoctor):
                        // No validation needed - doctor is optional
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


        public bool ValidateDateTimeSelection()
        {
            if (!AppointmentDate.HasValue || !SelectedAppointmentTime.HasValue)
            {
                IsTimeSlotValid = false;
                TimeSlotError = "Vui lòng chọn đầy đủ ngày và giờ hẹn";
                return false;
            }

            // Combine date and time
            DateTime appointmentDateTime = new DateTime(
                AppointmentDate.Value.Year,
                AppointmentDate.Value.Month,
                AppointmentDate.Value.Day,
                SelectedAppointmentTime.Value.Hour,
                SelectedAppointmentTime.Value.Minute,
                0);

            // Check if appointment is in the past
            if (appointmentDateTime < DateTime.Now)
            {
                IsTimeSlotValid = false;
                TimeSlotError = "Thời gian hẹn đã qua, vui lòng chọn thời gian trong tương lai";
                return false;
            }

            // Check if appointment time is within allowed hours (7:00 to 17:00)
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
                // Patient appointment check - check if patient has another appointment same day/time
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == OriginalAppointment.PatientId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date &&
                        a.AppointmentId != OriginalAppointment.AppointmentId) // Exclude current appointment
                    .ToList();

                // Check for exact match
                if (patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                {
                    IsTimeSlotValid = false;
                    TimeSlotError = $"Bệnh nhân {OriginalAppointment.Patient.FullName} đã có lịch hẹn khác vào cùng thời điểm";
                    return false;
                }

                // Check for close appointments within 30 minutes
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

                // Only check doctor's schedule if a doctor is selected
                if (SelectedDoctor != null)
                {
                    // Only check schedule if the doctor has one defined
                    if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
                    {
                        // Get the day of week for the appointment
                        DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                        string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                        // Parse working hours
                        var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                        // Check if doctor works on this day
                        if (!workingDays.Contains(dayCode))
                        {
                            IsTimeSlotValid = false;
                            TimeSlotError = $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)})";
                            return false;
                        }

                        // Check if time is within working hours
                        TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                        if (appointmentTime < startTime || appointmentTime > endTime)
                        {
                            IsTimeSlotValid = false;
                            TimeSlotError = $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName} ({startTime.ToString(@"hh\:mm")} - {endTime.ToString(@"hh\:mm")})";
                            return false;
                        }
                    }

                    // Get doctor's appointments for that day (excluding the current one)
                    var doctorAppointments = DataProvider.Instance.Context.Appointments
                        .Where(a =>
                            a.StaffId == SelectedDoctor.StaffId &&
                            a.IsDeleted != true &&
                            a.Status != "Đã hủy" &&
                            a.AppointmentDate.Date == appointmentDateTime.Date &&
                            a.AppointmentId != OriginalAppointment.AppointmentId) // Exclude current appointment
                        .ToList();

                    // Check for exact match first (same hour and minute)
                    if (doctorAppointments.Any(a =>
                        a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                        a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    {
                        IsTimeSlotValid = false;
                        TimeSlotError = $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc {appointmentDateTime.ToString("HH:mm")}";
                        return false;
                    }

                    // Check for overlapping appointments within 30 minutes
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

                // All validation passed
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


        #region Helper Methods
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

        private (List<string> WorkingDays, TimeSpan StartTime, TimeSpan EndTime) ParseWorkingSchedule(string schedule)
        {
            List<string> workingDays = new List<string>();
            TimeSpan startTime = TimeSpan.Zero;
            TimeSpan endTime = TimeSpan.Zero;
            try
            {
                // Example format: "T2-T6: 8h-17h" or "T2, T3, T4: 7h-13h" or "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
                string[] parts = schedule.Split(':');
                if (parts.Length < 2)
                    return (workingDays, startTime, endTime);

                // Parse days
                string daysSection = parts[0].Trim();
                if (daysSection.Contains('-'))
                {
                    // Range format: "T2-T6"
                    string[] dayRange = daysSection.Split('-');
                    if (dayRange.Length == 2)
                    {
                        string startDay = dayRange[0].Trim();
                        string endDay = dayRange[1].Trim();
                        // Convert to day numbers
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
                    // List format: "T2, T3, T4"
                    string[] daysList = daysSection.Split(',');
                    foreach (string day in daysList)
                    {
                        workingDays.Add(day.Trim());
                    }
                }
                else
                {
                    // Single day format: "T2"
                    workingDays.Add(daysSection);
                }

                // Parse time section - join all parts after the first ':'
                string timeSection = string.Join(":", parts.Skip(1)).Trim();

                // Handle multiple time ranges (e.g., "8h-12h, 13h30-17h")
                // For now, we'll take the first and last time to get the overall working period
                var timeRanges = timeSection.Split(',');
                if (timeRanges.Length > 0)
                {
                    // Get first time range for start time
                    var firstRange = timeRanges[0].Trim();
                    var firstRangeParts = firstRange.Split('-');
                    if (firstRangeParts.Length >= 2)
                    {
                        startTime = ParseTimeString(firstRangeParts[0].Trim());
                    }

                    // Get last time range for end time
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
                // Log the error for debugging purposes
                System.Diagnostics.Debug.WriteLine($"Error parsing schedule '{schedule}': {ex.Message}");
                // In case of parsing errors, return empty results
                workingDays.Clear();
            }
            return (workingDays, startTime, endTime);
        }

        private TimeSpan ParseTimeString(string timeStr)
        {
            // Remove 'h' suffix if present
            timeStr = timeStr.Replace("h", "").Trim();

            // Try to parse as hour:minute format first (e.g., "8:30", "13:30")
            timeStr = timeStr.Replace('.', ':'); // Replace dots with colons for consistency
            if (timeStr.Contains(':'))
            {
                string[] parts = timeStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int hrs) && int.TryParse(parts[1], out int mins))
                {
                    if (hrs >= 0 && hrs <= 23 && mins >= 0 && mins <= 59)
                        return new TimeSpan(hrs, mins, 0);
                }
            }

            // Try to parse as hour only (e.g., "8" -> 08:00, "17" -> 17:00)
            if (int.TryParse(timeStr, out int hours))
            {
                if (hours >= 0 && hours <= 23)
                    return new TimeSpan(hours, 0, 0);
            }

            // Try to parse as standard time format (e.g., "08:00:00")
            if (TimeSpan.TryParse(timeStr + ":00", out TimeSpan result))
                return result;

            return TimeSpan.Zero; // Default if parsing fails
        }

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
                "CN" => 8, // Treating Sunday as day 8 for ordering purposes
                _ => 0
            };
        }

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
        #endregion
    }

    // The AppointmentDisplayInfo class
    public class AppointmentDisplayInfo
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTimeString { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public Appointment OriginalAppointment { get; set; }
    }
}
