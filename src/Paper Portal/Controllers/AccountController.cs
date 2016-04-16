using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Logging;
using Paper_Portal.Helpers;
using Paper_Portal.Models;
using Paper_Portal.Services;
using Paper_Portal.ViewModels.Account;
using Paper_Portal.ViewModels.Email;

namespace Paper_Portal.Controllers
{
    [RequireHttps]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _context = context;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                // Require the user to have a confirmed email before they can log on.
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "You must have a confirmed email to log in.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            var ti = new CultureInfo("en-US", true).TextInfo; // to convert the lowercase string to Title Case
            // select all roles except the Admin
            var roles = _context.Roles.Where(r => r.Name != RoleHelper.Admin).ToList()
                .Select(s => new
                {
                    Text = ti.ToTitleCase(s.Name),
                    Value = s.Name
                });

            var departments = _context.Department
                .Select(s => new
                {
                    Text = s.Name,
                    Value = s.DepartmentId
                })
                .ToList();

            ViewBag.Roles = new SelectList(roles, "Value", "Text", roles.First());
            ViewBag.Departments = new SelectList(departments, "Value", "Text");
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var department = _context.Department.Where(d => d.DepartmentId == model.DepartmentId).First();
                var user = new ApplicationUser
                {
                    UserName = model.Name,
                    Email = model.Email,
                    Downloads = new List<Downloads>(),
                };

                if (model.Role.Equals(RoleHelper.Teacher))
                {
                    user.DepartmentId = model.DepartmentId;
                    user.Department = department;
                }

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (_context.Admin.Where(a => a.Email == model.Email).Count() == 1)
                    {
                        await _userManager.AddToRoleAsync(user, RoleHelper.Admin);
                    }
                    // Adds the Selected Role
                    await _userManager.AddToRoleAsync(user, model.Role);
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    var welcomeModel = new NameLinkVM() { Name = model.Name, Link = callbackUrl };
                    var messgaeBody = base.RenderPartialViewToString("EmailTemplates/Welcome", welcomeModel);
                    await _emailSender.SendEmailAsync(model.Email, "Confirm your account", messgaeBody);
                    //await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User created a new account with password.");

                    // Add user to the Department
                    if (user.DepartmentId != null)
                    {
                        department.Users.Add(user);
                        _context.Update(department);
                        _context.SaveChanges();
                    }

                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            var ti = new CultureInfo("en-US", true).TextInfo; // to convert the lowercase string to Title Case
            // select all roles except the Admin
            var roles = _context.Roles.Where(r => r.Name != RoleHelper.Admin).ToList()
                .Select(s => new
                {
                    Text = ti.ToTitleCase(s.Name),
                    Value = s.Name
                });
            var departments = _context.Department
               .Select(s => new
               {
                   Text = s.Name,
                   Value = s.DepartmentId
               })
               .ToList();

            ViewBag.Roles = new SelectList(roles, "Value", "Text", roles.First());
            // TODO: Modify for the Printer
            ViewBag.Departments = new SelectList(departments, "Value", "Text");

            return View(model);
        }

        //
        // GET: /Account/LogOff
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }


        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

                var viewModel = new NameLinkVM() { Name = user.UserName, Link = callbackUrl };
                var messageBody = base.RenderPartialViewToString("EmailTemplates/ForgotPassword", viewModel);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password", messageBody);
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(HttpContext.User.GetUserId());
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
