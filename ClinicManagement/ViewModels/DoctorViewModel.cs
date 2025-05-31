using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class DoctorViewModel : BaseViewModel
    {
        #region Properties
        #region DisplayProperties
        private ObservableCollection<Doctor> _DoctorList;
        public ObservableCollection<Doctor> DoctorList
        {
            get => _DoctorList;
            set
            {
                _DoctorList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<DoctorSpecialty> _ListSpecialty;
        public ObservableCollection<DoctorSpecialty> ListSpecialty
        {
            get => _ListSpecialty;
            set
            {
                _ListSpecialty = value;
                OnPropertyChanged();
            }
        }

        private DoctorSpecialty _SelectedItem;
        public DoctorSpecialty SelectedItem
        {
            get => _SelectedItem;
            set
            {
                _SelectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));

                if (SelectedItem != null)
                {
                    SpecialtyName = SelectedItem.SpecialtyName;
                    Description = SelectedItem.Description;
                }
            }
        }
        private DoctorSpecialty _SelectedSpecialty;
        public DoctorSpecialty SelectedSpecialty
        {
            get => _SelectedSpecialty;
            set
            {
                _SelectedSpecialty = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedSpecialtyId = GetSpecialtyIdByName(value.SpecialtyName);
                }
                else
                {
                    SelectedSpecialtyId = null;
                }
            }
        }
        private int? _SelectedSpecialtyId;
        public int? SelectedSpecialtyId
        {
            get => _SelectedSpecialtyId;
            set
            {
                _SelectedSpecialtyId = value;
                OnPropertyChanged();

                ExecuteSearch();
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

        private string _SpecialtyName;
        public string SpecialtyName
        {
            get => _SpecialtyName;
            set
            {
                _SpecialtyName = value;
                OnPropertyChanged();
            }
        }

        private string _Description;
        public string Description
        {
            get => _Description;
            set
            {
                _Description = value;
                OnPropertyChanged();
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
                ExecuteSearch();
            }
        }
        #endregion
        
        private ObservableCollection<Doctor> _allDoctors;
        #endregion

        #region Commands
     
        
        // Doctor Commands
        public ICommand AddDoctorCommand { get; set; }
        public ICommand EditDoctorCommand { get; set; }
        public ICommand DeleteDoctorCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ResetFiltersCommand { get; set; }

        public ICommand OpenDoctorDetailsCommand { get; set; }

        // Specialty Commands
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        #endregion

        public DoctorViewModel()
        {
        
             
                    LoadData();
               

            InitializeCommands();
        }

        #region Methods
        private void InitializeCommands()
        {
            ResetFiltersCommand = new RelayCommand<object>(
               (p) => ExecuteResetFilters(),
               (p) => true
           );
            // Doctor Commands
            SearchCommand = new RelayCommand<object>(
                (p) => ExecuteSearch(),
                (p) => true
            );

            AddDoctorCommand = new RelayCommand<object>(
                (p) =>
                {
                    // Open window to add new doctor
                    var addDoctorWindow = new AddDoctorWindow();
                    addDoctorWindow.ShowDialog();
                    // Refresh data after adding new doctor
                    LoadData();
                },
                (p) => true
            );

            OpenDoctorDetailsCommand = new RelayCommand<Doctor>(
                (p) => OpenDoctorDetails(p),
                (p) => p != null
            );

      
        // Specialty Commands
        AddCommand = new RelayCommand<object>(
                (p) => AddSpecialty(),
                (p) => !string.IsNullOrEmpty(SpecialtyName)
            );

            EditCommand = new RelayCommand<object>(
                (p) => EditSpecialty(),
                (p) => SelectedItem != null && !string.IsNullOrEmpty(SpecialtyName)
            );

            DeleteCommand = new RelayCommand<object>(
                (p) => DeleteSpecialty(),
                (p) => SelectedItem != null
            );
        }
        private void OpenDoctorDetails(Doctor doctor)
        {
            if (doctor == null) return;

            var detailsWindow = new DoctorDetailsWindow
            {
                DataContext = new DoctorDetailsWindowViewModel { Doctor = doctor }
            };
            detailsWindow.ShowDialog();
        }
        private void LoadData()
        {
            // Load doctors with their specialties
            _allDoctors = new ObservableCollection<Doctor>(
                DataProvider.Instance.Context.Doctors
                    .Include(d => d.Specialty)
                    .Where(d => (bool)!d.IsDeleted)
                    .ToList()
            );

            DoctorList = new ObservableCollection<Doctor>(_allDoctors);

            // Load specialties
            ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                DataProvider.Instance.Context.DoctorSpecialties
                    .Where(s => (bool)!s.IsDeleted)
                    .ToList()
            );
        }
        private void ExecuteResetFilters()
        {
          
            SearchText = string.Empty;
            SelectedSpecialty = null;
        
            DoctorList = new ObservableCollection<Doctor>(_allDoctors);
        }

        private void ExecuteSearch()
        {
            if (_allDoctors == null || _allDoctors.Count == 0)
            {
                DoctorList = new ObservableCollection<Doctor>();
                return;
            }

        
            var filteredList = _allDoctors.AsEnumerable();

            if (SelectedSpecialtyId.HasValue)
                filteredList = _allDoctors.Where(d => d.SpecialtyId == SelectedSpecialtyId && (bool)!d.IsDeleted);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower().Trim();
                filteredList = _allDoctors
                .Where(d => d.FullName != null && d.FullName.ToLower().Contains(searchTerm))
                .ToList();
            }


            DoctorList = new ObservableCollection<Doctor>(filteredList);
        }

        private int GetSpecialtyIdByName(string specialtyName)
        {
            var specialty = DataProvider.Instance.Context.DoctorSpecialties
                .FirstOrDefault(s => s.SpecialtyName.Trim().ToLower() == specialtyName.Trim().ToLower() && (bool)!s.IsDeleted);
            return specialty?.SpecialtyId ?? 0;
        }

        #region Specialty Methods
        private void AddSpecialty()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm chuyên khoa '{SpecialtyName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                    .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Chuyên khoa này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newSpecialty = new DoctorSpecialty
                {
                    SpecialtyName = SpecialtyName,
                    Description = Description ?? "",
                    IsDeleted = false
                };

                DataProvider.Instance.Context.DoctorSpecialties.Add(newSpecialty);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear fields
                SpecialtyName = "";
                Description = "";

                MessageBox.Show(
                    "Đã thêm chuyên khoa thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể thêm chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
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

        private void EditSpecialty()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa chuyên khoa '{SelectedItem.SpecialtyName}' thành '{SpecialtyName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.DoctorSpecialties
                    .Any(s => s.SpecialtyName.Trim().ToLower() == SpecialtyName.Trim().ToLower() && 
                              s.SpecialtyId != SelectedItem.SpecialtyId && 
                             (bool) !s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Tên chuyên khoa này đã tồn tại.");
                    return;
                }

                // Update specialty
                var specialtyToUpdate = DataProvider.Instance.Context.DoctorSpecialties
                    .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                if (specialtyToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy chuyên khoa cần sửa.");
                    return;
                }

                specialtyToUpdate.SpecialtyName = SpecialtyName;
                specialtyToUpdate.Description = Description ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Update doctor list as specialty names may have changed
                LoadData();

                MessageBox.Show(
                    "Đã cập nhật chuyên khoa thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa chuyên khoa: {ex.InnerException?.Message ?? ex.Message}",
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

        private void DeleteSpecialty()
        {
            try
            {
                // Check if specialty is in use by any doctors
                bool isInUse = DataProvider.Instance.Context.Doctors
                    .Any(d => d.SpecialtyId == SelectedItem.SpecialtyId && (bool)!d.IsDeleted);

                if (isInUse)
                {
                    MessageBox.Show(
                        "Không thể xóa chuyên khoa này vì đang được sử dụng bởi một hoặc nhiều bác sĩ.",
                        "Cảnh báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa chuyên khoa '{SelectedItem.SpecialtyName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the specialty
                var specialtyToDelete = DataProvider.Instance.Context.DoctorSpecialties
                    .FirstOrDefault(s => s.SpecialtyId == SelectedItem.SpecialtyId);

                if (specialtyToDelete == null)
                {
                    MessageBox.Show("Không tìm thấy chuyên khoa cần xóa.");
                    return;
                }

                specialtyToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                ListSpecialty = new ObservableCollection<DoctorSpecialty>(
                    DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear selection and fields
                SelectedItem = null;
                SpecialtyName = "";
                Description = "";

                MessageBox.Show(
                    "Đã xóa chuyên khoa thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa chuyên khoa: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

       
        #endregion
        #endregion
    }
}
