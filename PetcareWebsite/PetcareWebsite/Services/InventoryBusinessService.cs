using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models;

namespace PetcareWebsite.Services;

public sealed class InventoryBusinessService : IInventoryBusinessService
{
    private readonly PetCareDbContext _context;

    public InventoryBusinessService(PetCareDbContext context)
    {
        _context = context;
    }

    public async Task<BusinessRuleResult> ValidateCompletionAsync(IReadOnlyCollection<BookingDetail> details)
    {
        var serviceIds = details
            .Select(detail => detail.ServiceId)
            .Distinct()
            .ToList();

        var quotas = await _context.ServiceMaterialQuota
            .Include(quota => quota.Supply)
            .Where(quota => serviceIds.Contains(quota.ServiceId))
            .ToListAsync();

        var requiredSupplies = details
            .SelectMany(detail => quotas
                .Where(quota => quota.ServiceId == detail.ServiceId)
                .Select(quota => new
                {
                    quota.Supply,
                    Quantity = quota.QuantityUsed ?? 1
                }))
            .GroupBy(item => item.Supply.SupplyId)
            .Select(group => new
            {
                Supply = group.First().Supply,
                RequiredQuantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var expiredSupply = requiredSupplies.FirstOrDefault(item =>
            item.Supply.ExpiryDate.HasValue &&
            item.Supply.ExpiryDate.Value < today);

        if (expiredSupply != null)
        {
            return BusinessRuleResult.Failure(
                $"Không thể hoàn thành lịch vì vật tư \"{expiredSupply.Supply.SupplyName}\" đã hết hạn sử dụng.");
        }

        var insufficientSupply = requiredSupplies.FirstOrDefault(item =>
            (item.Supply.StockQuantity ?? 0) < item.RequiredQuantity);

        if (insufficientSupply != null)
        {
            return BusinessRuleResult.Failure(
                $"Không đủ tồn kho \"{insufficientSupply.Supply.SupplyName}\": cần {insufficientSupply.RequiredQuantity:N0}, hiện còn {(insufficientSupply.Supply.StockQuantity ?? 0):N0}.");
        }

        return BusinessRuleResult.Success();
    }

    public async Task ImportSupplyAsync(MedicalSupply supply, int quantity, int? employeeId, string? note)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng nhập phải lớn hơn 0.");
        }

        var now = DateTime.Now;

        supply.StockQuantity = (supply.StockQuantity ?? 0) + quantity;
        supply.ModifiedAt = now;

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            SupplyId = supply.SupplyId,
            TransactionType = InventoryTransactionKind.Import.ToDatabaseValue(),
            QuantityChange = quantity,
            EmployeeId = employeeId,
            Note = TextInputHelper.NullIfWhiteSpace(note) ?? "Nhập hàng mới vào kho",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
    }
}