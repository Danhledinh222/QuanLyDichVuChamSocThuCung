using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class PetBreed
{
    public int BreedId { get; set; }

    public int SpeciesId { get; set; }

    public string BreedName { get; set; } = null!;

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();

    public virtual PetSpecy Species { get; set; } = null!;
}
