using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetSync.Data;
using MeetSync.Models;
using BCrypt.Net;

namespace MeetSync.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Login page
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login processing
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Store user info in session
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name);
                    HttpContext.Session.SetString("UserEmail", user.Email);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid email or password.");
            }

            return View(model);
        }

        // GET: Registration page
        public IActionResult Register()
        {
            return View();
        }

        // POST: Registration processing
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Name = model.Name,
                    Workplace = model.Workplace,
                    AboutSection = model.AboutSection
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add interests if provided
                if (!string.IsNullOrEmpty(model.Interests))
                {
                    var interests = model.Interests.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var interest in interests)
                    {
                        _context.UserInterests.Add(new UserInterest
                        {
                            UserId = user.Id,
                            Interest = interest.Trim()
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Auto-login after registration
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
