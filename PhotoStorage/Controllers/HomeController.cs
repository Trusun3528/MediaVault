using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhotoStorage.Models;
using PhotoStorage.ViewModels;
using PhotoStorage.Services;

namespace PhotoStorage.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LMStudioService _lmStudioService;

    public HomeController(ILogger<HomeController> logger, LMStudioService lmStudioService)
    {
        _logger = logger;
        _lmStudioService = lmStudioService;
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

    [HttpGet]
    public IActionResult SetLMStudioUrl()
    {
        var viewModel = new SetLMStudioUrlViewModel
        {
            LMStudioUrl = _lmStudioService.GetLMStudioUrl()
        };
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult SetLMStudioUrl(SetLMStudioUrlViewModel model)
    {
        if (ModelState.IsValid)
        {
            _lmStudioService.SetLMStudioUrl(model.LMStudioUrl);
            TempData["SuccessMessage"] = "LM Studio URL updated successfully.";
            return RedirectToAction("Index");
        }
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
