using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;

namespace PetcareWebsite.Controllers;

public class PetController(DemoStore store) : Controller
{
    public IActionResult Index()
    {
        var pets = store.MemberCustomer.Pets.Where(pet => pet.IsDeleted != true).ToList();
        ViewBag.SpeciesList = store.Species;
        ViewBag.UpcomingCount = store.MemberCustomer.Bookings.Count(booking => booking.StatusId is 1 or 2);
        ViewBag.LatestService = "Cắt tỉa tạo kiểu chuyên nghiệp";
        ViewBag.MostVisitedPet = pets.First().Name;
        ViewBag.RecentActivities = store.MemberCustomer.Bookings
            .OrderByDescending(booking => booking.BookingDate)
            .SelectMany(booking => booking.BookingDetails)
            .Take(5)
            .ToList();
        return View(pets);
    }

    public IActionResult Detail(int id)
    {
        var pet = store.MemberCustomer.Pets.FirstOrDefault(item => item.PetId == id) ?? store.MemberCustomer.Pets.First();
        ViewBag.SpeciesList = store.Species;
        ViewBag.History = pet.BookingDetails.OrderByDescending(detail => detail.Booking.BookingDate).ToList();
        return View(pet);
    }

    public IActionResult History() => RedirectToAction("Detail", "Booking");

    [HttpGet]
    public IActionResult GetBreedsBySpecies(int speciesId) => Json(store.Breeds
        .Where(breed => breed.SpeciesId == speciesId)
        .Select(breed => new { breed.BreedId, breed.BreedName }));

    [HttpGet]
    public IActionResult GetPetDetail(int id)
    {
        var pet = store.Pets.FirstOrDefault(item => item.PetId == id) ?? store.Pets.First();
        return Json(new { petId = pet.PetId, name = pet.Name, speciesId = pet.SpeciesId, breedId = pet.BreedId, weight = pet.Weight, notes = pet.Notes });
    }

    [HttpPost]
    public IActionResult SavePet(string name)
    {
        TempData["SuccessMessage"] = $"Thông tin bé {name ?? "thú cưng"} đã được lưu trong bản trình diễn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DeletePet(int id) => Json(new { success = true, message = "Đây là thao tác mô phỏng của giao diện hardcode." });
}
