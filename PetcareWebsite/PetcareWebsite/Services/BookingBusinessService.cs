using Microsoft.EntityFrameworkCore;
using PetcareWebsite.Enums;
using PetcareWebsite.Models;
using System.Globalization;

namespace PetcareWebsite.Services;

public sealed class BookingBusinessService : IBookingBusinessService
{
    private const int Pending = (int)BookingStatusCode.Pending;
    private const int Confirmed = (int)BookingStatusCode.Confirmed;
    private const int Completed = (int)BookingStatusCode.Completed;
    private const int Cancelled = (int)BookingStatusCode.Cancelled;
    private const int Expired = (int)BookingStatusCode.Expired;
    private const int InProgress = (int)BookingStatusCode.InProgress;
    private const int DetailDone = (int)DetailStatusCode.Done;
    private const int ConfirmedGracePeriodMinutes = 15;
    private const int DefaultDurationMinutes = 60;

    private readonly PetCareDbContext _context;

    public BookingBusinessService(PetCareDbContext context)
    {
        _context = context;
    }

    public async Task MarkExpiredBookingsAsync(int? customerId = null)
    {
        if (!await _context.BookingStatuses.AnyAsync(status => status.StatusId == Expired))
        {
            return;
        }

        var now = DateTime.Now;
        var query = _context.Bookings.Where(booking =>
                booking.IsDeleted != true &&
                (booking.StatusId == Pending || booking.StatusId == Confirmed) &&
                booking.BookingDate < now);

        if (customerId.HasValue)
        {
            query = query.Where(booking => booking.CustomerId == customerId.Value);
        }

        var candidates = await query.ToListAsync();
        var expiredBookings = candidates.Where(booking =>
            booking.StatusId == Pending ||
            booking.BookingDate.AddMinutes(ConfirmedGracePeriodMinutes) <= now).ToList();

        if (!expiredBookings.Any())
        {
            return;
        }

        foreach (var booking in expiredBookings)
        {
            booking.StatusId = Expired;
            booking.ModifiedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    public BusinessRuleResult ValidateStatusTransition(Booking booking, int nextStatusId, bool hasAssignedEmployee)
    {
        if ((nextStatusId == Confirmed || nextStatusId == InProgress || nextStatusId == Completed) &&
            !hasAssignedEmployee)
        {
            return BusinessRuleResult.Failure("Vui lòng phân công nhân viên trước khi xử lý lịch.");
        }

        if (nextStatusId == booking.StatusId)
        {
            return BusinessRuleResult.Success();
        }

        return (booking.StatusId, nextStatusId) switch
        {
            (Pending, Confirmed) => BusinessRuleResult.Success(),
            (Confirmed, InProgress) => BusinessRuleResult.Success(),
            (InProgress, Completed) => BusinessRuleResult.Success(),
            (Pending, Cancelled) => BusinessRuleResult.Success(),
            (Confirmed, Cancelled) => BusinessRuleResult.Success(),
            (InProgress, Cancelled) => BusinessRuleResult.Success(),
            _ => BusinessRuleResult.Failure("Chuyển trạng thái không hợp lệ. Lịch phải được xác nhận, bắt đầu thực hiện rồi mới hoàn thành.")
        };
    }

    public BusinessRuleResult ValidateAppointmentTime(
        DateTime bookingDate,
        string? bookingTime,
        out DateTime appointmentTime)
    {
        appointmentTime = default;
        if (!TimeSpan.TryParseExact(bookingTime, @"hh\:mm", CultureInfo.InvariantCulture, out var selectedTime))
        {
            return BusinessRuleResult.Failure("Vui lòng chọn khung giờ hợp lệ.");
        }

        appointmentTime = bookingDate.Date.Add(selectedTime);
        if (appointmentTime <= DateTime.Now)
        {
            return BusinessRuleResult.Failure("Khung giờ đã qua. Vui lòng chọn thời gian hẹn khác.");
        }

        return BusinessRuleResult.Success();
    }

    public async Task<IReadOnlyList<string>> GetUnavailableTimesAsync(
        DateTime date,
        int serviceId,
        IReadOnlyCollection<string> timeSlots)
    {
        var service = await _context.ServiceCatalogs
            .FirstOrDefaultAsync(item =>
                item.ServiceId == serviceId &&
                item.IsActive == true &&
                item.IsDeleted != true);
        if (service == null)
        {
            return timeSlots.ToList();
        }

        var schedule = await LoadActiveScheduleAsync(date.Date, excludedBookingId: null);
        var activeStaffCount = await CountAvailableStaffAsync();
        var unavailableTimes = new List<string>();

        foreach (var timeSlot in timeSlots)
        {
            if (!TimeSpan.TryParseExact(timeSlot, @"hh\:mm", CultureInfo.InvariantCulture, out var selectedTime))
            {
                continue;
            }

            var appointmentTime = date.Date.Add(selectedTime);
            var result = EvaluateAvailability(service, appointmentTime, assignedEmployeeId: null, schedule, activeStaffCount);
            if (!result.Succeeded)
            {
                unavailableTimes.Add(timeSlot);
            }
        }

        return unavailableTimes;
    }

    public async Task<BusinessRuleResult> ValidateAvailabilityAsync(
        DateTime appointmentTime,
        int serviceId,
        int? assignedEmployeeId = null,
        int? excludedBookingId = null)
    {
        var service = await _context.ServiceCatalogs
            .FirstOrDefaultAsync(item => item.ServiceId == serviceId && item.IsDeleted != true);
        if (service == null)
        {
            return BusinessRuleResult.Failure("Dịch vụ đã chọn hiện không khả dụng.");
        }

        var schedule = await LoadActiveScheduleAsync(appointmentTime.Date, excludedBookingId);
        var activeStaffCount = await CountAvailableStaffAsync();
        return EvaluateAvailability(service, appointmentTime, assignedEmployeeId, schedule, activeStaffCount);
    }

    public bool IsCompleted(Booking booking)
    {
        if (booking.StatusId == Cancelled || booking.StatusId == Expired)
        {
            return false;
        }

        return booking.StatusId == Completed ||
               (booking.BookingDetails.Any() &&
                booking.BookingDetails.All(detail => detail.StatusId == DetailDone));
    }

    public bool IsExpired(Booking booking)
    {
        return booking.StatusId == Expired;
    }

    public bool CanReview(BookingDetail detail)
    {
        return detail.Booking.StatusId != Cancelled &&
               detail.Booking.StatusId != Expired &&
               (detail.StatusId == DetailDone || detail.Booking.StatusId == Completed);
    }

    private Task<List<BookingDetail>> LoadActiveScheduleAsync(DateTime appointmentDate, int? excludedBookingId)
    {
        var rangeStart = appointmentDate.AddDays(-1);
        var rangeEnd = appointmentDate.AddDays(2);

        return _context.BookingDetails
            .Include(detail => detail.Booking)
            .Include(detail => detail.Service)
            .Include(detail => detail.BookingDetailEmployees)
            .Where(detail =>
                detail.Booking.IsDeleted != true &&
                detail.Booking.StatusId != Cancelled &&
                detail.Booking.StatusId != Expired &&
                detail.Booking.BookingDate >= rangeStart &&
                detail.Booking.BookingDate < rangeEnd &&
                (!excludedBookingId.HasValue || detail.BookingId != excludedBookingId.Value))
            .ToListAsync();
    }

    private Task<int> CountAvailableStaffAsync()
    {
        return _context.Employees.CountAsync(employee =>
            employee.RoleId != (int)SystemRoleCode.Admin &&
            employee.RoleId != (int)SystemRoleCode.Customer &&
            employee.IsActive == true &&
            employee.IsDeleted != true);
    }

    private static BusinessRuleResult EvaluateAvailability(
        ServiceCatalog service,
        DateTime appointmentTime,
        int? assignedEmployeeId,
        IReadOnlyCollection<BookingDetail> schedule,
        int activeStaffCount)
    {
        var appointmentEnd = appointmentTime.AddMinutes(GetDurationMinutes(service));
        var overlaps = schedule
            .Where(detail => IsOverlapping(detail, appointmentTime, appointmentEnd))
            .ToList();

        if (overlaps.Count(detail => detail.ServiceId == service.ServiceId) >= Math.Max(1, service.MaxCapacity))
        {
            return BusinessRuleResult.Failure("Khung giờ này đã đủ sức chứa cho dịch vụ đã chọn.");
        }

        if (activeStaffCount <= 0 || overlaps.Count >= activeStaffCount)
        {
            return BusinessRuleResult.Failure("Khung giờ này cửa hàng không còn nhân viên phục vụ.");
        }

        if (assignedEmployeeId.HasValue &&
            overlaps.Any(detail => detail.BookingDetailEmployees.Any(assignment =>
                assignment.EmployeeId == assignedEmployeeId.Value)))
        {
            return BusinessRuleResult.Failure("Nhân viên đã có lịch giao nhau trong thời lượng dịch vụ.");
        }

        return BusinessRuleResult.Success();
    }

    private static bool IsOverlapping(BookingDetail detail, DateTime appointmentStart, DateTime appointmentEnd)
    {
        var existingStart = detail.Booking.BookingDate;
        var existingEnd = existingStart.AddMinutes(GetDurationMinutes(detail.Service));
        return existingStart < appointmentEnd && appointmentStart < existingEnd;
    }

    private static int GetDurationMinutes(ServiceCatalog service)
    {
        return service.EstimatedDuration is > 0 ? service.EstimatedDuration.Value : DefaultDurationMinutes;
    }

}
