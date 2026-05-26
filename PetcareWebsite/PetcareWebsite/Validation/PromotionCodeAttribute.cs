using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PetcareWebsite.Validation;

public sealed partial class PromotionCodeAttribute : ValidationAttribute
{
    public PromotionCodeAttribute()
    {
        ErrorMessage = "Mã chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.";
    }

    public override bool IsValid(object? value)
    {
        return value == null || PromotionRegex().IsMatch(value.ToString() ?? string.Empty);
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$")]
    private static partial Regex PromotionRegex();
}
