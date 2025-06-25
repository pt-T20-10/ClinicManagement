using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Account
{
    public string Username { get; set; } = null!;

    public int? StaffId { get; set; }

    public string Password { get; set; } = null!;

    public string? Role { get; set; }

    public bool? IsLogined { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Staff? Staff { get; set; }
}
