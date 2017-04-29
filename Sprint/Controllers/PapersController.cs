using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.PlatformAbstractions;
using Sprint.Helpers;
using Sprint.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Sprint.Services;
using Sprint.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Sprint.Models.PaperViewModels;
using Microsoft.AspNetCore.Hosting;
using Sprint.Models.EmailViewModels;

namespace Portal.Controllers
{
    //[RequireHttps]
    [Authorize]
    public class PapersController : BaseController
    {
        private ApplicationDbContext _context;
        private IHostingEnvironment _hostingEnvironment;
        private IEmailSender _emailSender;
        private UserManager<ApplicationUser> _userManager;
        // Place to store the Uploaded Encrypted Files
        public string UploadPath { get { return _hostingEnvironment.ContentRootPath + "\\Uploads\\"; } }

        public PapersController(
            ApplicationDbContext context, 
            IHostingEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ICompositeViewEngine viewEngine,
            IEmailSender emailSender) : base(viewEngine)
        {
            _hostingEnvironment = environment;
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: Papers
        public async Task<IActionResult> IndexAsync(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.FileUploadSuccess ? "Your Paper has been uploaded!"
                : message == ManageMessageId.FileDeletionSuccess ? "Your Paper has been deleted!"
                : message == ManageMessageId.FileDownloadSuccess ? "Your Paper has been downloded!"
                : message == ManageMessageId.FileEditSuccess ? "Your changes has been saved successfully!"
                : message == ManageMessageId.NotVerfied ? "Your Account has not yet been verified by the Admin"
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.JobsDoneSuccess ? "All Downloaded Jobs are Completed Successfully!"
                : message == ManageMessageId.JobDoneSuccess ? "Job Completed Successfully!"
                : message == ManageMessageId.JobDoneFailure ? "Papers should be downloaded before they are marked as Done"
                : "";

            // List of Papers according to the user
            List<Paper> Papers = null;
            // If user is a Teacher, only show the PDF that he uploaded
            if (User.IsInRole(RoleHelper.Teacher))
            {
                string UserId = _userManager.GetUserId(User);
                Papers = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloaders)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId && p.Done == false && p.Delete == false) // Job is not done
                    .OrderBy(p => p.CreatedAt)
                    .ThenBy(p => p.Due)
                    .ToList();
            }
            // show only the department's PDFs which haven't been printed yet
            else if (User.IsInRole(RoleHelper.HOD) || User.IsInRole(RoleHelper.IC))
            {
                var user = await _userManager.GetUserAsync(User);
                var depId = user.DepartmentId;
                Papers = _context.Paper
                     .Include(p => p.Uploader)
                     .Include(p => p.Uploader.Department)
                     .ThenInclude(d => d.DepartmentId)
                     .Where(p => p.Done == false && p.Delete == false && p.Uploader.DepartmentId == depId)
                     .OrderBy(p => p.CreatedAt)
                     .ThenBy(p => p.Due)
                     .ToList();
            }
            // otherwise (admin, super admin), show all the PDFs which haven't been printed yet
            else
            {
                Papers = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Where(p => p.Done == false && p.Delete == false)
                    .OrderBy(p => p.CreatedAt)
                    .ThenBy(p => p.Due)
                    .ToList();
            }

            return View(Papers);
        }

        // GET: Papers/Status
        public IActionResult Status(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] =
                message == ManageMessageId.JobUnDoneSuccess ? "Job Done Status changed to not Done!"
                : "";
            // List of Papers according to the user
            List<Paper> completed = null;
            List<Paper> incomplete = null;

            // Get the Currently logged in User
            string UserId =_userManager.GetUserId(User);

            // Only show the current teachers incomplete papers
            if (User.IsInRole(RoleHelper.Teacher))
            {
                // Store all the completed Papers in the list
                completed = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloaders)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId && p.Delete == false)
                    .Where(p => p.Done == true)
                    .ToList();
                // Store all the remaining papers uploaded by the teacher
                incomplete = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloaders)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId && p.Delete == false)
                    .Where(p => p.Done == false)
                    .ToList();
            }
            else // for printers
            {
                // Store all the completed Papers by the printer in the list
                var temp = _context.Users
                    .Where(u => u.Id ==_userManager.GetUserId(User))
                    .Include(u => u.CompletedJobs)
                    .Single()
                    .CompletedJobs;

                completed = temp != null ? temp.ToList() : new List<Paper>();
                //completed = _context.Downloads
                //    .Where(d => d.User.Id == UserId)
                //    .Select(d => d.Paper)
                //    .Where(p => p.Done == true)
                //    .Include(p => p.Uploader)
                //    .ToList();

                incomplete = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Where(p => p.Done == false && p.Delete == false)
                    .ToList();
            }

            var StatusVM = new StatusViewModel
            {
                Completed = completed,
                Incomplete = incomplete,
            };


            return View(StatusVM);
        }

        // GET: Papers/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Paper paper = _context.Paper.Include(p => p.Downloaders).Include(p => p.DoneBy).Single(m => m.PaperId == id);
            if (paper == null)
            {
                return NotFound();
            }

            return View(paper);
        }

        // GET: Papers/Create
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Create()
        {
            var user = _context.Users.Where(u => u.Id == _userManager.GetUserId(User)).First();
            if (!user.Verified)
            {
                return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.NotVerfied });
            }
            return View(new CreateViewModel()
            {
                Copies = 20,
                Instructor = user.FullName,
                Due = DateTime.Now.AddDays(2),
            });
        }

        // POST: Papers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Create(CreateViewModel model)
        {
            // create a unique fileName using TimeStamp, Remove the whitespace from the Title
            string fileName = DateTime.UtcNow.ToFileTimeUtc() + "-" + model.Title.Replace(" ", String.Empty);

            string filePath = UploadPath + fileName;

            var pdf = new PDF();
            bool uploaded = pdf.upload(model.File, filePath);

            if (uploaded && ModelState.IsValid)
            {
                Paper paper = new Paper();

                paper.Copies = model.Copies;
                paper.Due = model.Due;
                paper.CreatedAt = DateTime.Now;
                paper.Instructor = model.Instructor;
                paper.Title = model.Title;
                paper.Comment = model.Comment;

                paper.FileName = fileName;
                paper.EncKey = pdf.EncKey;
                paper.Hash = pdf.Hash;


                paper.UploaderId = _userManager.GetUserId(User);
                paper.Uploader = _context.Users.Where(u => u.Id == paper.UploaderId).First();

                paper.Downloaders = new List<Downloads>();

                _context.Paper.Add(paper);
                _context.SaveChanges();
                return RedirectToAction("Index", new { Message = ManageMessageId.FileUploadSuccess });

            }

            // if there was some error, store it and return the user to the Upload form
            ModelState.AddModelError("File", pdf.Error);
            return View(model);
        }

        // GET: Papers/Edit/5
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return NotFound();
            }
            return View(paper);
        }

        // POST: Papers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Edit(Paper paper)
        {
            // Retrieve the original paper from DB for modification
            Paper original = _context.Paper.Single(m => m.PaperId == paper.PaperId);
            // modify the values recieved from the form
            original.Copies = paper.Copies;
            original.Due = paper.Due;
            original.Instructor = paper.Instructor;
            original.Title = paper.Title;

            // save changes to the DB
            if (ModelState.IsValid)
            {
                _context.Update(original);
                _context.SaveChanges();
                return RedirectToAction("Index", new { Message = ManageMessageId.FileEditSuccess });
            }

            ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Uploader", paper.UploaderId);
            return View(paper);
        }

        // GET: Papers/Delete/5
        [ActionName("Delete")]
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return NotFound();
            }
            // TODO: Remove the Details
            return View(paper);
        }

        // POST: Papers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult DeleteConfirmed(int id)
        {
            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            paper.Delete = true;
            _context.Update(paper);
            _context.SaveChanges();
            return RedirectToAction("Index", new { Message = ManageMessageId.FileDeletionSuccess });
        }

        // GET: Papers/Download/5
        [ActionName("Download")]
        [Authorize(Roles = RoleHelper.IC)]
        public IActionResult Download(int id)
        {
            var user = _context.Users.Where(u => u.Id == _userManager.GetUserId(User)).First();
            if (!user.Verified)
            {
                return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.NotVerfied });
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return NotFound();
            }

            var filePath = UploadPath + paper.FileName;

            var pdf = new PDF();
            bool verified = pdf.Verify(paper.Hash, filePath);

            if (!verified)
            {
                ViewBag.Verified = "False";
            }
            return View(paper);
        }

        // POST: Papers/Download/5
        [HttpPost, ActionName("Download")]
        //[ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.IC)]
        public IActionResult DownloadConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = _context.Users.Include(u => u.Downloads).Single(u => u.Id == userId);
            if (!user.Verified)
            {
                return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.NotVerfied });
            }

            Paper paper = _context.Paper.Include(p => p.Downloaders).Single(m => m.PaperId == id);

            paper.DownloadsNum = paper.DownloadsNum + 1;


            var filePath = UploadPath + paper.FileName;

            var pdf = new PDF();
            var fileContents = pdf.download(filePath, userId, paper.DownloadsNum, paper.EncKey);

            var Downloads = _context.Downloads.Where(d => d.UserId == userId && d.PaperId == paper.PaperId).OrderBy(d => d.DownloadedAt).ToList();

            var download = new Downloads();

            download.User = user;
            download.Paper = paper;
            download.DownloadedAt = DateTime.UtcNow;

            // Want this because IDM tries to call this function twice
            if (Downloads.Count != 0) // the user already downloaded this paper, check if it was downloaded recently
            {

                if (paper.Downloaders == null)
                {
                    paper.Downloaders = new List<Downloads>();
                }
                if (user.Downloads == null)
                {
                    user.Downloads = new List<Downloads>();
                }

                var lastTime = Downloads.Last().DownloadedAt;
                var currentTime = DateTime.UtcNow;

                if (currentTime > lastTime.AddSeconds(5))
                {
                    _context.Downloads.Add(download);
                    paper.Downloaders.Add(download);
                    user.Downloads.Add(download);
                    _context.Update(paper);
                    _context.Update(user);
                }
            }
            else // not downloaded before
            {
                _context.Downloads.Add(download);
                paper.Downloaders.Add(download);
                user.Downloads.Add(download);
                _context.Update(paper);
                _context.Update(user);
            }

            _context.SaveChanges();

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = paper.Title + ".pdf",

                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };

            Response.Headers.Add("Content-Disposition", cd.ToString());
            return File(fileContents, "application/pdf");
        }



        // POST: Papers/Done
        [Authorize(Roles = RoleHelper.IC)]
        public async Task<IActionResult> Done(int[] selected)
        {
            foreach (var userId in selected)
            {
                await JobDone(userId);
            }
            return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.JobsDoneSuccess });
        }

        // GET: Papers/Done?id
        [Authorize(Roles = RoleHelper.IC)]
        public async Task<IActionResult> JobDone(int id)
        {
            var paper = _context.Paper
                .Include(p => p.Uploader)
                .Where(p => p.PaperId == id)
                .First();
            if (paper.DownloadsNum < 1)
            {
                return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.JobDoneFailure });
            }

            paper.Done = true;

            var user = _context.Users.Include(u => u.CompletedJobs).Single(u => u.Id == _userManager.GetUserId(User));
            paper.DoneById = user.Id;
            paper.DoneBy = user;
            
            if (user.CompletedJobs == null)
            {
                user.CompletedJobs = new List<Paper>();
            }
            user.CompletedJobs.Add(paper);

            _context.Update(paper);
            _context.Update(user);
            _context.SaveChanges();

            // Email to the Uploader
            var uploader = paper.Uploader;
            var JobStatusVM = new JobStatusViewModel()
            {
                Action = "Done",
                ActionBy = (user.FullName != null) ? user.FullName : user.UserName,
                At = DateTime.UtcNow.ToLocalTime(),
                Copies = paper.Copies,
                Title = paper.Title,
                Detail = Url.Action("Details", "Papers", new { id = paper.PaperId }, protocol: HttpContext.Request.Scheme),
                BaseURL = Url.Action("Index", "Home", null, protocol: HttpContext.Request.Scheme),
        };

            var messageBody = base.RenderViewAsString(JobStatusVM, "EmailTemplates/JobStatus");
            await _emailSender.SendEmailAsync(uploader.Email, paper.Title + ", Job Status!", messageBody);
            return RedirectToAction(nameof(IndexAsync), new { Message = ManageMessageId.JobDoneSuccess });
        }

        // GET: Papers/UnDone?id
        [Authorize(Roles = RoleHelper.IC)]
        public async Task<IActionResult> JobUnDone(int id)
        {
            var paper = _context.Paper.Include(p => p.Uploader).Single(p => p.PaperId == id);
            var user = _context.Users.Include(u => u.CompletedJobs).Single(u => u.Id == _userManager.GetUserId(User));

            user.CompletedJobs.Remove(paper);
            paper.Done = false;
            paper.DoneBy = null;
            paper.DoneById = null;

            _context.Update(paper);
            _context.Update(user);
            _context.SaveChanges();

            // Email to the Uploader
            var uploader = paper.Uploader;
            var JobStatusVM = new JobStatusViewModel()
            {
                Action = "Removed from Done",
                ActionBy = (user.FullName != null) ? user.FullName : user.UserName,
                At = DateTime.UtcNow.ToLocalTime(),
                Copies = paper.Copies,
                Title = paper.Title,
                Detail = Url.Action("Details", "Papers", new { id = paper.PaperId }, protocol: HttpContext.Request.Scheme),
                BaseURL = Url.Action("Index", "Home", null, protocol: HttpContext.Request.Scheme),
            };

            var messageBody = base.RenderViewAsString(JobStatusVM, "EmailTemplates/JobStatus");
            await _emailSender.SendEmailAsync(uploader.Email, paper.Title + ", Job Status!", messageBody);

            return RedirectToAction(nameof(Status), new { Message = ManageMessageId.JobUnDoneSuccess });
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
            FileUploadSuccess,
            FileDeletionSuccess,
            FileEditSuccess,
            FileDownloadSuccess,
            JobsDoneSuccess,
            JobDoneSuccess,
            JobDoneFailure,
            JobUnDoneSuccess,
            NotVerfied,
            Error
        }

        #endregion
    }
}
