namespace PhotoStorage.Models;

public class Photo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; } = false;
    public string? PublicId { get; set; }
    
    // New properties for media type
    public MediaType MediaType { get; set; } = MediaType.Image;
    public int? DurationSeconds { get; set; }
    public string? ThumbnailFileName { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    // Helper property to determine if this is a video
    public bool IsVideo => MediaType == MediaType.Video;
}

public enum MediaType
{
    Image,
    Video
}