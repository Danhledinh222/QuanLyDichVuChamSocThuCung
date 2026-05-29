using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class InvoiceStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
