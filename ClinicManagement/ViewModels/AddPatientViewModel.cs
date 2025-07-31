using ClinicManagement.Models;
using ClinicManagement.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho cửa sổ thêm bệnh nhân mới
    /// Kế thừa từ BaseViewModel để có sẵn các chức năng INotifyPropertyChanged và RelayCommand
    /// Triển khai IDataErrorInfo để hỗ trợ validation cho giao diện
    /// </summary>
    public class AddPatientViewModel : BaseViewModel, IDataErrorInfo
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

        #region Loại bệnh nhân

        // Loại bệnh nhân đã chọn (Thường, VIP, Bảo hiểm y tế)
        private PatientType _selectedPatientType;
        public PatientType SelectedPatientType
        {
            get => _selectedPatientType;
            set
            {
                _selectedPatientType = value;
                OnPropertyChanged();
            }
        }

        // Danh sách loại bệnh nhân để hiển thị trong ComboBox
        private ObservableCollection<PatientType> _patientTypeList;
        public ObservableCollection<PatientType> PatientTypeList
        {
            get => _patientTypeList;
            set
            {
                _patientTypeList = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Thông tin cá nhân bệnh nhân

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

        // Ngày sinh - trường tùy chọn, mặc định là ngày hiện tại
        private DateTime? _birthDate = DateTime.Now;
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (_birthDate != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
                    if (value.HasValue || _birthDate.HasValue)
                        _touchedFields.Add(nameof(BirthDate));
                    else
                        _touchedFields.Remove(nameof(BirthDate));

                    _birthDate = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Giới tính

        // Giá trị giới tính được lưu vào database
        private string _gender;
        public string Gender
        {
            get => _gender;
            set
            {
                _gender = value;
                OnPropertyChanged();
            }
        }

        // Checkbox cho giới tính Nam - mặc định được chọn
        private bool _isMale = true;
        public bool IsMale
        {
            get => _isMale;
            set
            {
                _isMale = value;
                OnPropertyChanged();
                if (value) Gender = "Nam";
            }
        }

        // Checkbox cho giới tính Nữ
        private bool _isFemale;
        public bool IsFemale
        {
            get => _isFemale;
            set
            {
                _isFemale = value;
                OnPropertyChanged();
                if (value) Gender = "Nữ";
            }
        }

        // Checkbox cho giới tính Khác
        private bool _isOther;
        public bool IsOther
        {
            get => _isOther;
            set
            {
                _isOther = value;
                OnPropertyChanged();
                if (value) Gender = "Khác";
            }
        }

        #endregion

        #region Thông tin liên hệ

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

        #endregion

        #region Thông tin bảo hiểm

        // Mã bảo hiểm y tế - trường tùy chọn nhưng phải đúng định dạng và unique nếu có nhập
        private string _insuranceCode;
        public string InsuranceCode
        {
            get => _insuranceCode;
            set
            {
                if (_insuranceCode != value)
                {
                    // Theo dõi trạng thái tương tác của người dùng
                    bool wasEmpty = string.IsNullOrWhiteSpace(_insuranceCode);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(InsuranceCode));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(InsuranceCode));

                    _insuranceCode = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        // Đối tượng bệnh nhân mới được tạo - để truy cập từ bên ngoài sau khi lưu
        private Patient _newPatient;
        public Patient NewPatient
        {
            get => _newPatient;
            set
            {
                _newPatient = value;
                OnPropertyChanged();
            }
        }

        #region Commands - Các lệnh xử lý sự kiện

        public ICommand LoadedWindowCommand { get; }    // Lệnh khi cửa sổ được tải
        public ICommand SaveCommand { get; }            // Lệnh lưu thông tin bệnh nhân
        public ICommand CancelCommand { get; }          // Lệnh hủy và đóng cửa sổ

        #endregion

        #endregion

        /// <summary>
        /// Constructor khởi tạo ViewModel
        /// </summary>
        public AddPatientViewModel()
        {
            // Khởi tạo các command
            LoadedWindowCommand = new RelayCommand<Window>((p) => { _window = p; }, (p) => true);
            SaveCommand = new RelayCommand<object>((p) => ExecuteSave(), (p) => CanSave());
            CancelCommand = new RelayCommand<object>((p) => ExecuteCancel(), (p) => true);

            // Khởi tạo giới tính mặc định
            Gender = "Nam";

            // Tải danh sách loại bệnh nhân
            LoadPatientTypes();
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
                        // Chỉ hiển thị lỗi "bắt buộc" nếu trường đã được tương tác và để trống
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ tên không được để trống";
                        }
                        // Kiểm tra độ dài tối thiểu
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        // Chỉ validate nếu người dùng đã nhập gì đó
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
                        // Chỉ validate nếu người dùng đã nhập gì đó
                        if (!string.IsNullOrWhiteSpace(Email) &&
                            !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(BirthDate):
                        // Kiểm tra ngày sinh không được lớn hơn ngày hiện tại
                        if (_birthDate.HasValue && _birthDate.Value > DateTime.Now)
                        {
                            error = "Ngày sinh không được lớn hơn ngày hiện tại";
                        }
                        break;

                    case nameof(InsuranceCode):
                        // Kiểm tra định dạng mã BHYT nếu có nhập
                        if (!string.IsNullOrWhiteSpace(InsuranceCode))
                        {
                            if (!Regex.IsMatch(InsuranceCode.Trim(), @"^\d{10}$"))
                            {
                                error = "Mã BHYT phải có đúng 10 chữ số";
                            }
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
                       !string.IsNullOrEmpty(this[nameof(BirthDate)]) ||
                       !string.IsNullOrEmpty(this[nameof(InsuranceCode)]);
            }
        }

        #endregion

        #region Methods - Các phương thức xử lý

        /// <summary>
        /// Tải danh sách loại bệnh nhân từ database
        /// </summary>
        private void LoadPatientTypes()
        {
            PatientTypeList = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => pt.IsDeleted != true)
                .ToList()
            );

            // Mặc định chọn loại bệnh nhân "Thường"
            SelectedPatientType = PatientTypeList.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals("Thường", StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Kiểm tra điều kiện để có thể lưu
        /// </summary>
        /// <returns>True nếu có thể lưu, False nếu chưa đủ điều kiện</returns>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone);
        }

        /// <summary>
        /// Thực hiện lưu thông tin bệnh nhân mới
        /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
        /// </summary>
        private void ExecuteSave()
        {
            try
            {
                // Bật validation cho tất cả các trường khi người dùng submit form
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(InsuranceCode));

                // Kích hoạt kiểm tra validation cho các trường bắt buộc
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(InsuranceCode));

                // Kiểm tra lỗi nhập liệu
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm bệnh nhân.",
                        "Lỗi thông tin"
                    );
                    return;
                }
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn thêm bệnh nhân mới không?",
                    "Xác nhận thêm bệnh nhân"
                );
                if (!result)
                {
                   
                    return;
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra số điện thoại đã tồn tại chưa
                        if (!string.IsNullOrWhiteSpace(Phone))
                        {
                            bool phoneExists = DataProvider.Instance.Context.Patients
                                .Any(p => p.Phone == Phone.Trim() && (p.IsDeleted == null || p.IsDeleted == false));

                            if (phoneExists)
                            {
                                MessageBoxService.ShowError(
                                    "Số điện thoại này đã được sử dụng bởi một bệnh nhân khác.",
                                    "Lỗi dữ liệu"
                                );
                                return;
                            }
                        }

                        // Kiểm tra mã BHYT đã tồn tại chưa (nếu có)
                        if (!string.IsNullOrWhiteSpace(InsuranceCode))
                        {
                            bool insuranceExists = DataProvider.Instance.Context.Patients
                                .Any(p => p.InsuranceCode == InsuranceCode.Trim() && (p.IsDeleted == null || p.IsDeleted == false));

                            if (insuranceExists)
                            {
                                MessageBoxService.ShowError(
                                    "Mã BHYT này đã được sử dụng bởi một bệnh nhân khác.",
                                    "Lỗi dữ liệu"
                                );
                                return;
                            }
                        }

                        // Chuyển đổi ngày sinh thành DateOnly nếu có (kiểu dữ liệu mới của .NET)
                        DateOnly? birthDate = null;
                        if (BirthDate.HasValue)
                        {
                            birthDate = DateOnly.FromDateTime(BirthDate.Value);
                        }

                        // Tạo đối tượng bệnh nhân mới
                        var newPatient = new Patient
                        {
                            FullName = FullName.Trim(),
                            DateOfBirth = birthDate,
                            Gender = Gender,
                            Phone = Phone?.Trim(),
                            Email = Email?.Trim(),
                            Address = Address?.Trim(),
                            InsuranceCode = InsuranceCode?.Trim(),
                            PatientTypeId = SelectedPatientType?.PatientTypeId,
                            CreatedAt = DateTime.Now,
                            IsDeleted = false
                        };

                        // Lưu vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Patients.Add(newPatient);
                        DataProvider.Instance.Context.SaveChanges();

                        // Lưu bệnh nhân vào property NewPatient để có thể truy cập từ bên ngoài
                        // Điều này đảm bảo đối tượng Patient có PatientId được gán bởi database
                        NewPatient = newPatient;

                        // Commit transaction khi mọi thứ thành công
                        transaction.Commit();
                       

                        // Hiển thị thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm bệnh nhân thành công!",
                            "Thành Công"
                        );
                        ResetForm();
                        // Đóng cửa sổ
                        _window?.Close();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có bất kỳ lỗi nào
                        transaction.Rollback();

                        // Ném lại ngoại lệ để được xử lý ở catch block bên ngoài
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý và hiển thị lỗi
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi thêm bệnh nhân: {ex.Message}",
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
            BirthDate = null;
            IsMale = true;
            IsFemale = false;
            IsOther = false;
            Phone = string.Empty;
            Email = string.Empty;
            Address = string.Empty;
            InsuranceCode = string.Empty;

            // Đặt lại loại bệnh nhân về mặc định
            SelectedPatientType = PatientTypeList.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals("Thường", StringComparison.OrdinalIgnoreCase) == true);

            // Đặt lại trạng thái validation
            _touchedFields.Clear();
            _isValidating = false;
        }

        #endregion
    }
}
