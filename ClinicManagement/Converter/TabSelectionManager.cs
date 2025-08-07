using ClinicManagement.ViewModels;
using System.Windows;

namespace ClinicManagement.Services
{
    public class TabSelectionManager
    {
        //Singleton cho TabSelectionManager
        private static TabSelectionManager _instance;
        public static TabSelectionManager Instance => _instance ??= new TabSelectionManager();

        // Dictionary để lưu trữ các hành động tải lại dữ liệu cho từng tab
        private Dictionary<string, Action> _tabReloadActions = new Dictionary<string, Action>();

        // Tab cuối cùng được chọn
        private string _lastActiveTab;

        // Đăng kí hành động tải lại dữ liệu cho một tab cụ thể
        public void RegisterTabReloadAction(string tabName, Action reloadAction)
        {
            if (string.IsNullOrEmpty(tabName) || reloadAction == null)
                return;

            _tabReloadActions[tabName] = reloadAction;
        }

    
        public void TabSelected(string tabName)
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM == null || mainVM.CurrentAccount == null)
            {
                return; // Chưa đăng nhập, không tải dữ liệu
            }

            // Tiến hành tải dữ liệu cho tab được chọn
            if (_tabReloadActions.TryGetValue(tabName, out Action reloadAction))
            {
                reloadAction?.Invoke();
            }
        }
    }
}
