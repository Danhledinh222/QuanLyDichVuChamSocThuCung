using PetcareWebsite.Models;

namespace PetcareWebsite.ViewModels;

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
