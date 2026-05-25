using Microsoft.AspNetCore.Mvc;
using PetcareWebsite.Data;

namespace PetcareWebsite.Controllers;

public class ServiceController(DemoStore store) : Controller
{
    public IActionResult Index() => View(store.Services.Where(service => service.IsActive == true));

    public IActionResult Detail(int id)
    {
        var service = store.Services.FirstOrDefault(item => item.ServiceId == id) ?? store.Services.First();
        return View(service);
    }
}
