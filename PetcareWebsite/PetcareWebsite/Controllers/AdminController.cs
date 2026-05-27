using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Data;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models;
using PetcareWebsite.Services;
using PetcareWebsite.ViewModels;

namespace PetcareWebsite.Controllers;

public class AdminController : Controller
{
    private const int AdminRoleId = (int)SystemRoleCode.Admin;
    private const int BookingStatusPending = (int)BookingStatusCode.Pending;
    private const int BookingStatusConfirmed = (int)BookingStatusCode.Confirmed;
    private const int BookingStatusCompleted = (int)BookingStatusCode.Completed;
    private const int BookingStatusInProgress = (int)BookingStatusCode.InProgress;
    private const int BookingStatusCancelled = (int)BookingStatusCode.Cancelled;
    private const int BookingStatusExpired = (int)BookingStatusCode.Expired;
    private const int DetailStatusNotStarted = (int)DetailStatusCode.NotStarted;
    private const int DetailStatusInProgress = (int)DetailStatusCode.InProgress;
    private const int DetailStatusDone = (int)DetailStatusCode.Done;
    private const int DetailStatusCancelled = (int)DetailStatusCode.Cancelled;

    private readonly DemoStore store;
    private readonly PetCareDbContext _context;
    private readonly IBookingBusinessService _bookingBusiness;
    private readonly IInvoiceBusinessService _invoiceBusiness;

    public AdminController(
        DemoStore store,
        PetCareDbContext context,
        IBookingBusinessService bookingBusiness,
        IInvoiceBusinessService invoiceBusiness)
    {
        this.store = store;
        _context = context;
        _bookingBusiness = bookingBusiness;
        _invoiceBusiness = invoiceBusiness;
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

    [HttpGet]
    public async Task<IActionResult> Bookings(string? search, int? statusId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        await _bookingBusiness.MarkExpiredBookingsAsync();

        var allBookings = _context.Bookings
            .Where(booking => booking.IsDeleted != true);
        var query = _context.Bookings
            .Include(booking => booking.Customer)
            .Include(booking => booking.Status)
            .Include(booking => booking.Invoice)
            .Include(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.Pet)
            .Include(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.Service)
            .Include(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.BookingDetailEmployees)
                .ThenInclude(assignment => assignment.Employee)
                .ThenInclude(employee => employee.Role)
            .Where(booking => booking.IsDeleted != true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(booking =>
                booking.BookingCode.Contains(keyword) ||
                booking.Customer.FullName.Contains(keyword) ||
                booking.Customer.PhoneNumber.Contains(keyword));
        }

        if (statusId.HasValue)
        {
            query = query.Where(booking => booking.StatusId == statusId.Value);
        }

        return View(new AdminBookingsViewModel
        {
            Search = search,
            StatusId = statusId,
            TotalCount = await allBookings.CountAsync(),
            PendingCount = await allBookings.CountAsync(booking => booking.StatusId == BookingStatusPending),
            CompletedCount = await allBookings.CountAsync(booking => booking.StatusId == BookingStatusCompleted),
            CancelledCount = await allBookings.CountAsync(booking => booking.StatusId == BookingStatusCancelled),
            ExpiredCount = await allBookings.CountAsync(booking => booking.StatusId == BookingStatusExpired),
            InProgressCount = await allBookings.CountAsync(booking => booking.StatusId == BookingStatusInProgress),
            Bookings = await query.OrderByDescending(booking => booking.BookingDate).ToListAsync(),
            Employees = await _context.Employees
                .Include(employee => employee.Role)
                .Where(employee =>
                    employee.RoleId != AdminRoleId &&
                    employee.IsActive == true &&
                    employee.IsDeleted != true)
                .OrderBy(employee => employee.FullName)
                .ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Invoices(string? search, int? statusId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        await _bookingBusiness.MarkExpiredBookingsAsync();
        var now = DateTime.Now;

        var allInvoices = _context.Invoices
            .Where(invoice => invoice.Booking.IsDeleted != true);
        var receivableInvoices = allInvoices
            .Where(invoice =>
                invoice.Booking.StatusId != BookingStatusCancelled &&
                invoice.Booking.StatusId != BookingStatusExpired);
        var query = _context.Invoices
            .Include(invoice => invoice.Status)
            .Include(invoice => invoice.Promotion)
            .Include(invoice => invoice.Payments)
                .ThenInclude(payment => payment.Method)
            .Include(invoice => invoice.Booking)
                .ThenInclude(booking => booking.Customer)
            .Include(invoice => invoice.Booking)
                .ThenInclude(booking => booking.Status)
            .Include(invoice => invoice.Booking)
                .ThenInclude(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.Service)
            .Where(invoice => invoice.Booking.IsDeleted != true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(invoice =>
                invoice.InvoiceCode.Contains(keyword) ||
                invoice.Booking.BookingCode.Contains(keyword) ||
                invoice.Booking.Customer.FullName.Contains(keyword) ||
                invoice.Booking.Customer.PhoneNumber.Contains(keyword));
        }

        if (statusId.HasValue)
        {
            query = query.Where(invoice => invoice.StatusId == statusId.Value);
            if (statusId.Value == (int)InvoiceStatusCode.Unpaid ||
                statusId.Value == (int)InvoiceStatusCode.Partial)
            {
                query = query.Where(invoice =>
                    invoice.Booking.StatusId != BookingStatusCancelled &&
                    invoice.Booking.StatusId != BookingStatusExpired);
            }
        }

        var model = new AdminInvoicesViewModel
        {
            Search = search,
            StatusId = statusId,
            TotalCount = await allInvoices.CountAsync(),
            UnpaidCount = await receivableInvoices.CountAsync(invoice => invoice.StatusId == (int)InvoiceStatusCode.Unpaid),
            PartialCount = await receivableInvoices.CountAsync(invoice => invoice.StatusId == (int)InvoiceStatusCode.Partial),
            PaidCount = await allInvoices.CountAsync(invoice => invoice.StatusId == (int)InvoiceStatusCode.Paid),
            OutstandingAmount = await receivableInvoices.SumAsync(invoice =>
                (decimal?)((invoice.TotalAmount ?? 0) - (invoice.DiscountAmount ?? 0) - (invoice.PaidAmount ?? 0))) ?? 0,
            Invoices = await query
                .OrderByDescending(invoice => invoice.CreatedAt)
                .ToListAsync(),
            PaymentMethods = await _context.PaymentMethods
                .OrderBy(method => method.MethodId)
                .ToListAsync(),
            Promotions = await _context.Promotions
                .Where(promotion =>
                    promotion.IsActive == true &&
                    promotion.StartDate <= now &&
                    promotion.EndDate >= now)
                .OrderBy(promotion => promotion.PromoCode)
                .ToListAsync()
        };

        return View(model);
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

    [HttpGet]
    public async Task<IActionResult> Reviews(string? search, int? rating, string? status)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var allReviews = _context.ServiceReviews.AsQueryable();
        var query = _context.ServiceReviews
            .Include(review => review.Customer)
            .Include(review => review.BookingDetail)
                .ThenInclude(detail => detail.Booking)
            .Include(review => review.BookingDetail)
                .ThenInclude(detail => detail.Pet)
            .Include(review => review.BookingDetail)
                .ThenInclude(detail => detail.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(review =>
                review.Customer.FullName.Contains(keyword) ||
                review.Customer.PhoneNumber.Contains(keyword) ||
                review.BookingDetail.Service.ServiceName.Contains(keyword) ||
                (review.Content != null && review.Content.Contains(keyword)));
        }

        if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            query = query.Where(review => review.Rating == rating.Value);
        }
        else
        {
            rating = null;
        }

        if (string.Equals(status, "Visible", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.IsVisible == true || review.IsVisible == null);
        }
        else if (string.Equals(status, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.IsVisible == false);
        }
        else if (string.Equals(status, "AwaitingReply", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.StoreReply == null || review.StoreReply == string.Empty);
        }
        else if (string.Equals(status, "Replied", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.StoreReply != null && review.StoreReply != string.Empty);
        }
        else
        {
            status = null;
        }

        var reviewCount = await allReviews.CountAsync();
        var model = new AdminReviewsViewModel
        {
            Search = search,
            Rating = rating,
            Status = status,
            TotalCount = reviewCount,
            VisibleCount = await allReviews.CountAsync(review => review.IsVisible == true || review.IsVisible == null),
            HiddenCount = await allReviews.CountAsync(review => review.IsVisible == false),
            AwaitingReplyCount = await allReviews.CountAsync(review =>
                review.StoreReply == null || review.StoreReply == string.Empty),
            RepliedCount = await allReviews.CountAsync(review =>
                review.StoreReply != null && review.StoreReply != string.Empty),
            AverageRating = reviewCount == 0
                ? 0
                : Math.Round(await allReviews.AverageAsync(review => (decimal)review.Rating), 1),
            Reviews = await query
                .OrderByDescending(review => review.CreatedAt)
                .ThenByDescending(review => review.ReviewId)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReviewModeration(
        int reviewId,
        bool isVisible,
        string? storeReply,
        string? search,
        int? rating,
        string? status)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var review = await _context.ServiceReviews.FindAsync(reviewId);
        if (review == null)
        {
            TempData["AdminError"] = "Không tìm thấy đánh giá cần cập nhật.";
            return RedirectToAction(nameof(Reviews), new { search, rating, status });
        }

        var reply = string.IsNullOrWhiteSpace(storeReply) ? null : storeReply.Trim();
        if (reply?.Length > 1000)
        {
            TempData["AdminError"] = "Phản hồi cửa hàng không được vượt quá 1000 ký tự.";
            return RedirectToAction(nameof(Reviews), new { search, rating, status });
        }

        review.IsVisible = isVisible;
        review.StoreReply = reply;
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = "Đã cập nhật xử lý đánh giá.";
        return RedirectToAction(nameof(Reviews), new { search, rating, status });
    }

    [HttpGet]
    public async Task<IActionResult> Customers(string? search, string? customerType)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var allCustomers = _context.Customers
            .Where(customer => customer.IsDeleted != true);
        var query = _context.Customers
            .Include(customer => customer.Account)
            .Include(customer => customer.Pets)
            .Include(customer => customer.Bookings)
            .Where(customer => customer.IsDeleted != true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(customer =>
                customer.FullName.Contains(keyword) ||
                customer.PhoneNumber.Contains(keyword) ||
                (customer.Email != null && customer.Email.Contains(keyword)));
        }

        if (customerType == "Member")
        {
            query = query.Where(customer => customer.AccountId != null);
        }
        else if (customerType == "Guest")
        {
            query = query.Where(customer => customer.AccountId == null);
        }

        return View(new AdminCustomersViewModel
        {
            Search = search,
            CustomerType = customerType,
            TotalCount = await allCustomers.CountAsync(),
            MemberCount = await allCustomers.CountAsync(customer => customer.AccountId != null),
            GuestCount = await allCustomers.CountAsync(customer => customer.AccountId == null),
            PetCount = await _context.Pets.CountAsync(pet =>
                pet.IsDeleted != true && pet.Customer.IsDeleted != true),
            Customers = await query
                .OrderBy(customer => customer.FullName)
                .ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> CustomerDetails(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        await _bookingBusiness.MarkExpiredBookingsAsync();

        var customer = await _context.Customers
            .Include(item => item.Account)
            .Include(item => item.Pets)
                .ThenInclude(pet => pet.Species)
            .Include(item => item.Pets)
                .ThenInclude(pet => pet.PetBreed)
            .FirstOrDefaultAsync(item => item.CustomerId == id && item.IsDeleted != true);

        if (customer == null)
        {
            TempData["AdminError"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction(nameof(Customers));
        }

        var bookings = await _context.Bookings
            .Include(booking => booking.Status)
            .Include(booking => booking.Invoice)
            .Include(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.Service)
            .Where(booking => booking.CustomerId == id && booking.IsDeleted != true)
            .OrderByDescending(booking => booking.BookingDate)
            .ToListAsync();

        return View(new AdminCustomerDetailViewModel
        {
            Customer = customer,
            Bookings = bookings,
            TotalPaid = bookings.Sum(booking => booking.Invoice?.PaidAmount ?? 0)
        });
    }

    [HttpGet]
    public IActionResult CreateCustomer()
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        return View("CustomerForm", new AdminCustomerEditorViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditCustomer(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == id && item.IsDeleted != true);

        if (customer == null)
        {
            TempData["AdminError"] = "Không tìm thấy hồ sơ khách hàng cần sửa.";
            return RedirectToAction(nameof(Customers));
        }

        return View("CustomerForm", new AdminCustomerEditorViewModel
        {
            CustomerId = customer.CustomerId,
            AccountId = customer.AccountId,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCustomer(AdminCustomerEditorViewModel model)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        Customer? customer = null;
        if (model.CustomerId.HasValue)
        {
            customer = await _context.Customers
                .Include(item => item.Account)
                .FirstOrDefaultAsync(item =>
                    item.CustomerId == model.CustomerId.Value && item.IsDeleted != true);

            if (customer == null)
            {
                TempData["AdminError"] = "Không tìm thấy hồ sơ khách hàng cần sửa.";
                return RedirectToAction(nameof(Customers));
            }

            model.AccountId = customer.AccountId;
        }

        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Vui lòng nhập họ tên khách hàng.");
        }

        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Vui lòng nhập số điện thoại.");
        }

        var existingId = model.CustomerId ?? 0;
        if (!string.IsNullOrWhiteSpace(model.PhoneNumber) &&
            await _context.Customers.AnyAsync(item =>
                item.PhoneNumber == model.PhoneNumber && item.CustomerId != existingId))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã thuộc về một hồ sơ khách hàng khác.");
        }

        if (model.Email != null &&
            await _context.Customers.AnyAsync(item =>
                item.Email == model.Email && item.CustomerId != existingId))
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã thuộc về một hồ sơ khách hàng khác.");
        }

        if (customer?.Account != null &&
            customer.PhoneNumber != model.PhoneNumber &&
            await _context.Accounts.AnyAsync(account =>
                account.Username == model.PhoneNumber && account.AccountId != customer.AccountId))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đang được dùng làm tài khoản đăng nhập khác.");
        }

        if (!ModelState.IsValid)
        {
            return View("CustomerForm", model);
        }

        var now = DateTime.Now;
        if (customer == null)
        {
            customer = new Customer
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                CreatedAt = now,
                IsDeleted = false
            };
            _context.Customers.Add(customer);
        }
        else
        {
            if (customer.Account != null && customer.PhoneNumber != model.PhoneNumber)
            {
                customer.Account.Username = model.PhoneNumber;
            }

            customer.FullName = model.FullName;
            customer.PhoneNumber = model.PhoneNumber;
            customer.Email = model.Email;
            customer.Address = model.Address;
            customer.ModifiedAt = now;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu khách hàng. Vui lòng kiểm tra số điện thoại hoặc email.");
            return View("CustomerForm", model);
        }

        TempData["AdminSuccess"] = model.IsEditing
            ? "Đã cập nhật hồ sơ khách hàng."
            : "Đã thêm hồ sơ khách vãng lai. Bạn có thể thêm thú cưng và tạo lịch hẹn cho khách này.";

        return RedirectToAction(nameof(CustomerDetails), new { id = customer.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCustomerAccountStatus(int customerId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var customer = await _context.Customers
            .Include(item => item.Account)
            .FirstOrDefaultAsync(item =>
                item.CustomerId == customerId && item.IsDeleted != true);

        if (customer == null)
        {
            TempData["AdminError"] = "Không tìm thấy hồ sơ khách hàng.";
            return RedirectToAction(nameof(Customers));
        }

        if (!customer.AccountId.HasValue || customer.Account == null)
        {
            TempData["AdminError"] = "Khách vãng lai không có tài khoản để khóa.";
            return RedirectToAction(nameof(CustomerDetails), new { id = customerId });
        }

        var isActive = customer.Account.IsActive == true;
        customer.Account.IsActive = !isActive;
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = isActive
            ? "Đã khóa tài khoản thành viên. Khách hàng không thể đăng nhập hoặc tiếp tục dùng phiên hiện tại."
            : "Đã mở khóa tài khoản thành viên. Khách hàng có thể đăng nhập lại.";

        return RedirectToAction(nameof(CustomerDetails), new { id = customerId });
    }

    [HttpGet]
    public async Task<IActionResult> CreatePet(int customerId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == customerId && item.IsDeleted != true);

        if (customer == null)
        {
            TempData["AdminError"] = "Không tìm thấy khách hàng để thêm thú cưng.";
            return RedirectToAction(nameof(Customers));
        }

        var model = new AdminPetEditorViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName
        };
        await LoadPetEditorListsAsync(model);
        model.SpeciesId = model.Species.FirstOrDefault()?.SpeciesId ?? 0;

        return View("PetForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditPet(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var pet = await _context.Pets
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.PetId == id && item.Customer.IsDeleted != true);

        if (pet == null)
        {
            TempData["AdminError"] = "Không tìm thấy thú cưng cần sửa.";
            return RedirectToAction(nameof(Customers));
        }

        var model = new AdminPetEditorViewModel
        {
            PetId = pet.PetId,
            CustomerId = pet.CustomerId,
            CustomerName = pet.Customer.FullName,
            Name = pet.Name,
            SpeciesId = pet.SpeciesId,
            BreedId = pet.BreedId,
            Weight = pet.Weight,
            Notes = pet.Notes
        };
        await LoadPetEditorListsAsync(model);

        return View("PetForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePet(AdminPetEditorViewModel model)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(item => item.CustomerId == model.CustomerId && item.IsDeleted != true);
        if (customer == null)
        {
            TempData["AdminError"] = "Không tìm thấy khách hàng của thú cưng.";
            return RedirectToAction(nameof(Customers));
        }

        model.CustomerName = customer.FullName;
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        if (!await _context.PetBreeds.AnyAsync(breed =>
            breed.BreedId == model.BreedId && breed.SpeciesId == model.SpeciesId))
        {
            ModelState.AddModelError(nameof(model.BreedId), "Giống thú cưng không thuộc loại đã chọn.");
        }

        Pet? pet = null;
        if (model.PetId.HasValue)
        {
            pet = await _context.Pets.FirstOrDefaultAsync(item =>
                item.PetId == model.PetId.Value && item.CustomerId == model.CustomerId);

            if (pet == null)
            {
                TempData["AdminError"] = "Không tìm thấy thú cưng cần sửa.";
                return RedirectToAction(nameof(CustomerDetails), new { id = model.CustomerId });
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadPetEditorListsAsync(model);
            return View("PetForm", model);
        }

        var now = DateTime.Now;
        if (pet == null)
        {
            pet = new Pet
            {
                CustomerId = model.CustomerId,
                Name = model.Name,
                SpeciesId = model.SpeciesId,
                BreedId = model.BreedId,
                Weight = model.Weight,
                Notes = model.Notes,
                CreatedAt = now,
                IsDeleted = false
            };
            _context.Pets.Add(pet);
        }
        else
        {
            pet.Name = model.Name;
            pet.SpeciesId = model.SpeciesId;
            pet.BreedId = model.BreedId;
            pet.Weight = model.Weight;
            pet.Notes = model.Notes;
            pet.ModifiedAt = now;
        }

        await _context.SaveChangesAsync();
        TempData["AdminSuccess"] = model.IsEditing
            ? "Đã cập nhật thông tin thú cưng."
            : "Đã thêm thú cưng vào hồ sơ khách hàng.";

        return RedirectToAction(nameof(CustomerDetails), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePetStatus(int petId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var pet = await _context.Pets
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.PetId == petId && item.Customer.IsDeleted != true);
        if (pet == null)
        {
            TempData["AdminError"] = "Không tìm thấy thú cưng cần cập nhật.";
            return RedirectToAction(nameof(Customers));
        }

        var isActive = pet.IsDeleted != true;
        if (isActive && await _context.BookingDetails.AnyAsync(detail =>
            detail.PetId == petId &&
            detail.Booking.IsDeleted != true &&
            ((detail.Booking.BookingDate >= DateTime.Now &&
              (detail.Booking.StatusId == BookingStatusPending ||
               detail.Booking.StatusId == BookingStatusConfirmed)) ||
             detail.Booking.StatusId == BookingStatusInProgress)))
        {
            TempData["AdminError"] = "Thú cưng đang có lịch hẹn sắp tới nên chưa thể ngừng sử dụng hồ sơ.";
            return RedirectToAction(nameof(CustomerDetails), new { id = pet.CustomerId });
        }

        pet.IsDeleted = isActive;
        pet.ModifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = isActive
            ? "Đã ngừng sử dụng hồ sơ thú cưng."
            : "Đã kích hoạt lại hồ sơ thú cưng.";
        return RedirectToAction(nameof(CustomerDetails), new { id = pet.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateBooking()
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var model = new AdminBookingEditorViewModel
        {
            BookingDate = DateTime.Today.AddDays(1).AddHours(9),
            StatusId = BookingStatusPending,
            CustomerMode = "Existing"
        };

        await LoadBookingEditorListsAsync(model);
        return View("BookingForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditBooking(int id)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var booking = await _context.Bookings
            .Include(item => item.Invoice)
            .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.BookingDetailEmployees)
            .FirstOrDefaultAsync(item => item.BookingId == id && item.IsDeleted != true);

        if (booking == null)
        {
            TempData["AdminError"] = "Không tìm thấy lịch hẹn cần sửa.";
            return RedirectToAction(nameof(Bookings));
        }

        if (booking.StatusId == BookingStatusCompleted ||
            booking.StatusId == BookingStatusCancelled ||
            booking.StatusId == BookingStatusInProgress)
        {
            TempData["AdminError"] = "Lịch đã bắt đầu thực hiện, đã hoàn thành hoặc đã hủy không thể sửa thông tin.";
            return RedirectToAction(nameof(Bookings));
        }

        var detail = booking.BookingDetails.FirstOrDefault();
        if (detail == null)
        {
            TempData["AdminError"] = "Lịch hẹn không có chi tiết dịch vụ để sửa.";
            return RedirectToAction(nameof(Bookings));
        }

        var model = new AdminBookingEditorViewModel
        {
            BookingId = booking.BookingId,
            BookingCode = booking.BookingCode,
            CustomerId = booking.CustomerId,
            PetId = detail.PetId,
            ServiceId = detail.ServiceId,
            BookingDate = booking.BookingDate,
            Notes = booking.Notes,
            StatusId = booking.StatusId,
            AssignedEmployeeId = detail.BookingDetailEmployees.FirstOrDefault()?.EmployeeId,
            HasPayment = (booking.Invoice?.PaidAmount ?? 0) > 0,
            CustomerMode = "Existing"
        };

        await LoadBookingEditorListsAsync(model);
        return View("BookingForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBooking(AdminBookingEditorViewModel model)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        if (model.BookingId.HasValue)
        {
            model.CustomerMode = "Existing";
        }

        var isGuestBooking = !model.BookingId.HasValue
                             && string.Equals(model.CustomerMode, "Guest", StringComparison.OrdinalIgnoreCase);

        if (model.BookingDate <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.BookingDate), "Thời gian hẹn phải ở tương lai.");
        }

        if (isGuestBooking)
        {
            model.GuestFullName = model.GuestFullName?.Trim();
            model.GuestPhoneNumber = model.GuestPhoneNumber?.Trim();
            model.GuestEmail = string.IsNullOrWhiteSpace(model.GuestEmail) ? null : model.GuestEmail.Trim();
            model.GuestPetName = model.GuestPetName?.Trim();

            if (string.IsNullOrWhiteSpace(model.GuestFullName))
            {
                ModelState.AddModelError(nameof(model.GuestFullName), "Vui lòng nhập tên khách vãng lai.");
            }

            if (string.IsNullOrWhiteSpace(model.GuestPhoneNumber))
            {
                ModelState.AddModelError(nameof(model.GuestPhoneNumber), "Vui lòng nhập số điện thoại khách vãng lai.");
            }
            else if (await _context.Customers.AnyAsync(customer => customer.PhoneNumber == model.GuestPhoneNumber))
            {
                ModelState.AddModelError(nameof(model.GuestPhoneNumber), "Số điện thoại đã có hồ sơ. Vui lòng chọn khách hàng có sẵn.");
            }

            if (!string.IsNullOrWhiteSpace(model.GuestEmail)
                && await _context.Customers.AnyAsync(customer => customer.Email == model.GuestEmail))
            {
                ModelState.AddModelError(nameof(model.GuestEmail), "Email đã có hồ sơ khách hàng trong hệ thống.");
            }

            if (string.IsNullOrWhiteSpace(model.GuestPetName))
            {
                ModelState.AddModelError(nameof(model.GuestPetName), "Vui lòng nhập tên thú cưng.");
            }

            var breedMatchesSpecies = await _context.PetBreeds.AnyAsync(breed =>
                breed.BreedId == model.GuestBreedId && breed.SpeciesId == model.GuestSpeciesId);
            if (!breedMatchesSpecies)
            {
                ModelState.AddModelError(nameof(model.GuestBreedId), "Vui lòng chọn giống thú cưng phù hợp.");
            }
        }
        else
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(item => item.CustomerId == model.CustomerId && item.IsDeleted != true);
            var pet = await _context.Pets
                .FirstOrDefaultAsync(item => item.PetId == model.PetId
                                             && item.CustomerId == model.CustomerId
                                             && item.IsDeleted != true);

            if (customer == null)
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Khách hàng không tồn tại.");
            }

            if (pet == null)
            {
                ModelState.AddModelError(nameof(model.PetId), "Thú cưng không thuộc khách hàng đã chọn.");
            }
        }

        var service = await _context.ServiceCatalogs
            .FirstOrDefaultAsync(item => item.ServiceId == model.ServiceId
                && ((item.IsActive == true && item.IsDeleted != true) ||
                    (model.BookingId.HasValue &&
                     item.BookingDetails.Any(detail => detail.BookingId == model.BookingId.Value))));

        if (service == null)
        {
            ModelState.AddModelError(nameof(model.ServiceId), "Dịch vụ không khả dụng.");
        }

        var employeeIsAvailable = !model.AssignedEmployeeId.HasValue ||
            await _context.Employees.AnyAsync(employee =>
                employee.EmployeeId == model.AssignedEmployeeId.Value &&
                employee.RoleId != AdminRoleId &&
                employee.IsActive == true &&
                employee.IsDeleted != true);
        if (!employeeIsAvailable)
        {
            ModelState.AddModelError(nameof(model.AssignedEmployeeId), "Nhân viên phụ trách không khả dụng.");
        }

        var requiresAssignment = (!model.BookingId.HasValue && model.StatusId == BookingStatusConfirmed) ||
                                 (model.BookingId.HasValue &&
                                  (model.StatusId == BookingStatusConfirmed || model.StatusId == BookingStatusExpired));
        if (requiresAssignment && !model.AssignedEmployeeId.HasValue)
        {
            ModelState.AddModelError(nameof(model.AssignedEmployeeId), "Lịch đã xác nhận phải có nhân viên phụ trách.");
        }

        if (service != null)
        {
            var availabilityValidation = await _bookingBusiness.ValidateAvailabilityAsync(
                model.BookingDate,
                model.ServiceId,
                model.AssignedEmployeeId,
                model.BookingId);
            if (!availabilityValidation.Succeeded)
            {
                ModelState.AddModelError(nameof(model.BookingDate), availabilityValidation.ErrorMessage!);
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadBookingEditorListsAsync(model);
            return View("BookingForm", model);
        }

        return model.BookingId.HasValue
            ? await UpdateBookingAsync(model, service!)
            : await AddBookingAsync(model, service!, isGuestBooking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBookingStatus(int bookingId, int? assignedEmployeeId, int? statusId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        await _bookingBusiness.MarkExpiredBookingsAsync();

        var allowedStatuses = new[]
        {
            BookingStatusConfirmed,
            BookingStatusInProgress,
            BookingStatusCompleted,
            BookingStatusCancelled
        };

        if (statusId.HasValue && !allowedStatuses.Contains(statusId.Value))
        {
            TempData["AdminError"] = "Trạng thái lịch hẹn không hợp lệ.";
            return RedirectToAction(nameof(Bookings));
        }

        var booking = await _context.Bookings
            .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.BookingDetailEmployees)
            .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.IsDeleted != true);

        if (booking == null)
        {
            TempData["AdminError"] = "Không tìm thấy lịch hẹn cần cập nhật.";
            return RedirectToAction(nameof(Bookings));
        }

        if (booking.StatusId == BookingStatusCompleted ||
            booking.StatusId == BookingStatusCancelled ||
            booking.StatusId == BookingStatusExpired)
        {
            TempData["AdminError"] = "Lịch đã chốt hoặc hết hạn không thể phân công trực tiếp. Hãy dời lịch nếu cần tiếp tục xử lý.";
            return RedirectToAction(nameof(Bookings));
        }

        if (booking.BookingDetails.Count == 0)
        {
            TempData["AdminError"] = "Lịch hẹn không có chi tiết dịch vụ để phân công.";
            return RedirectToAction(nameof(Bookings));
        }

        var currentEmployeeId = booking.BookingDetails
            .SelectMany(detail => detail.BookingDetailEmployees)
            .Select(assignment => (int?)assignment.EmployeeId)
            .FirstOrDefault();
        if (booking.StatusId == BookingStatusInProgress && assignedEmployeeId != currentEmployeeId)
        {
            TempData["AdminError"] = "Dịch vụ đang thực hiện không thể đổi nhân viên phụ trách.";
            return RedirectToAction(nameof(Bookings));
        }

        if (assignedEmployeeId.HasValue &&
            !await _context.Employees.AnyAsync(employee =>
                employee.EmployeeId == assignedEmployeeId.Value &&
                employee.RoleId != AdminRoleId &&
                employee.IsActive == true &&
                employee.IsDeleted != true))
        {
            TempData["AdminError"] = "Nhân viên phụ trách không khả dụng.";
            return RedirectToAction(nameof(Bookings));
        }

        var nextStatusId = statusId ?? booking.StatusId;
        var transitionValidation = _bookingBusiness.ValidateStatusTransition(
            booking,
            nextStatusId,
            assignedEmployeeId.HasValue);
        if (!transitionValidation.Succeeded)
        {
            TempData["AdminError"] = transitionValidation.ErrorMessage;
            return RedirectToAction(nameof(Bookings));
        }

        if (assignedEmployeeId.HasValue)
        {
            var firstDetail = booking.BookingDetails.First();
            var availabilityValidation = await _bookingBusiness.ValidateAvailabilityAsync(
                booking.BookingDate,
                firstDetail.ServiceId,
                assignedEmployeeId,
                booking.BookingId);
            if (!availabilityValidation.Succeeded)
            {
                TempData["AdminError"] = availabilityValidation.ErrorMessage;
                return RedirectToAction(nameof(Bookings));
            }
        }

        var now = DateTime.Now;
        foreach (var detail in booking.BookingDetails)
        {
            var keepsCurrentAssignment = detail.BookingDetailEmployees.Count == 1 &&
                                         detail.BookingDetailEmployees.First().EmployeeId == assignedEmployeeId;
            if (!keepsCurrentAssignment)
            {
                _context.BookingDetailEmployees.RemoveRange(detail.BookingDetailEmployees);
                if (assignedEmployeeId.HasValue)
                {
                    _context.BookingDetailEmployees.Add(new BookingDetailEmployee
                    {
                        BookingDetailId = detail.BookingDetailId,
                        EmployeeId = assignedEmployeeId.Value,
                        AssignedAt = now
                    });
                }
            }

            if (nextStatusId == BookingStatusInProgress)
            {
                detail.StatusId = DetailStatusInProgress;
                detail.StartTime ??= now;
                detail.EndTime = null;
                detail.ModifiedAt = now;
            }
            else if (nextStatusId == BookingStatusCompleted)
            {
                detail.StatusId = DetailStatusDone;
                detail.EndTime = now;
                detail.ModifiedAt = now;
            }
            else if (nextStatusId == BookingStatusCancelled)
            {
                detail.StatusId = DetailStatusCancelled;
                detail.ModifiedAt = now;
            }
        }

        booking.StatusId = nextStatusId;
        booking.ModifiedAt = now;
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = statusId.HasValue
            ? "Đã lưu phân công và cập nhật trạng thái lịch hẹn."
            : "Đã lưu nhân viên phụ trách lịch hẹn.";
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyInvoicePromotion(int invoiceId, int? promotionId)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var invoice = await _context.Invoices
            .Include(item => item.Booking)
            .FirstOrDefaultAsync(item => item.InvoiceId == invoiceId && item.Booking.IsDeleted != true);

        if (invoice == null)
        {
            TempData["AdminError"] = "Không tìm thấy hóa đơn cần áp dụng khuyến mãi.";
            return RedirectToAction(nameof(Invoices));
        }

        if ((invoice.PaidAmount ?? 0) > 0)
        {
            TempData["AdminError"] = "Hóa đơn đã ghi nhận thanh toán nên không thể thay đổi khuyến mãi.";
            return RedirectToAction(nameof(Invoices));
        }

        if (invoice.Booking.StatusId == BookingStatusCancelled || invoice.Booking.StatusId == BookingStatusExpired)
        {
            TempData["AdminError"] = "Lịch đã hủy hoặc hết hạn không thể áp dụng khuyến mãi.";
            return RedirectToAction(nameof(Invoices));
        }

        if (!promotionId.HasValue)
        {
            _invoiceBusiness.ApplyPromotion(invoice, null, invoice.CreatedAt ?? DateTime.Now);
            await _context.SaveChangesAsync();
            TempData["AdminSuccess"] = "Đã bỏ mã khuyến mãi khỏi hóa đơn.";
            return RedirectToAction(nameof(Invoices));
        }

        var now = DateTime.Now;
        var referenceDate = invoice.CreatedAt ?? now;
        var promotion = await _context.Promotions.FirstOrDefaultAsync(item =>
            item.PromotionId == promotionId.Value &&
            item.IsActive == true &&
            item.StartDate <= now &&
            item.EndDate >= now);

        if (promotion == null)
        {
            TempData["AdminError"] = "Mã khuyến mãi không còn hiệu lực.";
            return RedirectToAction(nameof(Invoices));
        }

        if (referenceDate < promotion.StartDate || referenceDate > promotion.EndDate)
        {
            TempData["AdminError"] = "Hóa đơn được tạo ngoài thời gian áp dụng của mã khuyến mãi.";
            return RedirectToAction(nameof(Invoices));
        }

        if ((invoice.TotalAmount ?? 0) < (promotion.MinOrderValue ?? 0))
        {
            TempData["AdminError"] = "Hóa đơn chưa đạt giá trị tối thiểu của mã khuyến mãi.";
            return RedirectToAction(nameof(Invoices));
        }

        _invoiceBusiness.ApplyPromotion(invoice, promotion, referenceDate);
        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] = $"Đã áp mã {promotion.PromoCode}; hóa đơn được giảm {(invoice.DiscountAmount ?? 0):N0} đ.";
        return RedirectToAction(nameof(Invoices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordInvoicePayment(int invoiceId, decimal amount, int methodId, string? note)
    {
        var accessRedirect = GetAdminAccessRedirect();
        if (accessRedirect != null)
        {
            return accessRedirect;
        }

        var invoice = await _context.Invoices
            .Include(item => item.Booking)
            .FirstOrDefaultAsync(item => item.InvoiceId == invoiceId && item.Booking.IsDeleted != true);

        if (invoice == null)
        {
            TempData["AdminError"] = "Không tìm thấy hóa đơn cần cập nhật.";
            return RedirectToAction(nameof(Invoices));
        }

        if (invoice.Booking.StatusId == BookingStatusCancelled || invoice.Booking.StatusId == BookingStatusExpired)
        {
            TempData["AdminError"] = "Lịch đã hủy hoặc đã hết hạn không thể ghi nhận thanh toán mới.";
            return RedirectToAction(nameof(Invoices));
        }

        var remainingAmount = (invoice.TotalAmount ?? 0)
                              - (invoice.DiscountAmount ?? 0)
                              - (invoice.PaidAmount ?? 0);

        if (amount <= 0 || amount > remainingAmount)
        {
            TempData["AdminError"] = "Số tiền thu phải lớn hơn 0 và không vượt quá số tiền còn thiếu.";
            return RedirectToAction(nameof(Invoices));
        }

        if (!await _context.PaymentMethods.AnyAsync(method => method.MethodId == methodId))
        {
            TempData["AdminError"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction(nameof(Invoices));
        }

        try
        {
            var paymentNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC dbo.sp_ProcessPayment @InvoiceID={invoice.InvoiceId}, @Amount={amount}, @MethodID={methodId}, @Note={paymentNote}");

            TempData["AdminSuccess"] = "Đã ghi nhận thanh toán cho lịch hẹn.";
        }
        catch
        {
            TempData["AdminError"] = "Không thể ghi nhận thanh toán. Vui lòng kiểm tra số tiền và thử lại.";
        }

        return RedirectToAction(nameof(Invoices));
    }

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

    private async Task<IActionResult> AddBookingAsync(
        AdminBookingEditorViewModel model,
        ServiceCatalog service,
        bool isGuestBooking)
    {
        var allowedStatuses = new[] { BookingStatusPending, BookingStatusConfirmed };
        var statusId = allowedStatuses.Contains(model.StatusId) ? model.StatusId : BookingStatusPending;
        var now = DateTime.Now;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var customerId = model.CustomerId;
            var petId = model.PetId;

            if (isGuestBooking)
            {
                var guestCustomer = new Customer
                {
                    AccountId = null,
                    FullName = model.GuestFullName!,
                    PhoneNumber = model.GuestPhoneNumber!,
                    Email = model.GuestEmail,
                    CreatedAt = now,
                    IsDeleted = false
                };
                _context.Customers.Add(guestCustomer);
                await _context.SaveChangesAsync();

                var guestPet = new Pet
                {
                    CustomerId = guestCustomer.CustomerId,
                    Name = model.GuestPetName!,
                    SpeciesId = model.GuestSpeciesId,
                    BreedId = model.GuestBreedId,
                    Weight = model.GuestPetWeight,
                    Notes = string.IsNullOrWhiteSpace(model.GuestPetNotes) ? null : model.GuestPetNotes.Trim(),
                    CreatedAt = now,
                    IsDeleted = false
                };
                _context.Pets.Add(guestPet);
                await _context.SaveChangesAsync();

                customerId = guestCustomer.CustomerId;
                petId = guestPet.PetId;
            }

            var booking = new Booking
            {
                BookingCode = ReferenceCodeHelper.Create("BK"),
                CustomerId = customerId,
                BookingDate = model.BookingDate,
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                StatusId = statusId,
                CreatedAt = now,
                IsDeleted = false
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var bookingDetail = new BookingDetail
            {
                BookingId = booking.BookingId,
                PetId = petId,
                ServiceId = model.ServiceId,
                ActualPrice = service.BasePrice,
                StatusId = DetailStatusNotStarted
            };
            _context.BookingDetails.Add(bookingDetail);
            await _context.SaveChangesAsync();

            if (model.AssignedEmployeeId.HasValue)
            {
                _context.BookingDetailEmployees.Add(new BookingDetailEmployee
                {
                    BookingDetailId = bookingDetail.BookingDetailId,
                    EmployeeId = model.AssignedEmployeeId.Value,
                    AssignedAt = now
                });
            }

            _context.Invoices.Add(new Invoice
            {
                InvoiceCode = ReferenceCodeHelper.Create("INV"),
                BookingId = booking.BookingId,
                TotalAmount = _invoiceBusiness.CalculateTotalAmount(service.BasePrice),
                DiscountAmount = 0,
                PaidAmount = 0,
                StatusId = (int)InvoiceStatusCode.Unpaid,
                CreatedAt = now
            });
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            TempData["AdminSuccess"] = isGuestBooking
                ? "Đã tạo hồ sơ khách vãng lai, lịch hẹn và hóa đơn chưa thanh toán."
                : "Đã thêm lịch hẹn mới và tạo hóa đơn chưa thanh toán.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["AdminError"] = "Không thể thêm lịch hẹn. Vui lòng kiểm tra dữ liệu và thử lại.";
        }

        return RedirectToAction(nameof(Bookings));
    }

    private async Task<IActionResult> UpdateBookingAsync(AdminBookingEditorViewModel model, ServiceCatalog service)
    {
        var booking = await _context.Bookings
            .Include(item => item.Invoice)
                .ThenInclude(invoice => invoice!.Promotion)
            .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.BookingDetailEmployees)
            .FirstOrDefaultAsync(item => item.BookingId == model.BookingId && item.IsDeleted != true);

        if (booking == null)
        {
            TempData["AdminError"] = "Không tìm thấy lịch hẹn cần sửa.";
            return RedirectToAction(nameof(Bookings));
        }

        if (booking.StatusId == BookingStatusCompleted ||
            booking.StatusId == BookingStatusCancelled ||
            booking.StatusId == BookingStatusInProgress)
        {
            TempData["AdminError"] = "Lịch đã bắt đầu thực hiện, đã hoàn thành hoặc đã hủy không thể sửa thông tin.";
            return RedirectToAction(nameof(Bookings));
        }

        var detail = booking.BookingDetails.FirstOrDefault();
        if (detail == null)
        {
            TempData["AdminError"] = "Lịch hẹn không có chi tiết dịch vụ để sửa.";
            return RedirectToAction(nameof(Bookings));
        }

        var hasPayment = (booking.Invoice?.PaidAmount ?? 0) > 0;
        if (hasPayment && (detail.ServiceId != model.ServiceId || detail.PetId != model.PetId))
        {
            model.HasPayment = true;
            ModelState.AddModelError(nameof(model.ServiceId), "Hóa đơn đã có thanh toán nên không thể đổi dịch vụ hoặc thú cưng.");
            await LoadBookingEditorListsAsync(model);
            return View("BookingForm", model);
        }

        var now = DateTime.Now;
        booking.CustomerId = model.CustomerId;
        booking.BookingDate = model.BookingDate;
        booking.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        booking.ModifiedAt = now;

        if (booking.StatusId == BookingStatusExpired)
        {
            booking.StatusId = BookingStatusConfirmed;
        }

        if (!hasPayment)
        {
            detail.PetId = model.PetId;
            detail.ServiceId = model.ServiceId;
            detail.ActualPrice = service.BasePrice;
            detail.ModifiedAt = now;
        }

        var keepsCurrentAssignment = detail.BookingDetailEmployees.Count == 1 &&
                                     detail.BookingDetailEmployees.First().EmployeeId == model.AssignedEmployeeId;
        if (!keepsCurrentAssignment)
        {
            _context.BookingDetailEmployees.RemoveRange(detail.BookingDetailEmployees);
            if (model.AssignedEmployeeId.HasValue)
            {
                _context.BookingDetailEmployees.Add(new BookingDetailEmployee
                {
                    BookingDetailId = detail.BookingDetailId,
                    EmployeeId = model.AssignedEmployeeId.Value,
                    AssignedAt = now
                });
            }
        }

        await _context.SaveChangesAsync();

        if (booking.Invoice != null && !hasPayment)
        {
            booking.Invoice.TotalAmount = _invoiceBusiness.CalculateTotalAmount(service.BasePrice);
            booking.Invoice.DiscountAmount = _invoiceBusiness.CalculateDiscountAmount(
                booking.Invoice.TotalAmount.Value,
                booking.Invoice.Promotion,
                booking.Invoice.CreatedAt ?? now);
            booking.Invoice.ModifiedAt = now;
            _context.Entry(booking.Invoice).Property(invoice => invoice.TotalAmount).IsModified = true;
            await _context.SaveChangesAsync();
        }

        TempData["AdminSuccess"] = "Đã cập nhật thông tin lịch hẹn.";
        return RedirectToAction(nameof(Bookings));
    }

    private async Task LoadBookingEditorListsAsync(AdminBookingEditorViewModel model)
    {
        model.Customers = await _context.Customers
            .Where(customer => customer.IsDeleted != true)
            .OrderBy(customer => customer.FullName)
            .ToListAsync();
        model.Pets = await _context.Pets
            .Where(pet => pet.IsDeleted != true)
            .OrderBy(pet => pet.Name)
            .ToListAsync();
        model.Services = await _context.ServiceCatalogs
            .Where(service =>
                (service.IsActive == true && service.IsDeleted != true) ||
                (model.BookingId.HasValue && service.ServiceId == model.ServiceId))
            .OrderBy(service => service.ServiceName)
            .ToListAsync();
        model.Employees = await _context.Employees
            .Include(employee => employee.Role)
            .Where(employee =>
                employee.RoleId != AdminRoleId &&
                employee.IsActive == true &&
                employee.IsDeleted != true)
            .OrderBy(employee => employee.FullName)
            .ToListAsync();
        model.Species = await _context.PetSpecies
            .OrderBy(species => species.SpeciesName)
            .ToListAsync();
        model.Breeds = await _context.PetBreeds
            .OrderBy(breed => breed.BreedName)
            .ToListAsync();
    }

    private async Task LoadPetEditorListsAsync(AdminPetEditorViewModel model)
    {
        model.Species = await _context.PetSpecies
            .OrderBy(species => species.SpeciesName)
            .ToListAsync();
        model.Breeds = await _context.PetBreeds
            .OrderBy(breed => breed.BreedName)
            .ToListAsync();
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
