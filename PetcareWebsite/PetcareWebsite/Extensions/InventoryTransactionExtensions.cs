using PetcareWebsite.Enums;

namespace PetcareWebsite.Extensions;

public static class InventoryTransactionExtensions
{
    public static string ToDatabaseValue(this InventoryTransactionKind kind)
    {
        return kind switch
        {
            InventoryTransactionKind.Import => "IMPORT",
            InventoryTransactionKind.ExportService => "EXPORT_SERVICE",
            InventoryTransactionKind.ReturnService => "RETURN_SERVICE",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}