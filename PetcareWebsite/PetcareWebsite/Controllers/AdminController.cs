using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Data;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Models;
using PetcareWebsite.ViewModels;

namespace PetcareWebsite.Controllers;

public class AdminController : Controller
{
    private const int BookingStatusPending = (int)BookingStatusCode.Pending;
    private const int BookingStatusConfirmed = (int)BookingStatusCode.Confirmed;
    private const int BookingStatusInProgress = (int)BookingStatusCode.InProgress;

    private readonly DemoStore store;
    private readonly PetCareDbContext _context;

    public AdminController(DemoStore store, PetCareDbContext context)
    {
        this.store = store;
        _context = context;
    }

    public IActionResult Index()
    {
        var model = new AdminDashboardViewModel
        {
            AdminName = "Lê Đình Danh",
            TodayBookingCount = store.Bookings.Count(booking => booking.BookingDate.Date == DateTime.Today),
            PendingBookingCount = store.Bookings.Count(booking => booking.StatusId == 1),
            CustomerCount = store.Customers.Count,
            PetCount = store.Pets.Count,
            MonthlyRevenue = store.Invoices.Sum(invoice => invoice.PaidAmount ?? 0),
            NewContactCount = store.ContactMessages.Count(message => message.Status == "New"),
            LowStockCount = store.Supplies.Count(supply => (supply.StockQuantity ?? 0) <= (supply.MinStockLevel ?? 0)),
            WeekLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"],
            WeeklyBookingCounts = [2, 3, 1, 4, 2, 5, 3],
            WeeklyRevenue = [350000, 550000, 150000, 850000, 400000, 1100000, 650000],
            RecentContacts = store.ContactMessages.Take(3).ToList(),
            LowStockSupplies = store.Supplies.Where(supply => (supply.StockQuantity ?? 0) <= (supply.MinStockLevel ?? 0)).ToList(),
            RecentBookings = store.Bookings.OrderByDescending(booking => booking.BookingDate).Take(4).Select(booking => new AdminBookingSummaryViewModel
            {
                BookingCode = booking.BookingCode,
                CustomerName = booking.Customer.FullName,
                PetName = booking.BookingDetails.First().Pet.Name,
                ServiceName = booking.BookingDetails.First().Service.ServiceName,
                BookingDate = booking.BookingDate,
                StatusId = booking.StatusId,
                TotalAmount = (booking.Invoice?.TotalAmount ?? 0) - (booking.Invoice?.DiscountAmount ?? 0)
            }).ToList()
        };
        return View(model);
    }

    public IActionResult Bookings(string? search, int? statusId)
    {
        var all = store.Bookings.OrderByDescending(booking => booking.BookingDate).ToList();
        var filtered = all.Where(booking =>
                (string.IsNullOrWhiteSpace(search) ||
                 booking.BookingCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 booking.Customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 booking.Customer.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (!statusId.HasValue || booking.StatusId == statusId))
            .ToList();
        return View(new AdminBookingsViewModel
        {
            Search = search,
            StatusId = statusId,
            TotalCount = all.Count,
            PendingCount = all.Count(booking => booking.StatusId == 1),
            CompletedCount = all.Count(booking => booking.StatusId == 3),
            CancelledCount = all.Count(booking => booking.StatusId == 4),
            ExpiredCount = all.Count(booking => booking.StatusId == 5),
            InProgressCount = all.Count(booking => booking.StatusId == 6),
            Bookings = filtered,
            Employees = store.Employees.Where(employee => employee.IsActive == true).ToList()
        });
    }

    public IActionResult Invoices(string? search, int? statusId)
    {
        var all = store.Invoices.OrderByDescending(invoice => invoice.CreatedAt).ToList();
        var filtered = all.Where(invoice =>
                (string.IsNullOrWhiteSpace(search) ||
                 invoice.InvoiceCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 invoice.Booking.BookingCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                 invoice.Booking.Customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (!statusId.HasValue || invoice.StatusId == statusId))
            .ToList();
        return View(new AdminInvoicesViewModel
        {
            Search = search,
            StatusId = statusId,
            TotalCount = all.Count,
            UnpaidCount = all.Count(invoice => invoice.StatusId == 1),
            PartialCount = all.Count(invoice => invoice.StatusId == 2),
            PaidCount = all.Count(invoice => invoice.StatusId == 3),
            OutstandingAmount = all.Where(invoice => invoice.Booking.StatusId is not 4 and not 5).Sum(invoice => (invoice.TotalAmount ?? 0) - (invoice.DiscountAmount ?? 0) - (invoice.PaidAmount ?? 0)),
            Invoices = filtered,
            PaymentMethods = store.PaymentMethods,
            Promotions = store.Promotions.Where(promotion => promotion.IsActive == true).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Services(string? search, int? categoryId, string? status)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var allServices = _context.ServiceCatalogs
            .Where(service => service.IsDeleted != true);
        var query = _context.ServiceCatalogs
            .Include(service => service.Category)
            .Include(service => service.BookingDetails)
            .Where(service => service.IsDeleted != true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(service =>
                service.ServiceName.Contains(keyword) ||
                (service.Description != null && service.Description.Contains(keyword)) ||
                service.Category.CategoryName.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(service => service.CategoryId == categoryId.Value);
        }

        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(service => service.IsActive == true);
        }
        else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(service => service.IsActive != true);
        }

        return View(new AdminServicesViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Status = status,
            TotalCount = await allServices.CountAsync(),
            ActiveCount = await allServices.CountAsync(service => service.IsActive == true),
            InactiveCount = await allServices.CountAsync(service => service.IsActive != true),
            Services = await query
                .OrderBy(service => service.Category.CategoryName)
                .ThenBy(service => service.ServiceName)
                .ToListAsync(),
            Categories = await _context.ServiceCategories
                .OrderBy(category => category.CategoryName)
                .ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateService()
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var model = new AdminServiceEditorViewModel
        {
            BasePrice = 100000,
            EstimatedDuration = 60,
            MaxCapacity = 1,
            IsActive = true
        };

        await LoadServiceEditorListsAsync(model);
        model.CategoryId = model.Categories.FirstOrDefault()?.CategoryId ?? 0;
        return View("ServiceForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var service = await _context.ServiceCatalogs
            .Include(item => item.BookingDetails)
            .FirstOrDefaultAsync(item => item.ServiceId == id && item.IsDeleted != true);

        if (service == null)
        {
            TempData["AdminError"] = "Không tìm thấy dịch vụ cần sửa.";
            return RedirectToAction(nameof(Services));
        }

        var model = new AdminServiceEditorViewModel
        {
            ServiceId = service.ServiceId,
            CategoryId = service.CategoryId,
            ServiceName = service.ServiceName,
            Description = service.Description,
            BasePrice = service.BasePrice,
            EstimatedDuration = service.EstimatedDuration,
            MaxCapacity = service.MaxCapacity,
            IsActive = service.IsActive == true,
            BookingCount = service.BookingDetails.Count
        };

        await LoadServiceEditorListsAsync(model);
        return View("ServiceForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveService(AdminServiceEditorViewModel model)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        model.ServiceName = model.ServiceName?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        if (!await _context.ServiceCategories.AnyAsync(category => category.CategoryId == model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Danh mục dịch vụ không tồn tại.");
        }

        var serviceId = model.ServiceId ?? 0;
        if (!string.IsNullOrWhiteSpace(model.ServiceName) &&
            await _context.ServiceCatalogs.AnyAsync(service =>
                service.ServiceId != serviceId &&
                service.IsDeleted != true &&
                service.CategoryId == model.CategoryId &&
                service.ServiceName == model.ServiceName))
        {
            ModelState.AddModelError(nameof(model.ServiceName), "Danh mục này đã có dịch vụ cùng tên.");
        }

        ServiceCatalog? service = null;
        if (model.ServiceId.HasValue)
        {
            service = await _context.ServiceCatalogs
                .Include(item => item.BookingDetails)
                .FirstOrDefaultAsync(item => item.ServiceId == model.ServiceId && item.IsDeleted != true);

            if (service == null)
            {
                TempData["AdminError"] = "Không tìm thấy dịch vụ cần sửa.";
                return RedirectToAction(nameof(Services));
            }

            model.BookingCount = service.BookingDetails.Count;
        }

        if (!ModelState.IsValid)
        {
            await LoadServiceEditorListsAsync(model);
            return View("ServiceForm", model);
        }

        if (service == null)
        {
            service = new ServiceCatalog
            {
                CategoryId = model.CategoryId,
                ServiceName = model.ServiceName,
                Description = model.Description,
                BasePrice = model.BasePrice,
                EstimatedDuration = model.EstimatedDuration,
                MaxCapacity = model.MaxCapacity,
                IsActive = model.IsActive,
                IsDeleted = false
            };
            _context.ServiceCatalogs.Add(service);
        }
        else
        {
            service.CategoryId = model.CategoryId;
            service.ServiceName = model.ServiceName;
            service.Description = model.Description;
            service.BasePrice = model.BasePrice;
            service.EstimatedDuration = model.EstimatedDuration;
            service.MaxCapacity = model.MaxCapacity;
            service.IsActive = model.IsActive;
        }

        await _context.SaveChangesAsync();
        TempData["AdminSuccess"] = model.IsEditing
            ? "Đã cập nhật dịch vụ. Giá mới chỉ áp dụng cho lịch hẹn được tạo hoặc cập nhật sau thời điểm này."
            : "Đã thêm dịch vụ mới vào danh mục.";
        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleServiceStatus(int serviceId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var service = await _context.ServiceCatalogs
            .FirstOrDefaultAsync(item => item.ServiceId == serviceId && item.IsDeleted != true);

        if (service == null)
        {
            TempData["AdminError"] = "Không tìm thấy dịch vụ cần cập nhật.";
            return RedirectToAction(nameof(Services));
        }

        var wasActive = service.IsActive == true;
        service.IsActive = !wasActive;
        await _context.SaveChangesAsync();

        if (wasActive)
        {
            var futureBookingCount = await _context.BookingDetails.CountAsync(detail =>
                detail.ServiceId == serviceId &&
                detail.Booking.IsDeleted != true &&
                ((detail.Booking.BookingDate >= DateTime.Now &&
                  (detail.Booking.StatusId == BookingStatusPending ||
                   detail.Booking.StatusId == BookingStatusConfirmed)) ||
                 detail.Booking.StatusId == BookingStatusInProgress));

            TempData["AdminSuccess"] = futureBookingCount > 0
                ? $"Đã ngừng nhận đặt mới cho dịch vụ. Còn {futureBookingCount} lịch sắp tới cần tiếp tục xử lý."
                : "Đã ngừng nhận đặt mới cho dịch vụ.";
        }
        else
        {
            TempData["AdminSuccess"] = "Đã mở lại dịch vụ cho khách hàng đặt lịch.";
        }

        return RedirectToAction(nameof(Services));
    }

    public IActionResult Inventory(string? search, string? status)
    {
        var all = store.Supplies;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var filtered = all.Where(supply =>
                (string.IsNullOrWhiteSpace(search) || supply.SupplyName.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(status) ||
                 status == "LowStock" && (supply.StockQuantity ?? 0) <= (supply.MinStockLevel ?? 0) ||
                 status == "Expired" && supply.ExpiryDate < today ||
                 status == "Expiring" && supply.ExpiryDate >= today && supply.ExpiryDate <= today.AddDays(30)))
            .ToList();
        return View(new AdminInventoryViewModel
        {
            Search = search,
            Status = status,
            TotalCount = all.Count,
            LowStockCount = all.Count(supply => (supply.StockQuantity ?? 0) <= (supply.MinStockLevel ?? 0)),
            ExpiredCount = all.Count(supply => supply.ExpiryDate < today),
            ExpiringCount = all.Count(supply => supply.ExpiryDate >= today && supply.ExpiryDate <= today.AddDays(30)),
            Supplies = filtered,
            RecentTransactions = store.InventoryTransactions.OrderByDescending(transaction => transaction.CreatedAt).ToList()
        });
    }

    public IActionResult CreateSupply() => View("SupplyForm", new AdminSupplyEditorViewModel());

    public IActionResult EditSupply(int id)
    {
        var supply = store.Supplies.First(item => item.SupplyId == id);
        return View("SupplyForm", new AdminSupplyEditorViewModel { SupplyId = supply.SupplyId, SupplyName = supply.SupplyName, Unit = supply.Unit, MinStockLevel = supply.MinStockLevel ?? 0, ExpiryDate = supply.ExpiryDate, StockQuantity = supply.StockQuantity ?? 0, TransactionCount = supply.InventoryTransactions.Count });
    }

    [HttpPost]
    public IActionResult SaveSupply() => DemoRedirect(nameof(Inventory), "Vật tư đã được lưu trong bản trình diễn.");

    [HttpGet]
    public IActionResult ImportSupply(int id)
    {
        var supply = store.Supplies.First(item => item.SupplyId == id);
        return View(new AdminSupplyImportViewModel { SupplyId = id, SupplyName = supply.SupplyName, Unit = supply.Unit, CurrentStock = supply.StockQuantity ?? 0, Quantity = 10 });
    }

    [HttpPost]
    public IActionResult ImportSupply(AdminSupplyImportViewModel model) => DemoRedirect(nameof(Inventory), "Phiếu nhập kho đã được mô phỏng.");

    public IActionResult InventoryQuotas() => View(new AdminInventoryQuotasViewModel { Quotas = store.MaterialQuotas, Services = store.Services, Supplies = store.Supplies, QuantityUsed = 1 });

    [HttpPost]
    public IActionResult SaveInventoryQuota() => DemoRedirect(nameof(InventoryQuotas), "Định mức vật tư đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult DeleteInventoryQuota() => DemoRedirect(nameof(InventoryQuotas), "Định mức vật tư đã được xóa trong bản trình diễn.");

    public IActionResult Employees(string? search, int? roleId, string? status)
    {
        var all = store.Employees;
        var filtered = all.Where(employee =>
                (string.IsNullOrWhiteSpace(search) || employee.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || employee.PhoneNumber.Contains(search)) &&
                (!roleId.HasValue || employee.RoleId == roleId) &&
                (string.IsNullOrWhiteSpace(status) || (status == "Active" ? employee.IsActive == true : employee.IsActive != true)))
            .ToList();
        return View(new AdminEmployeesViewModel { Search = search, RoleId = roleId, Status = status, TotalCount = all.Count, ActiveCount = all.Count(employee => employee.IsActive == true), InactiveCount = all.Count(employee => employee.IsActive != true), AssignedUpcomingCount = 2, Employees = filtered, Roles = store.Roles.Where(role => role.RoleId != 1).ToList() });
    }

    public IActionResult CreateEmployee() => View("EmployeeForm", EmployeeEditor(null));

    public IActionResult EditEmployee(int id) => View("EmployeeForm", EmployeeEditor(store.Employees.FirstOrDefault(employee => employee.EmployeeId == id)));

    [HttpPost]
    public IActionResult SaveEmployee() => DemoRedirect(nameof(Employees), "Hồ sơ nhân viên đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult ToggleEmployeeStatus() => DemoRedirect(nameof(Employees), "Trạng thái nhân viên đã được mô phỏng.");

    [HttpGet]
    public async Task<IActionResult> Promotions(string? search, string? status)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var now = DateTime.Now;
        var allPromotions = _context.Promotions.AsQueryable();
        var query = _context.Promotions
            .Include(promotion => promotion.Invoices)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(promotion =>
                promotion.PromoCode.Contains(keyword) ||
                (promotion.DiscountType != null && promotion.DiscountType.Contains(keyword)));
        }

        if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(promotion =>
                promotion.IsActive == true &&
                promotion.StartDate <= now &&
                promotion.EndDate >= now);
        }
        else if (string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(promotion => promotion.IsActive == true && promotion.StartDate > now);
        }
        else if (string.Equals(status, "Ended", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(promotion => promotion.IsActive == true && promotion.EndDate < now);
        }
        else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(promotion => promotion.IsActive != true);
        }

        return View(new AdminPromotionsViewModel
        {
            Search = search,
            Status = status,
            TotalCount = await allPromotions.CountAsync(),
            RunningCount = await allPromotions.CountAsync(promotion =>
                promotion.IsActive == true &&
                promotion.StartDate <= now &&
                promotion.EndDate >= now),
            ScheduledCount = await allPromotions.CountAsync(promotion =>
                promotion.IsActive == true && promotion.StartDate > now),
            EndedCount = await allPromotions.CountAsync(promotion =>
                promotion.IsActive == true && promotion.EndDate < now),
            InactiveCount = await allPromotions.CountAsync(promotion => promotion.IsActive != true),
            Promotions = await query
                .OrderByDescending(promotion => promotion.StartDate)
                .ThenBy(promotion => promotion.PromoCode)
                .ToListAsync()
        });
    }

    [HttpGet]
    public IActionResult CreatePromotion()
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        return View("PromotionForm", new AdminPromotionEditorViewModel
        {
            DiscountType = "Percentage",
            DiscountValue = 10,
            MaxDiscount = 100000,
            MinOrderValue = 0,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1).AddDays(1).AddSeconds(-1),
            IsActive = true
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditPromotion(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var promotion = await _context.Promotions
            .Include(item => item.Invoices)
            .FirstOrDefaultAsync(item => item.PromotionId == id);

        if (promotion == null)
        {
            TempData["AdminError"] = "Không tìm thấy chương trình khuyến mãi cần sửa.";
            return RedirectToAction(nameof(Promotions));
        }

        return View("PromotionForm", new AdminPromotionEditorViewModel
        {
            PromotionId = promotion.PromotionId,
            PromoCode = promotion.PromoCode,
            DiscountType = promotion.DiscountType ?? "Percentage",
            DiscountValue = promotion.DiscountValue,
            MaxDiscount = promotion.MaxDiscount,
            MinOrderValue = promotion.MinOrderValue,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            IsActive = promotion.IsActive == true,
            InvoiceCount = promotion.Invoices.Count
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePromotion(AdminPromotionEditorViewModel model)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        model.PromoCode = model.PromoCode?.Trim().ToUpperInvariant() ?? string.Empty;
        model.DiscountType = model.DiscountType?.Trim() ?? string.Empty;
        model.MinOrderValue ??= 0;
        model.MaxDiscount ??= 0;

        var allowedTypes = new[] { "Percentage", "FixedAmount" };
        if (!allowedTypes.Contains(model.DiscountType))
        {
            ModelState.AddModelError(nameof(model.DiscountType), "Loại giảm giá không hợp lệ.");
        }

        if (model.DiscountType == "Percentage" && model.DiscountValue > 100)
        {
            ModelState.AddModelError(nameof(model.DiscountValue), "Giảm theo phần trăm không được vượt quá 100%.");
        }

        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        var promotionId = model.PromotionId ?? 0;
        if (!string.IsNullOrWhiteSpace(model.PromoCode) &&
            await _context.Promotions.AnyAsync(promotion =>
                promotion.PromotionId != promotionId &&
                promotion.PromoCode == model.PromoCode))
        {
            ModelState.AddModelError(nameof(model.PromoCode), "Mã khuyến mãi đã tồn tại.");
        }

        Promotion? promotion = null;
        if (model.PromotionId.HasValue)
        {
            promotion = await _context.Promotions
                .Include(item => item.Invoices)
                .FirstOrDefaultAsync(item => item.PromotionId == model.PromotionId.Value);

            if (promotion == null)
            {
                TempData["AdminError"] = "Không tìm thấy chương trình khuyến mãi cần sửa.";
                return RedirectToAction(nameof(Promotions));
            }

            model.InvoiceCount = promotion.Invoices.Count;
            var termsChanged = promotion.PromoCode != model.PromoCode ||
                               promotion.DiscountType != model.DiscountType ||
                               promotion.DiscountValue != model.DiscountValue ||
                               (promotion.MaxDiscount ?? 0) != model.MaxDiscount ||
                               (promotion.MinOrderValue ?? 0) != model.MinOrderValue ||
                               promotion.StartDate != model.StartDate ||
                               promotion.EndDate != model.EndDate;

            if (model.HasUsage && termsChanged)
            {
                ModelState.AddModelError(string.Empty, "Mã đã được áp dụng cho hóa đơn nên không thể sửa điều kiện giảm giá. Hãy tạm ngừng mã nếu không muốn tiếp tục sử dụng.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("PromotionForm", model);
        }

        if (promotion == null)
        {
            promotion = new Promotion
            {
                PromoCode = model.PromoCode,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                MaxDiscount = model.MaxDiscount,
                MinOrderValue = model.MinOrderValue,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };
            _context.Promotions.Add(promotion);
        }
        else
        {
            promotion.PromoCode = model.PromoCode;
            promotion.DiscountType = model.DiscountType;
            promotion.DiscountValue = model.DiscountValue;
            promotion.MaxDiscount = model.MaxDiscount;
            promotion.MinOrderValue = model.MinOrderValue;
            promotion.StartDate = model.StartDate;
            promotion.EndDate = model.EndDate;
            promotion.IsActive = model.IsActive;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu khuyến mãi. Vui lòng kiểm tra mã và điều kiện giảm giá.");
            return View("PromotionForm", model);
        }

        TempData["AdminSuccess"] = model.IsEditing
            ? "Đã cập nhật chương trình khuyến mãi."
            : "Đã thêm chương trình khuyến mãi mới.";
        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePromotionStatus(int promotionId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var promotion = await _context.Promotions.FindAsync(promotionId);
        if (promotion == null)
        {
            TempData["AdminError"] = "Không tìm thấy mã khuyến mãi cần cập nhật.";
            return RedirectToAction(nameof(Promotions));
        }

        var wasActive = promotion.IsActive == true;
        promotion.IsActive = !wasActive;
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = wasActive
            ? "Đã tạm ngừng mã khuyến mãi. Hóa đơn đã áp dụng trước đó vẫn giữ nguyên số tiền giảm."
            : "Đã mở lại mã khuyến mãi.";
        return RedirectToAction(nameof(Promotions));
    }

    public IActionResult Reviews(string? search, int? rating, string? status)
    {
        var all = store.Reviews;
        var filtered = all.Where(review =>
                (string.IsNullOrWhiteSpace(search) || (review.Content?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) || review.Customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (!rating.HasValue || review.Rating == rating) &&
                (string.IsNullOrWhiteSpace(status) || ReviewState(review) == status))
            .ToList();
        return View(new AdminReviewsViewModel { Search = search, Rating = rating, Status = status, TotalCount = all.Count, VisibleCount = all.Count(review => review.IsVisible == true), HiddenCount = all.Count(review => review.IsVisible != true), AwaitingReplyCount = all.Count(review => string.IsNullOrWhiteSpace(review.StoreReply)), RepliedCount = all.Count(review => !string.IsNullOrWhiteSpace(review.StoreReply)), AverageRating = all.Any() ? (decimal)all.Average(review => review.Rating) : 0, Reviews = filtered });
    }

    [HttpPost]
    public IActionResult UpdateReviewModeration() => DemoRedirect(nameof(Reviews), "Phản hồi đánh giá đã được mô phỏng.");

    public IActionResult Customers(string? search, string? customerType)
    {
        var all = store.Customers;
        var filtered = all.Where(customer =>
                (string.IsNullOrWhiteSpace(search) || customer.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || customer.PhoneNumber.Contains(search)) &&
                (string.IsNullOrWhiteSpace(customerType) || (customerType == "Member" ? customer.AccountId.HasValue : !customer.AccountId.HasValue)))
            .ToList();
        return View(new AdminCustomersViewModel { Search = search, CustomerType = customerType, TotalCount = all.Count, MemberCount = all.Count(customer => customer.AccountId.HasValue), GuestCount = all.Count(customer => !customer.AccountId.HasValue), PetCount = store.Pets.Count, Customers = filtered });
    }

    public IActionResult CustomerDetails(int id)
    {
        var customer = store.Customers.FirstOrDefault(item => item.CustomerId == id) ?? store.MemberCustomer;
        return View(new AdminCustomerDetailViewModel { Customer = customer, Bookings = customer.Bookings.OrderByDescending(booking => booking.BookingDate).ToList(), TotalPaid = customer.Bookings.Sum(booking => booking.Invoice?.PaidAmount ?? 0) });
    }

    public IActionResult CreateCustomer() => View("CustomerForm", new AdminCustomerEditorViewModel());

    public IActionResult EditCustomer(int id)
    {
        var customer = store.Customers.First(item => item.CustomerId == id);
        return View("CustomerForm", new AdminCustomerEditorViewModel { CustomerId = customer.CustomerId, AccountId = customer.AccountId, FullName = customer.FullName, PhoneNumber = customer.PhoneNumber, Email = customer.Email, Address = customer.Address });
    }

    [HttpPost]
    public IActionResult SaveCustomer() => DemoRedirect(nameof(Customers), "Hồ sơ khách hàng đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult ToggleCustomerAccountStatus() => DemoRedirect(nameof(Customers), "Trạng thái tài khoản đã được mô phỏng.");

    public IActionResult CreatePet(int customerId) => View("PetForm", PetEditor(null, store.Customers.FirstOrDefault(customer => customer.CustomerId == customerId) ?? store.MemberCustomer));

    public IActionResult EditPet(int id)
    {
        var pet = store.Pets.First(item => item.PetId == id);
        return View("PetForm", PetEditor(pet, pet.Customer));
    }

    [HttpPost]
    public IActionResult SavePet(AdminPetEditorViewModel model) => DemoRedirect(nameof(CustomerDetails), "Thông tin thú cưng đã được lưu trong bản trình diễn.", new { id = model.CustomerId > 0 ? model.CustomerId : store.MemberCustomer.CustomerId });

    [HttpPost]
    public IActionResult TogglePetStatus() => DemoRedirect(nameof(Customers), "Trạng thái thú cưng đã được mô phỏng.");

    public IActionResult CreateBooking() => View("BookingForm", BookingEditor(null));

    public IActionResult EditBooking(int id) => View("BookingForm", BookingEditor(store.Bookings.FirstOrDefault(booking => booking.BookingId == id)));

    [HttpPost]
    public IActionResult SaveBooking() => DemoRedirect(nameof(Bookings), "Lịch hẹn đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult UpdateBookingStatus() => DemoRedirect(nameof(Bookings), "Trạng thái lịch và phân công đã được mô phỏng.");

    [HttpPost]
    public IActionResult ApplyInvoicePromotion() => DemoRedirect(nameof(Invoices), "Khuyến mãi hóa đơn đã được mô phỏng.");

    [HttpPost]
    public IActionResult RecordInvoicePayment() => DemoRedirect(nameof(Invoices), "Thanh toán hóa đơn đã được mô phỏng.");

    public IActionResult Contacts(string? search, string? status)
    {
        var all = store.ContactMessages;
        var filtered = all.Where(message =>
                (string.IsNullOrWhiteSpace(search) || message.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || message.PhoneNumber.Contains(search)) &&
                (string.IsNullOrWhiteSpace(status) || message.Status == status))
            .ToList();
        return View(new AdminContactsViewModel { Search = search, Status = status, TotalCount = all.Count, NewCount = all.Count(message => message.Status == "New"), ReadCount = all.Count(message => message.Status == "Read"), RepliedCount = all.Count(message => message.Status == "Replied"), Messages = filtered });
    }

    [HttpPost]
    public IActionResult UpdateContactStatus() => DemoRedirect(nameof(Contacts), "Liên hệ đã được xử lý trong bản trình diễn.");

    private AdminEmployeeEditorViewModel EmployeeEditor(Employee? employee) => new()
    {
        EmployeeId = employee?.EmployeeId,
        FullName = employee?.FullName ?? string.Empty,
        PhoneNumber = employee?.PhoneNumber ?? string.Empty,
        RoleId = employee?.RoleId ?? 3,
        IsActive = employee?.IsActive ?? true,
        HasAccount = employee?.Account != null,
        Username = employee?.Account?.Username,
        AssignmentCount = employee?.BookingDetailEmployees.Count ?? 0,
        UpcomingAssignmentCount = 1,
        Roles = store.Roles.Where(role => role.RoleId != 1).ToList()
    };

    private AdminPetEditorViewModel PetEditor(Pet? pet, Customer customer) => new()
    {
        PetId = pet?.PetId,
        CustomerId = customer.CustomerId,
        CustomerName = customer.FullName,
        Name = pet?.Name ?? string.Empty,
        SpeciesId = pet?.SpeciesId ?? store.Species.First().SpeciesId,
        BreedId = pet?.BreedId ?? store.Breeds.First().BreedId,
        Weight = pet?.Weight,
        Notes = pet?.Notes,
        Species = store.Species,
        Breeds = store.Breeds
    };

    private AdminBookingEditorViewModel BookingEditor(Booking? booking)
    {
        var detail = booking?.BookingDetails.FirstOrDefault();
        return new AdminBookingEditorViewModel
        {
            BookingId = booking?.BookingId,
            BookingCode = booking?.BookingCode,
            CustomerId = booking?.CustomerId ?? store.MemberCustomer.CustomerId,
            PetId = detail?.PetId ?? store.Pets.First().PetId,
            ServiceId = detail?.ServiceId ?? store.Services.First().ServiceId,
            BookingDate = booking?.BookingDate ?? DateTime.Today.AddDays(1).AddHours(9),
            Notes = booking?.Notes,
            StatusId = booking?.StatusId ?? 2,
            AssignedEmployeeId = detail?.BookingDetailEmployees.FirstOrDefault()?.EmployeeId,
            HasPayment = (booking?.Invoice?.PaidAmount ?? 0) > 0,
            Customers = store.Customers,
            Pets = store.Pets,
            Services = store.Services,
            Employees = store.Employees.Where(employee => employee.IsActive == true).ToList(),
            Species = store.Species,
            Breeds = store.Breeds
        };
    }

    private IActionResult DemoRedirect(string action, string message, object? values = null)
    {
        TempData["AdminSuccess"] = message;
        return RedirectToAction(action, values);
    }

    private async Task LoadServiceEditorListsAsync(AdminServiceEditorViewModel model)
    {
        model.Categories = await _context.ServiceCategories
            .OrderBy(category => category.CategoryName)
            .ToListAsync();
    }

    private IActionResult? GetAdminAccessRedirect()
    {
        if (HttpContext.Session.GetAccountId() == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!HttpContext.Session.IsAdmin())
        {
            return RedirectToAction("Index", "Home");
        }

        return null;
    }

    private static string PromotionState(Promotion promotion)
    {
        if (promotion.IsActive != true) return "Inactive";
        if (promotion.StartDate > DateTime.Now) return "Scheduled";
        if (promotion.EndDate < DateTime.Now) return "Ended";
        return "Running";
    }

    private static string ReviewState(ServiceReview review)
    {
        if (review.IsVisible != true) return "Hidden";
        return string.IsNullOrWhiteSpace(review.StoreReply) ? "AwaitingReply" : "Visible";
    }
}
