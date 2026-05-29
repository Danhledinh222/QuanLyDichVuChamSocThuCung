using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Services;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models;
using Microsoft.AspNetCore.Http;

namespace PetcareWebsite.Controllers
{
    public class ProfileController : Controller
    {
        private const int BookingStatusCompleted = (int)BookingStatusCode.Completed;
        private const int BookingStatusCancelled = (int)BookingStatusCode.Cancelled;
        private const int BookingStatusExpired = (int)BookingStatusCode.Expired;

        private readonly PetCareDbContext _context;
        private readonly IBookingBusinessService _bookingBusiness;

        public ProfileController(PetCareDbContext context, IBookingBusinessService bookingBusiness)
        {
            _context = context;
            _bookingBusiness = bookingBusiness;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsDeleted == false);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string FullName, string PhoneNumber, string Email, string Address)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            PhoneNumber = PhoneNumberHelper.Normalize(PhoneNumber);
            if (!PhoneNumberHelper.IsValid(PhoneNumber))
            {
                TempData["ErrorMessage"] = "Số điện thoại chỉ gồm 9 đến 15 chữ số.";
                return RedirectToAction("Index");
            }

            var customer = await _context.Customers
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer != null)
            {
                if (customer.PhoneNumber != PhoneNumber)
                {
                    var phoneExists = await _context.Accounts.AnyAsync(a => a.Username == PhoneNumber);
                    if (phoneExists)
                    {
                        TempData["ErrorMessage"] = "Số điện thoại này đã được sử dụng bởi một tài khoản khác!";
                        return RedirectToAction("Index");
                    }

                    customer.PhoneNumber = PhoneNumber;

                    if (customer.Account != null)
                    {
                        customer.Account.Username = PhoneNumber;
                    }
                }

                customer.FullName = FullName;
                customer.Email = Email;
                customer.Address = Address;

                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("CustomerName", FullName);

                TempData["SuccessMessage"] = "Hồ sơ đã được cập nhật thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.IsDeleted == false);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var reviews = await _context.ServiceReviews
                .Include(r => r.BookingDetail)
                    .ThenInclude(bd => bd.Booking)
                .Include(r => r.BookingDetail)
                    .ThenInclude(bd => bd.Pet)
                    .ThenInclude(p => p.PetBreed)
                .Include(r => r.BookingDetail)
                    .ThenInclude(bd => bd.Service)
                .Where(r => r.CustomerId == customerId.Value)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var pendingReviewDetails = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Include(bd => bd.Pet)
                    .ThenInclude(p => p.PetBreed)
                .Include(bd => bd.Service)
                .Include(bd => bd.ServiceReview)
                .Where(bd => bd.Booking.CustomerId == customerId.Value
                             && bd.Booking.IsDeleted != true
                             && bd.Booking.StatusId != BookingStatusCancelled
                             && bd.Booking.StatusId != BookingStatusExpired
                             && bd.StatusId != BookingStatusCancelled
                             && bd.ServiceReview == null
                             && (bd.StatusId == BookingStatusCompleted || bd.Booking.StatusId == BookingStatusCompleted))
                .OrderByDescending(bd => bd.Booking.BookingDate)
                .ToListAsync();

            ViewBag.Customer = customer;
            ViewBag.PendingReviewDetails = pendingReviewDetails;
            ViewBag.ReviewedCount = reviews.Count;
            ViewBag.PendingCount = pendingReviewDetails.Count;
            ViewBag.AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReview(int bookingDetailId, int rating, string? content, string[]? reviewTags)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction(nameof(Reviews));
            }

            var bookingDetail = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Include(bd => bd.ServiceReview)
                .FirstOrDefaultAsync(bd =>
                    bd.BookingDetailId == bookingDetailId &&
                    bd.Booking.CustomerId == customerId.Value &&
                    bd.Booking.IsDeleted != true);

            if (bookingDetail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ cần đánh giá.";
                return RedirectToAction(nameof(Reviews));
            }

            if (!_bookingBusiness.CanReview(bookingDetail))
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá dịch vụ đã hoàn thành.";
                return RedirectToAction(nameof(Reviews));
            }

            var normalizedTags = reviewTags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            var reviewContent = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
            var reviewTagText = normalizedTags.Any() ? string.Join(", ", normalizedTags) : null;

            if (bookingDetail.ServiceReview == null)
            {
                _context.ServiceReviews.Add(new ServiceReview
                {
                    BookingDetailId = bookingDetail.BookingDetailId,
                    CustomerId = customerId.Value,
                    Rating = rating,
                    Content = reviewContent,
                    ReviewTags = reviewTagText,
                    IsVisible = true,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                bookingDetail.ServiceReview.Rating = rating;
                bookingDetail.ServiceReview.Content = reviewContent;
                bookingDetail.ServiceReview.ReviewTags = reviewTagText;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu đánh giá dịch vụ thành công.";
            return RedirectToAction(nameof(Reviews));
        }

        [HttpGet]
        public async Task<IActionResult> Billing()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.IsDeleted == false);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var invoices = await _context.Invoices
                .Include(i => i.Status)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.Method)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Pet)
                .Where(i => i.Booking.CustomerId == customerId.Value && i.Booking.IsDeleted != true)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.Customer = customer;
            ViewBag.TotalInvoices = invoices.Count;
            ViewBag.TotalAmount = invoices.Sum(i => i.TotalAmount ?? 0);
            ViewBag.PaidAmount = invoices.Sum(i => i.PaidAmount ?? 0);
            ViewBag.BalanceAmount = invoices
                .Where(i => i.Booking.StatusId != BookingStatusCancelled &&
                            i.Booking.StatusId != BookingStatusExpired)
                .Sum(i => (i.TotalAmount ?? 0) - (i.DiscountAmount ?? 0) - (i.PaidAmount ?? 0));

            return View(invoices);
        }
    }
}
