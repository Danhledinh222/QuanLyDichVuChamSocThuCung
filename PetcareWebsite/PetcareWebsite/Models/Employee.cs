using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int? AccountId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<BookingDetailEmployee> BookingDetailEmployees { get; set; } = new List<BookingDetailEmployee>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual Role Role { get; set; } = null!;
}
