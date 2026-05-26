using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class BookingDetailEmployee
{
    public int BookingDetailId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
