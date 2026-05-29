using System;
using System.Collections.Generic;

namespace PetcareWebsite.Models;

public partial class PetSpecy
{
    public int SpeciesId { get; set; }

    public string SpeciesName { get; set; } = null!;

    public virtual ICollection<PetBreed> PetBreeds { get; set; } = new List<PetBreed>();

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
