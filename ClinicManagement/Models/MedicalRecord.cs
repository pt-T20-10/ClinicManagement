using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class MedicalRecord
{
    public int RecordId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public string? Diagnosis { get; set; }

    public string? Prescription { get; set; }

    public string? TestResults { get; set; }

    public string? DoctorAdvice { get; set; }

    public DateTime? RecordDate { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Patient Patient { get; set; } = null!;
}
