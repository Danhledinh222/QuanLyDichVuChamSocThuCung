using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models; // Lưu ý sửa lại đúng namespace theo tên project của sếp
using Microsoft.AspNetCore.Http;

namespace PetcareWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly PetCareDbContext _context;

        public AccountController(PetCareDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GIAO DIỆN ĐĂNG NHẬP (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì không cho vào trang Login nữa, đá về trang chủ
            if (HttpContext.Session.GetAccountId() != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ==========================================
        // 2. XỬ LÝ ĐĂNG NHẬP (POST) - ĐÃ FIX HỖ TRỢ CẢ EMAIL & SĐT
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Sếp ơi, vui lòng nhập đủ tài khoản và mật khẩu nhé!";
                return View();
            }

            Account? account = null;

            // KIỂM TRA KHÁCH NHẬP EMAIL HAY SỐ ĐIỆN THOẠI
            if (username.Contains("@"))
            {
                // 1. Nếu có dấu "@" -> Tìm Email trong bảng Customer trước
                var customerInfo = await _context.Customers.FirstOrDefaultAsync(c => c.Email == username && c.IsDeleted == false);

                if (customerInfo != null && customerInfo.AccountId != null)
                {
                    // Nếu thấy khách, móc AccountId sang bảng Account để check Mật khẩu
                    account = await _context.Accounts
                        .Include(a => a.Role)
                        .FirstOrDefaultAsync(a => a.AccountId == customerInfo.AccountId
                                               && a.Password == password
                                               && a.IsActive == true
                                               && a.IsDeleted == false);
                }
            }
            else
            {
                // 2. Nếu không có "@" (Là SĐT) -> Tìm thẳng trong cột Username của bảng Account
                account = await _context.Accounts
                    .Include(a => a.Role)
                    .FirstOrDefaultAsync(a => a.Username == username
                                           && a.Password == password
                                           && a.IsActive == true
                                           && a.IsDeleted == false);
            }

            // Nếu tìm cả 2 đường mà vẫn null -> Báo lỗi
            if (account == null)
            {
                TempData["ErrorMessage"] = "Tài khoản hoặc mật khẩu không đúng, hoặc đã bị khóa!";
                return View();
            }

            // --- NẾU THÀNH CÔNG: LƯU SESSION VÀ PHÂN LUỒNG NHƯ CŨ ---
            HttpContext.Session.SetInt32("AccountId", account.AccountId);
            HttpContext.Session.SetInt32("RoleId", account.RoleId);
            HttpContext.Session.SetString("RoleName", account.Role?.RoleName ?? "");

            if (account.RoleId == (int)SystemRoleCode.Customer)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountId == account.AccountId);
                if (customer != null)
                {
                    HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                    HttpContext.Session.SetString("CustomerName", customer.FullName);
                }
                return RedirectToAction("Index", "Home");
            }
            else // Admin/Bác sĩ/Groomer
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AccountId == account.AccountId);
                if (employee != null)
                {
                    HttpContext.Session.SetInt32("EmployeeId", employee.EmployeeId);
                    HttpContext.Session.SetString("EmployeeName", employee.FullName);
                }

                if (account.RoleId == (int)SystemRoleCode.Admin)
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa sạch bộ nhớ đăng nhập
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. GIAO DIỆN ĐĂNG KÝ (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            // Nếu khách đã đăng nhập rồi thì đá về trang chủ
            if (HttpContext.Session.GetAccountId() != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ==========================================
        // 5. XỬ LÝ ĐĂNG KÝ (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string phoneNumber, string email, string password, string confirmPassword)
        {
            fullName = fullName?.Trim() ?? string.Empty;
            phoneNumber = PhoneNumberHelper.Normalize(phoneNumber);
            email = email?.Trim() ?? string.Empty;

            // 1. Kiểm tra dữ liệu rỗng
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường bắt buộc.";
                return View();
            }

            if (!PhoneNumberHelper.IsValid(phoneNumber))
            {
                TempData["ErrorMessage"] = "Số điện thoại chỉ gồm 9 đến 15 chữ số.";
                return View();
            }

            // 2. Kiểm tra mật khẩu khớp nhau
            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

            if (existingCustomer?.AccountId != null)
            {
                TempData["ErrorMessage"] = "Số điện thoại này đã có tài khoản. Vui lòng đăng nhập.";
                return View();
            }

            // Số điện thoại có hồ sơ khách vãng lai vẫn được đăng ký tài khoản.
            var existingAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == phoneNumber);
            if (existingAccount != null)
            {
                TempData["ErrorMessage"] = "Số điện thoại này đã được đăng ký tài khoản rồi.";
                return View();
            }

            // Email của chính hồ sơ vãng lai được phép sử dụng lại khi gắn tài khoản.
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail = await _context.Customers.FirstOrDefaultAsync(c =>
                    c.Email == email
                    && (existingCustomer == null || c.CustomerId != existingCustomer.CustomerId));
                if (existingEmail != null)
                {
                    TempData["ErrorMessage"] = "Email này đã được sử dụng cho một hồ sơ khác.";
                    return View();
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newAccount = new Account
                {
                    Username = phoneNumber,
                    Password = password,
                    RoleId = (int)SystemRoleCode.Customer,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };
                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                var linkedExistingProfile = existingCustomer != null;
                if (existingCustomer == null)
                {
                    _context.Customers.Add(new Customer
                    {
                        AccountId = newAccount.AccountId,
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    });
                }
                else
                {
                    existingCustomer.AccountId = newAccount.AccountId;
                    existingCustomer.FullName = fullName;
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        existingCustomer.Email = email;
                    }

                    existingCustomer.ModifiedAt = DateTime.Now;
                    existingCustomer.IsDeleted = false;
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = linkedExistingProfile
                    ? "Tạo tài khoản thành công. Lịch sử đặt lịch trước đây đã được giữ trong hồ sơ của bạn."
                    : "Tạo tài khoản thành công. Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể tạo tài khoản lúc này. Vui lòng kiểm tra thông tin và thử lại.";
                return View();
            }
        }
    // ==========================================
        // 6. GIAO DIỆN QUÊN MẬT KHẨU (GET)
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // Nếu đã đăng nhập rồi thì không cho vào trang này
            if (HttpContext.Session.GetAccountId() != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ==========================================
        // 7. XỬ LÝ QUÊN MẬT KHẨU (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Sếp ơi, vui lòng nhập địa chỉ Email nhé!";
                return View();
            }

            // 1. Tìm xem Email này có trong bảng Customer không
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email && c.IsDeleted == false);

            if (customer == null || customer.AccountId == null)
            {
                TempData["ErrorMessage"] = "Email này chưa được đăng ký trong hệ thống!";
                return View();
            }

            // 2. Lấy tài khoản Account tương ứng ra
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == customer.AccountId);

            if (account != null)
            {
                // [Giả lập] Reset mật khẩu về mặc định cho dễ test đồ án
                account.Password = "123456";
                await _context.SaveChangesAsync();

                // Bắn thông báo màu xanh sang trang Login
                TempData["SuccessMessage"] = "Đã tìm thấy tài khoản! (Chế độ test đồ án: Mật khẩu của sếp đã được Reset về '123456')";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra, không thể khôi phục tài khoản lúc này!";
            return View();
        }
    } 
}
