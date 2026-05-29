using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class DetailStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
