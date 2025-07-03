using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.ViewModels
{
    // Tạo file mới ViewModelLocator.cs
    public class ViewModelLocator
    {
        private Dictionary<string, object> _viewModels = new Dictionary<string, object>();
        private bool _isInitialized = false;

        public object GetViewModel(string viewModelName)
        {
            if (!_isInitialized && viewModelName != "MainVM" && viewModelName != "LoginVM")
                return null;

            if (_viewModels.TryGetValue(viewModelName, out object vm))
                return vm;

            return CreateViewModel(viewModelName);
        }


        public void Initialize()
        {
            _isInitialized = true;
        }

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
                    viewModel = new MedicineSellViewModel();
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
                    // Thêm các ViewModel khác khi cần thiết
            }

            if (viewModel != null)
                _viewModels[viewModelName] = viewModel;

            return viewModel;
        }
    }
    
}
