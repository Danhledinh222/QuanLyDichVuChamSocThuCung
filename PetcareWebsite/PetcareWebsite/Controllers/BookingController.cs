using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;

namespace PetcareWebsite.Controllers;

public class BookingController(DemoStore store) : Controller
{
    public IActionResult Index(int? serviceId)
    {
        ViewBag.IsLoggedIn = true;
        ViewBag.Customer = store.MemberCustomer;
        ViewBag.MyPets = store.MemberCustomer.Pets.Where(pet => pet.IsDeleted != true).ToList();
        ViewBag.Services = store.Services.Where(service => service.IsActive == true).ToList();
        ViewBag.Breeds = store.Breeds;
        ViewBag.SelectedServiceId = serviceId;
        return View();
    }

    [HttpGet]
    public IActionResult Time(
        int? customerId, string? customerName, string? customerPhone, string? customerEmail,
        int? petId, string? petType, string? petName, int? breedId, decimal? petWeight, string? petAge, string? petGender,
        string? memberPetType, string? memberPetName, int? memberBreedId, decimal? memberPetWeight, string? memberPetAge, string? memberPetGender,
        int serviceId = 4, string? petNote = null)
    {
        var customer = store.MemberCustomer;
        var pet = store.Pets.First();
        ViewBag.CustomerId = customerId ?? customer.CustomerId;
        ViewBag.CustomerName = customerName ?? customer.FullName;
        ViewBag.CustomerPhone = customerPhone ?? customer.PhoneNumber;
        ViewBag.CustomerEmail = customerEmail ?? customer.Email;
        ViewBag.PetId = petId ?? pet.PetId;
        ViewBag.PetType = petType ?? memberPetType ?? pet.Species.SpeciesName;
        ViewBag.PetName = petName ?? memberPetName ?? pet.Name;
        ViewBag.BreedId = breedId ?? memberBreedId ?? pet.BreedId;
        ViewBag.PetWeight = petWeight ?? memberPetWeight ?? pet.Weight;
        ViewBag.PetAge = petAge ?? memberPetAge ?? "1 - 3 tuổi";
        ViewBag.PetGender = petGender ?? memberPetGender ?? "Đực";
        ViewBag.ServiceId = serviceId;
        ViewBag.PetNote = petNote;
        return View();
    }

    [HttpGet]
    public IActionResult GetBookedTimes() => Json(new[] { "09:30", "15:00" });

    [HttpGet]
    public IActionResult Confirm(
        int? customerId, string? customerName, string? customerPhone, string? customerEmail,
        int? petId, string? petType, string? petName, int? breedId, decimal? petWeight, string? petAge, string? petGender,
        int serviceId = 4, string? petNote = null, DateTime? bookingDate = null, string? bookingTime = null)
    {
        var customer = store.MemberCustomer;
        var pet = store.Pets.First();
        var service = store.Services.FirstOrDefault(item => item.ServiceId == serviceId) ?? store.Services[3];
        var appointmentDate = bookingDate ?? DateTime.Today.AddDays(1);

        ViewBag.CustomerId = customerId ?? customer.CustomerId;
        ViewBag.CustomerName = customerName ?? customer.FullName;
        ViewBag.CustomerPhone = customerPhone ?? customer.PhoneNumber;
        ViewBag.CustomerEmail = customerEmail ?? customer.Email;
        ViewBag.PetId = petId ?? pet.PetId;
        ViewBag.PetType = petType ?? pet.Species.SpeciesName;
        ViewBag.PetName = petName ?? pet.Name;
        ViewBag.BreedId = breedId ?? pet.BreedId;
        ViewBag.BreedName = store.Breeds.FirstOrDefault(item => item.BreedId == (breedId ?? pet.BreedId))?.BreedName;
        ViewBag.PetWeight = petWeight ?? pet.Weight;
        ViewBag.PetAge = petAge ?? "1 - 3 tuổi";
        ViewBag.PetGender = petGender ?? "Đực";
        ViewBag.ServiceId = service.ServiceId;
        ViewBag.SelectedService = service;
        ViewBag.PetNote = petNote;
        ViewBag.BookingDate = appointmentDate;
        ViewBag.BookingTime = bookingTime ?? "10:00";
        ViewBag.VATAmount = service.BasePrice * 0.1m;
        ViewBag.TotalAmount = service.BasePrice * 1.1m;
        ViewBag.AvailablePromotions = store.Promotions.Where(promotion => promotion.IsActive == true && promotion.StartDate <= DateTime.Now && promotion.EndDate >= DateTime.Now).ToList();
        return View();
    }

    [HttpPost]
    public IActionResult Checkout()
    {
        TempData["SuccessMessage"] = "Đặt lịch thành công trong bản trình diễn giao diện.";
        return RedirectToAction(nameof(Success), new { code = store.Bookings.First(booking => booking.StatusId == 2).BookingCode });
    }

    public IActionResult Success(string? code)
    {
        var booking = store.Bookings.FirstOrDefault(item => item.BookingCode == code) ?? store.Bookings.First();
        var detail = booking.BookingDetails.First();
        ViewBag.Pet = detail.Pet;
        ViewBag.Service = detail.Service;
        ViewBag.Invoice = booking.Invoice;
        TempData["PaymentMethod"] ??= "Thanh toán tại quầy";
        return View(booking);
    }

    public IActionResult Detail()
    {
        var bookings = store.MemberCustomer.Bookings.OrderByDescending(booking => booking.BookingDate).ToList();
        ViewBag.TotalBookings = bookings.Count;
        ViewBag.CompletedBookings = bookings.Count(booking => booking.StatusId == 3);
        ViewBag.UpcomingBookings = bookings.Count(booking => booking.StatusId is 1 or 2);
        return View(bookings);
    }

    [HttpPost]
    public IActionResult AddReview()
    {
        TempData["SuccessMessage"] = "Đánh giá đã được ghi nhận trong bản trình diễn.";
        return RedirectToAction(nameof(Detail));
    }
}
