using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.ViewModels
{
    public class ViewModelLocator
    {
        //dictionary để lưu trữ các ViewModel đã tạo
        private Dictionary<string, object> _viewModels = new Dictionary<string, object>();

        // Biến cờ để xác định xem đã khởi tạo ViewModels hay chưa
        private bool _isInitialized = false;

        // Phương thức để lấy ViewModel theo tên
        public object GetViewModel(string viewModelName)
        {
            // Chỉ cho phép MainVM và LoginVM trước khi khởi tạo
            if (!_isInitialized && viewModelName != "MainVM" && viewModelName != "LoginVM")
                return null;

            // Trả về ViewModel nếu đã tồn tại
            if (_viewModels.TryGetValue(viewModelName, out object vm))
                return vm;

            // Tạo và trả về ViewModel mới
            return CreateViewModel(viewModelName);
        }

        // Khởi tạo tất cả ViewModels sau khi đăng nhập thành công
        public void Initialize()
        {
            _isInitialized = true;

            // Khởi tạo các ViewModel cần thiết ngay sau đăng nhập
            // Lưu ý: Không khởi tạo MedicineSellVM ở đây - nó sẽ được khởi tạo khi cần thiết
            CreateViewModel("StockMedicineVM");
            CreateViewModel("PatientVM");
            CreateViewModel("AppointmentVM");
            CreateViewModel("InvoiceVM");
            CreateViewModel("StatisticsVM");
            CreateViewModel("StaffVM");
            CreateViewModel("SettingVM");
        }

        // Reset tất cả ViewModels khi đăng xuất
        public void Reset()
        {
            _viewModels.Clear();
            _isInitialized = false;
        }

        private object CreateViewModel(string viewModelName)
        {
            object viewModel = null;

            switch (viewModelName)
            {
                case "MainVM":
                    viewModel = new MainViewModel();
                    break;
                case "LoginVM":
                    viewModel = new LoginViewModel();
                    break;
                case "StockMedicineVM":
                    viewModel = new StockMedicineViewModel();
                    break;
                case "PatientVM":
                    viewModel = new PatientViewModel();
                    break;
                case "AppointmentVM":
                    viewModel = new AppointmentViewModel();
                    break;
                case "InvoiceVM":
                    viewModel = new InvoiceViewModel();
                    break;
                case "MedicineSellVM":
                    // Tạo với flag lazyInitialization = true để tránh tải dữ liệu ngay lập tức
                    viewModel = new MedicineSellViewModel(true);
                    break;
                case "StatisticsVM":
                    viewModel = new StatisticsViewModel();
                    break;
                case "StaffVM":
                    viewModel = new StaffViewModel();
                    break;
                case "SettingVM":
                    viewModel = new SettingViewModel();
                    break;
            }

            if (viewModel != null)
                _viewModels[viewModelName] = viewModel;

            return viewModel;
        }
    }
}
