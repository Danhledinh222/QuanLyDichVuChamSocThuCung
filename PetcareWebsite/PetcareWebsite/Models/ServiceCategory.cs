using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<ServiceCatalog> ServiceCatalogs { get; set; } = new List<ServiceCatalog>();
}
