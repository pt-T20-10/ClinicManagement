using ClinicManagement.Models;
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
        #region Command

        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        #endregion
        public PatientViewModel() 
        {
         
            
                PatientList = new ObservableCollection<Patient>(DataProvider.Instance.Context.Patients);
                PatientTypeList = new ObservableCollection<PatientType>(
                 DataProvider.Instance.Context.PatientTypes
                .Where(pt => (bool)!pt.IsDeleted)
                .ToList()
);


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
      (p) =>
      {
              return true;
          
      }
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

            // Tìm loại bệnh nhân cần sửa (giả sử bạn có ID đang lưu trong SelectedPatientType.Id)
            var patientTypeToUpdate = DataProvider.Instance.Context.PatientTypes
                .FirstOrDefault(pt => pt.PatientTypeId == SelectedType.PatientTypeId );

            if (patientTypeToUpdate == null)
            {
                MessageBox.Show("Không tìm thấy loại bệnh nhân cần sửa.");
                return;
            }

            // Kiểm tra TypeName mới có trùng với loại khác không (ngoại trừ chính nó)
            bool isExist = DataProvider.Instance.Context.PatientTypes
                .Any(pt => pt.TypeName.Trim().ToLower() == TypeName.Trim().ToLower());

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

                PatientTypeList.Remove(SelectedType);
                PatientTypeList.Add(patientTypeToUpdate);
                SelectedType = patientTypeToUpdate;
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
    (p) =>
    {
        return SelectedType != null; // chỉ bật khi có đối tượng đang được chọn
    }
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

                PatientTypeList.Remove(SelectedType);

            SelectedType = new PatientType(); // Đặt SelectedType về null sau khi xóa
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
    (p) =>
    {
        return SelectedType != null; // chỉ bật khi có dòng đang chọn
    }
);


        }
    }
}
