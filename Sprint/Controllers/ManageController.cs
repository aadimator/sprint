﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sprint.Models;
using Sprint.Services;
using Sprint.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Sprint.Models.ManageViewModels;

namespace Sprint.Controllers
{
    //[RequireHttps]
    [Authorize]
    public class ManageController : BaseController
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
        ICompositeViewEngine viewEngine,
        ILoggerFactory loggerFactory,
        ApplicationDbContext context) : base(viewEngine)
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
                .Where(u => u.Id == _userManager.GetUserId(this.User))
                .First();

            var dep = (user.Department != null) ? user.Department.Name : null;
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
                .Where(u => u.Id == _userManager.GetUserId(User))
                .First();

            var model = new EditViewModel
            {
                DepartmentId = (user.Department != null) ? user.Department.DepartmentId : -1,
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
        // POST: /Manage/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users
                    .Include(u => u.Department)
                    .Single(u => u.Id == _userManager.GetUserId(User));

                if (user.DepartmentId != model.DepartmentId)
                {
                    var prevDepartment = user.Department; // remove user from this 
                    prevDepartment.Users.Remove(user);

                    var newDepartment = _context.Department.Single(d => d.DepartmentId == model.DepartmentId);

                    user.DepartmentId = model.DepartmentId;
                    user.Department = newDepartment;

                    if (newDepartment.Users == null)
                    {
                        newDepartment.Users = new List<ApplicationUser>();
                    }
                    newDepartment.Users.Add(user);

                    _context.Update(prevDepartment);
                    _context.Update(newDepartment);
                }

                // update the FullName for both Teachers and Printers
                user.FullName = model.FullName;
                _context.Update(user);
                _context.SaveChanges();

                return RedirectToAction("Index", new { Message = ManageMessageId.EditSuccess });
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("Edit");
        }

        // GET: Manage/Users
        [HttpGet]
        [Authorize(Roles = RoleHelper.Admin)]
        public IActionResult Users(ManageMessageId? message = null)
        {
            //ViewData["StatusMessage"] =
            //    message == ManageMessageId.Error ? "Papers should be downloaded before they are marked as Done"
            //    : "";

            var Users = _context.Users.ToList();

            return View(Users);
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
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.GetUserAsync(User);
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


        ////
        //// GET: /Manage/ChangeEmail
        //[HttpGet]
        //public IActionResult ChangeEmail()
        //{
        //    return View();
        //}

        ////
        //// POST: /Manage/ChangeEmail
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        //{
        //    var modelError = "";

        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    var user = await GetCurrentUserAsync();
        //    if (user != null)
        //    {
        //        var correctPassword = await _userManager.CheckPasswordAsync(user, model.OldPassword);
        //        if (correctPassword)
        //        {
        //            // Check if email already exists
        //            if (_context.Users.Where(u => u.Email == model.NewEmail).Count() == 0)
        //            {
        //                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        //                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

        //                var EmailChangeNotification = new NameLinkVM()
        //                {
        //                    Name = (user.FullName != null) ? user.FullName : user.UserName,
        //                    BaseURL = Url.Action("Index", "Home", null, protocol: HttpContext.Request.Scheme),
        //                };

        //                var emailNotification = base.RenderViewAsString("EmailTemplates/EmailChanageNotification", EmailChangeNotification);
        //                await _emailSender.SendEmailAsync(user.Email, "Confirm your account", emailNotification);



        //                _logger.LogInformation(3, "User changed their email successfully.");
        //                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
        //            }
        //            else
        //            {
        //                modelError = "Email Already Exists!";
        //            }

        //        }
        //        else
        //        {
        //            modelError = "Incorrect Password!";
        //        }

        //        ModelState.AddModelError(String.Empty, modelError);
        //        return View(model);
        //    }
        //    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        //}


        // GET: Manage/Verfiy
        [Authorize(Roles = RoleHelper.Admin)]
        public IActionResult VerifyUsers()
        {
            List<VerifyUsersViewModel> UserList = new List<VerifyUsersViewModel>();

            var Data = _userManager.Users
                .Where(p => p.Verified == false)
                .ToList();

            foreach (var user in Data)
            {
                var department = _context.Users.Include(u => u.Department).Where(u => u.Id == user.Id).First().Department.Name;

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

        [Authorize(Roles = RoleHelper.Admin)]
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

        #endregion
    }
}
