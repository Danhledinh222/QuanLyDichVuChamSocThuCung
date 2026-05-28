namespace PetcareWebsite.Helpers;

public static class TextInputHelper
{
    public static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}