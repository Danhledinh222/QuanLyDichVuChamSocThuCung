using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class BookingStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
