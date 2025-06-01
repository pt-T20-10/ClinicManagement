using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class AppointmentViewModel : BaseViewModel
    {                                                                                                                                                       
        #region Properties

        #region TypeAppointment
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

        private AppointmentType _SelectedAppointmentType;
        public AppointmentType SelectedAppointmentType
        {
            get => _SelectedAppointmentType;
            set
            {
                _SelectedAppointmentType = value;
                OnPropertyChanged(nameof(SelectedAppointmentType));
                if (value != null)
                {
                    TypeDisplayName = value.TypeName;
                    TypeDescription = value.Description;
                }   
            }
        }

        private string? _TypeDescription;
        public string? TypeDescription
        {
            get => _TypeDescription;
            set
            {
                _TypeDescription = value;
                OnPropertyChanged();
            }
        }

        private string? _TypeDisplayName;
        public string? TypeDisplayName
        {
            get => _TypeDisplayName;
            set
            {
                _TypeDisplayName = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Appointment Form Properties
        // Patient search
        private string _patientSearch;
        public string PatientSearch
        {
            get => _patientSearch;
            set
            {
                _patientSearch = value;
                OnPropertyChanged();
            }
        }

        // Doctor selection
        private ObservableCollection<Doctor> _doctorList;
        public ObservableCollection<Doctor> DoctorList
        {
            get => _doctorList;
            set
            {
                _doctorList = value;
                OnPropertyChanged();
            }
        }

        private Doctor _selectedDoctor;
        public Doctor SelectedDoctor
        {
            get => _selectedDoctor;
            set
            {
                _selectedDoctor = value;
                OnPropertyChanged();
            }
        }

        // Appointment type selection
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

        // Date selection
        private DateTime? _appointmentDate = DateTime.Today;
        public DateTime? AppointmentDate
        {
            get => _appointmentDate;
            set
            {
                _appointmentDate = value;
                OnPropertyChanged();
            }
        }

        // Patient phone
        private string _patientPhone;
        public string PatientPhone
        {
            get => _patientPhone;
            set
            {
                _patientPhone = value;
                OnPropertyChanged();
            }
        }

        // Appointment notes
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
                // Auto-reload appointments when status changes
                LoadAppointments();
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
        public ICommand AddAppointmentTypeCommand { get; set; }
        public ICommand EditAppointmentTypeCommand { get; set; }
        public ICommand DeleteAppointmentTypeCommand { get; set; }

        // Appointment Form Commands
        public ICommand CancelCommand { get; set; }
        public ICommand AddAppointmentCommand { get; set; }

        // Appointment List Commands
        public ICommand SearchCommand { get; set; }
        public ICommand SearchAppointmentsCommand { get; set; }
        #endregion

        #endregion
        public AppointmentViewModel()
        {
            _filterDate = DateTime.Today; // Set default to current date
            LoadAppointmentTypeData();
            InitializeCommands();
            InitializeData();
        }

        private void LoadAppointmentTypeData()
        {
            ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                    .Where(a => (bool)!a.IsDeleted) 
                    .ToList()
                );
            
            // Initialize AppointmentTypes for the form
            AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);
        }

        private void InitializeCommands()
        {
            AddAppointmentTypeCommand = new RelayCommand<object>(
               (p) => AddAppontmentType(),
               (p) => !string.IsNullOrEmpty(TypeDisplayName)
            );
            
            EditAppointmentTypeCommand = new RelayCommand<object>(
                (p) => EditAppontmentType(),
                (p) => SelectedAppointmentType != null && !string.IsNullOrEmpty(TypeDisplayName)
            );

            DeleteAppointmentTypeCommand = new RelayCommand<object>(
                (p) => DeleteAppointmentType(),
                (p) => SelectedAppointmentType != null
            );

            // Initialize form commands
            CancelCommand = new RelayCommand<object>(
                (p) => ClearAppointmentForm(),
                (p) => true
            );

            AddAppointmentCommand = new RelayCommand<object>(
                (p) => AddNewAppointment(),
                (p) => CanAddAppointment()
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
        }

        private void InitializeData()
        {
            // Initialize appointment status list
            AppointmentStatusList = new ObservableCollection<string>
            {
                "Tất cả",
                "Đang chờ",
                "Đã xác nhận",
                "Đang khám",
                "Đã khám",
                "Đã hủy"
            };

            SelectedAppointmentStatus = "Tất cả";

            // Load doctors for dropdown
            LoadDoctors();

            // Initialize other properties
            PatientSearch = string.Empty;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
            SearchText = string.Empty;

            // Load appointments for today
            LoadAppointments();
        }

        private void LoadDoctors()
        {
            try
            {
                var doctors = DataProvider.Instance.Context.Doctors
                    .Where(d => d.IsDeleted != true)
                    .OrderBy(d => d.FullName)
                    .ToList();

                DoctorList = new ObservableCollection<Doctor>(doctors);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải danh sách bác sĩ: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                DoctorList = new ObservableCollection<Doctor>();
            }
        }

        private void LoadAppointments()
        {
            try
            {
                // Ensure FilterDate has a value, default to today
                if (_filterDate == default(DateTime))
                {
                    _filterDate = DateTime.Today;
                }

                var query = DataProvider.Instance.Context.Appointments
                    .Include(a => a.Patient) // Ensure loading Patient to get FullName
                    .Include(a => a.Doctor) // Include Doctor information
                    .Include(a => a.AppointmentType) // Include AppointmentType information
                    .Where(a =>
                        a.IsDeleted != true &&
                        a.AppointmentDate.Date == _filterDate.Date);

                // Apply status filter if not "Tất cả"
                if (!string.IsNullOrEmpty(SelectedAppointmentStatus) && SelectedAppointmentStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedAppointmentStatus);
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower().Trim();
                    query = query.Where(a => 
                        (a.Patient.FullName != null && a.Patient.FullName.ToLower().Contains(searchLower)) ||
                        (a.Doctor.FullName != null && a.Doctor.FullName.ToLower().Contains(searchLower))
                    );
                }

                var appointments = query
                    .OrderBy(a => a.AppointmentDate.TimeOfDay) // Sort by time of day
                    .ThenBy(a => a.PatientId) // If same time, sort by PatientId
                    .ToList();

                // Create display-friendly objects with formatted time and reason
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

                // Trigger property changed to update UI
                OnPropertyChanged(nameof(AppointmentsDisplay));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Ensure AppointmentsDisplay is not null when there's an error
                AppointmentsDisplay = new ObservableCollection<AppointmentDisplayInfo>();
            }
        }

        private void SearchAppointments()
        {
            // Simply call LoadAppointments which already handles search filtering
            LoadAppointments();
        }

        private bool CanAddAppointment()
        {
            // Basic validation for adding an appointment
            return SelectedDoctor != null && 
                   !string.IsNullOrWhiteSpace(PatientSearch) && 
                   AppointmentDate.HasValue &&
                   SelectedAppointmentTime.HasValue;
        }

        private void AddNewAppointment()
        {
            // Implementation would go here
            MessageBox.Show(
                "Chức năng thêm lịch hẹn đang được phát triển.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearAppointmentForm()
        {
            PatientSearch = string.Empty;
            SelectedDoctor = null;
            SelectedAppointmentType = null;
            AppointmentDate = DateTime.Today;
            SelectedAppointmentTime = null;
            PatientPhone = string.Empty;
            AppointmentNote = string.Empty;
        }

        #region AppoinmentType Methods
        private void AddAppontmentType()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm loaị lịch hẹn '{TypeDisplayName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newAppointmentType = new AppointmentType
                {
                    TypeName = TypeDisplayName,
                    Description = TypeDescription ?? "",
                    IsDeleted = false
                };

                DataProvider.Instance.Context.AppointmentTypes.Add(newAppointmentType);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                // Clear fields
                TypeDisplayName = "";
                TypeDescription = "";

                MessageBox.Show(
                    "Đã thêm loại lịch hẹn thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể thêm loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditAppontmentType()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa loại lịch hẹn '{SelectedAppointmentType.TypeName}' thành '{TypeDisplayName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.AppointmentTypes
                    .Any(s => s.TypeName.Trim().ToLower() == TypeDisplayName.Trim().ToLower() &&
                              s.AppointmentTypeId != SelectedAppointmentType.AppointmentTypeId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Tên loại lịch hẹn này đã tồn tại.");
                    return;
                }

                // Update specialty
                var appointmenttypeToUpdate = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy loại lịch hẹn cần sửa.");
                    return;
                }

                appointmenttypeToUpdate.TypeName = TypeDisplayName;
                appointmenttypeToUpdate.Description = TypeDescription ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                // Update doctor list as specialty names may have changed
                LoadAppointmentTypeData();

                MessageBox.Show(
                    "Đã cập nhật loại lịch hẹn thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa loại lịch hẹn: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteAppointmentType()
        {
            try
            {
               
                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa loại lịch hẹn '{SelectedAppointmentType.TypeName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the specialty
                var appointmenttypeToDelete = DataProvider.Instance.Context.AppointmentTypes
                    .FirstOrDefault(s => s.AppointmentTypeId == SelectedAppointmentType.AppointmentTypeId);

                if (appointmenttypeToDelete == null)
                {
                    MessageBox.Show("Không tìm thấy loại lịch hẹn cần xóa.");
                    return;
                }

                appointmenttypeToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Also update AppointmentTypes collection
                AppointmentTypes = new ObservableCollection<AppointmentType>(ListAppointmentType);

                // Clear selection and fields
                SelectedAppointmentType = null;
                TypeDisplayName = "";
                TypeDescription = "";

                MessageBox.Show(
                    "Đã xóa loại lịch hẹn thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa loại lịch hẹn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        #endregion

    }
}
