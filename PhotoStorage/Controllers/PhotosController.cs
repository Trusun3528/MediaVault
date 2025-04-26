using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoStorage.Data;
using PhotoStorage.Models;
using PhotoStorage.ViewModels;
using MediaToolkit;
using MediaToolkit.Model;
using PhotoStorage.Services;

namespace PhotoStorage.Controllers;

[Authorize]
public class PhotosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly LMStudioService _lmStudioService;

    public PhotosController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        LMStudioService lmStudioService)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _lmStudioService = lmStudioService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                return Challenge();
            }
            
            // Determine media type
            var mediaType = PhotoStorage.Models.MediaType.Image;
            bool isVideo = model.MediaFile.ContentType.StartsWith("video/");
            var isAudio = model.MediaFile.ContentType.StartsWith("audio/");
            if (isAudio)
            {
                mediaType = PhotoStorage.Models.MediaType.Audio;
            }
            if (isVideo)
            {
                mediaType = PhotoStorage.Models.MediaType.Video;
            }
            
            // Validate file type
            if (!IsAllowedFileType(model.MediaFile.ContentType))
            {
                ModelState.AddModelError("MediaFile", "Unsupported file type. Allowed types: jpg, png, gif, mp4, mov, avi, webm, mp3, wav, ogg");
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
            
            // Handle video-specific logic
            string? thumbnailFileName = null;
            if (isVideo)
            {
                // Generate a thumbnail for the video
                thumbnailFileName = Guid.NewGuid().ToString() + ".jpg";
                var thumbnailPath = Path.Combine(userFolder, thumbnailFileName);

                GenerateVideoThumbnail(filePath, thumbnailPath);
            }
            
            // Generate description using LM Studio if opted in
            if (model.UseAI)
            {
                var serviceMediaType = (PhotoStorage.Services.MediaType)mediaType;
                model.Description = await _lmStudioService.GenerateDescriptionAsync(model.Description, serviceMediaType);
            }
            
            // Create photo/audio/video record
            var mediaItem = new Photo
            {
                Title = model.Title,
                Description = model.Description,
                FileName = uniqueFileName,
                ContentType = model.MediaFile.ContentType,
                FileSize = model.MediaFile.Length,
                UserId = user.Id,
                UploadDate = DateTime.UtcNow,
                MediaType = mediaType,
                ThumbnailFileName = thumbnailFileName // Set the thumbnail file name here
            };

            if (isAudio)
            {
                mediaItem.AudioFileName = uniqueFileName;
            }

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
            "video/webm",
            // Audio
            "audio/mpeg",  // .mp3
            "audio/wav",   // .wav
            "audio/ogg"    // .ogg
        };
        
        return allowedTypes.Contains(contentType);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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
    public new async Task<IActionResult> View(string id)
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
                if (user == null || string.IsNullOrEmpty(user.Id) || photo.UserId != user.Id)
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
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                return Challenge();
            }

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
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Challenge();
        }

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

    [AllowAnonymous]
    public async Task<IActionResult> VideoGallery()
    {
        var videos = await _context.Photos
            .Where(p => p.IsPublic && p.MediaType == PhotoStorage.Models.MediaType.Video)
            .ToListAsync();

        return View(videos);
    }

    [AllowAnonymous]
    public async Task<IActionResult> MediaGallery()
    {
        var media = await _context.Photos
            .Where(p => p.IsPublic)
            .ToListAsync();

        return View(media);
    }

    [AllowAnonymous]
    public async Task<IActionResult> WatchVideo(int id)
    {
        var video = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == id && p.IsPublic && p.MediaType == PhotoStorage.Models.MediaType.Video);

        if (video == null)
        {
            return NotFound();
        }

        var otherVideos = await _context.Photos
            .Where(p => p.Id != id && p.IsPublic && p.MediaType == PhotoStorage.Models.MediaType.Video)
            .Take(10)
            .ToListAsync();

        return View(Tuple.Create(video, otherVideos));
    }

    [HttpPost]
    public async Task<IActionResult> GenerateThumbnails()
    {
        // Ensure the user is authenticated
        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrEmpty(user.Id))
        {
            return Unauthorized();
        }

        // Find all videos without thumbnails
        var videosWithoutThumbnails = await _context.Photos
            .Where(p => p.MediaType == PhotoStorage.Models.MediaType.Video && string.IsNullOrEmpty(p.ThumbnailFileName))
            .ToListAsync();

        foreach (var video in videosWithoutThumbnails)
        {
            var videoPath = Path.Combine(_environment.WebRootPath, "uploads", video.UserId, video.FileName);
            var thumbnailFileName = Guid.NewGuid().ToString() + ".jpg";
            var thumbnailPath = Path.Combine(_environment.WebRootPath, "uploads", video.UserId, thumbnailFileName);

            if (System.IO.File.Exists(videoPath))
            {
                try
                {
                    GenerateVideoThumbnail(videoPath, thumbnailPath);
                    video.ThumbnailFileName = thumbnailFileName;
                }
                catch (Exception ex)
                {
                    // Log the error and continue with the next video
                    Console.WriteLine($"Error generating thumbnail for video {video.Id}: {ex.Message}");
                }
            }
        }

        // Save changes to the database
        await _context.SaveChangesAsync();

        return Ok($"Generated thumbnails for {videosWithoutThumbnails.Count} videos.");
    }

    // Helper method to generate video thumbnails
    private void GenerateVideoThumbnail(string videoPath, string thumbnailPath)
    {
        var inputFile = new MediaFile { Filename = videoPath };
        var outputFile = new MediaFile { Filename = thumbnailPath };

        using (var engine = new Engine())
        {
            engine.GetMetadata(inputFile);

            // Extract a frame at 1 second into the video
            var options = new MediaToolkit.Options.ConversionOptions { Seek = TimeSpan.FromSeconds(1) };
            engine.GetThumbnail(inputFile, outputFile, options);
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> ViewPhoto(int id)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == id && p.IsPublic && p.MediaType == PhotoStorage.Models.MediaType.Image);

        if (photo == null)
        {
            return NotFound();
        }

        return View(photo);
    }
}