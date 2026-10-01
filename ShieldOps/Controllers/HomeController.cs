using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShieldOps.Models;
using System;
using System.Diagnostics;

namespace ShieldOps.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // PIPELINE SYNC: Checks if an authenticated user session is active to maintain layout alignment
            var sessionEmail = HttpContext.Session.GetString("VerifiedUserEmail");
            var sessionRole = HttpContext.Session.GetString("VerifiedUserRole");

            if (!string.IsNullOrEmpty(sessionEmail))
            {
                // Passing metadata via temporary storage to views if needed for landing summaries
                ViewData["UserSessionActive"] = true;
                ViewData["UserSessionRole"] = sessionRole;
            }
            else
            {
                ViewData["UserSessionActive"] = false;
            }

            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}