using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class Pet
{
    public int PetId { get; set; }

    public int CustomerId { get; set; }

    public string Name { get; set; } = null!;

    public int SpeciesId { get; set; }

    public int BreedId { get; set; }

    public decimal? Weight { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual PetBreed PetBreed { get; set; } = null!;

    public virtual PetSpecy Species { get; set; } = null!;
}
