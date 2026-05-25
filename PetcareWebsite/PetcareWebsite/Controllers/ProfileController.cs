using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;

namespace PetcareWebsite.Controllers;

public class ProfileController(DemoStore store) : Controller
{
    public IActionResult Index() => View(store.MemberCustomer);

    [HttpPost]
    public IActionResult Update()
    {
        TempData["SuccessMessage"] = "Hồ sơ đã được cập nhật trong bản trình diễn.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Reviews()
    {
        var reviews = store.Reviews.Where(review => review.CustomerId == store.MemberCustomer.CustomerId).ToList();
        var pending = store.MemberCustomer.Bookings
            .Where(booking => booking.StatusId == 3)
            .SelectMany(booking => booking.BookingDetails)
            .Where(detail => detail.ServiceReview == null)
            .ToList();
        ViewBag.Customer = store.MemberCustomer;
        ViewBag.PendingReviewDetails = pending;
        ViewBag.ReviewedCount = reviews.Count;
        ViewBag.PendingCount = pending.Count;
        ViewBag.AverageRating = reviews.Any() ? reviews.Average(review => review.Rating) : 0;
        return View(reviews);
    }

    [HttpPost]
    public IActionResult SaveReview()
    {
        TempData["SuccessMessage"] = "Đánh giá đã được lưu trong bản trình diễn.";
        return RedirectToAction(nameof(Reviews));
    }

    public IActionResult Billing()
    {
        var invoices = store.Invoices.Where(invoice => invoice.Booking.CustomerId == store.MemberCustomer.CustomerId).ToList();
        ViewBag.Customer = store.MemberCustomer;
        ViewBag.TotalInvoices = invoices.Count;
        ViewBag.TotalAmount = invoices.Sum(invoice => invoice.TotalAmount ?? 0);
        ViewBag.PaidAmount = invoices.Sum(invoice => invoice.PaidAmount ?? 0);
        ViewBag.BalanceAmount = invoices
            .Where(invoice => invoice.Booking.StatusId != 4 && invoice.Booking.StatusId != 5)
            .Sum(invoice => (invoice.TotalAmount ?? 0) - (invoice.DiscountAmount ?? 0) - (invoice.PaidAmount ?? 0));
        return View(invoices);
    }
}
