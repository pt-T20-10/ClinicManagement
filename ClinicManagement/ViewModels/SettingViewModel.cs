using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClinicManagement.ViewModels
{
    public class SettingViewModel : BaseViewModel
    {
        #region Doctor Information Properties

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

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
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
                _phone = value;
                OnPropertyChanged();
            }
        }

        private string _schedule = string.Empty;
        public string Schedule
        {
            get => _schedule;
            set
            {
                _schedule = value;
                OnPropertyChanged();
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
                _certificateLink = value;
                OnPropertyChanged();
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

        #endregion

        #region Application Settings Properties
        private MainViewModel _mainViewModel; 

        private ObservableCollection<FontFamily> _fontFamilies;
        public ObservableCollection<FontFamily> FontFamilies
        {
            get => _fontFamilies;
            set
            {
                _fontFamilies = value;
                OnPropertyChanged();
            }
        }

        private FontFamily _selectedFontFamily;
        public FontFamily SelectedFontFamily
        {
            get => _selectedFontFamily;
            set
            {
                _selectedFontFamily = value;
                OnPropertyChanged();
            }
        }

        private double _fontSize = 13;
        public double FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                OnPropertyChanged();
            }
        }

        private bool _isLightTheme = true;
        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                _isLightTheme = value;
                OnPropertyChanged();
                if (value)
                {
                    IsDarkTheme = false;
                    UpdatePreviewColors();
                }
            }
        }

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
                OnPropertyChanged();
                if (value)
                {
                    IsLightTheme = false;
                    UpdatePreviewColors();
                }
            }
        }

        private SolidColorBrush _previewBackground;
        public SolidColorBrush PreviewBackground
        {
            get => _previewBackground;
            set
            {
                _previewBackground = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush _previewForeground;
        public SolidColorBrush PreviewForeground
        {
            get => _previewForeground;
            set
            {
                _previewForeground = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush _primaryColorBrush;
        public SolidColorBrush PrimaryColorBrush
        {
            get => _primaryColorBrush;
            set
            {
                _primaryColorBrush = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand UpdateDoctorInfoCommand { get; set; }
        public ICommand ChangePasswordCommand { get; set; }
        public ICommand SaveSettingsCommand { get; set; }
        public ICommand ResetSettingsCommand { get; set; }
        public ICommand LoadedCommand { get; set; }
        public ICommand SignOutCommand { get; set; }

        #endregion

        private Account _currentAccount;
        private Doctor _currentDoctor;

        public SettingViewModel()
        {
            _mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;
            // Initialize commands
            InitializeCommands();
            
            // Initialize font families
            InitializeFontFamilies();
            
            // Initialize colors for preview
            InitializeColors();
            
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
                p => CanUpdateDoctorInfo()
            );

            ChangePasswordCommand = new RelayCommand<object>(
                p => ChangePassword(),
                p => _currentAccount != null
            );

            SaveSettingsCommand = new RelayCommand<object>(
                p => SaveSettings(),
                p => true
            );

            ResetSettingsCommand = new RelayCommand<object>(
                p => ResetSettings(),
                p => true
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
        }

        private bool CanExecuteSignOut()
        {
            return _mainViewModel != null && _mainViewModel.CurrentAccount != null;
        }

        // Thực hiện đăng xuất
        // Thực hiện đăng xuất
        private void ExecuteSignOut()
        {
            try
            {
                if (_mainViewModel == null || _mainViewModel.CurrentAccount == null)
                {
                    MessageBoxService.ShowWarning("Không thể đăng xuất vào lúc này.",
                        "Thông báo"     );
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                 bool  result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Xác nhận đăng xuất"
                     
                      );

                if (result)
                {
                    // Cập nhật trạng thái đăng nhập trong CSDL
                    var accountToUpdate = DataProvider.Instance.Context.Accounts
                        .FirstOrDefault(a => a.Username == _mainViewModel.CurrentAccount.Username);

                    if (accountToUpdate != null)
                    {
                        accountToUpdate.IsLogined = false;
                        DataProvider.Instance.Context.SaveChanges();
                    }

                    // Lấy MainWindow
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Ẩn MainWindow
                        mainWindow.Hide();

                        // Reset CurrentAccount trong MainViewModel
                        var oldAccount = _mainViewModel.CurrentAccount; // Lưu lại để kiểm tra sau này
                        _mainViewModel.CurrentAccount = null;

                        // Hiển thị màn hình đăng nhập
                        LoginWindow loginWindow = new LoginWindow();
                        loginWindow.ShowDialog();

                        // Kiểm tra kết quả đăng nhập
                        var loginVM = loginWindow.DataContext as LoginViewModel;

                        // Nếu đăng nhập thành công (IsLogin = true và CurrentAccount khác null), hiển thị lại MainWindow
                        if (loginVM != null && loginVM.IsLogin && _mainViewModel.CurrentAccount != null)
                        {
                            mainWindow.Show();
                        }
                        else
                        {
                            // Nếu đã ấn Cancel hoặc đóng cửa sổ đăng nhập mà không đăng nhập lại thành công
                            // QUAN TRỌNG: Đảm bảo rằng chúng ta đóng MainWindow và thoát ứng dụng
                            mainWindow.Close(); // Đóng cửa sổ chính
                            Application.Current.Shutdown(); // Đảm bảo ứng dụng thoát hẳn
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi đăng xuất: {ex.Message}",
                    "Lỗi"    );
            }
        }

        private void InitializeFontFamilies()
        {
            FontFamilies = new ObservableCollection<FontFamily>(Fonts.SystemFontFamilies.OrderBy(f => f.Source));
            SelectedFontFamily = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source == "Segoe UI") 
                ?? Fonts.SystemFontFamilies.FirstOrDefault();
        }

        private void InitializeColors()
        {
            // Default colors
            PreviewBackground = new SolidColorBrush(Colors.White);
            PreviewForeground = new SolidColorBrush(Colors.Black);
            PrimaryColorBrush = new SolidColorBrush(Color.FromRgb(63, 81, 181)); // Material Design primary color
            
            UpdatePreviewColors();
        }

        private void UpdatePreviewColors()
        {
            if (IsDarkTheme)
            {
                PreviewBackground = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                PreviewForeground = new SolidColorBrush(Colors.White);
            }
            else
            {
                PreviewBackground = new SolidColorBrush(Colors.White);
                PreviewForeground = new SolidColorBrush(Colors.Black);
            }
        }

        public void LoadDoctorInformation(Account account)
        {
            if (account == null) return;

            _currentAccount = account;
            
            try
            {
                // Load doctor information from account
                _currentDoctor = DataProvider.Instance.Context.Doctors
                    .FirstOrDefault(d => d.DoctorId == account.DoctorId && d.IsDeleted != true);

                if (_currentDoctor != null)
                {
                    DoctorID = _currentDoctor.DoctorId;
                    FullName = _currentDoctor.FullName;
                    Phone = _currentDoctor.Phone ?? string.Empty;
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
                MessageBoxService.ShowError($"Lỗi khi tải thông tin bác sĩ: {ex.Message}", "Lỗi"    );
            }
        }

        private bool CanUpdateDoctorInfo()
        {
            return _currentDoctor != null &&
                   !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone) &&
                   SelectedSpecialty != null;
        }

        private void UpdateDoctorInfo()
        {
            try
            {
                if (_currentDoctor == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!", "Lỗi"    );
                    return;
                }

                // Validate phone number format
                if (!string.IsNullOrWhiteSpace(Phone) && !Regex.IsMatch(Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                {
                    MessageBoxService.ShowError("Số điện thoại không đúng định dạng (VD: 0901234567)", 
                                    "Lỗi định dạng"    );
                    return;
                }

                // Check if phone number already exists (excluding current doctor)
                bool phoneExists = DataProvider.Instance.Context.Doctors
                    .Any(d => d.Phone == Phone.Trim() && d.DoctorId != _currentDoctor.DoctorId && d.IsDeleted == false);

                if (phoneExists)
                {
                    MessageBoxService.ShowError("Số điện thoại này đã được sử dụng bởi một bác sĩ khác.",
                                   "Lỗi dữ liệu"     );
                    return;
                }

                // Get doctor record from database
                var doctorToUpdate = DataProvider.Instance.Context.Doctors
                    .FirstOrDefault(d => d.DoctorId == _currentDoctor.DoctorId);

                if (doctorToUpdate != null)
                {
                    // Update properties
                    doctorToUpdate.FullName = FullName.Trim();
                    doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                    doctorToUpdate.CertificateLink = CertificateLink?.Trim();
                    doctorToUpdate.Schedule = Schedule?.Trim();
                    doctorToUpdate.Phone = Phone?.Trim();
                    doctorToUpdate.Address = Address?.Trim();

                    DataProvider.Instance.Context.SaveChanges();

                    // Update local copy
                    _currentDoctor = doctorToUpdate;

                    MessageBoxService.ShowSuccess("Đã cập nhật thông tin bác sĩ thành công!", 
                                   "Thành Công"    );
                }
                else
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!",
                                   "Lỗi"    );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi cập nhật thông tin bác sĩ: {ex.Message}",
                               "Lỗi"    );
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



        private void SaveSettings()
        {
            try
            {
                // Save application settings (this would typically use a settings service)
                MessageBoxService.ShowSuccess("Đã lưu cài đặt ứng dụng thành công!", 
                               "Thành Công"   );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lưu cài đặt: {ex.Message}", 
                               "Lỗi"    );
            }
        }

        private void ResetSettings()
        {
            try
            {
                // Reset to default settings
                FontSize = 13;
                SelectedFontFamily = Fonts.SystemFontFamilies.FirstOrDefault(f => f.Source == "Segoe UI") 
                    ?? Fonts.SystemFontFamilies.FirstOrDefault();
                IsLightTheme = true;
                PrimaryColorBrush = new SolidColorBrush(Color.FromRgb(63, 81, 181));
                
                MessageBoxService.ShowSuccess("Đã đặt lại cài đặt về mặc định!", 
                               "Thành Công"     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi đặt lại cài đặt: {ex.Message}", 
                               "Lỗi"    );
            }
        }
    }
}
