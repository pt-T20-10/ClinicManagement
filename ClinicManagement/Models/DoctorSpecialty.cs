using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class DoctorSpecialty
{
    public int SpecialtyId { get; set; }

    public string SpecialtyName { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Staff> Staffs { get; set; } = new List<Staff>();
}
