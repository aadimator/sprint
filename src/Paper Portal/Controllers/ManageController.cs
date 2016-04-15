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
using Microsoft.AspNet.Mvc.Rendering;

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
        public IActionResult Index(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.EditSuccess ? "User Profile has been updated!"
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            var user = _context.Users
                .Include(u => u.Department)
                .Where(u => u.Id == User.GetUserId())
                .First();

            var dep = (user.Department != null) ? user.Department.Name : "Printer";
            var model = new IndexViewModel
            {
                Department = dep,
                Email = user.Email,
                FullName = user.FullName,
                UserName = user.UserName,
            };
            return View(model);
        }

        //
        // GET: /Manage/Edit
        [HttpGet]
        public IActionResult Edit()
        {

            var user = _context.Users
                .Include(u => u.Department)
                .Where(u => u.Id == User.GetUserId())
                .First();

            var dep = user.Department.Name;
            var model = new EditVM
            {
                DepartmentId = user.Department.DepartmentId,
                FullName = user.FullName,
            };

            var departments = _context.Department
                .Select(s => new
                {
                    Text = s.Name,
                    Value = s.DepartmentId
                })
                .ToList();
            ViewBag.Departments = new SelectList(departments, "Value", "Text");
            return View(model);
        }

        //
        // Post: /Manage/Edit
        [HttpPost]
        public IActionResult Edit(EditVM model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.Include(u => u.Department).Single(u => u.Id == User.GetUserId());
                var prevDepartment = user.Department; // remove user from this 
                var newDepartment = _context.Department.Single(d => d.DepartmentId == model.DepartmentId);
                prevDepartment.Users.Remove(user);

                user.FullName = model.FullName;
                user.DepartmentId = model.DepartmentId;
                user.Department = newDepartment;
                newDepartment.Users.Add(user);

                _context.Update(user);
                _context.Update(prevDepartment);
                _context.Update(newDepartment);
                _context.SaveChanges();

                return RedirectToAction("Index", new { Message = ManageMessageId.EditSuccess });
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("Edit");
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
        [Authorize(Roles = RoleHelper.Admin)]
        public async Task<IActionResult> VerifyUsers()
        {
            List<VerifyUsersViewModel> UserList = new List<VerifyUsersViewModel>();

            var Data = _userManager.Users
                .Where(p => p.Verified == false)
                .ToList();

            foreach (var user in Data)
            {
                var department = "Printer";
                if (await _userManager.IsInRoleAsync(user, RoleHelper.Teacher))
                {
                    department = _context.Users.Include(u => u.Department).Where(u => u.Id == user.Id).First().Department.Name;
                }
                UserList.Add(new VerifyUsersViewModel(
                    user.Id,
                    user.FullName,
                    user.Email,
                    department,
                    user.EmailConfirmed)
                    );
            }
            return View(UserList);
        }

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
            EditSuccess,
            Error
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(HttpContext.User.GetUserId());
        }

        #endregion
    }
}
