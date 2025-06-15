using ClinicManagement.ViewModels;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClinicManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Đăng ký sự kiện Closing
        
            this.Closing -= MainWindow_Closing;

            this.Closing += MainWindow_Closing;
        }

        // Xử lý khi cửa sổ đang đóng
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            var mainViewModel = this.DataContext as MainViewModel;

        }
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            {
                string tabName = selectedTab.Name;

                // Execute the command in MainViewModel
                if (DataContext is MainViewModel mainVM && mainVM.TabSelectedCommand != null)
                {
                    mainVM.TabSelectedCommand.Execute(tabName);
                }
            }
        }
    }
}