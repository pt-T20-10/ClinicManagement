using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClinicManagement
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Đăng ký sự kiện KeyDown
            this.KeyDown += LoginWindow_KeyDown;
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Xử lý khi phím Esc được nhấn
            if (e.Key == Key.Escape)
            {
                // Lấy ViewModel từ DataContext
                var viewModel = DataContext as LoginViewModel;

                // Gọi lệnh đóng của ViewModel
                if (viewModel != null)
                {
                    viewModel.CloseCommand.Execute(this);
                }
                else
                {
                    // Trường hợp không tìm thấy ViewModel, đóng cửa sổ trực tiếp
                    this.Close();
                }

                // Đánh dấu sự kiện đã được xử lý
                e.Handled = true;
            }
        }
    }

}
