namespace PetcareWebsite.Helpers;

public static class ReferenceCodeHelper
{
    public static string Create(string prefix, DateTime? createdAt = null)
    {
        var time = createdAt ?? DateTime.Now;
        return prefix + time.ToString("yyyyMMddHHmmssfff");
    }
}
