namespace PetcareWebsite.Enums;

public enum SystemRoleCode
{
    Admin = 1,
    Veterinarian = 2,
    Groomer = 3,
    Customer = 4
}

public enum BookingStatusCode
{
    Pending = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5,
    InProgress = 6
}

public enum DetailStatusCode
{
    NotStarted = 1,
    InProgress = 2,
    Done = 3,
    Cancelled = 4
}

public enum InvoiceStatusCode
{
    Unpaid = 1,
    Partial = 2,
    Paid = 3
}

public enum PaymentMethodCode
{
    Cash = 1,
    Transfer = 2,
    Card = 3
}

public enum PetSpeciesCode
{
    Dog = 1,
    Cat = 2
}

public enum InventoryTransactionKind
{
    Import,
    ExportService,
    ReturnService
}
