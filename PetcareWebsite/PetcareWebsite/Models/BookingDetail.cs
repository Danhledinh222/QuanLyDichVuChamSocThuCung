using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class BookingDetail
{
    public int BookingDetailId { get; set; }

    public int BookingId { get; set; }

    public int PetId { get; set; }

    public int ServiceId { get; set; }

    public decimal ActualPrice { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int StatusId { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<BookingDetailEmployee> BookingDetailEmployees { get; set; } = new List<BookingDetailEmployee>();

    public virtual Pet Pet { get; set; } = null!;

    public virtual ServiceCatalog Service { get; set; } = null!;

    public virtual ServiceReview? ServiceReview { get; set; }

    public virtual DetailStatus Status { get; set; } = null!;
}
