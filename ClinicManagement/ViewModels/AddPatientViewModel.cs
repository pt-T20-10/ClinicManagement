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
    public class AddPatientViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        private Window _window;

        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        // Selected patient type
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

        // Patient information properties
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
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

        private DateTime? _birthDate = DateTime.Now;
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (_birthDate != value)
                {
                    if (value.HasValue || _birthDate.HasValue)
                        _touchedFields.Add(nameof(BirthDate));
                    else
                        _touchedFields.Remove(nameof(BirthDate));

                    _birthDate = value;
                    OnPropertyChanged();
                }
            }
        }

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

        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
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

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
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

        // Insurance Code
        private string _insuranceCode;
        public string InsuranceCode
        {
            get => _insuranceCode;
            set
            {
                if (_insuranceCode != value)
                {
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
        // Commands
        public ICommand LoadedWindowCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        public AddPatientViewModel()
        {
            // Initialize commands
            LoadedWindowCommand = new RelayCommand<Window>((p) => { _window = p; }, (p) => true);
            SaveCommand = new RelayCommand<object>((p) => ExecuteSave(), (p) => CanSave());
            CancelCommand = new RelayCommand<object>((p) => ExecuteCancel(), (p) => true);

            // Initialize gender
            Gender = "Nam";

            // Load patient types
            LoadPatientTypes();
        }

        #region Validation
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
                    case nameof(FullName):
                        // Only show "required" error if field was touched and is empty
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ tên không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        // Only validate if user has entered something
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
                        // Only validate if user has entered something
                        if (!string.IsNullOrWhiteSpace(Email) &&
                            !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(BirthDate):
                        if (_birthDate.HasValue && _birthDate.Value > DateTime.Now)
                        {
                            error = "Ngày sinh không được lớn hơn ngày hiện tại";
                        }
                        break;

                    case nameof(InsuranceCode):
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

        #region Methods
        private void LoadPatientTypes()
        {
            PatientTypeList = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => pt.IsDeleted != true)
                .ToList()
            );

            // Default to Normal patient type
            SelectedPatientType = PatientTypeList.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals("Thường", StringComparison.OrdinalIgnoreCase) == true);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FullName) &&
                   !string.IsNullOrWhiteSpace(Phone);
        }

        // Then modify your ExecuteSave method to set this property
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

                        // Chuyển đổi ngày sinh thành DateOnly nếu có
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




        private void ExecuteCancel()
        {
            // Reset form
            ResetForm();

            // Optionally close window
            _window?.Close();
        }

        private void ResetForm()
        {
            FullName = string.Empty;
            BirthDate = null;
            IsMale = true;
            IsFemale = false;
            IsOther = false;
            Phone = string.Empty;
            Email = string.Empty;
            Address = string.Empty;
            InsuranceCode = string.Empty;

            // Reset patient type to default
            SelectedPatientType = PatientTypeList.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals("Thường", StringComparison.OrdinalIgnoreCase) == true);

            // Reset validation state
            _touchedFields.Clear();
            _isValidating = false;
        }
        #endregion
    }
}
