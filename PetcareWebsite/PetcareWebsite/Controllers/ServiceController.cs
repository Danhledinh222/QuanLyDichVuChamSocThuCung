using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Models; // Đảm bảo đúng namespace Models của dự án

namespace PetcareWebsite.Controllers
{
    public class ServiceController : Controller
    {
        private readonly PetCareDbContext _context;

        public ServiceController(PetCareDbContext context)
        {
            _context = context;
        }

        // Action hiển thị danh sách dịch vụ
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách dịch vụ từ DB, kèm thông tin Danh mục
            var services = await _context.ServiceCatalogs
                .Include(s => s.Category)
                .Where(s => s.IsActive == true && s.IsDeleted == false)
                .ToListAsync();

            return View(services);
        }

        // Action xem chi tiết một dịch vụ (Chuẩn bị sẵn cho trang sau)
        public async Task<IActionResult> Detail(int id)
        {
            var service = await _context.ServiceCatalogs
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.ServiceId == id && s.IsActive == true && s.IsDeleted == false);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }
    }
}