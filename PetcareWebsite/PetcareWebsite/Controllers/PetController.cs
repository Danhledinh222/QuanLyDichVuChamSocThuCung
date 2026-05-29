using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Models;
using Microsoft.AspNetCore.Http;

namespace PetcareWebsite.Controllers
{
    public class PetController : Controller
    {
        private const int BookingStatusCompleted = (int)BookingStatusCode.Completed;
        private const int BookingStatusCancelled = (int)BookingStatusCode.Cancelled;
        private const int BookingStatusExpired = (int)BookingStatusCode.Expired;

        private readonly PetCareDbContext _context;

        public PetController(PetCareDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GIAO DIỆN CHÍNH: DANH SÁCH THÚ CƯNG (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách thú cưng của khách hàng này
            var myPets = await _context.Pets
                .Include(p => p.Species)
                .Include(p => p.PetBreed) // Gắn kèm thông tin giống loài
                .Where(p => p.CustomerId == customerId && p.IsDeleted == false)
                .ToListAsync();

            // Load sẵn danh sách Loài (Chó/Mèo) truyền vào Modal Thêm/Sửa
            ViewBag.SpeciesList = await _context.PetSpecies.ToListAsync();
            var now = DateTime.Now;
            int upcomingCount = await _context.Bookings
                .Where(b => b.CustomerId == customerId
                            && b.BookingDate >= now
                            && b.StatusId != BookingStatusCompleted
                            && b.StatusId != BookingStatusCancelled
                            && b.StatusId != BookingStatusExpired
                            && b.IsDeleted == false)
                .CountAsync();

            ViewBag.UpcomingCount = upcomingCount;
            ViewBag.LatestService = await _context.BookingDetails
                .Where(bd => bd.Booking.CustomerId == customerId
                             && bd.Booking.StatusId == BookingStatusCompleted
                             && bd.Booking.IsDeleted == false)
                .OrderByDescending(bd => bd.Booking.BookingDate)
                .Select(bd => bd.Service.ServiceName)
                .FirstOrDefaultAsync() ?? "Chưa có dữ liệu";

            var mostBookedPetId = await _context.BookingDetails
                .Where(bd => bd.Booking.CustomerId == customerId
                             && bd.Booking.StatusId == BookingStatusCompleted
                             && bd.Booking.IsDeleted == false
                             && bd.Pet.IsDeleted == false)
                .GroupBy(bd => bd.PetId)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefaultAsync();
            ViewBag.MostVisitedPet = myPets.FirstOrDefault(p => p.PetId == mostBookedPetId)?.Name ?? "Chưa có dữ liệu";

            ViewBag.RecentActivities = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Include(bd => bd.Pet)
                .Include(bd => bd.Service)
                .Where(bd => bd.Booking.CustomerId == customerId
                             && bd.Booking.IsDeleted == false
                             && bd.Pet.IsDeleted == false)
                .OrderByDescending(bd => bd.Booking.BookingDate)
                .Take(5)
                .ToListAsync();

            return View(myPets);
        }

        // ==========================================
        // API AJAX: LOAD GIỐNG THEO LOÀI (Dùng cho JavaScript)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetBreedsBySpecies(int speciesId)
        {
            var breeds = await _context.PetBreeds
                .Where(b => b.SpeciesId == speciesId)
                .Select(b => new { b.BreedId, b.BreedName })
                .ToListAsync();
            return Json(breeds);
        }

        // ==========================================
        // API AJAX: LẤY CHI TIẾT 1 BÉ THÚ CƯNG (Để đổ dữ liệu lên Modal Sửa)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetPetDetail(int id)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            var pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.PetId == id && p.CustomerId == customerId && p.IsDeleted == false);

            if (pet == null) return NotFound();

            return Json(new
            {
                petId = pet.PetId,
                name = pet.Name,
                speciesId = pet.SpeciesId,
                breedId = pet.BreedId,
                weight = pet.Weight,
                notes = pet.Notes
            });
        }

        // ==========================================
        // 2. XỬ LÝ THÊM HOẶC CẬP NHẬT THÚ CƯNG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> SavePet(int? PetId, string Name, int SpeciesId, int BreedId, decimal Weight, string Notes)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(Name) || Weight <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng điền tên và cân nặng hợp lệ cho thú cưng.";
                return RedirectToAction("Index");
            }

            try
            {
                if (PetId.HasValue && PetId.Value > 0)
                {
                    // LUỒNG CẬP NHẬT (SỬA)
                    var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == PetId.Value && p.CustomerId == customerId);
                    if (pet != null)
                    {
                        pet.Name = Name;
                        pet.SpeciesId = SpeciesId;
                        pet.BreedId = BreedId;
                        pet.Weight = Weight;
                        pet.Notes = Notes;
                        pet.ModifiedAt = DateTime.Now;

                        TempData["SuccessMessage"] = $"Đã cập nhật thông tin bé {Name} thành công!";
                    }
                }
                else
                {
                    // LUỒNG THÊM MỚI
                    var newPet = new Pet
                    {
                        CustomerId = customerId.Value,
                        Name = Name,
                        SpeciesId = SpeciesId,
                        BreedId = BreedId,
                        Weight = Weight,
                        Notes = Notes,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };
                    _context.Pets.Add(newPet);
                    TempData["SuccessMessage"] = $"Đã thêm bé {Name} vào hồ sơ gia đình!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. XỬ LÝ XÓA MỀM THÚ CƯNG (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> DeletePet(int id)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == id && p.CustomerId == customerId);
            if (pet != null)
            {
                pet.IsDeleted = true;
                pet.ModifiedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã xóa bé {pet.Name} khỏi danh sách." });
            }

            return Json(new { success = false, message = "Không tìm thấy thú cưng!" });
        }

        // ==========================================
        // 4. TRANG CHI TIẾT THÚ CƯNG (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null) return RedirectToAction("Login", "Account");

            // 1. Kéo thông tin chi tiết của Thú cưng
            var pet = await _context.Pets
                .Include(p => p.Species)
                .Include(p => p.PetBreed)
                .FirstOrDefaultAsync(p => p.PetId == id && p.CustomerId == customerId && p.IsDeleted == false);

            if (pet == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thú cưng!";
                return RedirectToAction("Index");
            }

            var history = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Include(bd => bd.Service)
                .Where(bd => bd.PetId == id && bd.Booking.IsDeleted == false)
                .OrderByDescending(bd => bd.Booking.BookingDate)
                .ToListAsync();

            ViewBag.History = history;
            ViewBag.SpeciesList = await _context.PetSpecies.ToListAsync();
            return View(pet);
        }
        // ==========================================
        // XEM LỊCH SỬ ĐẶT LỊCH (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kéo toàn bộ Booking, kèm theo Chi tiết, Pet, và Dịch vụ
            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Pet) // Móc Pet
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service) // Móc Dịch vụ
                .Where(b => b.CustomerId == customerId && b.IsDeleted == false)
                .OrderByDescending(b => b.BookingDate) // Lịch gần nhất lên đầu
                .ToListAsync();

            // Tính toán 3 thẻ thống kê theo trạng thái nghiệp vụ, không tự hoàn thành theo ngày.
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.CompletedBookings = bookings.Count(b => b.StatusId == BookingStatusCompleted || b.BookingDetails.Any(bd => bd.StatusId == BookingStatusCompleted));
            ViewBag.UpcomingBookings = bookings.Count(b =>
                b.StatusId != BookingStatusCompleted &&
                b.StatusId != BookingStatusCancelled &&
                b.StatusId != BookingStatusExpired &&
                b.BookingDate >= DateTime.Now);

            return View(bookings);
        }
    }
}
