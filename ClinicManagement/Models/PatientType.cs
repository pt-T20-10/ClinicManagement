using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class PatientType
{
    public int PatientTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public decimal? Discount { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
