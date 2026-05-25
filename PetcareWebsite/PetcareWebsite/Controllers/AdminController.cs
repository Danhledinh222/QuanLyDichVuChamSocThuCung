using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;
using PetcareWebsite.Models;
using PetcareWebsite.ViewModels;

namespace PetcareWebsite.Controllers;

public class AdminController(DemoStore store) : Controller
{
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

    public IActionResult Services(string? search, int? categoryId, string? status)
    {
        var all = store.Services.ToList();
        var filtered = all.Where(service =>
                (string.IsNullOrWhiteSpace(search) || service.ServiceName.Contains(search, StringComparison.OrdinalIgnoreCase) || (service.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (!categoryId.HasValue || service.CategoryId == categoryId) &&
                (string.IsNullOrWhiteSpace(status) || (status == "Active" ? service.IsActive == true : service.IsActive != true)))
            .ToList();
        return View(new AdminServicesViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Status = status,
            TotalCount = all.Count,
            ActiveCount = all.Count(service => service.IsActive == true),
            InactiveCount = all.Count(service => service.IsActive != true),
            Services = filtered,
            Categories = store.Categories
        });
    }

    public IActionResult CreateService() => View("ServiceForm", ServiceEditor(null));

    public IActionResult EditService(int id) => View("ServiceForm", ServiceEditor(store.Services.FirstOrDefault(service => service.ServiceId == id)));

    [HttpPost]
    public IActionResult SaveService() => DemoRedirect(nameof(Services), "Thông tin dịch vụ đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult ToggleServiceStatus() => DemoRedirect(nameof(Services), "Trạng thái dịch vụ đã được mô phỏng.");

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

    public IActionResult Promotions(string? search, string? status)
    {
        var all = store.Promotions;
        var filtered = all.Where(promotion =>
                (string.IsNullOrWhiteSpace(search) || promotion.PromoCode.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(status) || PromotionState(promotion) == status))
            .ToList();
        return View(new AdminPromotionsViewModel { Search = search, Status = status, TotalCount = all.Count, RunningCount = all.Count(promotion => PromotionState(promotion) == "Running"), ScheduledCount = all.Count(promotion => PromotionState(promotion) == "Scheduled"), EndedCount = all.Count(promotion => PromotionState(promotion) == "Ended"), InactiveCount = all.Count(promotion => PromotionState(promotion) == "Inactive"), Promotions = filtered });
    }

    public IActionResult CreatePromotion() => View("PromotionForm", PromotionEditor(null));

    public IActionResult EditPromotion(int id) => View("PromotionForm", PromotionEditor(store.Promotions.FirstOrDefault(promotion => promotion.PromotionId == id)));

    [HttpPost]
    public IActionResult SavePromotion() => DemoRedirect(nameof(Promotions), "Mã khuyến mãi đã được lưu trong bản trình diễn.");

    [HttpPost]
    public IActionResult TogglePromotionStatus() => DemoRedirect(nameof(Promotions), "Trạng thái khuyến mãi đã được mô phỏng.");

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

    private AdminServiceEditorViewModel ServiceEditor(ServiceCatalog? service) => new()
    {
        ServiceId = service?.ServiceId,
        CategoryId = service?.CategoryId ?? store.Categories.First().CategoryId,
        ServiceName = service?.ServiceName ?? string.Empty,
        Description = service?.Description,
        BasePrice = service?.BasePrice ?? 150000,
        EstimatedDuration = service?.EstimatedDuration ?? 45,
        MaxCapacity = service?.MaxCapacity ?? 2,
        IsActive = service?.IsActive ?? true,
        BookingCount = service?.BookingDetails.Count ?? 0,
        Categories = store.Categories
    };

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

    private AdminPromotionEditorViewModel PromotionEditor(Promotion? promotion) => new()
    {
        PromotionId = promotion?.PromotionId,
        PromoCode = promotion?.PromoCode ?? string.Empty,
        DiscountType = promotion?.DiscountType ?? "Percentage",
        DiscountValue = promotion?.DiscountValue ?? 10,
        MaxDiscount = promotion?.MaxDiscount,
        MinOrderValue = promotion?.MinOrderValue ?? 200000,
        StartDate = promotion?.StartDate ?? DateTime.Today,
        EndDate = promotion?.EndDate ?? DateTime.Today.AddDays(30),
        IsActive = promotion?.IsActive ?? true,
        InvoiceCount = promotion?.Invoices.Count ?? 0
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
