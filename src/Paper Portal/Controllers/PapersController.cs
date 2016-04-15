using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.PlatformAbstractions;
using System.Security.Claims;
using Paper_Portal.Helpers;
using Paper_Portal.Models;
using Paper_Portal.ViewModels.Papers;
using Microsoft.AspNet.Authorization;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Data.Entity;

namespace Portal.Controllers
{
    [RequireHttps]
    [Authorize]
    public class PapersController : Controller
    {
        private ApplicationDbContext _context;
        private IApplicationEnvironment _appEnvironment;

        // Place to store the Uploaded Encrypted Files
        public string UploadPath { get { return _appEnvironment.ApplicationBasePath + "\\Uploads\\"; } }

        public PapersController(ApplicationDbContext context, IApplicationEnvironment environment)
        {
            _appEnvironment = environment;
            _context = context;
        }

        // GET: Papers
        public IActionResult Index(ManageMessageId? message = null)
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
                string UserId = User.GetUserId();
                Papers = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloader)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId && p.Complete == false) // Job is not done
                    .OrderBy(p => p.Created)
                    .ThenBy(p => p.Due)
                    .ToList();
            }
            // otherwise (printer), show all the PDFs
            else
            {
                Papers = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Where(p => p.Complete == false)
                    .OrderBy(p => p.Created)
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
            string UserId = User.GetUserId();

            // Only show the current teachers incomplete papers
            if (User.IsInRole(RoleHelper.Teacher))
            {
                // Store all the completed Papers in the list
                completed = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloader)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId)
                    .Where(p => p.Complete == true)
                    .ToList();
                // Store all the remaining papers uploaded by the teacher
                incomplete = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Include(p => p.Downloader)
                    .ThenInclude(d => d.User)
                    .Where(p => p.Uploader.Id == UserId)
                    .Where(p => p.Complete == false)
                    .ToList();
            }
            else // for printers, show all the incomplete jobs
            {
                // Store all the completed Papers by the printer in the list
                completed = _context.Downloads
                    .Where(d => d.User.Id == UserId)
                    .Select(d => d.Paper)
                    .Where(p => p.Complete == true)
                    .Include(p => p.Uploader)
                    .ToList();

                incomplete = _context.Paper
                    .Include(p => p.Uploader)
                    .Include(p => p.Uploader.Department)
                    .Where(p => p.Complete == false)
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
                return HttpNotFound();
            }

            Paper paper = _context.Paper.Include(p => p.Downloader).Single(m => m.PaperId == id);
            if (paper == null)
            {
                return HttpNotFound();
            }

            return View(paper);
        }

        // GET: Papers/Create
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Create()
        {
            var user = _context.Users.Where(u => u.Id == User.GetUserId()).First();
            if (!user.Verified)
            {
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.NotVerfied });
            }
            return View(new CreateViewModel()
            {
                Copies = 20,
                Instructor = User.GetUserName(),
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
                paper.Created = DateTime.Now;
                paper.Instructor = model.Instructor;
                paper.Title = model.Title;
                paper.Comment = model.Comment;

                paper.FileName = fileName;
                paper.EncKey = pdf.EncKey;
                paper.Hash = pdf.Hash;


                paper.UploaderId = User.GetUserId();
                paper.Uploader = _context.Users.Where(u => u.Id == paper.UploaderId).First();

                paper.Downloader = new List<Downloads>();

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
                return HttpNotFound();
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return HttpNotFound();
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
                return HttpNotFound();
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return HttpNotFound();
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
            _context.Paper.Remove(paper);
            _context.SaveChanges();
            return RedirectToAction("Index", new { Message = ManageMessageId.FileDeletionSuccess });
        }

        // GET: Papers/Download/5
        [ActionName("Download")]
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult Download(int id)
        {
            var user = _context.Users.Where(u => u.Id == User.GetUserId()).First();
            if (!user.Verified)
            {
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.NotVerfied });
            }

            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            if (paper == null)
            {
                return HttpNotFound();
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
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult DownloadConfirmed(int id)
        {
            var UserId = User.GetUserId();
            var user = _context.Users.Include(u => u.Downloads).Single(u => u.Id == UserId);
            if (!user.Verified)
            {
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.NotVerfied });
            }

            Paper paper = _context.Paper.Include(p => p.Downloader).Single(m => m.PaperId == id);

            paper.DownloadsNum = paper.DownloadsNum + 1;


            var filePath = UploadPath + paper.FileName;

            var pdf = new PDF();
            var fileContents = pdf.download(filePath, User.GetUserId(), paper.DownloadsNum, paper.EncKey);

            var Downloads = _context.Downloads.Where(d => d.UserId == UserId && d.PaperId == paper.PaperId).OrderBy(d => d.DownloadedAt).ToList();

            var download = new Downloads();

            download.User = user;
            download.Paper = paper;
            download.DownloadedAt = DateTime.UtcNow;

            // Want this because IDM tries to call this function twice
            if (Downloads.Count != 0) // the user already downloaded this paper, check if it was downloaded recently
            {

                if (paper.Downloader == null)
                {
                    paper.Downloader = new List<Downloads>();
                }
                if (user.Downloads == null)
                {
                    user.Downloads = new List<Downloads>();
                }

                var lastTime = Downloads.Last().DownloadedAt;
                var currentTime = DateTime.UtcNow;

                if (lastTime.AddSeconds(5) > currentTime)
                {
                    _context.Downloads.Add(download);
                    paper.Downloader.Add(download);
                    user.Downloads.Add(download);
                    _context.Update(paper);
                    _context.Update(user);
                }
            }
            else // not downloaded before
            {
                _context.Downloads.Add(download);
                paper.Downloader.Add(download);
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
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult Done(int[] selected)
        {
            foreach (var userId in selected)
            {
                JobDone(userId);
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.JobsDoneSuccess });
        }

        // GET: Papers/Done?id
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult JobDone(int id)
        {
            var temp = _context.Paper
                .Where(p => p.PaperId == id)
                .First();
            if (temp.DownloadsNum < 1)
            {
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.JobDoneFailure });
            }

            temp.Complete = true;
            _context.Update(temp);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.JobDoneSuccess });
        }

        // GET: Papers/UnDone?id
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult JobUnDone(int id)
        {
            var temp = _context.Paper
                .Single(p => p.PaperId == id);

            temp.Complete = false;
            _context.Update(temp);
            _context.SaveChanges();
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
