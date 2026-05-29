using PetcareWebsite.Models;
using PetcareWebsite.Enums;
using PetcareWebsite.Validation;
using System.ComponentModel.DataAnnotations;

namespace PetcareWebsite.ViewModels;

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

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục dịch vụ.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(100, ErrorMessage = "Tên dịch vụ tối đa 100 ký tự.")]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [Range(1000, 100000000, ErrorMessage = "Giá dịch vụ phải từ 1,000 đ trở lên.")]
    public decimal BasePrice { get; set; }

    [Range(1, 1440, ErrorMessage = "Thời lượng phải từ 1 đến 1440 phút.")]
    public int? EstimatedDuration { get; set; }

    [Range(1, 100, ErrorMessage = "Sức chứa tối đa phải từ 1 đến 100 thú cưng.")]
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

    [Required(ErrorMessage = "Vui lòng nhập tên vật tư.")]
    [StringLength(100, ErrorMessage = "Tên vật tư tối đa 100 ký tự.")]
    public string SupplyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
    [StringLength(20, ErrorMessage = "Đơn vị tính tối đa 20 ký tự.")]
    public string Unit { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Mức tồn tối thiểu phải từ 0 đến 1,000,000.")]
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

    [Range(1, 1000000, ErrorMessage = "Số lượng nhập phải từ 1 đến 1,000,000.")]
    public int Quantity { get; set; } = 1;

    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }
}

public class AdminInventoryQuotasViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vật tư.")]
    public int SupplyId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Định mức phải từ 1 đến 1,000,000.")]
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

    [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
    [VietnamPhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn vai trò nhân viên.")]
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

    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
    [StringLength(20, ErrorMessage = "Mã khuyến mãi tối đa 20 ký tự.")]
    [PromotionCode]
    public string PromoCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại giảm giá.")]
    public string DiscountType { get; set; } = "Percentage";

    [Range(0.01, 100000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal DiscountValue { get; set; }

    [Range(0, 100000000, ErrorMessage = "Mức giảm tối đa không hợp lệ.")]
    public decimal? MaxDiscount { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ.")]
    public decimal? MinOrderValue { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
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

    [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
    [VietnamPhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? Email { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    public string? Address { get; set; }

    public bool IsEditing => CustomerId.HasValue;

    public bool IsMember => AccountId.HasValue;
}

public class AdminPetEditorViewModel
{
    public int? PetId { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên thú cưng.")]
    [StringLength(50, ErrorMessage = "Tên thú cưng tối đa 50 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại thú cưng.")]
    public int SpeciesId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giống thú cưng.")]
    public int BreedId { get; set; }

    [PetWeight]
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

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian hẹn.")]
    public DateTime BookingDate { get; set; }

    public string? Notes { get; set; }

    public int StatusId { get; set; } = (int)BookingStatusCode.Confirmed;

    public int? AssignedEmployeeId { get; set; }

    public bool IsEditing => BookingId.HasValue;

    public bool HasPayment { get; set; }

    public string CustomerMode { get; set; } = "Existing";

    [StringLength(100)]
    public string? GuestFullName { get; set; }

    [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
    [VietnamPhoneNumber]
    public string? GuestPhoneNumber { get; set; }

    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string? GuestEmail { get; set; }

    [StringLength(50)]
    public string? GuestPetName { get; set; }

    public int GuestSpeciesId { get; set; }

    public int GuestBreedId { get; set; }

    [PetWeight]
    public decimal? GuestPetWeight { get; set; }

    public string? GuestPetNotes { get; set; }

    public List<Customer> Customers { get; set; } = new();

    public List<Pet> Pets { get; set; } = new();

    public List<ServiceCatalog> Services { get; set; } = new();

    public List<Employee> Employees { get; set; } = new();

    public List<PetSpecy> Species { get; set; } = new();

    public List<PetBreed> Breeds { get; set; } = new();
}
