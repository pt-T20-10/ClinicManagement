using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Azure.Core.HttpHeader;

namespace ClinicManagement.ViewModels
{
     public class DashBoardViewModel:BaseViewModel
    {
        private string _TotalPatients;
        public string TotalPatients
        {
            get => _TotalPatients;
            set
            {
                _TotalPatients = value; OnPropertyChanged();
            }
        }
        private string _TotalAppointments;
        public string TotalAppointments
        {
            get => _TotalAppointments;
            set
            {
                _TotalAppointments = value; OnPropertyChanged();
            }
        }

        private string _PendingAppointments;
        public string PendingAppointments
        {
            get => _PendingAppointments;
            set
            {
                _PendingAppointments = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<TodayAppointment> _TodayAppointments;
        public ObservableCollection<TodayAppointment> TodayAppointments
        {
            get => _TodayAppointments;
            set
            {
                _TodayAppointments = value; OnPropertyChanged();
            }
        }
        private DateTime _CurrentDate;
        public DateTime CurrentDate 
        { 
            get => _CurrentDate;
            set
            {
                _CurrentDate = value; OnPropertyChanged();
            }
        }



        public DashBoardViewModel()
        {
            LoadDashBoard();
        }
        private void LoadDashBoard() 
        {
            CurrentDate = DateTime.Now.Date;
            TodayAppointments = new ObservableCollection<TodayAppointment>();

            var appointments = DataProvider.Instance.Context.Appointments
              .Include(a => a.Patient)
              .Include(a => a.Doctor)
              .ToList();
            int waitingCount = appointments.Count(a => a.Status == "Đang chờ");
            foreach (var appointment in appointments)
            {
                TodayAppointment app = new TodayAppointment
                {
                    Appointment = appointment,
                    Initials = GetInitialsFromFullName(appointment.Patient?.FullName),
                    PatientName = appointment.Patient?.FullName,
                    DoctorName = appointment.Doctor?.FullName,
                    Notes = appointment.Notes,
                    Status = appointment.Status,
                    Time = appointment.AppointmentDate.TimeOfDay
                };
                TodayAppointments.Add(app);

                TotalAppointments = appointments.Count().ToString();

            }
            var count = DataProvider.Instance.Context.Patients.Count();
            PendingAppointments = waitingCount.ToString();
            TotalPatients = count.ToString();
        }
        private void LoadTotalPatients()
        {
            var today = DateTime.Today;

            // So sánh phần ngày của CreatedAt với ngày hiện tại
            var count = DataProvider.Instance.Context.Patients
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value.Date == today)
                .Count();

            TotalPatients = count.ToString();
        }


        private string GetInitialsFromFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length >= 2)
            {
                var middle = parts[parts.Length - 2];
                var last = parts[parts.Length - 1];
                return $"{char.ToUpper(middle[0])}.{char.ToUpper(last[0])}";
            }
            else if (parts.Length == 1)
            {
                return char.ToUpper(parts[0][0]).ToString();
            }

            return string.Empty;
        }
    }
    }

