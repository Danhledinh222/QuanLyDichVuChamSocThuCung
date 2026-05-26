using PetcareWebsite.Models;

namespace PetcareWebsite.Services;

public sealed class InvoiceBusinessService : IInvoiceBusinessService
{
    private const decimal VatRate = 0.10m;

    public decimal CalculateTotalAmount(decimal subtotal)
    {
        return decimal.Round(subtotal * (1 + VatRate), 2, MidpointRounding.AwayFromZero);
    }

    public decimal CalculateDiscountAmount(decimal totalAmount, Promotion? promotion, DateTime referenceDate)
    {
        if (promotion == null ||
            promotion.IsActive != true ||
            referenceDate < promotion.StartDate ||
            referenceDate > promotion.EndDate ||
            totalAmount < (promotion.MinOrderValue ?? 0))
        {
            return 0;
        }

        var discount = string.Equals(promotion.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase)
            ? promotion.DiscountValue
            : totalAmount * promotion.DiscountValue / 100m;

        if (!string.Equals(promotion.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase) &&
            promotion.MaxDiscount is > 0 &&
            discount > promotion.MaxDiscount.Value)
        {
            discount = promotion.MaxDiscount.Value;
        }

        return decimal.Round(Math.Clamp(discount, 0, totalAmount), 2, MidpointRounding.AwayFromZero);
    }

    public void ApplyPromotion(Invoice invoice, Promotion? promotion, DateTime referenceDate)
    {
        invoice.PromotionId = promotion?.PromotionId;
        invoice.DiscountAmount = CalculateDiscountAmount(invoice.TotalAmount ?? 0, promotion, referenceDate);
        invoice.ModifiedAt = DateTime.Now;
    }
}
