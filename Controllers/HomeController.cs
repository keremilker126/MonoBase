using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MonoBase.Models;

namespace MonoBase.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }


}
