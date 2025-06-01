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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClinicManagement.UserControlToUse
{
    /// <summary>
    /// Interaction logic for AppointmentUC.xaml
    /// </summary>
    public partial class AppointmentUC : UserControl
    {
        public AppointmentUC()
        {
            InitializeComponent();
        }

        private void TimePickerButton_Click(object sender, RoutedEventArgs e)
        {
            TimePickerPopup.IsOpen = true;
        }

        private void ConfirmTimeSelection_Click(object sender, RoutedEventArgs e)
        {
            TimePickerPopup.IsOpen = false;
            // Time đã được binding tự động với ViewModel
        }

        private void CancelTimeSelection_Click(object sender, RoutedEventArgs e)
        {
            TimePickerPopup.IsOpen = false;
            // Có thể reset về giá trị cũ nếu cần
        }
    }
}
