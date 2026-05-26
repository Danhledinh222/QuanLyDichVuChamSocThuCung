using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class VwInvoiceDetail
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public string BookingCode { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime? InvoiceDate { get; set; }
}
