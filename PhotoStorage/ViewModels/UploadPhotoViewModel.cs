using System.ComponentModel.DataAnnotations;
using PhotoStorage.Models;

namespace PhotoStorage.ViewModels;

public class UploadPhotoViewModel
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Media File")]
    public IFormFile MediaFile { get; set; } = null!;
    
    // This will be automatically determined based on file type
    public MediaType MediaType { get; set; } = MediaType.Image;
}