namespace PetcareWebsite.Services;

public sealed record BusinessRuleResult(bool Succeeded, string? ErrorMessage = null)
{
    public static BusinessRuleResult Success() => new(true);

    public static BusinessRuleResult Failure(string message) => new(false, message);
}
