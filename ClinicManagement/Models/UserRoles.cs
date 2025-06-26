using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Quản lí";
        public const string Doctor = "Bác sĩ";
        public const string Pharmacist = "Dược sĩ";
        public const string Cashier = "Thu ngân";
        // Map roles to their tab access permissions
        // Kiểm tra file UserRoles.cs
        public static readonly Dictionary<string, List<string>> RoleTabPermissions = new Dictionary<string, List<string>>
{
    // Admin và Manager có quyền truy cập tất cả
    {Admin, new List<string>{
        "PatientTab", "ExamineTab", "AppointmentTab",
        "StockTab", "InvoiceTab", "MedicineSellTab", "DoctorTab",
        "StatisticsTab", "SettingsTab"
    }},
    {Manager, new List<string>{
        "PatientTab", "AppointmentTab",
        "StockTab", "InvoiceTab", "DoctorTab",
        "StatisticsTab", "SettingsTab"
    }},
    
    // Bác sĩ có quyền truy cập các tab này
    {Doctor, new List<string>{"PatientTab", "ExamineTab", "AppointmentTab", "SettingsTab"}},
    
    // Dược sĩ có quyền truy cập các tab này
    {Pharmacist, new List<string>{"StockTab", "MedicineSellTab", "InvoiceTab", "SettingsTab"}},

            // Cashiers have access to these tabs
    {Cashier, new List<string>{"PatientTab", "AppointmentTab", "InvoiceTab", "DoctorTab", "Settings"} }
};

    }

}
