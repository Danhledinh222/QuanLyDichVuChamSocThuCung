using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Models;

namespace PetcareWebsite.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAccountSessionGuard(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var accountId = context.Session.GetAccountId();
            if (accountId.HasValue)
            {
                var dbContext = context.RequestServices.GetRequiredService<PetCareDbContext>();
                var canUseAccount = await dbContext.Accounts.AnyAsync(account =>
                    account.AccountId == accountId.Value &&
                    account.IsActive == true &&
                    account.IsDeleted == false);

                if (!canUseAccount)
                {
                    context.Session.Clear();
                }
            }

            await next();
        });
    }
}
