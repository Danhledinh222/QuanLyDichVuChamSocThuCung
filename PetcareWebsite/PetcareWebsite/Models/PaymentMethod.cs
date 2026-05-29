using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class PaymentMethod
{
    public int MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
