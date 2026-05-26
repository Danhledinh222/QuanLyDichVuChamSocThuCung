using Microsoft.AspNetCore.Http;
using PetcareWebsite.Enums;

namespace PetcareWebsite.Extensions;

public static class SessionExtensions
{
    public static int? GetAccountId(this ISession session) => session.GetInt32("AccountId");

    public static int? GetRoleId(this ISession session) => session.GetInt32("RoleId");

    public static int? GetCustomerId(this ISession session) => session.GetInt32("CustomerId");

    public static int? GetEmployeeId(this ISession session) => session.GetInt32("EmployeeId");

    public static bool IsAdmin(this ISession session) =>
        session.GetRoleId() == (int)SystemRoleCode.Admin;
}
