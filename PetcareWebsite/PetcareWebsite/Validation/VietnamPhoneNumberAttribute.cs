using System.ComponentModel.DataAnnotations;
using PetcareWebsite.Helpers;

namespace PetcareWebsite.Validation;

public sealed class VietnamPhoneNumberAttribute : ValidationAttribute
{
    public VietnamPhoneNumberAttribute()
    {
        ErrorMessage = "Số điện thoại chỉ gồm 9 đến 15 chữ số.";
    }

    public override bool IsValid(object? value)
    {
        var phoneNumber = value?.ToString();
        return string.IsNullOrWhiteSpace(phoneNumber) || PhoneNumberHelper.IsValid(phoneNumber);
    }
}
