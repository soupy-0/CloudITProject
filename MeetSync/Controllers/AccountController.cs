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
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", model.Email);

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

                        _logger.LogInformation("Login successful for user: {UserId}", user.Id);
                        return RedirectToAction("Index", "Home");
                    }

                    _logger.LogWarning("Login failed for email: {Email}", model.Email);
                    ModelState.AddModelError("", "Invalid email or password.");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation("Registration attempt started for email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Registration failed - ModelState invalid for email: {Email}", model.Email);
                    return View(model);
                }

                _logger.LogInformation("ModelState is valid, checking if user exists...");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed - User already exists: {Email}", model.Email);
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                _logger.LogInformation("User does not exist, creating new user...");

                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Name = model.Name,
                    Workplace = model.Workplace,
                    AboutSection = model.AboutSection,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _logger.LogInformation("Adding user to database context...");
                _context.Users.Add(user);
                
                _logger.LogInformation("Saving user to database...");
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User successfully created with ID: {UserId}", user.Id);

                if (!string.IsNullOrEmpty(model.Interests))
                {
                    _logger.LogInformation("Processing interests for user: {UserId}", user.Id);
                    
                    var interests = model.Interests.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var userInterests = new List<UserInterest>();
                    
                    foreach (var interest in interests)
                    {
                        var trimmedInterest = interest.Trim();
                        if (!string.IsNullOrEmpty(trimmedInterest))
                        {
                            userInterests.Add(new UserInterest
                            {
                                UserId = user.Id,
                                Interest = trimmedInterest,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    if (userInterests.Any())
                    {
                        _context.UserInterests.AddRange(userInterests);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Added {InterestCount} interests for user: {UserId}", 
                            userInterests.Count, user.Id);
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed successfully for user: {UserId}", user.Id);

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                _logger.LogInformation("Registration completed successfully for user: {UserId}", user.Id);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Registration failed for email: {Email}. Error: {Message}", 
                    model?.Email ?? "unknown", ex.Message);

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerException}", ex.InnerException.Message);
                }

                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}