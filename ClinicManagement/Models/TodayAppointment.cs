using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{
   public class TodayAppointment
    {
        public Appointment Appointment { get; set; }

        public string? Initials { get; set; }    
        

        public string? PatientName { get; set; }
     
        public string? Notes { get; set; }
          
        public string? DoctorName { get; set; }
        
        public string? Status { get; set; }

        public TimeSpan? Time { get; set; }  
       

    }
}
