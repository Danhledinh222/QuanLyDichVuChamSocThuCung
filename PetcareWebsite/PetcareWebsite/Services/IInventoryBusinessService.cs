using PetcareWebsite.Models;

namespace PetcareWebsite.Services;

public interface IInventoryBusinessService
{
    Task<BusinessRuleResult> ValidateCompletionAsync(IReadOnlyCollection<BookingDetail> details);

    Task ImportSupplyAsync(MedicalSupply supply, int quantity, int? employeeId, string? note);
}