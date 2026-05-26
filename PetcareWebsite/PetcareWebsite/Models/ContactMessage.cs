using System;

namespace PetcareWebsite.Models;

public partial class ContactMessage
{
    public int ContactMessageId { get; set; }

    public int? CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? Topic { get; set; }

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? RepliedAt { get; set; }

    public string? AdminNote { get; set; }

    public virtual Customer? Customer { get; set; }
}
