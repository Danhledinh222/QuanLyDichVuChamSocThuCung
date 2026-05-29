using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Extensions;
using PetcareWebsite.Models;

var builder = WebApplication.CreateBuilder(args);

// Dang ky MVC, database va cac nghiep vu cua he thong.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PetCareDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddPetCareBusiness();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.MapStaticAssets();

// Session phai duoc khoi tao truoc middleware kiem tra tai khoan.
app.UseSession();

app.UseAccountSessionGuard();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
