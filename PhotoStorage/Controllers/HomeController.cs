using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhotoStorage.Models;

namespace PhotoStorage.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult EasyUpload()
    {
        return View();
    }

    public IActionResult PrivateByDefault()
    {
        return View();
    }

    public IActionResult SelectiveSharing()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
