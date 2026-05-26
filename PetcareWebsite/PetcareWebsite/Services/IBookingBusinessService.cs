using PetcareWebsite.Models;

namespace PetcareWebsite.Services;

public interface IBookingBusinessService
{
    Task MarkExpiredBookingsAsync(int? customerId = null);

    BusinessRuleResult ValidateStatusTransition(Booking booking, int nextStatusId, bool hasAssignedEmployee);

    BusinessRuleResult ValidateAppointmentTime(DateTime bookingDate, string? bookingTime, out DateTime appointmentTime);

    Task<IReadOnlyList<string>> GetUnavailableTimesAsync(
        DateTime date,
        int serviceId,
        IReadOnlyCollection<string> timeSlots);

    Task<BusinessRuleResult> ValidateAvailabilityAsync(
        DateTime appointmentTime,
        int serviceId,
        int? assignedEmployeeId = null,
        int? excludedBookingId = null);

    bool IsCompleted(Booking booking);

    bool IsExpired(Booking booking);

    bool CanReview(BookingDetail detail);
}
