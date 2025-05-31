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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class PatientViewModel : BaseViewModel
    {
        #region Properties  
        #region DisplayProperties
        private PatientType? _SelectedType;
        public PatientType? SelectedType
        {
            get => _SelectedType;
            set
            {
                _SelectedType = value;
                OnPropertyChanged(nameof(SelectedType));

                if (SelectedType != null)
                {
                    PatientTypeId = SelectedType.PatientTypeId;
                    TypeName = SelectedType.TypeName;
                    Discount = SelectedType.Discount.ToString();
                }
            }
        }

        private int _PatientTypeId;
        public int PatientTypeId
        {
            get => _PatientTypeId;
            set
            {
                _PatientTypeId = value; OnPropertyChanged();
            }
        }

        private string _TypeName;
        public string TypeName
        {
            get => _TypeName;
            set
            {
                _TypeName = value; OnPropertyChanged();
            }
        }

        private string _Discount;
        public string Discount
        {
            get => _Discount;
            set
            {
                _Discount = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<Patient> _PatientList;
        public ObservableCollection<Patient> PatientList
        {
            get => _PatientList;
            set
            {
                _PatientList = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<PatientType> _PatientTypeList;
        public ObservableCollection<PatientType> PatientTypeList
        {
            get => _PatientTypeList;
            set
            {
                _PatientTypeList = value; OnPropertyChanged();
            }
        }
        #endregion

        #region Filter Properties

        private ObservableCollection<Patient> _AllPatients;


        private DateTime? _SelectedDate;
        public DateTime? SelectedDate
        {
            get => _SelectedDate;
            set
            {
                _SelectedDate = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi ngày
                ExecuteAutoFilter();
            }
        }

        private string _SearchText;
        public string SearchText
        {
            get => _SearchText;
            set
            {
                _SearchText = value;
                OnPropertyChanged();
                ExecuteAutoFilter();
            }
        }

        private int? _FilterPatientTypeId;
        public int? FilterPatientTypeId
        {
            get => _FilterPatientTypeId;
            set
            {
                _FilterPatientTypeId = value;
                OnPropertyChanged();
                // Tự động lọc khi thay đổi loại bệnh nhân
                ExecuteAutoFilter();
            }
        }
        private bool _IsVIPSelected;
        public bool IsVIPSelected
        {
            get => _IsVIPSelected;
            set
            {
                _IsVIPSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("VIP");
                }
            }
        }

        private bool _IsAllSelected;
        public bool IsAllSelected
        {
            get => _IsAllSelected;
            set
            {
                _IsAllSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsInsuranceSelected = false;
                    IsNormalSelected = false;
                    IsVIPSelected = false;
                    FilterPatientTypeId = null; // ← Thêm dòng này để xóa filter theo loại
                    ExecuteAutoFilter();
                }
            }
        }


        private bool _IsInsuranceSelected;
        public bool IsInsuranceSelected
        {
            get => _IsInsuranceSelected;
            set
            {
                _IsInsuranceSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsNormalSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Bảo hiểm y tế");
                }
            }
        }

        private bool _IsNormalSelected;
        public bool IsNormalSelected
        {
            get => _IsNormalSelected;
            set
            {
                _IsNormalSelected = value;
                OnPropertyChanged();
                if (value)
                {
                    IsVIPSelected = false;
                    IsInsuranceSelected = false;
                    FilterPatientTypeId = GetPatientTypeIdByName("Thường");
                }
            }
        }
        #endregion
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set { _selectedPatient = value; OnPropertyChanged(); }
        }

        #endregion

        #region Command

        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand AddPatientCommand { get; set; }
        public ICommand OpenPatientDetailsCommand { get; set; }

        // Filter Commands
        public ICommand SearchCommand { get; set; }
        public ICommand VIPSelectedCommand { get; set; }
        public ICommand InsuranceSelectedCommand { get; set; }
        public ICommand NormalSelectedCommand { get; set; }
        public ICommand AllSelectedCommand { get; set; }
        public ICommand ResetFiltersCommand { get; set; }
        #endregion

        public PatientViewModel()
        {


            LoadData();
            InitializTypeCommands();
            InitializeFilterCommands();
            IsAllSelected = true;

            AddPatientCommand = new RelayCommand<object>(
                   (p) =>
                   {
                       // Mở cửa sổ thêm bệnh nhân mới
                       AddPatientWindow addPatientWindow = new AddPatientWindow();
                       addPatientWindow.ShowDialog();
                       // Refresh data after adding a new patient
                       LoadData();
                   },
                   (p) => true
               );


        }
        #region Type Commands

        private void InitializTypeCommands()
        {
            AddCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                        MessageBoxResult result = MessageBox.Show(
                             $"Bạn có chắc muốn thêm loại bệnh nhân '{TypeName}' không?",
                             "Xác Nhận Thêm",
                             MessageBoxButton.YesNo,
                             MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;

                        bool isExist = DataProvider.Instance.Context.PatientTypes
                              .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() && pt.IsDeleted == false);

                        if (isExist)
                        {
                            MessageBox.Show("Loại bệnh nhân này đã tồn tại.");
                        }
                        else
                        {
                            decimal parsedDiscount = 0;
                            if (decimal.TryParse(Discount, out parsedDiscount))
                            {
                                var objectt = new PatientType()
                                {
                                    TypeName = TypeName,
                                    Discount = parsedDiscount
                                };

                                DataProvider.Instance.Context.PatientTypes.Add(objectt);
                                DataProvider.Instance.Context.SaveChanges();

                                // Refresh data after add
                                LoadData();

                                // Hiển thị thông báo thành công
                                MessageBox.Show(
                                     "Đã thêm loại bệnh nhân thành công!",
                                     "Thành Công",
                                     MessageBoxButton.OK,
                                     MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Vui lòng nhập giảm giá hợp lệ (số thực).");
                            }
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        // Xử lý lỗi liên quan đến cơ sở dữ liệu
                        MessageBox.Show(
                             $"Không thể thêm loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
                             "Lỗi Cơ Sở Dữ Liệu",
                             MessageBoxButton.OK,
                             MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        // Xử lý lỗi chung
                        MessageBox.Show(
                             $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                             "Lỗi",
                             MessageBoxButton.OK,
                             MessageBoxImage.Error);
                    }
                },
                (p) => true
            );

            EditCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                        MessageBoxResult result = MessageBox.Show(
                             $"Bạn có chắc muốn sửa loại bệnh nhân '{TypeName}' không?",
                             "Xác Nhận Sửa",
                             MessageBoxButton.YesNo,
                             MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                            return;

                        // Tìm loại bệnh nhân cần sửa
                        var patientTypeToUpdate = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToUpdate == null)
                        {
                            MessageBox.Show("Không tìm thấy loại bệnh nhân cần sửa.");
                            return;
                        }

                        // Kiểm tra TypeName mới có trùng với loại khác không (ngoại trừ chính nó)
                        bool isExist = DataProvider.Instance.Context.PatientTypes
                            .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower() &&
                                  pt.PatientTypeId != SelectedType.PatientTypeId);

                        if (isExist)
                        {
                            MessageBox.Show("Loại bệnh nhân này đã tồn tại.");
                            return;
                        }

                        // Kiểm tra Discount
                        if (decimal.TryParse(Discount, out decimal parsedDiscount))
                        {
                            patientTypeToUpdate.TypeName = TypeName;
                            patientTypeToUpdate.Discount = parsedDiscount;

                            DataProvider.Instance.Context.SaveChanges();

                            MessageBox.Show(
                                 "Đã cập nhật loại bệnh nhân thành công!",
                                 "Thành Công",
                                 MessageBoxButton.OK,
                                 MessageBoxImage.Information);

                            // Refresh data after edit
                            LoadData();
                        }
                        else
                        {
                            MessageBox.Show("Vui lòng nhập giảm giá hợp lệ (số thực).");
                        }
                    }
                    catch (DbUpdateException ex)
                    {
                        MessageBox.Show(
                             $"Không thể sửa loại bệnh nhân: {ex.InnerException?.Message ?? ex.Message}",
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
                },
                (p) => SelectedType != null
            );

            DeleteCommand = new RelayCommand<object>(
                (p) =>
                {
                    try
                    {
                        // Hiển thị hộp thoại xác nhận
                        MessageBoxResult result = MessageBox.Show(
                            $"Bạn có chắc muốn xóa loại bệnh nhân '{SelectedType?.TypeName}' không?",
                            "Xác Nhận Xóa",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                            return;

                        // Tìm đối tượng cần xóa
                        var patientTypeToDelete = DataProvider.Instance.Context.PatientTypes
                            .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId);

                        if (patientTypeToDelete == null)
                        {
                            MessageBox.Show("Không tìm thấy loại bệnh nhân để xóa.");
                            return;
                        }

                        // Đánh dấu IsDeleted = true
                        patientTypeToDelete.IsDeleted = true;

                        DataProvider.Instance.Context.SaveChanges();

                        MessageBox.Show(
                            "Đã xóa (ẩn) loại bệnh nhân thành công.",
                            "Xóa Thành Công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Refresh data after delete
                        LoadData();
                        SelectedType = null;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Đã xảy ra lỗi khi xóa: {ex.Message}",
                            "Lỗi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                },
                (p) => SelectedType != null
            );
        }

        #endregion

        #region Data Loading Methods

        private void LoadData()
        {
            _AllPatients = new ObservableCollection<Patient>(
                DataProvider.Instance.Context.Patients
                .Include(p => p.PatientType)
                .Where(p => p.IsDeleted != true)
                .ToList()
            );

            PatientList = new ObservableCollection<Patient>(_AllPatients);

            PatientTypeList = new ObservableCollection<PatientType>(
                DataProvider.Instance.Context.PatientTypes
                .Where(pt => (bool)!pt.IsDeleted)
                .ToList()
            );

            OpenPatientDetailsCommand = new RelayCommand<Patient>(
                (p) => OpenPatientDetails(p),
                (p) => p != null
                );
                }

        private void OpenPatientDetails(Patient patient)
        {
            if (patient == null) return;

            var detailsWindow = new PatientDetailsWindow
            {
                DataContext = new PatientDetailsWindowViewModel { Patient = patient }
            };
            detailsWindow.ShowDialog();
            LoadData();
        }

        #endregion

        #region Filter Methods

        private void InitializeFilterCommands()
        {
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );
            AllSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteAllFilter(),
                (p) => true
            );

            VIPSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteVIPFilter(),
                (p) => true
            );

            InsuranceSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteInsuranceFilter(),
                (p) => true
            );

            NormalSelectedCommand = new RelayCommand<object>(
                (p) => ExecuteNormalFilter(),
                (p) => true
            );

            ResetFiltersCommand = new RelayCommand<object>(
                (p) => ExecuteResetFilters(),
                (p) => true
            );
        }

        private void ExecuteSearch()
        {
            ApplyFilters();
        }
        private void ExecuteAllFilter()
        {
            ApplyFilters();
        }

        private void ExecuteVIPFilter()
        {
            IsVIPSelected = true;
        }

        private void ExecuteInsuranceFilter()
        {
            IsInsuranceSelected = true;
        }

        private void ExecuteNormalFilter()
        {
            IsNormalSelected = true;
        }

        private void ExecuteAutoFilter()
        {
            // Lọc tự động khi thay đổi ngày hoặc loại bệnh nhân
            ApplyFilters();
        }

        private void ExecuteResetFilters()
        {
            SelectedDate = null;
            SearchText = string.Empty;
            FilterPatientTypeId = null;
            IsVIPSelected = false;
            IsInsuranceSelected = false;
            IsNormalSelected = false;

            PatientList = new ObservableCollection<Patient>(_AllPatients);
        }

        private void ApplyFilters()
        {
            if (_AllPatients == null || _AllPatients.Count == 0)
            {
                PatientList = new ObservableCollection<Patient>();
                return;
            }

            var filteredList = _AllPatients.AsEnumerable();

            // Lọc theo ngày tạo
            if (SelectedDate.HasValue)
            {
                filteredList = filteredList.Where(p => p.CreatedAt?.Date == SelectedDate.Value.Date);
            }

            // Lọc theo loại bệnh nhân
            if (FilterPatientTypeId.HasValue)
            {
                filteredList = filteredList.Where(p => p.PatientTypeId == FilterPatientTypeId.Value);
            }

            // Lọc theo tên hoặc mã bảo hiểm
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = filteredList.Where(p =>
                    (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(p.InsuranceCode) && p.InsuranceCode.ToLower().Contains(searchTerm))
                );
            }

            PatientList = new ObservableCollection<Patient>(filteredList.ToList());
        }

        private int? GetPatientTypeIdByName(string typeName)
        {
            var patientType = PatientTypeList?.FirstOrDefault(pt =>
                pt.TypeName?.Trim().Equals(typeName.Trim(), StringComparison.OrdinalIgnoreCase) == true);
            return patientType?.PatientTypeId;
        }

        #endregion




    }

}
