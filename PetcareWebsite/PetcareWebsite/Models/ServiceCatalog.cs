using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class ServiceCatalog
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public int? EstimatedDuration { get; set; }

    public int MaxCapacity { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<ServiceMaterialQuotum> ServiceMaterialQuota { get; set; } = new List<ServiceMaterialQuotum>();
}
