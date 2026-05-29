using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Services;
using PetcareWebsite.Enums;
using PetcareWebsite.Extensions;
using PetcareWebsite.Helpers;
using PetcareWebsite.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PetcareWebsite.Controllers
{
    public class BookingController : Controller
    {
        private const int BookingStatusPending = (int)BookingStatusCode.Pending;
        private const int BookingStatusConfirmed = (int)BookingStatusCode.Confirmed;
        private const int BookingStatusCompleted = (int)BookingStatusCode.Completed;
        private const int BookingStatusCancelled = (int)BookingStatusCode.Cancelled;
        private const int BookingStatusExpired = (int)BookingStatusCode.Expired;
        private const int DetailStatusNotStarted = (int)DetailStatusCode.NotStarted;
        private static readonly string[] CustomerTimeSlots =
        {
            "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00",
            "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00"
        };

        private readonly PetCareDbContext _context;
        private readonly IBookingBusinessService _bookingBusiness;
        private readonly IInvoiceBusinessService _invoiceBusiness;

        public BookingController(
            PetCareDbContext context,
            IBookingBusinessService bookingBusiness,
            IInvoiceBusinessService invoiceBusiness)
        {
            _context = context;
            _bookingBusiness = bookingBusiness;
            _invoiceBusiness = invoiceBusiness;
        }

        // ==========================================
        // BƯỚC 1: NHẬP THÔNG TIN (INDEX)
        // ==========================================
        public async Task<IActionResult> Index(int? serviceId)
        {
            int? currentCustomerId = HttpContext.Session.GetCustomerId();

            if (currentCustomerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(currentCustomerId.Value);
                var myPets = await _context.Pets
                    .Include(p => p.PetBreed)
                    .Where(p => p.CustomerId == currentCustomerId.Value && p.IsDeleted == false)
                    .ToListAsync();

                ViewBag.IsLoggedIn = true;
                ViewBag.Customer = customer;
                ViewBag.MyPets = myPets;
            }
            else
            {
                ViewBag.IsLoggedIn = false;
            }

            ViewBag.Services = await _context.ServiceCatalogs
                .Include(s => s.Category)
                .Where(s => s.IsActive == true && s.IsDeleted == false)
                .ToListAsync();
            ViewBag.Breeds = await _context.PetBreeds.ToListAsync();
            ViewBag.SelectedServiceId = serviceId;

            return View();
        }

        // ==========================================
        // BƯỚC 2: GIAO DIỆN CHỌN THỜI GIAN (TIME)
        // ==========================================
        [HttpGet]
        public IActionResult Time(
            int? customerId, string customerName, string customerPhone, string customerEmail,
            int? petId, string petType, string petName, int? breedId, decimal? petWeight, string petAge, string petGender,
            string memberPetType, string memberPetName, int? memberBreedId, decimal? memberPetWeight, string memberPetAge, string memberPetGender,
            int serviceId, string petNote)
        {
            // Bảo lưu dữ liệu truyền từ form Bước 1
            ViewBag.CustomerId = customerId;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerPhone = customerPhone;
            ViewBag.CustomerEmail = customerEmail;
            ViewBag.ServiceId = serviceId;
            ViewBag.PetNote = petNote;

            // Xử lý Thú Cưng (Khách vãng lai / Thành viên)
            ViewBag.PetId = petId;
            if (petId == 0 && customerId != null)
            {
                ViewBag.PetType = memberPetType;
                ViewBag.PetName = memberPetName;
                ViewBag.BreedId = memberBreedId;
                ViewBag.PetWeight = memberPetWeight;
                ViewBag.PetAge = memberPetAge;
                ViewBag.PetGender = memberPetGender;
            }
            else
            {
                ViewBag.PetType = petType;
                ViewBag.PetName = petName;
                ViewBag.BreedId = breedId;
                ViewBag.PetWeight = petWeight;
                ViewBag.PetAge = petAge;
                ViewBag.PetGender = petGender;
            }

            return View();
        }

        // ==========================================
        // API BƯỚC 2: GET GIỜ ĐÃ KÍN (DÂN SỰ ĐỘNG + CÔNG SUẤT DỊCH VỤ)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetBookedTimes(DateTime date, int serviceId)
        {
            var unavailableTimes = await _bookingBusiness.GetUnavailableTimesAsync(
                date,
                serviceId,
                CustomerTimeSlots);

            return Json(unavailableTimes);
        }
        // ==========================================
        // BƯỚC 3: XÁC NHẬN & THANH TOÁN (CONFIRM)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Confirm(
            int? customerId, string customerName, string customerPhone, string? customerEmail,
            int? petId, string petType, string petName, int? breedId, decimal? petWeight, string petAge, string petGender,
            int serviceId, string petNote,
            DateTime bookingDate, string bookingTime)
        {
            var appointmentValidation = _bookingBusiness.ValidateAppointmentTime(bookingDate, bookingTime, out _);
            if (!appointmentValidation.Succeeded)
            {
                TempData["BookingError"] = appointmentValidation.ErrorMessage;
                return RedirectToAction(nameof(Time), new
                {
                    customerId,
                    customerName,
                    customerPhone,
                    customerEmail,
                    petId,
                    petType,
                    petName,
                    breedId,
                    petWeight,
                    petAge,
                    petGender,
                    memberPetType = petType,
                    memberPetName = petName,
                    memberBreedId = breedId,
                    memberPetWeight = petWeight,
                    memberPetAge = petAge,
                    memberPetGender = petGender,
                    serviceId,
                    petNote
                });
            }

            var sessionCustomerId = HttpContext.Session.GetCustomerId();
            if (sessionCustomerId.HasValue)
            {
                customerId = sessionCustomerId.Value;
            }
            // 1. NẾU LÀ THÀNH VIÊN -> MÓC DATA TỪ SQL
            if (customerId.HasValue && customerId.Value > 0)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    customerName = customer.FullName;
                    customerPhone = customer.PhoneNumber;
                    customerEmail = customer.Email;
                }
            }

            // 2. NẾU CHỌN THÚ CƯNG CŨ -> MÓC DATA TỪ SQL
            if (petId.HasValue && petId.Value > 0)
            {
                var pet = await _context.Pets
                    .Include(p => p.PetBreed)
                    .FirstOrDefaultAsync(p => p.PetId == petId.Value);

                if (pet != null)
                {
                    petName = pet.Name;
                    breedId = pet.BreedId;
                    ViewBag.BreedName = pet.PetBreed?.BreedName;
                }
            }
            else if (breedId.HasValue)
            {
                var breed = await _context.PetBreeds.FindAsync(breedId.Value);
                ViewBag.BreedName = breed?.BreedName;
            }

            // 3. GÁN TOÀN BỘ DỮ LIỆU SANG VIEW
            ViewBag.CustomerId = customerId;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerPhone = customerPhone;
            ViewBag.CustomerEmail = customerEmail;

            ViewBag.PetId = petId;
            ViewBag.PetType = petType;
            ViewBag.PetName = petName;
            ViewBag.BreedId = breedId;
            ViewBag.PetWeight = petWeight;
            ViewBag.PetAge = petAge;
            ViewBag.PetGender = petGender;

            ViewBag.PetNote = petNote;
            ViewBag.BookingDate = bookingDate;
            ViewBag.BookingTime = bookingTime;

            // BỔ SUNG DÒNG NÀY ĐỂ KHÔNG BỊ RỚT DATA DỊCH VỤ KHI CHUYỂN TRANG
            ViewBag.ServiceId = serviceId;

            // 4. LẤY THÔNG TIN DỊCH VỤ VÀ TÍNH TIỀN
            var selectedService = await _context.ServiceCatalogs.FindAsync(serviceId);
            ViewBag.SelectedService = selectedService;

            if (selectedService != null)
            {
                var totalAmount = _invoiceBusiness.CalculateTotalAmount(selectedService.BasePrice);
                ViewBag.VATAmount = totalAmount - selectedService.BasePrice;
                ViewBag.TotalAmount = totalAmount;

                var now = DateTime.Now;
                ViewBag.AvailablePromotions = await _context.Promotions
                    .Where(promotion =>
                        promotion.IsActive == true &&
                        promotion.StartDate <= now &&
                        promotion.EndDate >= now &&
                        totalAmount >= (promotion.MinOrderValue ?? 0))
                    .OrderBy(promotion => promotion.PromoCode)
                    .ToListAsync();
            }

            return View();
        }

        // ==========================================
        // BƯỚC 4: XỬ LÝ CHỐT ĐƠN (LƯU VÀO CSDL THẬT)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            int? customerId, string customerName, string customerPhone, string customerEmail,
            int? petId, string petType, string petName, int? breedId, decimal? petWeight, string petAge, string petGender,
            int serviceId, string petNote,
            DateTime bookingDate, string bookingTime,
            int paymentMethodId,
            int? promotionId) // ID Phương thức thanh toán và mã ưu đãi đã chọn
        {
            var sessionCustomerId = HttpContext.Session.GetCustomerId();
            var isLoggedInCustomer = sessionCustomerId.HasValue;

            var appointmentValidation = _bookingBusiness.ValidateAppointmentTime(bookingDate, bookingTime, out var appointmentTime);
            if (!appointmentValidation.Succeeded)
            {
                TempData["BookingError"] = appointmentValidation.ErrorMessage;
                return RedirectToAction(nameof(Time), new
                {
                    customerId,
                    customerName,
                    customerPhone,
                    customerEmail,
                    petId,
                    petType,
                    petName,
                    breedId,
                    petWeight,
                    petAge,
                    petGender,
                    memberPetType = petType,
                    memberPetName = petName,
                    memberBreedId = breedId,
                    memberPetWeight = petWeight,
                    memberPetAge = petAge,
                    memberPetGender = petGender,
                    serviceId,
                    petNote
                });
            }

            customerName = customerName?.Trim() ?? string.Empty;
            customerPhone = PhoneNumberHelper.Normalize(customerPhone);
            customerEmail = customerEmail?.Trim() ?? string.Empty;
            petName = petName?.Trim() ?? string.Empty;

            Customer? customer;
            if (sessionCustomerId.HasValue)
            {
                customerId = sessionCustomerId.Value;
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.IsDeleted != true);

                if (customer == null)
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                customerId = null;
                petId = null;

                if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone))
                {
                    TempData["BookingError"] = "Vui lòng nhập họ tên và số điện thoại để đặt lịch.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }

                if (!PhoneNumberHelper.IsValid(customerPhone))
                {
                    TempData["BookingError"] = "Số điện thoại chỉ gồm 9 đến 15 chữ số.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }

                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == customerPhone && c.IsDeleted != true);

                if (customer?.AccountId != null)
                {
                    TempData["ErrorMessage"] = "Số điện thoại này đã có tài khoản. Vui lòng đăng nhập để tiếp tục đặt lịch.";
                    return RedirectToAction("Login", "Account");
                }

                if (customer == null
                    && !string.IsNullOrWhiteSpace(customerEmail)
                    && await _context.Customers.AnyAsync(c => c.Email == customerEmail && c.IsDeleted != true))
                {
                    TempData["BookingError"] = "Email này đã có hồ sơ khách hàng. Vui lòng dùng đúng số điện thoại hoặc đăng nhập.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }
            }

            var service = await _context.ServiceCatalogs
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId && s.IsActive == true && s.IsDeleted != true);
            if (service == null)
            {
                TempData["BookingError"] = "Dịch vụ đã chọn hiện không khả dụng.";
                return RedirectToAction(nameof(Index));
            }

            var availabilityValidation = await _bookingBusiness.ValidateAvailabilityAsync(appointmentTime, serviceId);
            if (!availabilityValidation.Succeeded)
            {
                TempData["BookingError"] = availabilityValidation.ErrorMessage;
                return RedirectToAction(nameof(Time), new
                {
                    customerId,
                    customerName,
                    customerPhone,
                    customerEmail,
                    petId,
                    petType,
                    petName,
                    breedId,
                    petWeight,
                    petAge,
                    petGender,
                    memberPetType = petType,
                    memberPetName = petName,
                    memberBreedId = breedId,
                    memberPetWeight = petWeight,
                    memberPetAge = petAge,
                    memberPetGender = petGender,
                    serviceId,
                    petNote
                });
            }

            var invoiceTotal = _invoiceBusiness.CalculateTotalAmount(service.BasePrice);
            Promotion? promotion = null;
            if (promotionId.HasValue)
            {
                var now = DateTime.Now;
                promotion = await _context.Promotions
                    .FirstOrDefaultAsync(item =>
                        item.PromotionId == promotionId.Value &&
                        item.IsActive == true &&
                        item.StartDate <= now &&
                        item.EndDate >= now);

                if (promotion == null)
                {
                    TempData["BookingError"] = "Mã khuyến mãi đã chọn không còn hiệu lực.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }

                if (invoiceTotal < (promotion.MinOrderValue ?? 0))
                {
                    TempData["BookingError"] = "Đơn đặt lịch chưa đạt giá trị tối thiểu của mã khuyến mãi.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }
            }

            Pet? pet = null;
            if (isLoggedInCustomer && petId.HasValue && petId.Value > 0)
            {
                pet = await _context.Pets.FirstOrDefaultAsync(p =>
                    p.PetId == petId.Value
                    && p.CustomerId == customer!.CustomerId
                    && p.IsDeleted != true);
                if (pet == null)
                {
                    TempData["BookingError"] = "Thú cưng đã chọn không thuộc hồ sơ của bạn.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }
            }

            if (pet == null)
            {
                var speciesId = petType == "Cat" ? (int)PetSpeciesCode.Cat : (int)PetSpeciesCode.Dog;
                var breedIsValid = breedId.HasValue && await _context.PetBreeds.AnyAsync(b =>
                    b.BreedId == breedId.Value && b.SpeciesId == speciesId);

                if (string.IsNullOrWhiteSpace(petName) || !breedIsValid || petWeight is null or <= 0 or > 150)
                {
                    TempData["BookingError"] = "Vui lòng kiểm tra tên, giống và cân nặng thú cưng trước khi đặt lịch.";
                    return RedirectToAction(nameof(Index), new { serviceId });
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Khách vãng lai quay lại được dùng lại hồ sơ theo số điện thoại.
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = customerName,
                        PhoneNumber = customerPhone,
                        Email = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // LƯU THÚ CƯNG
                if (pet == null)
                {
                    int speciesId = petType == "Cat" ? (int)PetSpeciesCode.Cat : (int)PetSpeciesCode.Dog;
                    pet = new Pet
                    {
                        CustomerId = customer.CustomerId,
                        Name = petName,
                        SpeciesId = speciesId,
                        BreedId = breedId ?? 1,
                        Weight = petWeight ?? 0,
                        Notes = petAge + " - " + petGender,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };
                    _context.Pets.Add(pet);
                    await _context.SaveChangesAsync();
                }

                // TẠO PHIẾU ĐẶT LỊCH (BOOKING)
                string newBookingCode = ReferenceCodeHelper.Create("BK");
                var booking = new Booking
                {
                    BookingCode = newBookingCode,
                    CustomerId = customer.CustomerId,
                    BookingDate = appointmentTime,
                    Notes = petNote,
                    StatusId = BookingStatusPending,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // TẠO CHI TIẾT ĐẶT LỊCH (BOOKING DETAIL)
                var bookingDetail = new BookingDetail
                {
                    BookingId = booking.BookingId,
                    PetId = pet.PetId,
                    ServiceId = serviceId,
                    ActualPrice = service.BasePrice,
                    StatusId = DetailStatusNotStarted
                };
                _context.BookingDetails.Add(bookingDetail);

                // TẠO HÓA ĐƠN GHI NHẬN THANH TOÁN (INVOICE)
                var createdAt = DateTime.Now;
                var invoice = new Invoice
                {
                    InvoiceCode = ReferenceCodeHelper.Create("INV"),
                    BookingId = booking.BookingId,
                    PromotionId = promotion?.PromotionId,
                    TotalAmount = invoiceTotal,
                    DiscountAmount = _invoiceBusiness.CalculateDiscountAmount(invoiceTotal, promotion, createdAt),
                    PaidAmount = 0,
                    StatusId = (int)InvoiceStatusCode.Unpaid,
                    CreatedAt = createdAt
                };
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Lưu lại phương thức khách chọn để hiển thị ở trang Success
                TempData["PaymentMethod"] = paymentMethodId == (int)PaymentMethodCode.Cash ? "Tiền mặt tại quầy" : "Chuyển khoản (QR)";

                return RedirectToAction("Success", new { code = newBookingCode });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["BookingError"] = "Không thể tạo lịch hẹn lúc này. Vui lòng kiểm tra thông tin và thử lại.";
                return RedirectToAction(nameof(Index), new { serviceId });
            }
        }

        // ==========================================
        // BƯỚC 5: TRANG THÔNG BÁO THÀNH CÔNG VÀ CHI TIẾT (ĐÃ FIX LỖI EF CORE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Success(string code)
        {
            if (string.IsNullOrEmpty(code))
                return RedirectToAction("Index", "Home");

            // 1. Chỉ lấy thông tin Booking và Customer trước cho an toàn
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingCode == code);

            if (booking == null)
                return NotFound("Không tìm thấy mã đặt lịch này.");

            // 2. Lấy thông tin Chi tiết Lịch hẹn riêng lẻ
            var bookingDetail = await _context.BookingDetails
                .FirstOrDefaultAsync(bd => bd.BookingId == booking.BookingId);

            if (bookingDetail != null)
            {
                // Móc thông tin Pet
                ViewBag.Pet = await _context.Pets
                    .Include(p => p.PetBreed)
                    .FirstOrDefaultAsync(p => p.PetId == bookingDetail.PetId);

                // Móc thông tin Dịch vụ
                ViewBag.Service = await _context.ServiceCatalogs
                    .FirstOrDefaultAsync(s => s.ServiceId == bookingDetail.ServiceId);
            }

            // 3. Lấy thông tin Hóa đơn
            ViewBag.Invoice = await _context.Invoices
                .Include(i => i.Promotion)
                .FirstOrDefaultAsync(i => i.BookingId == booking.BookingId);

            return View(booking);
        }

        // ==========================================
        // LỊCH SỬ ĐẶT LỊCH CỦA KHÁCH HÀNG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Detail()
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await _bookingBusiness.MarkExpiredBookingsAsync(customerId.Value);

            var bookings = await _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.Invoice)
                    .ThenInclude(invoice => invoice!.Promotion)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Pet)
                    .ThenInclude(p => p.PetBreed)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Status)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ServiceReview)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.BookingDetailEmployees)
                    .ThenInclude(bde => bde.Employee)
                .Where(b => b.CustomerId == customerId.Value && b.IsDeleted != true)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.CompletedBookings = bookings.Count(_bookingBusiness.IsCompleted);
            ViewBag.UpcomingBookings = bookings.Count(b =>
                b.StatusId != BookingStatusCancelled &&
                b.StatusId != BookingStatusExpired &&
                !_bookingBusiness.IsCompleted(b));
            ViewBag.ExpiredBookings = bookings.Count(_bookingBusiness.IsExpired);
            ViewBag.CancelledBookings = bookings.Count(b => b.StatusId == BookingStatusCancelled);

            return View(bookings);
        }

        // ==========================================
        // THÊM / CẬP NHẬT ĐÁNH GIÁ DỊCH VỤ ĐÃ HOÀN THÀNH
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int bookingDetailId, int rating, string? content, string[]? reviewTags)
        {
            var customerId = HttpContext.Session.GetCustomerId();
            if (customerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction(nameof(Detail));
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
                return RedirectToAction(nameof(Detail));
            }

            if (!_bookingBusiness.CanReview(bookingDetail))
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá dịch vụ đã hoàn thành.";
                return RedirectToAction(nameof(Detail));
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
                if (bookingDetail.ServiceReview.CustomerId != customerId.Value)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền cập nhật đánh giá này.";
                    return RedirectToAction(nameof(Detail));
                }

                bookingDetail.ServiceReview.Rating = rating;
                bookingDetail.ServiceReview.Content = reviewContent;
                bookingDetail.ServiceReview.ReviewTags = reviewTagText;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã lưu đánh giá dịch vụ thành công.";
            return RedirectToAction(nameof(Detail));
        }

    }
}
