using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int StaffId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int AppointmentTypeId { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual AppointmentType AppointmentType { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Patient Patient { get; set; } = null!;
}
