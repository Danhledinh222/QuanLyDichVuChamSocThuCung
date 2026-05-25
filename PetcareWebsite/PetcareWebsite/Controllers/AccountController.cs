using Microsoft.AspNetCore.Mvc;

namespace PetcareWebsite.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        HttpContext.Session.SetInt32("DemoSessionReady", 1);
        HttpContext.Session.SetInt32("AccountId", 1);

        if (username.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetInt32("RoleId", 1);
            HttpContext.Session.SetString("EmployeeName", "Lê Đình Danh");
            return RedirectToAction("Index", "Admin");
        }

        HttpContext.Session.SetInt32("RoleId", 4);
        HttpContext.Session.SetString("CustomerName", "Danh dz");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("DemoSessionReady", 1);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public IActionResult Register(string fullName)
    {
        TempData["SuccessMessage"] = $"Đã tạo giao diện tài khoản mẫu cho {fullName}.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public IActionResult ForgotPassword(string email)
    {
        TempData["SuccessMessage"] = $"Yêu cầu khôi phục cho {email} đã được mô phỏng.";
        return RedirectToAction(nameof(Login));
    }
}
