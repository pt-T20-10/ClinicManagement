using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

// Staff model
public class Staff
{
    public int StaffId { get; set; }
    public string FullName { get; set; }
    public int? SpecialtyId { get; set; }
    public int? RoleId { get; set; }
    public string? CertificateLink { get; set; }
    public string? Schedule { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool? IsDeleted { get; set; }

    // Navigation properties
    public DoctorSpecialty Specialty { get; set; }
    public Role Role { get; set; }
    public ICollection<Account> Accounts { get; set; }
    public ICollection<Appointment> Appointments { get; set; }
    public ICollection<MedicalRecord> MedicalRecords { get; set; }
}
