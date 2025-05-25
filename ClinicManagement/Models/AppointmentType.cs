using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class AppointmentType
{
    public int AppointmentTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
