using System.ComponentModel.DataAnnotations;

namespace PetcareWebsite.Validation;

public sealed class PetWeightAttribute : RangeAttribute
{
    public PetWeightAttribute()
        : base(typeof(decimal), "0.01", "150")
    {
        ErrorMessage = "Cân nặng phải lớn hơn 0 và không vượt quá 150 kg.";
    }
}
