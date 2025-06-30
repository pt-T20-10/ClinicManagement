using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ClinicManagement.Services
{
    public class TabSelectionManager
    {
        private static TabSelectionManager _instance;
        public static TabSelectionManager Instance => _instance ??= new TabSelectionManager();

        // Dictionary to store tab reload actions
        private Dictionary<string, Action> _tabReloadActions = new Dictionary<string, Action>();

        // Last active tab
        private string _lastActiveTab;

        // Register a reload action for a specific tab
        public void RegisterTabReloadAction(string tabName, Action reloadAction)
        {
            if (string.IsNullOrEmpty(tabName) || reloadAction == null)
                return;

            _tabReloadActions[tabName] = reloadAction;
        }

        /// Trong TabSelectionManager hoặc MainViewModel
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


        // Get the name of the last active tab
        public string GetLastActiveTab()
        {
            return _lastActiveTab;
        }
    }
}
