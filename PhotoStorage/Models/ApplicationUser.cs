using Microsoft.AspNetCore.Identity;

namespace PhotoStorage.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}