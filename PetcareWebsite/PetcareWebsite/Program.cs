using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<PetcareWebsite.Data.DemoStore>();
builder.Services.AddDataProtection()
    .SetApplicationName("PetCareHardcodeDemo")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// The prototype opens in a signed-in customer state so all designed screens are visible.
app.Use(async (context, next) =>
{
    if (context.Session.GetInt32("DemoSessionReady") == null)
    {
        context.Session.SetInt32("DemoSessionReady", 1);
        context.Session.SetInt32("AccountId", 4);
        context.Session.SetInt32("RoleId", 4);
        context.Session.SetString("CustomerName", "Danh dz");
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
