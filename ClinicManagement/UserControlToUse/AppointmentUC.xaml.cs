﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private void ConfirmTimeSelection_Click(object sender, RoutedEventArgs e)
        {
            // Close the popup
            TimePickerPopup.IsOpen = false;

            // Get the selected time from the clock
            DateTime? selectedTime = AppointmentClock.Time;

            // Pass it to the ViewModel
            if (DataContext is ViewModels.AppointmentViewModel viewModel && selectedTime.HasValue)
            {
                viewModel.SelectedAppointmentTime = selectedTime;
              
            }
        }



    }
}
