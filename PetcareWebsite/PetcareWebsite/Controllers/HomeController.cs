using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models;
using System.Diagnostics;

namespace PetcareWebsite.Controllers
{
    public class HomeController : Controller
    {


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private readonly PetCareDbContext _context;

        public HomeController(PetCareDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _context.ServiceCatalogs
                .Where(s => s.IsActive == true && s.IsDeleted == false)
                .Take(4)
                .ToListAsync();

            return View(services);
        }

        [HttpGet]
        public async Task<IActionResult> Pricing()
        {
            var services = await _context.ServiceCatalogs
                .Include(s => s.Category)
                .Where(s => s.IsActive == true && s.IsDeleted == false)
                .OrderBy(s => s.Category.CategoryName)
                .ThenBy(s => s.BasePrice)
                .ToListAsync();

            return View(services);
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContact(string fullName, string phoneNumber, string email, string topic, string message, string? returnUrl)
        {
            var redirectUrl = GetContactRedirectUrl(returnUrl);

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(phoneNumber) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ContactError"] = "Vui lòng nhập họ tên, số điện thoại và nội dung cần tư vấn.";
                return Redirect(redirectUrl);
            }

            phoneNumber = PhoneNumberHelper.Normalize(phoneNumber);
            if (!PhoneNumberHelper.IsValid(phoneNumber))
            {
                TempData["ContactError"] = "Số điện thoại chỉ gồm 9 đến 15 chữ số.";
                return Redirect(redirectUrl);
            }

            var customerId = HttpContext.Session.GetCustomerId();
            var contactMessage = new ContactMessage
            {
                CustomerId = customerId,
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Topic = string.IsNullOrWhiteSpace(topic) ? "Tư vấn dịch vụ" : topic.Trim(),
                Message = message.Trim(),
                Status = "New",
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "PetCare đã nhận thông tin liên hệ. Nhân viên sẽ gọi lại cho bạn trong thời gian sớm nhất.";
            return Redirect(redirectUrl);
        }

        private string GetContactRedirectUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action(nameof(Contact), "Home") ?? "/Home/Contact";
        }
    }

}
