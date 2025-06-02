using ClinicManagement.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class AppointmentDetailsViewModel : BaseViewModel
    {
        private Window _window;
        private AppointmentDisplayInfo _appointment;
        public AppointmentDisplayInfo Appointment
        {
            get => _appointment;
            set
            {
                _appointment = value;
                OnPropertyChanged();
                LoadRelatedData();

            }
        }


        private DateTime? _selectedAppointmentTime;
        public DateTime? SelectedAppointmentTime
        {
            get => _selectedAppointmentTime;
            set
            {
                _selectedAppointmentTime = value;
                OnPropertyChanged();
            }
        }

        public ICommand CancelAppointmentCommand { get; set; }
        public ICommand EditAppointmentCommand { get; set; }
        public ICommand AcceptAppointmentCommand { get; set; }

        public ICommand ConfirmTimeSelectionCommand { get; set; }
        public ICommand CancelTimeSelectionCommand { get; set; }




        public AppointmentDetailsViewModel()
        {

        }


        private void LoadRelatedData()
        {
            if (Appointment?.OriginalAppointment != null)
            {
             
            }
        }

        private void InitCommands()
        {
              AcceptAppointmentCommand = new RelayCommand<object>(
                (p) => AcceptAppointment(),
                (p) => true
            );
              EditAppointmentCommand = new RelayCommand<object>(
                (p) => EditAppointment(),
                (p) => true
            );
             CancelAppointmentCommand = new RelayCommand<object>(
                (p) => CancelAppointment(),
                (p) => true
            );

            CancelTimeSelectionCommand = new RelayCommand<object>(
                (p) => { },
                (p) => true
            );

        }

        private void AcceptAppointment() { }

        private void EditAppointment() { }

        private void CancelAppointment() { }










    }
}
