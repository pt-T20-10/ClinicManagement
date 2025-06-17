// ProgressDialog.xaml.cs
using System.Windows;

namespace ClinicManagement.SubWindow
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int value)
        {
            // Update from UI thread
            Dispatcher.Invoke(() =>
            {
                ProgressValue.Value = value;
                ProgressText.Text = $"{value}%";
            });
        }
    }
}