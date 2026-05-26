using System.Text.RegularExpressions;

namespace PetcareWebsite.Helpers;

public static partial class PhoneNumberHelper
{
    public static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && VietnamesePhoneRegex().IsMatch(value.Trim());
    }

    [GeneratedRegex(@"^[0-9]{9,15}$")]
    private static partial Regex VietnamesePhoneRegex();
}
