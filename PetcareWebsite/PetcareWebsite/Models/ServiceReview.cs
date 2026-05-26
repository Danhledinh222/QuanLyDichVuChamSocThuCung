using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class ServiceReview
{
    public int ReviewId { get; set; }

    public int BookingDetailId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }

    public string? ReviewTags { get; set; }

    public string? StoreReply { get; set; }

    public bool? IsVisible { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
