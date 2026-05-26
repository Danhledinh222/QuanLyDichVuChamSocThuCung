using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class InventoryTransaction
{
    public int TransactionId { get; set; }

    public int SupplyId { get; set; }

    public string TransactionType { get; set; } = null!;

    public int QuantityChange { get; set; }

    public int? ReferenceId { get; set; }

    public string? Note { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual MedicalSupply Supply { get; set; } = null!;
}
