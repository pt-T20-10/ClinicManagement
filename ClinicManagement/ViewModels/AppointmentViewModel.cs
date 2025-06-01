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

     
        #region Commands

        // AppontmentType Commands
        public ICommand AddAppointmentTypeCommand { get; set; }
        public ICommand EditAppointmentTypeCommand { get; set; }
        public ICommand DeleteAppointmentTypeCommand { get; set; }
 
        #endregion



        #endregion
        public AppointmentViewModel()
        {
            LoadAppointmentTypeData();
            InitializeCommands();
        }
        private void LoadAppointmentTypeData()
        {
            ListAppointmentType = new ObservableCollection<AppointmentType>(
                    DataProvider.Instance.Context.AppointmentTypes
                    .Where(a => (bool)!a.IsDeleted) 
                    .ToList()
                );
        }

        private void InitializeCommands()
        {
            AddAppointmentTypeCommand = new RelayCommand<object>(
               (p) => AddAppontmentType(),
               (p) => !string.IsNullOrEmpty(TypeDisplayName)
           );
            // Doctor Commands
            EditAppointmentTypeCommand = new RelayCommand<object>(
                (p) => EditAppontmentType(),
                (p) => SelectedAppointmentType != null && !string.IsNullOrEmpty(TypeDisplayName)
            );

            DeleteAppointmentTypeCommand = new RelayCommand<object>(
                (p) => DeleteAppointmentType(),
                (p) => SelectedAppointmentType != null
            );
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
