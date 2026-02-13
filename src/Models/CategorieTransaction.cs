using System;

namespace FatouraDZ.Models;

public class CategorieTransaction
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public TypeTransaction Type { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.Now;
    
    // Navigation
    public Business Business { get; set; } = null!;
}
