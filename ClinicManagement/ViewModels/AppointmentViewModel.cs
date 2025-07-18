using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class AppointmentViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties

        #region Permission Properties

        //Tài khoản hiện tại đang đăng nhập, phục vụ việc phân quyền
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                UpdatePermissions(); // Thay đổi phân quyền khi tài khoản thay đổi
            }
        }
        //Xác định quyền liên quan đến Lịch hẹn
        private bool _canManageAppointments;
        public bool CanManageAppointments
        {
            get => _canManageAppointments;
            set
            {
                if (_canManageAppointments != value)
                {
                    _canManageAppointments = value;
                    OnPropertyChanged();
         
                }
            }
        }
        // Xác định quyền liên quan đến Quản lý loại lịch hẹn
        private bool _canManageAppointmentTypes;
        public bool CanManageAppointmentTypes
        {
            get => _canManageAppointmentTypes;
            set
            {
                if (_canManageAppointmentTypes != value) //Điều kiện kiểm tra thay đổi giá trị
                {
                    _canManageAppointmentTypes = value;
                    OnPropertyChanged();
               
                }
            }
        }

        private bool _canViewAppointmentTypes = true;  // Mặc định ai cũng có thể xem loại lịch hẹn
        public bool CanViewAppointmentTypes
        {
            get => _canViewAppointmentTypes;
            set
            {
                _canViewAppointmentTypes = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region TypeAppointment
        //Danh sách loại lịch hẹn
        private ObservableCollection<AppointmentType> _ListAppointmentType;
        public ObservableCollection<AppointmentType> ListAppointmentType
        {
            get => _ListAppointmentType;
            set
            {
                _ListAppointmentType = value;
                OnPropertyChanged();
            }
        }

        //Loại lịch hẹn được chọn
        private AppointmentType? _SelectedAppointmentType;
        public AppointmentType? SelectedAppointmentType
        {
            get => _SelectedAppointmentType;
            set
            {
                if (_SelectedAppointmentType != value)
                {
                    if (value != null)
                        _touchedFields.Add(nameof(SelectedAppointmentType)); // Thêm vào touchedFields khi người dùng chọn loại lịch hẹn

                    _SelectedAppointmentType = value; // Cập nhật loại lịch hẹn được chọn
                    OnPropertyChanged();

                    if (value != null) // Nếu có loại lịch hẹn được chọn, cập nhật các thuộc tính hiển thị
                    {
                        TypeDisplayName = value.TypeName;
                        TypeDescription = value.Description;
                        TypePrice = value.Price;
                    }
                }
            }
        }

        // Các thuộc tính hiển thị của loại lịch hẹn
        //Tên hiển thị của loại lịch hẹn
        private string? _TypeDisplayName;
        public string? TypeDisplayName
        {
            get => _TypeDisplayName;
            set
            {
                if (_TypeDisplayName != value) // Kiểm tra nếu giá trị mới khác giá trị cũ
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_TypeDisplayName); // Kiểm tra nếu giá trị cũ là rỗng
                    bool isEmpty = string.IsNullOrWhiteSpace(value); //Kiểm tra nếu giá trị mới là rỗng

                    // Cập nhật touchedFields dựa trên trạng thái rỗng
                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(TypeDisplayName));

                    else if (!wasEmpty && isEmpty) //Nếu cả giá trị cũ và mới đều rỗng thì xóa khỏi touchedFields
                    {
                        _touchedFields.Remove(nameof(TypeDisplayName));
                        
                        OnPropertyChanged(nameof(Error)); //Thông báo thay đổi lỗi nếu có
                    }

                    _TypeDisplayName = value;
                    OnPropertyChanged();

                    //Nếu có trường TypeDisplayName trong touchedFields thì thông báo thay đổi lỗi
                    if (_touchedFields.Contains(nameof(TypeDisplayName)))
                        OnPropertyChanged(nameof(Error));

                    //Cập nhật trạng thái của các lệnh liên quan
                    CommandManager.InvalidateRequerySuggested();


                    //Đây là cơ chế đồng bộ hóa trạng thái UI trong WPF,
                    //đảm bảo giao diện người dùng luôn phản ánh đúng trạng thái logic của ứng dụng.
                    //Khi không có dòng code này, các Button có thể vẫn giữ trạng thái cũ mặc dù điều kiện đã thay đổi.
                }
            }
        }
        // Mô tả loại lịch hẹn
        private string? _TypeDescription;
        public string? TypeDescription
        {
            get => _TypeDescription;
            set
            {
                //Điều kiện của việc cập nhật mô tả loại lịch hẹn ẽ chỉ xảy ra khi giá trị mới khác giá trị cũ
                //Thực hiện báo lỗi dựa trên các điều kiện
                if (_TypeDescription != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_TypeDescription);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(TypeDescription));
                    else if (!wasEmpty && isEmpty)
                    {
                        _touchedFields.Remove(nameof(TypeDescription));
                        OnPropertyChanged(nameof(Error));
                    }

                    _TypeDescription = value;
                    OnPropertyChanged();

                    if (_touchedFields.Contains(nameof(TypeDescription)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }
        //Giá cuả loại lịch hẹn
        private decimal? _TypePrice;
        public decimal? TypePrice
        {
            get => _TypePrice;
            set
            {
                if (_TypePrice != value)
                {
                    if (value.HasValue)
                        _touchedFields.Add(nameof(TypePrice));
                    else
                        _touchedFields.Remove(nameof(TypePrice));

                    _TypePrice = value;
                    OnPropertyChanged();

                    if (_touchedFields.Contains(nameof(TypePrice)))
                        OnPropertyChanged(nameof(Error));

                    
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        #endregion

        #region Appointment Form Properties
        // Giá trị giữ chuỗi để search tên bệnh nhân
        private string _patientSearch;
        public string PatientSearch
        {
            get => _patientSearch;
            set
            {
                // Kiểm tra nếu giá trị mới khác giá trị cũ
                if (_patientSearch != value)
                {
                    if (!string.IsNullOrEmpty(value))
                        _touchedFields.Add(nameof(PatientSearch));
                    else
                        _touchedFields.Remove(nameof(PatientSearch)); // Xóa khỏi touchedFields khi giá trị trống
                    // Cập nhật giá trị và thông báo thay đổi
                    _patientSearch = value;
                    OnPropertyChanged();
                    //Thực hiện search
                    SearchPatients();
                }
            }
        }
        //Danh sách bệnh nhân tìm kiếm
        private ObservableCollection<Patient> _searchedPatients;
        public ObservableCollection<Patient> SearchedPatients
        {
            get => _searchedPatients ??= new ObservableCollection<Patient>();
            set
            {
                _searchedPatients = value;
                OnPropertyChanged();
            }
        }
        //Bệnh nhân được chọn là kết quả của search
        private Patient? _selectedPatient;
        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();
                // Nếu có bệnh nhân được chọn, cập nhật các trường thông tin
                if (value != null)
                {
                    PatientPhone = value.Phone ?? string.Empty;
                }
                // Xóa lệnh gọi ValidateFormSequence() để tránh reset ngày/giờ
                // Chỉ cập nhật cờ xác thực
                _isPatientInfoValid = value != null;
            }
        }


        // Danh sách bác sĩ
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
        // Bác sĩ được chọn
        private Staff? _selectedDoctor;
        public Staff? SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                if (_selectedDoctor != value)
                {
                    if (value != null)
                        _touchedFields.Add(nameof(SelectedDoctor));

                    _selectedDoctor = value;
                    OnPropertyChanged();
                    _isStaffselected = value != null; // Cập nhật cờ xác thực khi bác sĩ được chọn
                }
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

        //Ngày hẹn
        private DateTime? _appointmentDate = DateTime.Today;
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                //Nếu ngày mới khác ngày cũ thì cập nhật
                if (_appointmentDate != value)
                {
                    if (value.HasValue)
                        _touchedFields.Add(nameof(AppointmentDate));

                    _appointmentDate = value;
                    OnPropertyChanged();
                    if (value.HasValue)
                    {
                        // Đặt lại thời gian khi ngày thay đổi để đảm bảo xác thực
                        SelectedAppointmentTime = null;
                    }
                }
            }
        }

        // Số điện thoại bệnh nhân 
        private string _patientPhone;
    public string PatientPhone
{
    get => _patientPhone;
    set
    {
        if (_patientPhone != value)
        {
            if (!string.IsNullOrEmpty(value))
                _touchedFields.Add(nameof(PatientPhone));
            else
                _touchedFields.Remove(nameof(PatientPhone));

            _patientPhone = value;
            OnPropertyChanged();
        }
    }
}


        // Ghi chú lịch hẹn
        private string _appointmentNote;
        public string AppointmentNote
        {
            get => _appointmentNote;
            set
            {
                _appointmentNote = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Appointment Properties
        // Danh sách các cuộc hẹn
        private ObservableCollection<Appointment> _Appointments = new();
        public ObservableCollection<Appointment> Appointments
        {
            get => _Appointments;
            set
            {
                _Appointments = value;
                OnPropertyChanged();
            }
        }
        // Danh sách các cuộc hẹn hiển thị
        private ObservableCollection<AppointmentDisplayInfo>? _AppointmentsDisplay;
        public ObservableCollection<AppointmentDisplayInfo> AppointmentsDisplay
        {
            get => _AppointmentsDisplay ??= new ObservableCollection<AppointmentDisplayInfo>();
            set
            {
                _AppointmentsDisplay = value;
                OnPropertyChanged();
            }
        }
        // Danh sách trạng thái cuộc hẹn
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
        // Trạng thái cuộc hẹn được chọn
        private string _selectedAppointmentStatus = string.Empty;
        public string SelectedAppointmentStatus
        {
            get => _selectedAppointmentStatus;
            set
            {
                _selectedAppointmentStatus = value;
                OnPropertyChanged();
                // Tự động tải lại cuộc hẹn khi trạng thái thay đổi
                LoadAppointments();
            }
        }
        // Thời gian hẹn được chọn
        private DateTime? _selectedAppointmentTime;
        public DateTime? SelectedAppointmentTime
        {
            get => _selectedAppointmentTime;
            set
            {
                if (_selectedAppointmentTime != value)
                {
                    if (value.HasValue)
                        _touchedFields.Add(nameof(SelectedAppointmentTime));

                    _selectedAppointmentTime = value;

                    OnPropertyChanged();
                    //Đặt giá trị của _isDateTimeValid dựa trên giá trị mới bằng các kiểm tra
                    _isDateTimeValid = value.HasValue && AppointmentDate.HasValue && IsAppointmentTimeValid();
                }
            }
        }
       


        // Fix the property name - consistent naming
        private DateTime _filterDate = DateTime.Today;
        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
                // Auto-reload appointments when date changes
                LoadAppointments();
            }
        }

        // Search text for appointment list
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        // AppontmentType Commands
        public ICommand AddAppointmentTypeCommand { get; set; } // Thêm loại lịch hẹn
        public ICommand EditAppointmentTypeCommand { get; set; } // Sửa loại lịch hẹn
        public ICommand DeleteAppointmentTypeCommand { get; set; } // Xóa loại li hẹn
        public ICommand RefreshTypeCommand { get; set; } // Làm mới danh sách loại lịch hẹn

        // Appointment Form Commands
        public ICommand CancelCommand { get; set; } // Hủy thao tác tạo lịch hẹn
        public ICommand AddAppointmentCommand { get; set; } // Thêm lịch hẹn mới
        public ICommand SelectPatientCommand { get; set; } // Chọn bệnh nhân từ danh sách tìm kiếm
        public ICommand FindPatientCommand { get; set; } // Tìm hoặc tạo bệnh nhân mới
        public ICommand CancelAppointmentCommand { get; set; } // Hủy lịch hẹn đã chọn
        public ICommand CancelTimeSelectionCommand { get; set; }// Hủy chọn thời gian hẹn

        // Appointment List Commands
        public ICommand SearchCommand { get; set; } // Tìm kiếm lịch hẹn
        public ICommand SearchAppointmentsCommand { get; set; } // Tìm kiếm lịch hẹn theo từ khóa
        public ICommand OpenAppointmentDetailsCommand { get; set; } // Mở chi tiết lịch hẹn
        #endregion

        #region Validation
        // Thuộc tính bắt buộc của IDataErrorInfo
        public string? Error => null;
        // Theo dõi các trường người dùng đã tương tác
        private HashSet<string> _touchedFields = new HashSet<string>();
        // Cờ để bật validation khi cần thiết
        private bool _isValidating = false;

        //Cờ xác thực thông tin bệnh nhân
        private bool _isStaffselected = false; // Cờ xác thực xem đã chọn bác sĩ hay chưa
        private bool _isPatientInfoValid = false; // Cờ xác thực thông tin bệnh nhân
        private bool _isDateTimeValid = false; // Cờ xác thực ngày và giờ hẹn
        #endregion

        #endregion
        //Constructor cho AppointmentViewModel
        public AppointmentViewModel()
        {
            // Lấy tài khoản hiện tại từ MainViewModel
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                CurrentAccount = mainVM.CurrentAccount;
            }

            LoadData();
        }
        //Method tải dữ liệu ban đầu cho AppointmentViewModel
        public void LoadData()
        {
            _filterDate = DateTime.Today;

            _appointmentNote = string.Empty;
            _searchText = string.Empty;

            // Khởi tạo các ObservableCollection
            _ListAppointmentType = new ObservableCollection<AppointmentType>();
            _searchedPatients = new ObservableCollection<Patient>();
            _doctorList = new ObservableCollection<Staff>();
            _appointmentTypes = new ObservableCollection<AppointmentType>();

            // Khởi tạo các thuộc tính liên quan đến cuộc hẹn
            _selectedPatient = null!;
            _selectedDoctor = null!;
            _SelectedAppointmentType = null!;

            LoadAppointmentTypeData();
            InitializeCommands();
            InitializeData();

            //Khởi tạo các trường đã tương tác
            _touchedFields = new HashSet<string>();

            //Cập nhật quyền dựa trên tài khoản hiện tại
            UpdatePermissions();
        }
        
        //Method tải dữ liệu cho loại lịch hẹn 
        private void LoadAppointmentTypeData()
        {
            ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                    .Where(a => (bool)!a.IsDeleted)
                    .ToList()
                );

            //Tạo danh sách trạng loại lịch hẹn cho form tạo lịch hẹn
            AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);
        }
        
        private void InitializeCommands() //Method khởi tạo các lệnh 
        {
            AddAppointmentTypeCommand = new RelayCommand<object>(
               (p) => AddAppontmentType(),
               (p) => CanManageAppointmentTypes && !string.IsNullOrEmpty(TypeDisplayName)
            );

            EditAppointmentTypeCommand = new RelayCommand<object>(
                (p) => EditAppontmentType(),
                (p) => CanManageAppointmentTypes && SelectedAppointmentType != null && !string.IsNullOrEmpty(TypeDisplayName)
            );

            DeleteAppointmentTypeCommand = new RelayCommand<object>(
                (p) => DeleteAppointmentType(),
                (p) => CanManageAppointmentTypes && SelectedAppointmentType != null
            );

            RefreshTypeCommand = new RelayCommand<object>
            ((p) => ExecuteRefreshType(),
                (p) => true
            );

            
            CancelCommand = new RelayCommand<object>(
                (p) => ClearAppointmentForm(),
                (p) => true
            );

            AddAppointmentCommand = new RelayCommand<object>(
                (p) => AddNewAppointment(),
                (p) => CanAddAppointment()
            );

            SelectPatientCommand = new RelayCommand<Patient>(
                (p) => SelectPatient(p),
                (p) => p != null
            );

            // Initialize search commands
            SearchCommand = new RelayCommand<object>(
                (p) => SearchAppointments(),
                (p) => true
            );

            SearchAppointmentsCommand = new RelayCommand<object>(
                (p) => SearchAppointments(),
                (p) => true
            );

            FindPatientCommand = new RelayCommand<object>(
              (p) => FindOrCreatePatient(),
              (p) => !string.IsNullOrWhiteSpace(PatientSearch) || !string.IsNullOrWhiteSpace(PatientPhone)
            );

            CancelTimeSelectionCommand = new RelayCommand<object>(
                (p) => { },
                (p) => true
            );

            OpenAppointmentDetailsCommand = new RelayCommand<AppointmentDisplayInfo>(
               (p) => OpenAppointmentDetails(p),
               (p) => p != null
            );

            CancelAppointmentCommand = new RelayCommand<AppointmentDisplayInfo>(
               (p) => CancelAppointmentDirectly(p),
               (p) => CanManageAppointments && p != null && p.Status != "Đã hủy" && p.Status != "Đã khám" && p.Status != "Đang khám"
            );
        }

        private void CancelAppointmentDirectly(AppointmentDisplayInfo appointmentInfo) //Method hủy lịch hẹn trực tiếp
        {
            try
            {
                // Kiểm tra tính hợp lệ của thông tin lịch hẹn
                if (appointmentInfo?.OriginalAppointment == null) 
                {
                    MessageBoxService.ShowError(
                        "Không tìm thấy thông tin lịch hẹn để hủy.",
                        "Lỗi dữ liệu");
                    return;
                }

                // Hiển thị hộp thoại nhập lý do hủy lịch hẹn
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Vui lòng nhập lý do hủy lịch hẹn:",
                    "Xác nhận hủy",
                    appointmentInfo.OriginalAppointment.Notes ?? "");

                // Kiểm tra người dùng có nhập lý do hay không
                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng nhập lý do hủy lịch hẹn.",
                        "Thiếu thông tin");
                    return;
                }

                // Xác nhận cuối cùng từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy lịch hẹn này không?",
                    "Xác nhận hủy");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm lịch hẹn trong cơ sở dữ liệu
                        var appointmentToUpdate = DataProvider.Instance.Context.Appointments
                            .FirstOrDefault(a => a.AppointmentId == appointmentInfo.OriginalAppointment.AppointmentId);

                        if (appointmentToUpdate == null)
                        {
                            MessageBoxService.ShowError(
                                "Không tìm thấy lịch hẹn trong cơ sở dữ liệu.",
                                "Lỗi dữ liệu");
                            return;
                        }

                        // Cập nhật trạng thái và lý do hủy
                        appointmentToUpdate.Status = "Đã hủy";
                        appointmentToUpdate.Notes = reason;

                        // Lưu thay đổi vào cơ sở dữ liệu
                        DataProvider.Instance.Context.SaveChanges();

                        // Commit transaction khi mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách lịch hẹn để cập nhật giao diện
                        LoadAppointments();

                        // Thông báo thành công cho người dùng
                        MessageBoxService.ShowSuccess(
                            "Đã hủy lịch hẹn thành công!",
                            "Thông báo");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi trong quá trình cập nhật
                        transaction.Rollback();
                        
                        // Ném lại ngoại lệ để được xử lý bởi khối catch bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi không mong muốn
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi hủy lịch hẹn: {ex.Message}",
                    "Lỗi");
            }
        }
        private void OpenAppointmentDetails(AppointmentDisplayInfo appointmentInfo) //Method mở chi tiết lịch hẹn
        {
            try
            {
                if (appointmentInfo == null || appointmentInfo.OriginalAppointment == null)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng chọn một lịch hẹn để xem chi tiết.",
                        "Thông báo");
                    return;
                }

                // Tải lịch hẹn với tất cả dữ liệu liên quan
                var fullAppointment = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Staff)
                        .ThenInclude(d => d.Specialty)
                    .Include(a => a.AppointmentType)
                    .FirstOrDefault(a => a.AppointmentId == appointmentInfo.AppointmentId);

                if (fullAppointment == null)
                {
                    MessageBoxService.ShowError(
                        "Không thể tải thông tin chi tiết lịch hẹn."
                   );
                    return;
                }

                // Tạo và hiển thị cửa sổ chi tiết lịch hẹn
                var detailsWindow = new AppointmentDetailsWindow();
                var viewModel = detailsWindow.DataContext as AppointmentDetailsViewModel;

                if (viewModel != null)
                {
                    viewModel.OriginalAppointment = fullAppointment;
                    detailsWindow.ShowDialog();

                    //Tải lại dữ liệu của tab khi diaglog tắt
                    LoadAppointments();
                }
                else
                {
                    MessageBoxService.ShowError(
                        "Không thể khởi tạo cửa sổ chi tiết lịch hẹn."
                  );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi mở chi tiết lịch hẹn: {ex.Message}");
            }
        }

        private void SelectPatient(Patient patient) //Method chọn bệnh nhân từ danh sách tìm kiếm
        {
            SelectedPatient = patient;
            PatientSearch = patient?.FullName ?? string.Empty;
            SearchedPatients.Clear();
        }
        private void SearchPatients() //Method tìm kiếm bệnh nhân
        {
            
            if (string.IsNullOrWhiteSpace(PatientSearch) || PatientSearch.Length < 2) 
            {
                SearchedPatients.Clear();
                return;
            }

            try //Bắt lỗi khi tìm kiếm bệnh nhân
            {
                string searchLower = PatientSearch.ToLower().Trim();
                var patients = DataProvider.Instance.Context.Patients // Lấy danh sách bệnh nhân từ cơ sở dữ liệu
                    .Where(p => p.IsDeleted != true && //Điều kiện là bệnh nhân không bị xóa 
                           (p.FullName.ToLower().Contains(searchLower) || //Tên bệnh nhân hoặc mã bảo hiểm chứa chuỗi tìm kiếm
                           (p.InsuranceCode != null && p.InsuranceCode.ToLower().Contains(searchLower)) ||
                           (p.Phone != null && p.Phone.Contains(searchLower)))) // Hoặc số điện thoại chứa chuỗi tìm kiếm
                    .OrderBy(p => p.FullName)
                    .Take(5)
                    .ToList(); // Lấy tối đa 5 kết quả

                SearchedPatients = new ObservableCollection<Patient>(patients);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm bệnh nhân: {ex.Message}"
            );
            }
        }
        private void FindOrCreatePatient(bool silentMode = false) //Method tìm hoặc tạo bệnh nhân mới với silentMode là dùng để tránh hiển thị quá nhiều thông báo không cần thiết
        {
            try
            {
                // Kiểm tra nếu không có thông tin tìm kiếm bệnh nhân
                if (string.IsNullOrWhiteSpace(PatientSearch) && string.IsNullOrWhiteSpace(PatientPhone))
                {
                    if (!silentMode)
                    {
                        MessageBoxService.ShowWarning(
                            "Vui lòng nhập tên/mã BHYT hoặc số điện thoại của bệnh nhân.",
                            "Thiếu thông tin");
                    }
                    return;
                }

                //Kiểm tra nếu chuỗi tìm kiếm giống mã bảo hiểm (chỉ có số và đúng 10 ký tự)
                bool looksLikeInsuranceCode = !string.IsNullOrWhiteSpace(PatientSearch) &&
                                              PatientSearch.All(char.IsDigit) &&
                                              PatientSearch.Length == 10;

                //Nếu đang tìm kiếm theo mã bảo hiểm, không cần số điện thoại
                // Nếu không tìm kiếm theo mã bảo hiểm, thì xác thực số điện thoại nếu có
                if (!looksLikeInsuranceCode &&
                    !string.IsNullOrWhiteSpace(PatientPhone) &&
                    !Regex.IsMatch(PatientPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))// Kiểm tra định dạng số điện thoại, truyền số điện thoại được trim và xét bằng pattern số điện thoại Việt Nam
                {
                    if (!silentMode) //
                    {
                        MessageBoxService.ShowWarning(
                            "Số điện thoại không đúng định dạng. Vui lòng nhập số điện thoại hợp lệ (VD: 0901234567).",
                            "Số điện thoại không hợp lệ"
                        );
                    }
                    return;
                }

                // Đầu tiên thực hiện tìm bệnh nhân theo tên hoặc mã bảo hiểm
                var patient = FindPatient();

                if (patient != null)
                {
                    // Nếu tìm thấy bệnh nhân, cập nhật thông tin và thông báo
                    SelectedPatient = patient;
                    if (!silentMode)
                    {
                        MessageBoxService.ShowInfo(
                            $"Đã tìm thấy bệnh nhân: {patient.FullName}"
                        );
                    }
                    return;
                }

                // Nếu không tìm thấy bệnh nhân và đang ở chế độ silent, chỉ trả về mà không hiển thị thông báo hay tạo bệnh nhân mới
                if (silentMode)
                    return;

                // Thông báo đặc biệt khi tìm kiếm theo mã bảo hiểm
                if (looksLikeInsuranceCode)
                {
                    var result = MessageBoxService.ShowQuestion(
                        $"Không tìm thấy bệnh nhân với mã bảo hiểm '{PatientSearch.Trim()}'.\n" +
                        "Bạn có muốn tạo hồ sơ bệnh nhân mới không?",
                        "Mã bảo hiểm không tồn tại"
                    );

                    if (!result)
                        return;

                    //Nếu đang tạo bệnh nhân mới với mã bảo hiểm, cần lấy thêm thông tin
                    var nameResult = Microsoft.VisualBasic.Interaction.InputBox(
                        "Nhập họ tên bệnh nhân:", "Thông tin bệnh nhân", "");

                    if (string.IsNullOrWhiteSpace(nameResult))
                    {
                        MessageBoxService.ShowWarning(
                            "Không thể tạo bệnh nhân mới vì thiếu họ tên.");
                        return;
                    }

                    //Lấy số điện thoại nếu không có
                    string phone = PatientPhone;
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        var phoneResult = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập số điện thoại bệnh nhân:", "Thông tin bệnh nhân", "");

                        if (string.IsNullOrWhiteSpace(phoneResult) ||
                            !Regex.IsMatch(phoneResult.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            MessageBoxService.ShowWarning(
                                "Số điện thoại không hợp lệ. Không thể tạo bệnh nhân mới.",
                                "Số điện thoại không hợp lệ");
                            return;
                        }
                        phone = phoneResult.Trim();
                    }

                    // Sử dụng transaction khi tạo bệnh nhân mới với mã bảo hiểm
                    using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Kiểm tra trùng lặp trong transaction
                            bool phoneExists = DataProvider.Instance.Context.Patients
                                .Any(p => p.Phone == phone && (p.IsDeleted == null || p.IsDeleted == false));

                            if (phoneExists)
                            {
                                MessageBoxService.ShowError(
                                    "Số điện thoại này đã được sử dụng bởi một bệnh nhân khác.",
                                    "Lỗi dữ liệu");
                                return;
                            }

                            bool insuranceExists = DataProvider.Instance.Context.Patients
                                .Any(p => p.InsuranceCode == PatientSearch.Trim() && (p.IsDeleted == null || p.IsDeleted == false));

                            if (insuranceExists)
                            {
                                MessageBoxService.ShowError(
                                    "Mã BHYT này đã được sử dụng bởi một bệnh nhân khác.",
                                    "Lỗi dữ liệu");
                                return;
                            }

                            //Tạo bệnh nhân mới với mã bảo hiểm
                            var newPatient = new Patient
                            {
                                FullName = nameResult.Trim(),
                                Phone = phone,
                                InsuranceCode = PatientSearch.Trim(),
                                IsDeleted = false,
                                CreatedAt = DateTime.Now,
                                PatientTypeId = 1  // Loại mặc định là bệnh nhân thường
                            };

                            DataProvider.Instance.Context.Patients.Add(newPatient);
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            // Đặt bệnh nhân mới là bệnh nhân đã chọn
                            SelectedPatient = newPatient;
                            PatientPhone = phone;

                            MessageBoxService.ShowSuccess(
                                $"Đã tạo bệnh nhân mới: {newPatient.FullName}",
                                "Thành công");

                            return;
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction nếu có lỗi
                            transaction.Rollback();
                            throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                        }
                    }
                }

                // Thông báo và hỏi người dùng có muốn tạo bệnh nhân mới không
                var standardResult = MessageBoxService.ShowQuestion(
                    "Không tìm thấy bệnh nhân với thông tin đã nhập. Bạn có muốn tạo mới không?",
                    "Tạo bệnh nhân mới?"
                );

                if (standardResult)
                {
                    string name = PatientSearch.Trim();
                    string insuranceCode = null;

                    // Nếu chuỗi tìm kiếm có số nhưng không đúng 10 ký tự, hỏi thông tin chi tiết
                    if (PatientSearch.Any(char.IsDigit) && PatientSearch.Length < 20)
                    {
                        var nameResult = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập họ tên bệnh nhân:", "Thông tin bệnh nhân", "");

                        if (string.IsNullOrWhiteSpace(nameResult))
                        {
                            MessageBoxService.ShowWarning(
                                "Không thể tạo bệnh nhân mới vì thiếu họ tên.",
                                "Thiếu thông tin"
                            );
                            return; // User cancelled or provided empty name
                        }

                        name = nameResult.Trim();

                        // Hỏi mã BHYT nếu có
                        var insuranceResult = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập mã BHYT (10 số) nếu có:", "Thông tin bệnh nhân", "");

                        if (!string.IsNullOrWhiteSpace(insuranceResult))
                        {
                            // Validate insurance code format
                            if (insuranceResult.All(char.IsDigit) && insuranceResult.Length == 10)
                                insuranceCode = insuranceResult.Trim();
                            else
                            {
                                MessageBoxService.ShowWarning(
                                    "Mã BHYT không hợp lệ. Mã BHYT phải có đúng 10 chữ số.",
                                    "Mã BHYT không hợp lệ");
                            }
                        }
                    }

                    // Kiểm tra độ dài tên
                    if (name.Length < 2)
                    {
                        MessageBoxService.ShowWarning(
                            "Tên bệnh nhân phải có ít nhất 2 ký tự.",
                            "Tên không hợp lệ");
                        return;
                    }

                    // Sử dụng transaction khi tạo bệnh nhân thường
                    using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Kiểm tra bệnh nhân đã tồn tại với cùng tên và số điện thoại
                            var existingPatient = DataProvider.Instance.Context.Patients
                                .FirstOrDefault(p =>
                                    p.IsDeleted != true &&
                                    p.Phone == PatientPhone.Trim() &&
                                    p.FullName.ToLower() == name.ToLower());

                            if (existingPatient != null)
                            {
                                MessageBoxService.ShowWarning(
                                    $"Đã tồn tại bệnh nhân '{existingPatient.FullName}' với số điện thoại này.",
                                    "Bệnh nhân đã tồn tại");

                                // Đặt bệnh nhân đã tồn tại là bệnh nhân được chọn
                                SelectedPatient = existingPatient;
                                return;
                            }

                            // Kiểm tra trùng lặp số điện thoại
                            bool phoneExists = DataProvider.Instance.Context.Patients
                                .Any(p => p.Phone == PatientPhone.Trim() && (p.IsDeleted == null || p.IsDeleted == false));

                            if (phoneExists)
                            {
                                MessageBoxService.ShowError(
                                    "Số điện thoại này đã được sử dụng bởi một bệnh nhân khác.",
                                    "Lỗi dữ liệu");
                                return;
                            }

                            // Kiểm tra trùng lặp mã BHYT nếu có
                            if (!string.IsNullOrWhiteSpace(insuranceCode))
                            {
                                bool insuranceExists = DataProvider.Instance.Context.Patients
                                    .Any(p => p.InsuranceCode == insuranceCode && (p.IsDeleted == null || p.IsDeleted == false));

                                if (insuranceExists)
                                {
                                    MessageBoxService.ShowError(
                                        "Mã BHYT này đã được sử dụng bởi một bệnh nhân khác.",
                                        "Lỗi dữ liệu");
                                    return;
                                }
                            }

                            // Tạo và lưu bệnh nhân mới
                            var newPatient = new Patient
                            {
                                FullName = name,
                                Phone = PatientPhone.Trim(),
                                InsuranceCode = insuranceCode,
                                IsDeleted = false,
                                CreatedAt = DateTime.Now,
                                PatientTypeId = 1  // Default patient type
                            };

                            DataProvider.Instance.Context.Patients.Add(newPatient);
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction khi mọi thứ thành công
                            transaction.Commit();

                            // Đặt bệnh nhân mới là bệnh nhân được chọn
                            SelectedPatient = newPatient;

                            MessageBoxService.ShowSuccess(
                                $"Đã tạo bệnh nhân mới: {newPatient.FullName}");
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction nếu có lỗi
                            transaction.Rollback();
                            throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                        }
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi lưu thông tin bệnh nhân: {dbEx.InnerException?.Message ?? dbEx.Message}",
                    "Lỗi cơ sở dữ liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tìm kiếm hoặc tạo bệnh nhân: {ex.Message}",
                    "Lỗi");
            }
        }

        // Tìm kiếm bệnh nhân theo các tiêu chí đã nhập
        private Patient? FindPatient()
        {
            //Kiểm tra nếu không có thông tin tìm kiếm
            if (string.IsNullOrWhiteSpace(PatientSearch) && string.IsNullOrWhiteSpace(PatientPhone))
                return null;

            //  Tìm kiếm theo mã bảo hiểm nếu chuỗi tìm kiếm là một chuỗi số 10 ký tự
            if (!string.IsNullOrWhiteSpace(PatientSearch) &&
                PatientSearch.All(char.IsDigit) && // Kiểm tra xem chuỗi chỉ chứa số
                PatientSearch.Length == 10)
            {
                var patientByInsurance = DataProvider.Instance.Context.Patients // Tìm kiếm bệnh nhân theo mã bảo hiểm
                    .FirstOrDefault(p =>
                        p.IsDeleted != true &&
                        p.InsuranceCode == PatientSearch.Trim());

                if (patientByInsurance != null) 
                    return patientByInsurance; //Nếu tìm thấy bệnh nhân theo mã bảo hiểm, trả về bệnh nhân đó
            }

            //Tìm kiếm bằng số điện thoại nếu có
            if (!string.IsNullOrWhiteSpace(PatientPhone))
            {
                var patientByPhone = DataProvider.Instance.Context.Patients
                    .FirstOrDefault(p =>
                        p.IsDeleted != true &&
                        p.Phone == PatientPhone.Trim());

                if (patientByPhone != null)
                    return patientByPhone;
            }

            //Tìm kiếm theo tên và số điện thoại nếu cả hai đều có
            if (!string.IsNullOrWhiteSpace(PatientSearch) && !string.IsNullOrWhiteSpace(PatientPhone))
            {
                // Tìm kiếm bệnh nhân theo tên và số điện thoại chính xác
                var patientByNameAndPhone = DataProvider.Instance.Context.Patients
                    .FirstOrDefault(p =>
                        p.IsDeleted != true &&
                        p.Phone == PatientPhone.Trim() &&
                        p.FullName.ToLower() == PatientSearch.Trim().ToLower());

                if (patientByNameAndPhone != null)
                    return patientByNameAndPhone;

                //Tìm kiếm bệnh nhân theo tên chứa chuỗi tìm kiếm
                patientByNameAndPhone = DataProvider.Instance.Context.Patients
                    .FirstOrDefault(p =>
                        p.IsDeleted != true &&
                        p.Phone == PatientPhone.Trim() &&
                        p.FullName.ToLower().Contains(PatientSearch.Trim().ToLower()));

                if (patientByNameAndPhone != null)
                    return patientByNameAndPhone;
            }

            //Tìm kiếm bệnh nhân theo tên nếu chuỗi tìm kiếm không phải là một chuỗi số
            if (!string.IsNullOrWhiteSpace(PatientSearch) && !PatientSearch.All(char.IsDigit))
            {
                var patientsByName = DataProvider.Instance.Context.Patients
                    .Where(p =>
                        p.IsDeleted != true &&
                        p.FullName.ToLower().Contains(PatientSearch.Trim().ToLower()))
                    .ToList();

                //Nếu chỉ có đúng 1 tên trùng thì trả về tên đó
                if (patientsByName.Count == 1)
                    return patientsByName[0];
            }

            //Trả về null nếu không tìm thấy bệnh nhân nào
            return null;
        }
        
        private void InitializeData() //Khởi tạo dữ liệu ban đầu cho AppointmentViewModel
        {
            // Initialize appointment status list
            AppointmentStatusList = new ObservableCollection<string> //Khởi tạo Collection trạng thái cuộc hẹn
            {
                "Tất cả",
                "Đang chờ",
                "Đang khám",
                "Đã khám",
                "Đã hủy"
            };

            SelectedAppointmentStatus = "Tất cả"; // Trạng thái mặc định để lọc là "Tất cả"

            //Tải danh sách bác sĩ
            LoadStaffs();

            //Tạo các thuộc tính ban đầu là rỗng cho các ô nhập liệu
            PatientSearch = string.Empty;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
            SearchText = string.Empty;

            //Tải danh sách cuộc hẹn mặc định là hôm nay.
            LoadAppointments();
        }
        private void LoadStaffs() //Tải danh sách bác sĩ
        {
            try
            {
                var Staffs = DataProvider.Instance.Context.Staffs
                    .Where(d => d.IsDeleted != true && d.RoleId == 1) //Chỉ lấy bác sĩ là Staff có RoleId là 1
                    .OrderBy(d => d.FullName)
                    .ToList();

                DoctorList = new ObservableCollection<Staff>(Staffs);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}",
                    "Lỗi");

                
            }
        }

        public void LoadAppointments() //Method tải danh sách cuộc hẹn
        {
            try
            {
                //Đảm bảo FilterDate có giá trị, nếu không thì mặc định là hôm nay
                if (_filterDate == default(DateTime))
                {
                    _filterDate = DateTime.Today;
                }

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Staff)
                    .Include(a => a.AppointmentType)
                    .Where(a =>
                        a.IsDeleted != true &&
                        a.AppointmentDate.Date == _filterDate.Date);

                //Áp dụng lọc trạng thái nếu không phải là "Tất cả"
                if (!string.IsNullOrEmpty(SelectedAppointmentStatus) && SelectedAppointmentStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus);
                }

                //Áp dụng bộ lọc tìm kiếm nếu có
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower().Trim();
                    query = query.Where(a =>
                        (a.Patient.FullName != null && a.Patient.FullName.ToLower().Contains(searchLower)) ||
                        (a.Staff.FullName != null && a. Staff.FullName.ToLower().Contains(searchLower)) 
                    );
                }

                var appointments = query
                    .OrderBy(a => a.AppointmentDate.TimeOfDay) // Sắp xếp theo thời gian cuộc hẹn
                    .ThenBy(a => a.PatientId) //Nếu cùng giờ thì xếp theo ID bệnh nhân
                    .ToList();

                // Tạo danh sách AppointmentDisplayInfo để hiển thị
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>(
                    appointments.Select(a => new AppointmentDisplayInfo
                    {
                        AppointmentId = a.AppointmentId,
                        PatientName = a.Patient?.FullName ?? "N/A",
                        AppointmentDate = a.AppointmentDate.Date,
                        AppointmentTimeString = a.AppointmentDate.ToString("HH:mm"),
                        Status = a.Status ?? "Chưa xác định",
                        Reason = a.Notes ?? string.Empty,
                        OriginalAppointment = a
                    })
                );

                //Thông báo cho UI biết rằng danh sách cuộc hẹn đã thay đổi
                OnPropertyChanged(nameof(AppointmentsDisplay));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}");

                // Đảm bảo AppointmentsDisplay không null khi có lỗi
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
            }
        }

        private void SearchAppointments()
        {
            
            LoadAppointments();
        }

        private bool CanAddAppointment() //Method kiểm tra xem có thể thêm cuộc hẹn hay không
        {
            //Kiểm tra xem có đang tìm kiếm theo mã bảo hiểm hay không
            bool usingInsuranceCode = !string.IsNullOrWhiteSpace(PatientSearch) &&
                                       PatientSearch.Any(char.IsDigit) &&
                                       !PatientSearch.Contains(" ");

            //Nếu đang tìm kiếm theo mã bảo hiểm, không cần số điện thoại
            bool hasPatient = SelectedPatient != null ||
                              (usingInsuranceCode && !string.IsNullOrWhiteSpace(PatientSearch)) ||
                              (!string.IsNullOrWhiteSpace(PatientSearch) && !string.IsNullOrWhiteSpace(PatientPhone));

            // Với thêm lịch hẹn thì bắt buộc phải có bệnh nhân còn bác sĩ thì không bắt buộc
            return hasPatient &&
                   AppointmentDate.HasValue &&
                   SelectedAppointmentTime.HasValue &&
                   SelectedAppointmentType != null;
        }

        private bool IsAppointmentTimeValid() //Method kiểm tra xem thời gian cuộc hẹn có hợp lệ hay không
        {
            if (!AppointmentDate.HasValue || !SelectedAppointmentTime.HasValue)// Kiểm tra xem ngày và giờ đã được chọn chưa
                return false;

            // Nối giờ và ngày lại
            DateTime appointmentDateTime = AppointmentDate.Value.Date
                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

            // Kiểm tra xem cuộc hẹn có ở trong quá khứ hay không
            if (appointmentDateTime < DateTime.Now)
                return false;

            //Kiểm tra bệnh nhân đã có cuộc hẹn nào vào thời gian này chưa
            if (SelectedPatient != null)
            {
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == SelectedPatient.PatientId && 
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date) 
                    .ToList();

                //Kiểm tra xem có cuộc hẹn nào trùng giờ và phút không
                if (patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    return false;

                // Kiểm tra các cuộc hẹn trùng lặp trong vòng 30 phút. Vì các lịch hẹn của bệnh nhân phải cách nhau ít nhất 30 phút
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                foreach (var existingAppointment in patientAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference > 0)
                        return false;
                }
            }

            //Chỉ kiểm tra lịch làm việc của bác sĩ nếu đã chọn bác sĩ
            if (SelectedDoctor != null && !string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
            {
                //Lấy ngày trong tuần của cuộc hẹn
                DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

                // Parse working hours
                var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

                // Check if doctor works on this day
                if (!workingDays.Contains(dayCode))
                    return false;

                //Kiểm tra xem thời gian cuộc hẹn có nằm trong giờ làm việc của bác sĩ hay không
                TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                if (appointmentTime < startTime || appointmentTime > endTime)
                    return false;

                // Lấy tất cả cuộc hẹn của bác sĩ trong ngày
                var doctorAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.StaffId == SelectedDoctor.StaffId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                //Kiểm tra xem có cuộc hẹn nào trùng giờ và phút không
                if (doctorAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                    return false;

                // Kiểm tra các cuộc hẹn trùng lặp trong vòng 30 phút
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
                foreach (var existingAppointment in doctorAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes); // Tính khoảng cách thời gian giữa các cuộc hẹn, lấy giá trị tuyệt đối

                    if (timeDifference < 30 && timeDifference % 30 != 0) // Kiểm tra nếu khoảng cách thời gian nhỏ hơn 30 phút và không phải là bội số của 30
                        return false;
                }
            }

            return true;
        }
        private string ConvertDayOfWeekToVietnameseCode(DayOfWeek dayOfWeek)//Method chuyển đổi ngày trong tuần sang mã tiếng Việt
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
        private int ConvertVietNameseCodeToDayNumber(string code) //Method chuyển đổi mã ngày tiếng Việt sang số thứ tự ngày
        {
            return code switch
            {
                "T2" => 2,
                "T3" => 3,
                "T4" => 4,
                "T5" => 5,
                "T6" => 6,
                "T7" => 7,
                "CN" => 8,
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


        private void AddNewAppointment() //Method thêm cuộc hẹn mới
        {
            try
            {
                // Bật validation cho tất cả các trường bắt buộc
                _isValidating = true;
                _touchedFields.Add(nameof(PatientSearch));
                _touchedFields.Add(nameof(PatientPhone));
                _touchedFields.Add(nameof(SelectedAppointmentType));
                _touchedFields.Add(nameof(AppointmentDate));
                _touchedFields.Add(nameof(SelectedAppointmentTime));

                // Thêm SelectedDoctor vào danh sách trường cần kiểm tra (nếu có)
                if (SelectedDoctor != null)
                    _touchedFields.Add(nameof(SelectedDoctor));

                // Kích hoạt validation cho các trường bắt buộc
                OnPropertyChanged(nameof(PatientSearch));
                OnPropertyChanged(nameof(PatientPhone));
                OnPropertyChanged(nameof(SelectedAppointmentType));
                OnPropertyChanged(nameof(AppointmentDate));
                OnPropertyChanged(nameof(SelectedAppointmentTime));

                if (SelectedDoctor != null)
                    OnPropertyChanged(nameof(SelectedDoctor));
                // Xác thực ngày/giờ đã chọn
                if (!ValidateDateTimeSelection())
                    return;

                // Kiểm tra lỗi nhập liệu cho các trường bắt buộc
                if (!string.IsNullOrEmpty(this[nameof(PatientSearch)]) ||
                    !string.IsNullOrEmpty(this[nameof(PatientPhone)]) ||
                    !string.IsNullOrEmpty(this[nameof(SelectedAppointmentType)]) ||
                    !string.IsNullOrEmpty(this[nameof(AppointmentDate)]) ||
                    !string.IsNullOrEmpty(this[nameof(SelectedAppointmentTime)]))
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi thêm lịch hẹn.", "Lỗi thông tin");
                    return;
                }

                // Kiểm tra lỗi nhập liệu cho bác sĩ (nếu đã chọn)
                if (SelectedDoctor != null && !string.IsNullOrEmpty(this[nameof(SelectedDoctor)]))
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi thêm lịch hẹn.", "Lỗi thông tin");
                    return;
                }

                // Xác thực thông tin bệnh nhân hoặc tìm/tạo mới bệnh nhân nếu cần
                if (SelectedPatient == null &&
                    !string.IsNullOrWhiteSpace(PatientSearch) &&
                    !string.IsNullOrWhiteSpace(PatientPhone))
                {
                    // Tìm bệnh nhân ở chế độ im lặng (không hiển thị thông báo)
                    FindOrCreatePatient(true);  // silentMode = true

                    // Nếu vẫn không tìm thấy, hiển thị hộp thoại tạo mới
                    if (SelectedPatient == null)
                    {
                        FindOrCreatePatient(false); // silentMode = false
                        if (SelectedPatient == null) return;
                    }
                }
                else if (!ValidatePatientSelection())
                {
                    return;
                }
                // Xác thực loại lịch hẹn
                if (!ValidateAppointmentType())
                    return;

               

                // Tạo thông báo xác nhận lịch hẹn
                string doctorInfo = SelectedDoctor != null
                    ? $" với bác sĩ {SelectedDoctor.FullName}"
                    : " (chưa chọn bác sĩ)";

                // Hiển thị thông báo xác nhận tạo lịch hẹn
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có muốn tạo lịch hẹn cho bệnh nhân {SelectedPatient.FullName}{doctorInfo} vào {AppointmentDate?.ToString("dd/MM/yyyy")} lúc {SelectedAppointmentTime?.ToString("HH:mm")} không?",
                    "Xác nhận");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tạo DateTime chính xác từ ngày và giờ đã chọn
                        DateTime appointmentDateTime;
                        if (AppointmentDate.HasValue && SelectedAppointmentTime.HasValue)
                        {
                            // Kết hợp ngày và giờ thành DateTime đầy đủ
                            appointmentDateTime = new DateTime(
                                AppointmentDate.Value.Year,
                                AppointmentDate.Value.Month,
                                AppointmentDate.Value.Day,
                                SelectedAppointmentTime.Value.Hour,
                                SelectedAppointmentTime.Value.Minute,
                                0);
                        }
                        else
                        {
                            // Sử dụng thời gian hiện tại nếu có lỗi (không nên xảy ra do đã validate ở trên)
                            appointmentDateTime = DateTime.Now;
                        }

                        // Tạo đối tượng lịch hẹn mới
                        Appointment newAppointment = new Appointment
                        {
                            PatientId = SelectedPatient.PatientId,
                            StaffId = SelectedDoctor?.StaffId, // Có thể null nếu không chọn bác sĩ
                            AppointmentDate = appointmentDateTime,
                            AppointmentTypeId = SelectedAppointmentType.AppointmentTypeId,
                            Status = "Đang chờ",
                            Notes = AppointmentNote,
                            CreatedAt = DateTime.Now,
                            IsDeleted = false
                        };

                        // In thông tin debug để kiểm tra DateTime
                        System.Diagnostics.Debug.WriteLine($"Saving appointment date/time: {newAppointment.AppointmentDate:yyyy-MM-dd HH:mm:ss}");

                        // Lưu vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Appointments.Add(newAppointment);
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn tất transaction
                        transaction.Commit();

                        // Hiển thị thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã tạo lịch hẹn thành công!");

                        // Xóa form và làm mới danh sách lịch hẹn
                        ClearAppointmentForm();
                        LoadAppointments();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi
                        transaction.Rollback();

                        // Ném lại ngoại lệ để xử lý ở phần catch bên ngoài
                        throw;
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Xử lý lỗi cập nhật cơ sở dữ liệu
                MessageBoxService.ShowError(
                    $"Lỗi khi lưu lịch hẹn vào cơ sở dữ liệu: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi tạo lịch hẹn: {ex.Message}");
            }
        }
        private bool ValidatePatientSelection()//Method kiểm tra xem đã chọn bệnh nhân hay chưa
        {
            _touchedFields.Add(nameof(PatientSearch));
            _touchedFields.Add(nameof(PatientPhone));

            if (SelectedPatient == null)
            {
                string errorMessage = string.IsNullOrWhiteSpace(PatientSearch) ?
                    "Vui lòng chọn hoặc nhập thông tin bệnh nhân." :
                    "Không tìm thấy bệnh nhân với thông tin đã nhập. Vui lòng kiểm tra lại.";

                MessageBoxService.ShowWarning(
                    errorMessage,
                    "Lỗi - Thông tin bệnh nhân"
              );

                return false;
            }

            return true;
        }

   

        private bool ValidateAppointmentType()//Method kiểm tra xem đã chọn loại lịch hẹn hay chưa
        {
            if (SelectedAppointmentType == null)
            {
                MessageBoxService.ShowWarning(
                    "Vui lòng chọn loại lịch hẹn.",
                    "Lỗi - Thông tin lịch hẹn");

                return false;
            }

            return true;
        }

        private bool ValidateDateTimeSelection()//ethod kiểm tra xem đã chọn ngày và giờ hẹn hợp lệ hay chưa
        {
            //Kiểm tra xem đã chọn ngày hẹn hay chưa
            if (!AppointmentDate.HasValue)
            {
                MessageBoxService.ShowWarning(
                    "Vui lòng chọn ngày hẹn.",
                    "Lỗi - Ngày hẹn");

                return false;
            }

           
            if (AppointmentDate.Value.Date < DateTime.Today)
            {
                MessageBoxService.ShowWarning(
                    "Ngày hẹn không hợp lệ. Vui lòng chọn ngày hiện tại hoặc trong tương lai.",
                    "Lỗi - Ngày hẹn");

                return false;
            }

           
            if (!SelectedAppointmentTime.HasValue)
            {
                MessageBoxService.ShowError(
                    "Vui lòng chọn giờ hẹn.",
                    "Lỗi - Giờ hẹn");

                return false;
            }

            
            DateTime appointmentDateTime = AppointmentDate.Value.Date
                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

            
            if (appointmentDateTime < DateTime.Now)
            {
                MessageBoxService.ShowError(
                    "Thời gian hẹn đã qua. Vui lòng chọn thời gian trong tương lai.",
                    "Lỗi - Thời gian hẹn");

                return false;
            }

            // Tạo khoảng thời gian hợp lệ có thể lựa chọn
            TimeSpan minTime = new TimeSpan(7, 0, 0);  // 07:00
            TimeSpan maxTime = new TimeSpan(17, 0, 0); // 17:00
            TimeSpan selectedTime = appointmentDateTime.TimeOfDay;
            if (selectedTime < minTime || selectedTime > maxTime)
            {
                MessageBoxService.ShowWarning(
                    "Giờ hẹn chỉ được phép trong khoảng từ 07:00 đến 17:00.",
                    "Lỗi - Giờ hẹn");
                return false;
            }

        //Kiểm tra xem bệnh nhân đã có cuộc hẹn nào vào thời gian này chưa
            if (SelectedPatient != null)
            {
                var patientAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.PatientId == SelectedPatient.PatientId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

               
                bool hasExactSameTime = patientAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute);

                if (hasExactSameTime)
                {
                    MessageBoxService.ShowError(
                        $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn vào lúc " +
                        $"{appointmentDateTime.ToString("HH:mm")}.\n" +
                        $"Vui lòng chọn thời gian khác.",
                        "Lỗi - Trùng lịch");

                    return false;
                }


                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

                foreach (var existingAppointment in patientAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference > 0)
                    {
                        MessageBoxService.ShowError(
                            $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn khác vào lúc " +
                            $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
                            $"Vui lòng chọn thời gian cách ít nhất 30 phút.",
                            "Lỗi - Trùng lịch");

                        return false;
                    }
                }
            }


            if (SelectedDoctor != null)
            {
            
                if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
                {
                 
                    DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
                    string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

         
                    var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

             
                    if (!workingDays.Contains(dayCode))
                    {
                        MessageBoxService.ShowError(
                            $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)}).",
                            "Lỗi - Lịch làm việc");

                        return false;
                    }

    
                    TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
                    if (appointmentTime < startTime || appointmentTime > endTime)
                    {
                        MessageBoxService.ShowError(
                            $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName}.\n" +
                            $"Thời gian làm việc: {startTime.ToString("hh\\:mm")} - {endTime.ToString("hh\\:mm")}.",
                            "Lỗi - Giờ làm việc");

                        return false;
                    }
                }

       
                var doctorAppointments = DataProvider.Instance.Context.Appointments
                    .Where(a =>
                        a.StaffId == SelectedDoctor.StaffId &&
                        a.IsDeleted != true &&
                        a.Status != "Đã hủy" &&
                        a.AppointmentDate.Date == appointmentDateTime.Date)
                    .ToList();

                
                if (doctorAppointments.Any(a =>
                    a.AppointmentDate.Hour == appointmentDateTime.Hour &&
                    a.AppointmentDate.Minute == appointmentDateTime.Minute))
                {
                    MessageBoxService.ShowError(
                        $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
                        $"{appointmentDateTime.ToString("HH:mm")}.\n" +
                        $"Vui lòng chọn thời gian khác.",
                        "Lỗi - Trùng lịch");

                    return false;
                }

      
                var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
                foreach (var existingAppointment in doctorAppointments)
                {
                    double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
                    double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

                    if (timeDifference < 30 && timeDifference % 30 != 0)
                    {
                        MessageBoxService.ShowError(
                            $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
                            $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
                            $"Vui lòng chọn thời gian cách ít nhất 30 phút hoặc đúng khung giờ 30 phút.",
                            "Lỗi - Trùng lịch");

                        return false;
                    }
                }
            }

            return true;
        }
        private string GetVietnameseDayName(DayOfWeek dayOfWeek)//Method lấy tên ngày trong tuần bằng tiếng Việt
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
        private void ClearAppointmentForm() //Method xóa form cuộc hẹn
        {
            PatientSearch = string.Empty;
            SelectedPatient = null;
            SelectedDoctor = null;
            SelectedAppointmentType = null;
            AppointmentDate = DateTime.Today;
            SelectedAppointmentTime = null;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
            _isStaffselected = false;
            _isPatientInfoValid = false;
            _isDateTimeValid = false;

            // Xóa _tocedFields để không giữ lại các trường đã được kiểm tra
            _touchedFields.Clear();
            _isValidating = false;
        }

        #region Validation 
        public string this[string columnName]// Method bắt buộc của IDataErrorInfo để kiểm tra lỗi nhập liệu
        {
            get
            {
                //Đảm bảo không validate nếu chưa tương tác
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(PatientSearch):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(PatientSearch))
                        {
                            error = "Vui lòng nhập tên hoặc mã BHYT của bệnh nhân";
                        }
                        break;

                    case nameof(PatientPhone):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(PatientPhone))
                        {
                            error = "Vui lòng nhập số điện thoại";
                        }
                        else if (!string.IsNullOrWhiteSpace(PatientPhone) &&
                                !Regex.IsMatch(PatientPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không hợp lệ (VD: 0901234567)";
                        }
                        break;


                    case nameof(SelectedAppointmentType):
                        if (_touchedFields.Contains(columnName) && SelectedAppointmentType == null)
                        {
                            error = "Vui lòng chọn loại lịch hẹn";
                        }
                        break;

                    case nameof(AppointmentDate):
                        if (_touchedFields.Contains(columnName) && !AppointmentDate.HasValue)
                        {
                            error = "Vui lòng chọn ngày hẹn";
                        }
                        else if (AppointmentDate.HasValue && AppointmentDate.Value < DateTime.Today)
                        {
                            error = "Ngày hẹn không hợp lệ";
                        }
                        break;

                    case nameof(SelectedAppointmentTime):
                        if (_touchedFields.Contains(columnName) && !SelectedAppointmentTime.HasValue)
                        {
                            error = "Vui lòng chọn giờ hẹn";
                        }
                        else if (_touchedFields.Contains(columnName) && AppointmentDate.HasValue && SelectedAppointmentTime.HasValue)
                        {
                            //Chỉ kiểm tra thời gian nếu đã chọn ngày và giờ
                            DateTime appointmentDateTime = AppointmentDate.Value.Date
                                .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

                            if (appointmentDateTime < DateTime.Now)
                            {
                                error = "Thời gian hẹn đã qua";
                            }
                        }
                        break;
                    //Kiểm tra các trường hợp nhập liệu cho loại lịch hẹn
                    case nameof(TypeDisplayName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(TypeDisplayName))
                        {
                            error = "Tên loại lịch hẹn không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(TypeDisplayName) && TypeDisplayName.Length < 2)
                        {
                            error = "Tên loại lịch hẹn phải có ít nhất 2 ký tự";
                        }
                        else if (!string.IsNullOrWhiteSpace(TypeDisplayName) && TypeDisplayName.Length > 50)
                        {
                            error = "Tên loại lịch hẹn không được vượt quá 50 ký tự";
                        }
                        break;

                    case nameof(TypePrice):
                        if (_touchedFields.Contains(columnName) && !TypePrice.HasValue)
                        {
                            error = "Giá không được để trống";
                        }
                        else if (TypePrice.HasValue && TypePrice.Value < 0)
                        {
                            error = "Giá phải là số không âm";
                        }
                        else if (TypePrice.HasValue && TypePrice.Value > 1000000000)
                        {
                            error = "Giá quá cao, vui lòng nhập lại";
                        }
                        break;

                    case nameof(TypeDescription):
                        if (!string.IsNullOrWhiteSpace(TypeDescription) && TypeDescription.Length > 255)
                        {
                            error = "Mô tả không được vượt quá 255 ký tự";
                        }
                        break;
                }

                return error;
            }
        }

        #endregion

        #region AppoinmentType Methods

        private void AddAppontmentType()
        {
            try
            {
                //"Bật" validation cho tất cả các trường bắt buộc
                _isValidating = true;
                _touchedFields.Add(nameof(TypeDisplayName));
                _touchedFields.Add(nameof(TypePrice));
                _touchedFields.Add(nameof(TypeDescription));

                // Thông báo thay đổi để kích hoạt validation
                OnPropertyChanged(nameof(TypeDisplayName));
                OnPropertyChanged(nameof(TypePrice));
                OnPropertyChanged(nameof(TypeDescription));

                //Kiểm tra lỗi nhập liệu cho các trường bắt buộc
                if (!string.IsNullOrEmpty(this[nameof(TypeDisplayName)]) ||
                    !string.IsNullOrEmpty(this[nameof(TypePrice)]) ||
                    !string.IsNullOrEmpty(this[nameof(TypeDescription)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm loại lịch hẹn.",
                        "Lỗi thông tin");
                    return;
                }

               
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm loại lịch hẹn '{TypeDisplayName}' không?",
                    "Xác Nhận Thêm"
                );

                if (!result)
                    return;

                // Kiểm tra xem loại lịch hẹn đã tồn tại hay chưa
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Loại lịch hẹn này đã tồn tại.");
                    return;
                }

              
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        
                        var newAppointmentType = new AppointmentType
                        {
                            TypeName = TypeDisplayName,
                            Description = TypeDescription ?? "",
                            Price = TypePrice ?? 0,
                            IsDeleted = false
                        };

                        DataProvider.Instance.Context.AppointmentTypes.Add(newAppointmentType);
                        DataProvider.Instance.Context.SaveChanges();

                 
                        transaction.Commit();

              
                        ListAppointmentType = new ObservableCollection<AppointmentType>(
                            DataProvider.Instance.Context.AppointmentTypes
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

      
                        AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                        ExecuteRefreshType();

                        MessageBoxService.ShowSuccess(
                            "Đã thêm loại lịch hẹn thành công!",
                            "Thành Công"
                        );
                    }
                    catch (Exception innerEx)
                    {
                    
                        transaction.Rollback();
                        throw innerEx;
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể thêm loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
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
        }

        private void EditAppontmentType()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận trước khi sửa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa loại lịch hẹn '{SelectedAppointmentType.TypeName}' thành '{TypeDisplayName}' không?",
                    "Xác Nhận Sửa"
                );

                if (!result)
                    return;

                // Kiểm tra xem tên loại lịch hẹn đã tồn tại chưa (trừ loại hiện tại)
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() &&
                             s.AppointmentTypeId != SelectedAppointmentType.AppointmentTypeId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Tên loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm loại lịch hẹn cần cập nhật
                        var appointmenttypeToUpdate = DataProvider.Instance.Context.AppointmentTypes
                            .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                        if (appointmenttypeToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại lịch hẹn cần sửa.");
                            return;
                        }

                        // Cập nhật thông tin loại lịch hẹn
                        appointmenttypeToUpdate.TypeName = TypeDisplayName;
                        appointmenttypeToUpdate.Price = TypePrice ?? 0;
                        appointmenttypeToUpdate.Description = TypeDescription ?? "";

                        // Lưu thay đổi vào cơ sở dữ liệu
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn tất giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Cập nhật lại giao diện với dữ liệu mới
                        ListAppointmentType = new ObservableCollection<AppointmentType>(
                            DataProvider.Instance.Context.AppointmentTypes
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Cập nhật danh sách loại lịch hẹn trong form
                        AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                        // Cập nhật loại lịch hẹn vì tên có thể đã thay đổi
                        LoadAppointmentTypeData();
                        ExecuteRefreshType();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã cập nhật loại lịch hẹn thành công!",
                            "Thành Công"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để được xử lý ở khối catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
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
        }

        private void DeleteAppointmentType()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận trước khi xóa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa loại lịch hẹn '{SelectedAppointmentType.TypeName}' không?",
                    "Xác Nhận Xóa"
                );

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra xem loại lịch hẹn có đang được sử dụng trong lịch hẹn không
                        bool isInUse = DataProvider.Instance.Context.Appointments
                            .Any(a => a.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId &&
                                      (a.Status == "Đang chờ" || a.Status == "Đã xác nhận") &&
                                      (bool)!a.IsDeleted);

                        if (isInUse)
                        {
                            MessageBoxService.ShowWarning(
                                "Không thể xóa loại lịch hẹn này vì đang được sử dụng trong các lịch hẹn đang hoạt động.",
                                "Không thể xóa"
                            );
                            return;
                        }

                        // Tìm loại lịch hẹn cần xóa
                        var appointmenttypeToDelete = DataProvider.Instance.Context.AppointmentTypes
                            .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                        if (appointmenttypeToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại lịch hẹn cần xóa.");
                            return;
                        }

                        // Đánh dấu là đã xóa (xóa mềm)
                        appointmenttypeToDelete.IsDeleted = true;

                        // Lưu thay đổi vào cơ sở dữ liệu
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn tất giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Cập nhật lại giao diện với dữ liệu mới
                        ListAppointmentType = new ObservableCollection<AppointmentType>(
                            DataProvider.Instance.Context.AppointmentTypes
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Cập nhật danh sách loại lịch hẹn trong form
                        AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                        // Làm mới form
                        ExecuteRefreshType();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã xóa loại lịch hẹn thành công.",
                            "Thành Công"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để được xử lý ở khối catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa loại lịch hẹn: {ex.Message}",
                    "Lỗi"
                );
            }
        }


        private void UpdatePermissions()
        {
            //Mặc định hủy các quyền
            CanManageAppointments = false;
            CanManageAppointmentTypes = false;

            //  Kiểm tra xem tài khoản hiện tại có tồn tại không
            if (CurrentAccount == null)
                return;

            //Lấy role hiện tại từ CurrentAccount
            string role = CurrentAccount.Role?.Trim() ?? string.Empty;

           
            if (role.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase) || //StringComparison.OrdinalIgnoreCase: So sánh không phân biệt chữ hoa/thường
                role.Equals(UserRoles.Manager, StringComparison.OrdinalIgnoreCase))
            {
                CanManageAppointments = true;
                CanManageAppointmentTypes = true;
            }
            
            else if (role.Equals(UserRoles.Doctor, StringComparison.OrdinalIgnoreCase) ||
                    role.Equals(UserRoles.Cashier, StringComparison.OrdinalIgnoreCase) ||
                    role.Equals(UserRoles.Pharmacist, StringComparison.OrdinalIgnoreCase))
            {
                CanManageAppointments = true;
                CanManageAppointmentTypes = false;
            }
            else
            {
                CanManageAppointments = false;
                CanManageAppointmentTypes = false;
            }

            // Bắt buộc các lệnh phải được đánh giá lại
            CommandManager.InvalidateRequerySuggested();
        }

        private void ExecuteRefreshType()// Method làm mới loại lịch hẹn
        {
            // Xóa thông tin loại lịch hẹn đã chọn
            SelectedAppointmentType = null;
            TypeDisplayName = string.Empty;
            TypeDescription = string.Empty;
            TypePrice = null;
            // Cập nhật lại danh sách loại lịch hẹn
            LoadAppointmentTypeData();
        }
      
        #endregion
    }
}
