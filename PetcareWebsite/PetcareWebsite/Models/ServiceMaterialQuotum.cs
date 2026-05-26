using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class ServiceMaterialQuotum
{
    public int ServiceId { get; set; }

    public int SupplyId { get; set; }

    public int? QuantityUsed { get; set; }

    public virtual ServiceCatalog Service { get; set; } = null!;

    public virtual MedicalSupply Supply { get; set; } = null!;
}
