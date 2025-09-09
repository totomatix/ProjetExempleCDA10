using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;

namespace ProjetExempleCDA10.Controllers;

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("/Home/HandleError/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        if (statusCode == 403)
        {
            return View("AccessDenied");
        }
        else if (statusCode == 404)
        {
            return View("NotFound");
        }
        else
        {
            return View("AutresErreurs");
        }
    }
}
