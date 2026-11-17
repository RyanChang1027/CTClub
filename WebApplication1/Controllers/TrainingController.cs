using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class TrainingController : Controller
    {
        // ---------------------------------------------------------
        // 4. API ENDPOINT: Returning JSON for AJAX
        // ---------------------------------------------------------
        [HttpGet]
        public ActionResult Api()
        {
            // This tells MVC to render Views/Training/Api.cshtml
            return View();
        }

        [HttpGet]
        public JsonResult ApiDemo(string query)
        {
            // Simulate a database lookup
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { Success = false, Message = "Query is required." }, JsonRequestBehavior.AllowGet);
            }

            // Return data as JSON
            return Json(new
            {
                Success = true,
                Message = $"You searched for '{query}'. This data came from the server via AJAX!",
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            }, JsonRequestBehavior.AllowGet);
        }

        // ---------------------------------------------------------
        // 3. FORM SUBMISSION: GET and POST
        // ---------------------------------------------------------
        [HttpGet]
        public ActionResult Form()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Security: Prevents Cross-Site Request Forgery
        public ActionResult Form(string name, string email)
        {
            // Here we simulate saving to a database.
            // In a real app, we would call a Repository here.

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please fill in all fields.";
                return View();
            }

            ViewBag.SuccessMessage = $"Success! Received data for {name} ({email}).";
            return View();
        }

        // ---------------------------------------------------------
        // 2. PASSING DATA: Controller -> View
        // ---------------------------------------------------------
        [HttpGet]
        public ActionResult Data()
        {
            // ViewBag is dynamic (evaluated at runtime)
            ViewBag.Message = "Hello from the Controller!";
            ViewBag.CurrentTime = DateTime.Now.ToString("HH:mm:ss");

            // ViewData requires casting when reading
            ViewData["DeveloperName"] = "Junior Dev";

            return View();
        }

        // ---------------------------------------------------------
        // 1. BASIC VIEW: Renders HTML
        // ---------------------------------------------------------
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }
    }
}