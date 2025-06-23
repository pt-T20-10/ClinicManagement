using System.Windows;

namespace ClinicManagement.Services
{
    public static class MessageBoxService
    {
        public static bool ShowMessage(string message, string caption = "", SubWindow.MessageType messageType = SubWindow.MessageType.Information, bool showCancelButton = false)
        {
            return SubWindow.CustomMessageBox.ShowDialog(message, caption, messageType, showCancelButton);
        }

        public static bool ShowInfo(string message, string caption = "Thông báo")
        {
            return ShowMessage(message, caption, SubWindow.MessageType.Information);
        }

        public static bool ShowSuccess(string message, string caption = "Thành công")
        {
            return ShowMessage(message, caption, SubWindow.MessageType.Success);
        }

        public static bool ShowWarning(string message, string caption = "Cảnh báo")
        {
            return ShowMessage(message, caption, SubWindow.MessageType.Warning);
        }

        public static bool ShowError(string message, string caption = "Lỗi")
        {
            return ShowMessage(message, caption, SubWindow.MessageType.Error);
        }

        public static bool ShowQuestion(string message, string caption = "Xác nhận", bool showCancelButton = true)
        {
            return ShowMessage(message, caption, SubWindow.MessageType.Question, showCancelButton);
        }
    }
}
