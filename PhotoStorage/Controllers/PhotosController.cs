using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoStorage.Data;
using PhotoStorage.Models;
using PhotoStorage.ViewModels;

namespace PhotoStorage.Controllers;

[Authorize]
public class PhotosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public PhotosController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var photos = await _context.Photos
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.UploadDate)
            .ToListAsync();
        
        return View(photos);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(UploadPhotoViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();
            
            // Determine media type
            MediaType mediaType = MediaType.Image;
            bool isVideo = model.MediaFile.ContentType.StartsWith("video/");
            if (isVideo)
            {
                mediaType = MediaType.Video;
            }
            
            // Validate file type
            if (!IsAllowedFileType(model.MediaFile.ContentType))
            {
                ModelState.AddModelError("MediaFile", "Unsupported file type. Allowed types: jpg, png, gif, mp4, mov, avi, webm");
                return View(model);
            }
            
            
            
            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);
            
            // Create user-specific folder
            var userFolder = Path.Combine(uploadsFolder, user.Id);
            Directory.CreateDirectory(userFolder);
            
            // Generate unique filename
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.MediaFile.FileName);
            var filePath = Path.Combine(userFolder, uniqueFileName);
            
            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.MediaFile.CopyToAsync(stream);
            }
            
            // Create photo/video record
            var mediaItem = new Photo
            {
                Title = model.Title,
                Description = model.Description,
                FileName = uniqueFileName,
                ContentType = model.MediaFile.ContentType,
                FileSize = model.MediaFile.Length,
                UserId = user.Id,
                UploadDate = DateTime.UtcNow,
                MediaType = mediaType
            };
            
            // TODO: For videos, you could generate a thumbnail and set duration
            // This would require additional libraries for video processing
            
            _context.Photos.Add(mediaItem);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }
        
        return View(model);
    }

    private bool IsAllowedFileType(string contentType)
    {
        var allowedTypes = new[] 
        { 
            // Images
            "image/jpeg", 
            "image/png", 
            "image/gif", 
            "image/webp",
            // Videos
            "video/mp4", 
            "video/quicktime",  // .mov
            "video/x-msvideo",  // .avi
            "video/webm" 
        };
        
        return allowedTypes.Contains(contentType);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

        if (photo == null)
        {
            return NotFound();
        }

        // Fetch the next and previous photos
        var previousPhoto = await _context.Photos
            .Where(p => p.UserId == user.Id && p.UploadDate < photo.UploadDate)
            .OrderByDescending(p => p.UploadDate)
            .FirstOrDefaultAsync();

        var nextPhoto = await _context.Photos
            .Where(p => p.UserId == user.Id && p.UploadDate > photo.UploadDate)
            .OrderBy(p => p.UploadDate)
            .FirstOrDefaultAsync();

        ViewBag.PreviousPhotoId = previousPhoto?.Id;
        ViewBag.NextPhotoId = nextPhoto?.Id;

        return View(photo);
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);
        
        if (photo == null)
        {
            return NotFound();
        }
        
        photo.IsPublic = true;
        photo.PublicId = Guid.NewGuid().ToString("N");
        
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Details), new { id = photo.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Unpublish(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);
        
        if (photo == null)
        {
            return NotFound();
        }
        
        photo.IsPublic = false;
        photo.PublicId = null;
        
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Details), new { id = photo.Id });
    }

    [AllowAnonymous]
    public async Task<IActionResult> View(string id)
    {
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.PublicId == id && p.IsPublic);
        
        if (photo == null)
        {
            return NotFound();
        }
        
        return View(photo);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Download(int? id, string? publicId)
    {
        Photo? photo = null;
        
        if (id.HasValue)
        {
            // Get photo by internal ID - could be public or private
            photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);
            
            // If photo is private, verify the current user owns it
            if (photo != null && !photo.IsPublic)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || photo.UserId != user.Id)
                {
                    return Forbid();
                }
            }
        }
        else if (!string.IsNullOrEmpty(publicId))
        {
            // Get photo by public ID - must be published
            photo = await _context.Photos.FirstOrDefaultAsync(p => p.PublicId == publicId && p.IsPublic);
        }
        
        if (photo == null)
        {
            return NotFound();
        }
        
        // Get the file path
        var filePath = Path.Combine(_environment.WebRootPath, "uploads", photo.UserId, photo.FileName);
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Photo file not found on server");
        }
        
        // Return the file
        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        
        return File(memory, photo.ContentType, Path.GetFileName(filePath));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

        if (photo == null)
        {
            return NotFound();
        }

        var model = new EditPhotoViewModel
        {
            Title = photo.Title,
            Description = photo.Description
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EditPhotoViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (photo == null)
            {
                return NotFound();
            }

            // Update the photo's title and description
            photo.Title = model.Title;
            photo.Description = model.Description;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = photo.Id });
        }

        // Log validation errors for debugging
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

        if (photo == null)
        {
            return NotFound();
        }

        // Delete the file from the server
        var filePath = Path.Combine(_environment.WebRootPath, "uploads", photo.UserId, photo.FileName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _context.Photos.Remove(photo);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}