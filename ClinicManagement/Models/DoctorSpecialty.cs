using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class DoctorSpecialty
{
    public int SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
