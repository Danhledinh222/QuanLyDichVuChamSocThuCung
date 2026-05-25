using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;
using PetcareWebsite.Models;

namespace PetcareWebsite.Controllers
{

public class HomeController(DemoStore store) : Controller
{
    public IActionResult Index() => View(store.Services);

    public IActionResult Pricing() => View(store.Services);

    public IActionResult Contact() => View();

    public IActionResult Privacy() => View();

    [HttpPost]
    public IActionResult SendContact(string? returnUrl)
    {
        TempData["SuccessMessage"] = "Thông tin liên hệ đã được ghi nhận trong bản trình diễn.";
        return string.IsNullOrWhiteSpace(returnUrl)
            ? RedirectToAction(nameof(Contact))
            : LocalRedirect(returnUrl);
    }

    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
}

namespace PetcareWebsite.Data
{

public sealed class DemoStore
{
    public DemoStore()
    {
        CreateCatalog();
        CreatePeopleAndPets();
        CreateBookingsAndInvoices();
        CreateInventory();
        CreateContacts();
    }

    public List<ServiceCategory> Categories { get; } = new();

    public List<ServiceCatalog> Services { get; } = new();

    public List<PetSpecy> Species { get; } = new();

    public List<PetBreed> Breeds { get; } = new();

    public List<Customer> Customers { get; } = new();

    public List<Pet> Pets { get; } = new();

    public List<Role> Roles { get; } = new();

    public List<Employee> Employees { get; } = new();

    public List<Booking> Bookings { get; } = new();

    public List<BookingDetail> BookingDetails { get; } = new();

    public List<Invoice> Invoices { get; } = new();

    public List<InvoiceStatus> InvoiceStatuses { get; } = new();

    public List<PaymentMethod> PaymentMethods { get; } = new();

    public List<Promotion> Promotions { get; } = new();

    public List<ServiceReview> Reviews { get; } = new();

    public List<MedicalSupply> Supplies { get; } = new();

    public List<InventoryTransaction> InventoryTransactions { get; } = new();

    public List<ServiceMaterialQuotum> MaterialQuotas { get; } = new();

    public List<ContactMessage> ContactMessages { get; } = new();

    public Customer MemberCustomer => Customers[0];

    private void CreateCatalog()
    {
        var grooming = AddCategory(1, "Cắt tỉa & Làm đẹp");
        var spa = AddCategory(2, "Spa & Thư giãn");
        var health = AddCategory(3, "Chăm sóc sức khỏe");

        AddService(1, grooming, "Cắt tỉa tạo kiểu chuyên nghiệp", "Cắt tỉa theo giống và tạo kiểu phù hợp với bé.", 300000, 60, 3);
        AddService(2, spa, "Tắm gội & Sấy khô cơ bản", "Làm sạch nhẹ nhàng, sấy khô an toàn.", 150000, 45, 4);
        AddService(3, spa, "Massage thư giãn tinh dầu", "Thư giãn cơ bắp với tinh dầu dịu nhẹ.", 225000, 45, 2);
        AddService(4, spa, "Spa VIP & Sục Microbubble", "Gói làm sạch sâu, dưỡng lông và thư giãn cao cấp.", 500000, 90, 2);
        AddService(5, health, "Vệ sinh lấy cao răng siêu âm", "Chăm sóc răng miệng và kiểm tra ban đầu.", 400000, 45, 2);
        AddService(6, health, "Tiêm phòng Vaccine đại trà", "Tư vấn và tiêm phòng đúng lịch.", 250000, 30, 4);

        var dog = new PetSpecy { SpeciesId = 1, SpeciesName = "Chó" };
        var cat = new PetSpecy { SpeciesId = 2, SpeciesName = "Mèo" };
        Species.AddRange([dog, cat]);

        AddBreed(1, dog, "Poodle");
        AddBreed(2, dog, "Corgi");
        AddBreed(3, dog, "Pomeranian");
        AddBreed(4, cat, "Mèo Ta");
        AddBreed(5, cat, "Mèo Anh lông dài (ALD)");
        AddBreed(6, cat, "Mèo Sphynx");
    }

    private void CreatePeopleAndPets()
    {
        var adminRole = new Role { RoleId = 1, RoleName = "Quản trị viên" };
        var vetRole = new Role { RoleId = 2, RoleName = "Bác sĩ thú y" };
        var groomerRole = new Role { RoleId = 3, RoleName = "Groomer" };
        Roles.AddRange([adminRole, vetRole, groomerRole]);

        var memberAccount = new Account { AccountId = 4, Username = "0906455741", Password = "demo", RoleId = 4, IsActive = true };
        var member = new Customer
        {
            CustomerId = 4,
            AccountId = 4,
            Account = memberAccount,
            FullName = "Danh dz",
            PhoneNumber = "0906455741",
            Email = "danhdz@gmail.com",
            Address = "12 Nguyễn Văn Cừ, Quận 5, TP. Hồ Chí Minh",
            CreatedAt = DateTime.Today.AddMonths(-4),
            IsDeleted = false
        };
        memberAccount.Customer = member;

        var vip = new Customer
        {
            CustomerId = 14,
            AccountId = 14,
            Account = new Account { AccountId = 14, Username = "0906455642", Password = "demo", RoleId = 4, IsActive = true },
            FullName = "Danh VIP",
            PhoneNumber = "0906455642",
            Email = "vip@petcare.vn",
            Address = "18 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
            CreatedAt = DateTime.Today.AddMonths(-2),
            IsDeleted = false
        };
        vip.Account!.Customer = vip;

        var guest = new Customer
        {
            CustomerId = 15,
            FullName = "Nguyễn Hoàng Mai",
            PhoneNumber = "0976543210",
            Email = "mai.nguyen@gmail.com",
            Address = "TP. Hồ Chí Minh",
            CreatedAt = DateTime.Today.AddDays(-12),
            IsDeleted = false
        };
        Customers.AddRange([member, vip, guest]);

        AddPet(1, member, "Bé florentino", Species[0], Breeds[0], 6.60m, "Hiền, thích được vuốt ve.");
        AddPet(2, member, "cat", Species[1], Breeds[3], 3.80m, "Cần sấy nhẹ.");
        AddPet(3, member, "doggy", Species[1], Breeds[4], 3.00m, "Nhạy cảm với tiếng ồn.");
        AddPet(4, vip, "nakrok", Species[0], Breeds[1], 9.20m, "Cắt kiểu gọn.");
        AddPet(5, guest, "Milo", Species[0], Breeds[2], 4.10m, "Khách vãng lai.");

        Employees.Add(CreateEmployee(1, "Lê Đình Danh", "0906000001", adminRole, "admin"));
        Employees.Add(CreateEmployee(2, "Nguyễn Văn Tài", "0906123456", vetRole, "tai.vet"));
        Employees.Add(CreateEmployee(3, "Trần Thu Hà", "0906234567", groomerRole, "ha.groomer"));
        Employees.Add(CreateEmployee(4, "Phạm Minh An", "0906345678", groomerRole, null, false));
    }

    private void CreateBookingsAndInvoices()
    {
        var unpaid = new InvoiceStatus { StatusId = 1, StatusName = "Chưa thanh toán" };
        var partial = new InvoiceStatus { StatusId = 2, StatusName = "Thanh toán một phần" };
        var paid = new InvoiceStatus { StatusId = 3, StatusName = "Đã thanh toán" };
        InvoiceStatuses.AddRange([unpaid, partial, paid]);

        PaymentMethods.AddRange([
            new PaymentMethod { MethodId = 1, MethodName = "Tiền mặt" },
            new PaymentMethod { MethodId = 2, MethodName = "Chuyển khoản" },
            new PaymentMethod { MethodId = 3, MethodName = "Ví điện tử" }
        ]);

        var welcome = new Promotion
        {
            PromotionId = 1,
            PromoCode = "WELCOME10",
            DiscountType = "Percentage",
            DiscountValue = 10,
            MaxDiscount = 50000,
            MinOrderValue = 200000,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.Today.AddDays(-35)
        };
        var spaWeekend = new Promotion
        {
            PromotionId = 2,
            PromoCode = "SPA50K",
            DiscountType = "FixedAmount",
            DiscountValue = 50000,
            MinOrderValue = 400000,
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.Today
        };
        var oldPromo = new Promotion
        {
            PromotionId = 3,
            PromoCode = "TET2026",
            DiscountType = "Percentage",
            DiscountValue = 15,
            MaxDiscount = 80000,
            MinOrderValue = 300000,
            StartDate = DateTime.Today.AddMonths(-3),
            EndDate = DateTime.Today.AddMonths(-2),
            IsActive = false,
            CreatedAt = DateTime.Today.AddMonths(-3)
        };
        Promotions.AddRange([welcome, spaWeekend, oldPromo]);

        var completeOne = AddBooking(7, MemberCustomer, Pets[0], Services[0], DateTime.Today.AddDays(-4).AddHours(15), 3, Employees[2], "Cắt tỉa gọn phần tai.");
        AddInvoice(completeOne, 330000, 0, 330000, paid, null);
        AddReview(completeOne.BookingDetails.First(), MemberCustomer, 5, "Nhân viên nhẹ nhàng, bé rất thoải mái.", "Nhân viên thân thiện, Đúng giờ", "PetCare cảm ơn bạn đã tin tưởng dịch vụ.");

        var completeTwo = AddBooking(8, MemberCustomer, Pets[0], Services[1], DateTime.Today.AddDays(-2).AddHours(9), 3, Employees[1], "Tắm gội cơ bản.");
        AddInvoice(completeTwo, 165000, 0, 165000, paid, null);

        var running = AddBooking(17, MemberCustomer, Pets[1], Services[5], DateTime.Today.AddHours(9), 6, Employees[2], "Theo dõi sau tiêm.");
        AddInvoice(running, 275000, 27500, 100000, partial, welcome);

        var confirmed = AddBooking(18, MemberCustomer, Pets[0], Services[3], DateTime.Today.AddDays(1).AddHours(10), 2, Employees[2], "Sấy lông kỹ.");
        AddInvoice(confirmed, 550000, 50000, 0, unpaid, welcome);

        var pending = AddBooking(19, Customers[1], Pets[3], Services[2], DateTime.Today.AddDays(2).AddHours(14), 1, null, "Xác nhận lại trước khi đến.");
        AddInvoice(pending, 247500, 0, 0, unpaid, null);

        var expired = AddBooking(20, Customers[2], Pets[4], Services[4], DateTime.Today.AddDays(-1).AddHours(8), 5, null, "Khách chưa đến.");
        AddInvoice(expired, 440000, 0, 0, unpaid, null);

        var cancelled = AddBooking(21, MemberCustomer, Pets[2], Services[0], DateTime.Today.AddDays(-8).AddHours(14), 4, Employees[2], "Khách đổi lịch.");
        AddInvoice(cancelled, 330000, 0, 0, unpaid, null);
    }

    private void CreateInventory()
    {
        var shampoo = AddSupply(1, "Dầu tắm dịu nhẹ", "chai", 18, 5, DateOnly.FromDateTime(DateTime.Today.AddMonths(8)));
        var vaccine = AddSupply(2, "Vaccine tổng hợp", "liều", 3, 8, DateOnly.FromDateTime(DateTime.Today.AddMonths(2)));
        var earCleaner = AddSupply(3, "Dung dịch vệ sinh tai", "chai", 11, 4, DateOnly.FromDateTime(DateTime.Today.AddDays(20)));
        var expiredCream = AddSupply(4, "Kem dưỡng lông", "hộp", 6, 3, DateOnly.FromDateTime(DateTime.Today.AddDays(-3)));

        AddQuota(Services[1], shampoo, 1);
        AddQuota(Services[3], shampoo, 2);
        AddQuota(Services[5], vaccine, 1);
        AddQuota(Services[0], earCleaner, 1);
        AddQuota(Services[3], expiredCream, 1);

        AddTransaction(1, shampoo, "IMPORT", 20, Employees[0], "Nhập kho đầu tháng");
        AddTransaction(2, vaccine, "IMPORT", 10, Employees[0], "Bổ sung vaccine");
        AddTransaction(3, shampoo, "EXPORT_SERVICE", -2, Employees[2], "Sử dụng cho lịch BK000007");
    }

    private void CreateContacts()
    {
        ContactMessages.AddRange([
            new ContactMessage { ContactMessageId = 1, FullName = "Nguyễn Lan", PhoneNumber = "0909123123", Email = "lan@gmail.com", Topic = "Tư vấn spa", Message = "Mèo nhỏ có dùng gói spa VIP được không?", Status = "New", CreatedAt = DateTime.Now.AddHours(-2) },
            new ContactMessage { ContactMessageId = 2, FullName = "Trần Minh", PhoneNumber = "0918222333", Email = "minh@gmail.com", Topic = "Đổi lịch", Message = "Tôi muốn dời lịch sang cuối tuần.", Status = "Read", CreatedAt = DateTime.Now.AddDays(-1), AdminNote = "Đã gọi lại cho khách." },
            new ContactMessage { ContactMessageId = 3, FullName = "Phạm Ngọc", PhoneNumber = "0987000111", Email = "ngoc@gmail.com", Topic = "Bảng giá", Message = "Nhờ tư vấn giá cắt tỉa cho Poodle.", Status = "Replied", CreatedAt = DateTime.Now.AddDays(-2), RepliedAt = DateTime.Now.AddDays(-1), AdminNote = "Đã gửi bảng giá." }
        ]);
    }

    private ServiceCategory AddCategory(int id, string name)
    {
        var category = new ServiceCategory { CategoryId = id, CategoryName = name };
        Categories.Add(category);
        return category;
    }

    private void AddService(int id, ServiceCategory category, string name, string description, decimal price, int minutes, int capacity)
    {
        var service = new ServiceCatalog { ServiceId = id, CategoryId = category.CategoryId, Category = category, ServiceName = name, Description = description, BasePrice = price, EstimatedDuration = minutes, MaxCapacity = capacity, IsActive = true, IsDeleted = false };
        category.ServiceCatalogs.Add(service);
        Services.Add(service);
    }

    private void AddBreed(int id, PetSpecy species, string name)
    {
        var breed = new PetBreed { BreedId = id, SpeciesId = species.SpeciesId, Species = species, BreedName = name };
        species.PetBreeds.Add(breed);
        Breeds.Add(breed);
    }

    private void AddPet(int id, Customer customer, string name, PetSpecy species, PetBreed breed, decimal weight, string note)
    {
        var pet = new Pet { PetId = id, CustomerId = customer.CustomerId, Customer = customer, Name = name, SpeciesId = species.SpeciesId, Species = species, BreedId = breed.BreedId, PetBreed = breed, Weight = weight, Notes = note, CreatedAt = DateTime.Today.AddMonths(-2), IsDeleted = false };
        customer.Pets.Add(pet);
        species.Pets.Add(pet);
        breed.Pets.Add(pet);
        Pets.Add(pet);
    }

    private static Employee CreateEmployee(int id, string name, string phone, Role role, string? username, bool active = true)
    {
        var employee = new Employee { EmployeeId = id, FullName = name, PhoneNumber = phone, RoleId = role.RoleId, Role = role, IsActive = active, IsDeleted = false };
        role.Employees.Add(employee);
        if (username != null)
        {
            employee.Account = new Account { AccountId = 100 + id, Username = username, Password = "demo", RoleId = role.RoleId, Role = role, IsActive = active, Employee = employee };
        }
        return employee;
    }

    private Booking AddBooking(int id, Customer customer, Pet pet, ServiceCatalog service, DateTime date, int statusId, Employee? employee, string note)
    {
        var booking = new Booking { BookingId = id, BookingCode = $"BK{DateTime.Today:yyyyMMdd}{id:000000}", CustomerId = customer.CustomerId, Customer = customer, BookingDate = date, Notes = note, StatusId = statusId, Status = new BookingStatus { StatusId = statusId, StatusName = statusId.ToString() }, CreatedAt = date.AddDays(-2), IsDeleted = false };
        var detail = new BookingDetail { BookingDetailId = id, BookingId = id, Booking = booking, PetId = pet.PetId, Pet = pet, ServiceId = service.ServiceId, Service = service, ActualPrice = service.BasePrice, StatusId = statusId == 6 ? 2 : statusId, Status = new DetailStatus { StatusId = statusId, StatusName = statusId.ToString() } };
        if (statusId == 3)
        {
            detail.StartTime = date;
            detail.EndTime = date.AddMinutes(service.EstimatedDuration ?? 60);
        }
        if (employee != null)
        {
            var assignment = new BookingDetailEmployee { BookingDetailId = detail.BookingDetailId, BookingDetail = detail, EmployeeId = employee.EmployeeId, Employee = employee, AssignedAt = date.AddDays(-1) };
            detail.BookingDetailEmployees.Add(assignment);
            employee.BookingDetailEmployees.Add(assignment);
        }
        booking.BookingDetails.Add(detail);
        customer.Bookings.Add(booking);
        pet.BookingDetails.Add(detail);
        service.BookingDetails.Add(detail);
        BookingDetails.Add(detail);
        Bookings.Add(booking);
        return booking;
    }

    private void AddInvoice(Booking booking, decimal total, decimal discount, decimal paid, InvoiceStatus status, Promotion? promotion)
    {
        var invoice = new Invoice { InvoiceId = booking.BookingId, InvoiceCode = $"INV{DateTime.Today:yyyyMMdd}{booking.BookingId:0000}", BookingId = booking.BookingId, Booking = booking, PromotionId = promotion?.PromotionId, Promotion = promotion, TotalAmount = total, DiscountAmount = discount, PaidAmount = paid, StatusId = status.StatusId, Status = status, CreatedAt = booking.CreatedAt };
        booking.Invoice = invoice;
        status.Invoices.Add(invoice);
        promotion?.Invoices.Add(invoice);
        Invoices.Add(invoice);
    }

    private void AddReview(BookingDetail detail, Customer customer, int rating, string content, string tags, string reply)
    {
        var review = new ServiceReview { ReviewId = detail.BookingDetailId, BookingDetailId = detail.BookingDetailId, BookingDetail = detail, CustomerId = customer.CustomerId, Customer = customer, Rating = rating, Content = content, ReviewTags = tags, StoreReply = reply, IsVisible = true, CreatedAt = DateTime.Today.AddDays(-2) };
        detail.ServiceReview = review;
        customer.ServiceReviews.Add(review);
        Reviews.Add(review);
    }

    private MedicalSupply AddSupply(int id, string name, string unit, int quantity, int minimum, DateOnly expiry)
    {
        var supply = new MedicalSupply { SupplyId = id, SupplyName = name, Unit = unit, StockQuantity = quantity, MinStockLevel = minimum, ExpiryDate = expiry, IsDeleted = false, CreatedAt = DateTime.Today.AddMonths(-1) };
        Supplies.Add(supply);
        return supply;
    }

    private void AddQuota(ServiceCatalog service, MedicalSupply supply, int quantity)
    {
        var quota = new ServiceMaterialQuotum { ServiceId = service.ServiceId, Service = service, SupplyId = supply.SupplyId, Supply = supply, QuantityUsed = quantity };
        service.ServiceMaterialQuota.Add(quota);
        supply.ServiceMaterialQuota.Add(quota);
        MaterialQuotas.Add(quota);
    }

    private void AddTransaction(int id, MedicalSupply supply, string type, int quantity, Employee employee, string note)
    {
        var transaction = new InventoryTransaction { TransactionId = id, SupplyId = supply.SupplyId, Supply = supply, TransactionType = type, QuantityChange = quantity, EmployeeId = employee.EmployeeId, Employee = employee, Note = note, CreatedAt = DateTime.Now.AddDays(-id) };
        supply.InventoryTransactions.Add(transaction);
        employee.InventoryTransactions.Add(transaction);
        InventoryTransactions.Add(transaction);
    }
}
}

namespace PetcareWebsite.Models
{

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Role Role { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int CustomerId { get; set; }

    public DateTime BookingDate { get; set; }

    public string? Notes { get; set; }

    public int StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    public virtual BookingStatus Status { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class BookingDetail
{
    public int BookingDetailId { get; set; }

    public int BookingId { get; set; }

    public int PetId { get; set; }

    public int ServiceId { get; set; }

    public decimal ActualPrice { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int StatusId { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<BookingDetailEmployee> BookingDetailEmployees { get; set; } = new List<BookingDetailEmployee>();

    public virtual Pet Pet { get; set; } = null!;

    public virtual ServiceCatalog Service { get; set; } = null!;

    public virtual ServiceReview? ServiceReview { get; set; }

    public virtual DetailStatus Status { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class BookingDetailEmployee
{
    public int BookingDetailId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class BookingStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
}

namespace PetcareWebsite.Models
{

public partial class ContactMessage
{
    public int ContactMessageId { get; set; }

    public int? CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? Topic { get; set; }

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? RepliedAt { get; set; }

    public string? AdminNote { get; set; }

    public virtual Customer? Customer { get; set; }
}
}

namespace PetcareWebsite.Models
{

public partial class Customer
{
    public int CustomerId { get; set; }

    public int? AccountId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<ContactMessage> ContactMessages { get; set; } = new List<ContactMessage>();

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public virtual ICollection<ServiceReview> ServiceReviews { get; set; } = new List<ServiceReview>();
}
}

namespace PetcareWebsite.Models
{

public partial class DetailStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
}

namespace PetcareWebsite.Models
{

public partial class Employee
{
    public int EmployeeId { get; set; }

    public int? AccountId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<BookingDetailEmployee> BookingDetailEmployees { get; set; } = new List<BookingDetailEmployee>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual Role Role { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

namespace PetcareWebsite.Models
{

public partial class InventoryTransaction
{
    public int TransactionId { get; set; }

    public int SupplyId { get; set; }

    public string TransactionType { get; set; } = null!;

    public int QuantityChange { get; set; }

    public int? ReferenceId { get; set; }

    public string? Note { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual MedicalSupply Supply { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int BookingId { get; set; }

    public int? PromotionId { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public int StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual InvoiceStatus Status { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class InvoiceStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
}

namespace PetcareWebsite.Models
{

public partial class MedicalSupply
{
    public int SupplyId { get; set; }

    public string SupplyName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int? StockQuantity { get; set; }

    public int? MinStockLevel { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<ServiceMaterialQuotum> ServiceMaterialQuota { get; set; } = new List<ServiceMaterialQuotum>();
}
}

namespace PetcareWebsite.Models
{

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public int MethodId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? Note { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual PaymentMethod Method { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class PaymentMethod
{
    public int MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
}

namespace PetcareWebsite.Models
{

public partial class Pet
{
    public int PetId { get; set; }

    public int CustomerId { get; set; }

    public string Name { get; set; } = null!;

    public int SpeciesId { get; set; }

    public int BreedId { get; set; }

    public decimal? Weight { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual PetBreed PetBreed { get; set; } = null!;

    public virtual PetSpecy Species { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class PetBreed
{
    public int BreedId { get; set; }

    public int SpeciesId { get; set; }

    public string BreedName { get; set; } = null!;

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public virtual PetSpecy Species { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class PetSpecy
{
    public int SpeciesId { get; set; }

    public string SpeciesName { get; set; } = null!;

    public virtual ICollection<PetBreed> PetBreeds { get; set; } = new List<PetBreed>();

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
}

namespace PetcareWebsite.Models
{

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromoCode { get; set; } = null!;

    public string? DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public decimal? MinOrderValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
}

namespace PetcareWebsite.Models
{

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
}

namespace PetcareWebsite.Models
{

public partial class ServiceCatalog
{
    public int ServiceId { get; set; }

    public int CategoryId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public int? EstimatedDuration { get; set; }

    public int MaxCapacity { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual ICollection<ServiceMaterialQuotum> ServiceMaterialQuota { get; set; } = new List<ServiceMaterialQuotum>();
}
}

namespace PetcareWebsite.Models
{

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<ServiceCatalog> ServiceCatalogs { get; set; } = new List<ServiceCatalog>();
}
}

namespace PetcareWebsite.Models
{

public partial class ServiceMaterialQuotum
{
    public int ServiceId { get; set; }

    public int SupplyId { get; set; }

    public int? QuantityUsed { get; set; }

    public virtual ServiceCatalog Service { get; set; } = null!;

    public virtual MedicalSupply Supply { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class ServiceReview
{
    public int ReviewId { get; set; }

    public int BookingDetailId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Content { get; set; }

    public string? ReviewTags { get; set; }

    public string? StoreReply { get; set; }

    public bool? IsVisible { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual BookingDetail BookingDetail { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
}

namespace PetcareWebsite.Models
{

public partial class VwInvoiceDetail
{
    public int InvoiceId { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public string BookingCode { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal? BalanceAmount { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public DateTime? InvoiceDate { get; set; }
}
}

namespace PetcareWebsite.ViewModels
{

public class AdminDashboardViewModel
{
    public string AdminName { get; set; } = "Admin";

    public int TodayBookingCount { get; set; }

    public int PendingBookingCount { get; set; }

    public int CustomerCount { get; set; }

    public int PetCount { get; set; }

    public decimal MonthlyRevenue { get; set; }

    public int NewContactCount { get; set; }

    public int LowStockCount { get; set; }

    public List<string> WeekLabels { get; set; } = new();

    public List<int> WeeklyBookingCounts { get; set; } = new();

    public List<decimal> WeeklyRevenue { get; set; } = new();

    public List<AdminBookingSummaryViewModel> RecentBookings { get; set; } = new();

    public List<ContactMessage> RecentContacts { get; set; } = new();

    public List<MedicalSupply> LowStockSupplies { get; set; } = new();
}

public class AdminBookingSummaryViewModel
{
    public string BookingCode { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string PetName { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }

    public int StatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
}
}

namespace PetcareWebsite.ViewModels
{

public class AdminBookingsViewModel
{
    public string? Search { get; set; }

    public int? StatusId { get; set; }

    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int InProgressCount { get; set; }

    public int CompletedCount { get; set; }

    public int CancelledCount { get; set; }

    public int ExpiredCount { get; set; }

    public List<Booking> Bookings { get; set; } = new();

    public List<Employee> Employees { get; set; } = new();
}

public class AdminContactsViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int NewCount { get; set; }

    public int ReadCount { get; set; }

    public int RepliedCount { get; set; }

    public List<ContactMessage> Messages { get; set; } = new();
}

public class AdminInvoicesViewModel
{
    public string? Search { get; set; }

    public int? StatusId { get; set; }

    public int TotalCount { get; set; }

    public int UnpaidCount { get; set; }

    public int PartialCount { get; set; }

    public int PaidCount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public List<Invoice> Invoices { get; set; } = new();

    public List<PaymentMethod> PaymentMethods { get; set; } = new();

    public List<Promotion> Promotions { get; set; } = new();
}

public class AdminServicesViewModel
{
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }

    public List<ServiceCatalog> Services { get; set; } = new();

    public List<ServiceCategory> Categories { get; set; } = new();
}

public class AdminServiceEditorViewModel
{
    public int? ServiceId { get; set; }
    public int CategoryId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int? EstimatedDuration { get; set; }
    public int MaxCapacity { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public bool IsEditing => ServiceId.HasValue;

    public int BookingCount { get; set; }

    public List<ServiceCategory> Categories { get; set; } = new();
}

public class AdminInventoryViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int LowStockCount { get; set; }

    public int ExpiredCount { get; set; }

    public int ExpiringCount { get; set; }

    public List<MedicalSupply> Supplies { get; set; } = new();

    public List<InventoryTransaction> RecentTransactions { get; set; } = new();
}

public class AdminSupplyEditorViewModel
{
    public int? SupplyId { get; set; }
    public string SupplyName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int MinStockLevel { get; set; } = 5;

    public DateOnly? ExpiryDate { get; set; }

    public int StockQuantity { get; set; }

    public int TransactionCount { get; set; }

    public bool IsEditing => SupplyId.HasValue;
}

public class AdminSupplyImportViewModel
{
    public int SupplyId { get; set; }

    public string SupplyName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public int CurrentStock { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
}

public class AdminInventoryQuotasViewModel
{
    public int ServiceId { get; set; }
    public int SupplyId { get; set; }
    public int QuantityUsed { get; set; } = 1;

    public List<ServiceMaterialQuotum> Quotas { get; set; } = new();

    public List<ServiceCatalog> Services { get; set; } = new();

    public List<MedicalSupply> Supplies { get; set; } = new();
}

public class AdminEmployeesViewModel
{
    public string? Search { get; set; }

    public int? RoleId { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }

    public int AssignedUpcomingCount { get; set; }

    public List<Employee> Employees { get; set; } = new();

    public List<Role> Roles { get; set; } = new();
}

public class AdminEmployeeEditorViewModel
{
    public int? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool HasAccount { get; set; }

    public string? Username { get; set; }

    public int AssignmentCount { get; set; }

    public int UpcomingAssignmentCount { get; set; }

    public bool IsEditing => EmployeeId.HasValue;

    public List<Role> Roles { get; set; } = new();
}

public class AdminPromotionsViewModel
{
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int RunningCount { get; set; }

    public int ScheduledCount { get; set; }

    public int EndedCount { get; set; }

    public int InactiveCount { get; set; }

    public List<Promotion> Promotions { get; set; } = new();
}

public class AdminReviewsViewModel
{
    public string? Search { get; set; }

    public int? Rating { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }

    public int VisibleCount { get; set; }

    public int HiddenCount { get; set; }

    public int AwaitingReplyCount { get; set; }

    public int RepliedCount { get; set; }

    public decimal AverageRating { get; set; }

    public List<ServiceReview> Reviews { get; set; } = new();
}

public class AdminPromotionEditorViewModel
{
    public int? PromotionId { get; set; }
    public string PromoCode { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int InvoiceCount { get; set; }

    public bool IsEditing => PromotionId.HasValue;

    public bool HasUsage => InvoiceCount > 0;
}

public class AdminCustomersViewModel
{
    public string? Search { get; set; }

    public string? CustomerType { get; set; }

    public int TotalCount { get; set; }

    public int MemberCount { get; set; }

    public int GuestCount { get; set; }

    public int PetCount { get; set; }

    public List<Customer> Customers { get; set; } = new();
}

public class AdminCustomerDetailViewModel
{
    public Customer Customer { get; set; } = null!;

    public List<Booking> Bookings { get; set; } = new();

    public decimal TotalPaid { get; set; }
}

public class AdminCustomerEditorViewModel
{
    public int? CustomerId { get; set; }

    public int? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }

    public bool IsEditing => CustomerId.HasValue;

    public bool IsMember => AccountId.HasValue;
}

public class AdminPetEditorViewModel
{
    public int? PetId { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SpeciesId { get; set; }
    public int BreedId { get; set; }

    public decimal? Weight { get; set; }

    public string? Notes { get; set; }

    public bool IsEditing => PetId.HasValue;

    public List<PetSpecy> Species { get; set; } = new();

    public List<PetBreed> Breeds { get; set; } = new();
}

public class AdminBookingEditorViewModel
{
    public int? BookingId { get; set; }

    public string? BookingCode { get; set; }

    public int CustomerId { get; set; }

    public int PetId { get; set; }
    public int ServiceId { get; set; }
    public DateTime BookingDate { get; set; }

    public string? Notes { get; set; }

    public int StatusId { get; set; } = 2;

    public int? AssignedEmployeeId { get; set; }

    public bool IsEditing => BookingId.HasValue;

    public bool HasPayment { get; set; }

    public string CustomerMode { get; set; } = "Existing";
    public string? GuestFullName { get; set; }
    public string? GuestPhoneNumber { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPetName { get; set; }

    public int GuestSpeciesId { get; set; }

    public int GuestBreedId { get; set; }

    public decimal? GuestPetWeight { get; set; }

    public string? GuestPetNotes { get; set; }

    public List<Customer> Customers { get; set; } = new();

    public List<Pet> Pets { get; set; } = new();

    public List<ServiceCatalog> Services { get; set; } = new();

    public List<Employee> Employees { get; set; } = new();

    public List<PetSpecy> Species { get; set; } = new();

    public List<PetBreed> Breeds { get; set; } = new();
}
}
