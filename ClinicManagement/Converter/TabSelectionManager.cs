using System;
using System.Collections.Generic;

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

        // Handle tab selection change
        public void TabSelected(string tabName)
        {
            if (string.IsNullOrEmpty(tabName))
                return;

            // Store the last active tab
            _lastActiveTab = tabName;

            // Execute the reload action if registered
            if (_tabReloadActions.TryGetValue(tabName, out var reloadAction))
            {
                reloadAction();
            }
        }

        // Get the name of the last active tab
        public string GetLastActiveTab()
        {
            return _lastActiveTab;
        }
    }
}
