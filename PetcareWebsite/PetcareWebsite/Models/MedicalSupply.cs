using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class MedicalSupply
{
    public int SupplyId { get; set; }

    public string SupplyName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int? StockQuantity { get; set; }

    public int? MinStockLevel { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<ServiceMaterialQuotum> ServiceMaterialQuota { get; set; } = new List<ServiceMaterialQuotum>();
}
