using PetcareWebsite.Models;

namespace PetcareWebsite.Services;

public interface IInvoiceBusinessService
{
    decimal CalculateTotalAmount(decimal subtotal);

    decimal CalculateDiscountAmount(decimal totalAmount, Promotion? promotion, DateTime referenceDate);

    void ApplyPromotion(Invoice invoice, Promotion? promotion, DateTime referenceDate);
}
