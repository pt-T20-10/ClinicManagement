using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int AppointmentId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public string? Method { get; set; }

    public string? Status { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
