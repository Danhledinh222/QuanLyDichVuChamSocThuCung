using Microsoft.AspNetCore.Mvc;

namespace PetcareWebsite.Controllers;

public class LegacyController : Controller
{
    private static readonly IReadOnlyDictionary<string, string> Views =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["index"] = "~/Views/Home/Index.cshtml",
            ["contact"] = "~/Views/Home/Contact.cshtml",
            ["dichvu"] = "~/Views/Service/Index.cshtml",
            ["detailservice"] = "~/Views/Service/Detail.cshtml",
            ["review"] = "~/Views/Service/Review.cshtml",
            ["pets"] = "~/Views/Pet/Index.cshtml",
            ["pet-detail"] = "~/Views/Pet/Detail.cshtml",
            ["history"] = "~/Views/Pet/History.cshtml",
            ["booking"] = "~/Views/Booking/Index.cshtml",
            ["time"] = "~/Views/Booking/Time.cshtml",
            ["xacnhan"] = "~/Views/Booking/Confirm.cshtml",
            ["thankyou"] = "~/Views/Booking/Success.cshtml",
            ["bookingdetail"] = "~/Views/Booking/Detail.cshtml",
            ["profile"] = "~/Views/Profile/Index.cshtml",
            ["customer-billing"] = "~/Views/Profile/Billing.cshtml",
            ["customer-reviews"] = "~/Views/Profile/Reviews.cshtml",
            ["login"] = "~/Views/Account/Login.cshtml",
            ["register"] = "~/Views/Account/Register.cshtml",
            ["forgot-password"] = "~/Views/Account/ForgotPassword.cshtml",
            ["admin-dashboard"] = "~/Views/Admin/Index.cshtml",
            ["admin-bookings"] = "~/Views/Admin/Bookings.cshtml",
            ["admin-customers"] = "~/Views/Admin/Customers.cshtml",
            ["admin-services"] = "~/Views/Admin/Services.cshtml",
            ["admin-billing"] = "~/Views/Admin/Invoices.cshtml",
            ["admin-inventory"] = "~/Views/Admin/Inventory.cshtml",
            ["admin-promotions"] = "~/Views/Admin/Promotions.cshtml",
            ["admin-reviews"] = "~/Views/Admin/Reviews.cshtml",
            ["admin-employees"] = "~/Views/Admin/Employees.cshtml",
            ["pricing"] = "~/Views/Service/Index.cshtml",
            ["services"] = "~/Views/Service/Index.cshtml",
            ["payment"] = "~/Views/Profile/Billing.cshtml",
            ["reviews"] = "~/Views/Profile/Reviews.cshtml"
        };

    public IActionResult Page(string page)
    {
        return Views.TryGetValue(page, out var viewPath)
            ? View(viewPath)
            : NotFound();
    }
}