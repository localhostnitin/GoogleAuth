using GoogleAuthentication.Data;
using GoogleAuthentication.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;
using System.Security.Claims;

namespace GoogleAuthentication.Controllers
{
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpFactory;
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext _applicationDbContext, IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _context = _applicationDbContext;
            _httpFactory = httpFactory;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.Name = User.FindFirst(ClaimTypes.Name)?.Value ?? "Guest";
            ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";
            base.OnActionExecuting(context);
        }
        public IActionResult Index()
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var name = User.Identity?.Name ?? "Guest";
            var email = User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value ?? "unknown";
            ViewBag.Name = name;
            ViewBag.Email = email;
            return View();
        }
        [HttpGet("login-google")]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Home");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            //// Force Google account chooser every time
            //properties.SetParameter("prompt", "select_account");

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [Route("signin-google")]
        public IActionResult SignInGoogle()
        {
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> GoogleResponse()
        {
            // Authenticate using Google
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
                return RedirectToAction("Login");

            // Create local cookie identity
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                result.Principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.Now.AddHours(1)
                });

            // Sign out Google scheme to avoid re-auth
            try
            {
                await HttpContext.SignOutAsync(GoogleDefaults.AuthenticationScheme);
            }
            catch { }

            return RedirectToAction("Index");
        }
        
        public async Task<IActionResult> Logout()
        {
            // Save logout history (optional)
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                _context.LoginHistories.Add(new LoginHistory
                {
                    UserEmail = email,
                    Provider = "Google",
                    ActionType = "Logout",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ActionTime = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // 1) If tokens were saved, revoke access token at Google's revoke endpoint
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var client = _httpFactory.CreateClient();
                    var revokeUrl = $"https://oauth2.googleapis.com/revoke?token={accessToken}";
                    var resp = await client.PostAsync(revokeUrl, null);
                    // ignore response code — revoke may succeed or token may already be invalid
                }
                catch
                {
                    // log but don't block logout
                }
            }

            // 2) Sign out the local cookie (this is what actually logs the user out of your app)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 3) Remove cookie explicitly by the cookie name you set in options
            Response.Cookies.Delete(".AspNetCore.GoogleAuthCookie"); // same as options.Cookie.Name

            // 4) Clear session (if used)
            HttpContext.Session?.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 5) Redirect to Login (show manual Login page)
            return RedirectToAction("Login", "Login");
        }

        public async Task<IActionResult> Medicines(string search, string sortOrder, int page = 1)
        {
            int pageSize = 10;

            var medicines = from m in _context.Medicines
                            select m;
            // Search
            if (!string.IsNullOrEmpty(search))
            {
                medicines = medicines.Where(m =>
                    m.Name.Contains(search) ||
                    m.Company.Contains(search));
            }

            // Sorting
            ViewBag.NameSort = sortOrder == "name_desc" ? "name_asc" : "name_desc";
            ViewBag.PriceSort = sortOrder == "price_desc" ? "price_asc" : "price_desc";

            medicines = sortOrder switch
            {
                "name_desc" => medicines.OrderByDescending(m => m.Name),
                "name_asc" => medicines.OrderBy(m => m.Name),
                "price_desc" => medicines.OrderByDescending(m => m.Price),
                "price_asc" => medicines.OrderBy(m => m.Price),
                _ => medicines.OrderBy(m => m.MedicineId)
            };

            // Pagination
            var totalCount = await medicines.CountAsync();
            var items = await medicines
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;

            return View(items);
        }
        [HttpGet]
        public async Task<IActionResult> AddEditMedicine(int? id)
        {
            if (id == null)
                return View(new Medicine()); // Create new

            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
                return NotFound();

            return View(medicine); // Edit existing
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditMedicine(Medicine model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.MedicineId == 0)
            {
                // Create new
                _context.Medicines.Add(model);
            }
            else
            {
                // Update existing
                _context.Medicines.Update(model);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Medicines));
        }
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedOn)
                .ToListAsync();
            return View(users);
        }
        // Login History List
        public async Task<IActionResult> LoginHistory()
        {
            var history = await _context.LoginHistories
                .OrderByDescending(h => h.ActionTime)
                .ToListAsync();
            return View(history);
        }
        // Delete
        public async Task<IActionResult> Delete(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return NotFound();

            _context.Medicines.Remove(med);
            await _context.SaveChangesAsync();
            return RedirectToAction("Medicines");
        }
        public IActionResult AboutApp()
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
