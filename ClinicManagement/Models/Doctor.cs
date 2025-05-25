using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string FullName { get; set; } = null!;

    public int? SpecialtyId { get; set; }

    public string? CertificateLink { get; set; }

    public string? Schedule { get; set; }

    public string? Phone { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

    public virtual DoctorSpecialty? Specialty { get; set; }
}
