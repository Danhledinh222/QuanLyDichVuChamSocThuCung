using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromoCode { get; set; } = null!;

    public string? DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public decimal? MinOrderValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
