using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Sprint.Models.ManageViewModels;
using Sprint.Models;
using Sprint.Services;
using Sprint.Helpers;

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
                : message == ManageMessageId.RoleChangeSuccess ? "Roles have been successfully updated"
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            var user = _context.Users
                .Include(u => u.Department)
                .Where(u => u.Id == _userManager.GetUserId(this.User))
                .First();

            var dep = user.Department?.Name;
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

        // GET: Manage/Users
        [HttpGet]
        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public async Task<IActionResult> Users(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.Error ? "Error Message"
                : "";

            var userList = new List<UsersViewModel>();

            var users = _context.Users.Include(u => u.Department).ToList();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // don't show the details of super admin and other admins 
                // to the Admin
                if (User.IsInRole(RoleHelper.Admin))
                {
                    if (roles.Contains(RoleHelper.SuperAdmin) || roles.Contains(RoleHelper.Admin))
                        continue;
                }

                userList.Add(new UsersViewModel(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Department.Name,
                    user.EmailConfirmed,
                    user.Verified,
                    roles)
                    );
            }

            return View(userList);
        }

        // GET: Manage/Roles/5
        [HttpGet]
        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public async Task<IActionResult> Roles(string id)
        {

            var user = _context.Users.Where(u => u.Id == id).First();

            if (user == null)
            {
                return NotFound();
            }

            var allRoles = _context.Roles.Select(u => u.Name).ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            if (User.IsInRole(RoleHelper.Admin))
            {
                if (userRoles.Contains(RoleHelper.SuperAdmin) || userRoles.Contains(RoleHelper.Admin))
                    return Unauthorized();
                allRoles.Remove(RoleHelper.Admin);
                allRoles.Remove(RoleHelper.SuperAdmin);
            }

            var roles = new List<Roles>();
            foreach (var role in allRoles)
            {
                roles.Add(new Roles
                {
                    Name = role,
                    Selected = userRoles.Contains(role)
                }
                );
            }

            var model = new RolesViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles
                
            };

            return View(model);
        }

        // POST: Manage/Roles/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public async Task<IActionResult> Roles(RolesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _userManager.Users.Where(u => u.Id == model.Id).First();
                var userRoles = await _userManager.GetRolesAsync(user);

                var selectedRoles = model.Roles
                                        .Where(x => x.Selected == true && !userRoles.Contains(x.Name))
                                        .Select(x => x.Name)
                                        .ToList();
                var unselectedRoles = model.Roles
                                            .Where(x => x.Selected == false && userRoles.Contains(x.Name))
                                            .Select(x => x.Name)
                                            .ToList();

                await _userManager.AddToRolesAsync(user, selectedRoles);
                await _userManager.RemoveFromRolesAsync(user, unselectedRoles);

                return RedirectToAction("Users", new { Message = ManageMessageId.RoleChangeSuccess });
            }

            return View(model);
        }

        // GET: Manage/Verfiy
        [HttpGet]
        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
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

        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public IActionResult Verify(string[] selected)
        {
            foreach (var userId in selected)
            {
                VerifyUser(userId);
            }
            _context.SaveChanges();
            return RedirectToAction(nameof(VerifyUsers));
        }

        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public IActionResult VerifyUser(string id)
        {
            var temp = _context.Users
                .Where(u => u.Id == id)
                .First();

            if (temp == null)
            {
                return NotFound();
            }

            temp.Verified = true;
            _context.Update(temp);
            _context.SaveChanges();
            return RedirectToAction(nameof(VerifyUsers));
        }

        [Authorize(Roles = RoleHelper.Admin + "," + RoleHelper.SuperAdmin)]
        public IActionResult UnverifyUser(string id)
        {
            var temp = _context.Users
                .Where(u => u.Id == id)
                .First();
            temp.Verified = false;
            _context.Update(temp);
            _context.SaveChanges();
            return RedirectToAction(nameof(Users));
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
            RoleChangeSuccess,
            Error
        }

        #endregion
    }
}
