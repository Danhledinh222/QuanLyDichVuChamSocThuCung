using PetcareWebsite.Services;

namespace PetcareWebsite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPetCareBusiness(this IServiceCollection services)
    {
        services.AddScoped<IBookingBusinessService, BookingBusinessService>();
        services.AddScoped<IInvoiceBusinessService, InvoiceBusinessService>();
        return services;
    }
}
