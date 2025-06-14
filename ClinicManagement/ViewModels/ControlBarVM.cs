using ClinicManagement;
using ClinicManagement.UserControlToUse;
using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StorageManagement.ViewModels  
{
  

    public class ControlBarVM : BaseViewModel
    {
        #region commands
        public ICommand  ClosingWindowCommand {  get; set; }
        public ICommand MaximizingWindowCommand { get; set; }
        public ICommand MinimizingWindowCommand { get; set; }
        public ICommand MouseDragMoveCommand { get; set; }
        #endregion
        public ControlBarVM()
        {

              ClosingWindowCommand = new RelayCommand<ControlBarUC>(
            (controlBar) =>
            {
                var window = Window.GetWindow(controlBar);
                if (window == null) return;

                // Nếu là MainWindow, gọi Close() để kích hoạt sự kiện Closing
                if (window is MainWindow)
                {
                    MessageBox.Show("Bạn có chắc chắn muốn đăng xuất và thoát chương trình?", "Xác nhận thoát", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    window.Close();
                }
                // Nếu là cửa sổ khác, đóng trực tiếp
                else
                {
                    window.Close();
                }
            },
            (controlBar) => controlBar != null
        );
            MaximizingWindowCommand = new RelayCommand<ControlBarUC>(

               (controlBar) =>
               {
                   var window = Window.GetWindow(controlBar);
                   if (window.WindowState != WindowState.Maximized)
                       window.WindowState = WindowState.Maximized;
                   else
                       window.WindowState = WindowState.Normal;
                  
               },
            (controlBar) => controlBar != null  // Chỉ thực thi khi controlBar tồn tại
        );
            MinimizingWindowCommand = new RelayCommand<ControlBarUC>(

               (controlBar) =>
               {
                   var window = Window.GetWindow(controlBar);
                   if (window.WindowState != WindowState.Minimized)
                       window.WindowState = WindowState.Minimized;
                   else
                       window.WindowState = WindowState.Maximized;

               },
            (controlBar) => controlBar != null  // Chỉ thực thi khi controlBar tồn tại
        );
            MouseDragMoveCommand = new RelayCommand<ControlBarUC>(

               (controlBar) =>
               {
                   var window = Window.GetWindow(controlBar);
                   window.DragMove();

               },
            (controlBar) => controlBar != null  // Chỉ thực thi khi controlBar tồn tại
        );
        }
    }
}
