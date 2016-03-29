using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Logging;
using Paper_Portal.Models;
using Paper_Portal.Services;
using Paper_Portal.ViewModels.Manage;
using Microsoft.Data.Entity;
using Paper_Portal.Helpers;

namespace Paper_Portal.Controllers
{
    [RequireHttps]
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private ApplicationDbContext _context;

        public ManageController(
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
            _logger = loggerFactory.CreateLogger<ManageController>();
            _context = context;
        }

        //
        // GET: /Manage/Index
        [HttpGet]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            var user = _context.Users
                .Include(u => u.Department)
                .Where(u => u.Id == User.GetUserId())
                .First();

            var dep = user.Department.Name;
            var model = new IndexViewModel
            {
                Department = user.Department.Name,
                Email = user.Email,
                FullName = user.FullName,
                UserName = user.UserName,
            };
            return View(model);
        }

        //
        // GET: /Manage/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User changed their password successfully.");
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        // GET: Manage/Verfiy
        public IActionResult VerifyUsers()
        {
            List<VerifyUsersViewModel> Users = new List<VerifyUsersViewModel>();

            var Data = _userManager.Users
                .Where(p => p.Verified == false)
                .Select(p => new
                {
                    p.Id,
                    p.FullName,
                    p.Email,
                    Department = p.Department.Name,
                    p.EmailConfirmed
                })
                .ToList();

            foreach (var user in Data)
            {
                Users.Add(new VerifyUsersViewModel(user.Id, user.FullName, user.Email, user.Department, user.EmailConfirmed));
            }
            return View(Users);
        }

        // TODO: Implement it later
        [Authorize (Roles = RoleHelper.Admin)]
        public IActionResult Verify(string[] selected)
        {
            foreach (var userId in selected)
            {
                VerifyUser(userId);
            }
            _context.SaveChanges();
            return RedirectToAction(nameof(VerifyUsers));
        }

        [Authorize(Roles = RoleHelper.Admin)]
        public IActionResult VerifyUser(string id)
        {
            var temp = _context.Users
                .Where(u => u.Id == id)
                .First();
            temp.Verified = true;
            _context.Update(temp);
            _context.SaveChanges();
            return RedirectToAction(nameof(VerifyUsers));
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            Error
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(HttpContext.User.GetUserId());
        }

        #endregion
    }
}
