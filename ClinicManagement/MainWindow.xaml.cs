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

            // Nếu có người dùng đang đăng nhập, hiển thị hộp thoại xác nhận
            if (mainViewModel != null && mainViewModel.CurrentAccount != null)
            {
                
            }
        }
    }
}