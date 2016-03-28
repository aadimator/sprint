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

namespace Portal.Controllers
{
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
        public IActionResult Index()
        {
            // List of Papers according to the user
            List<Paper> Papers = null;
            // If user is a Teacher, only show the PDF that he uploaded
            if (User.IsInRole(RoleHelper.Teacher))
            {
                string UserId = User.GetUserId();
                Papers = _context.Paper
                    .Where(p => p.Uploader.Id == UserId)
                    .OrderBy(p => p.DownloadsNum)
                    .ThenBy(p => p.Due)
                    .ToList();
                //Papers = _context.Users.Where(p => p.Id == UserId).First().Papers.ToList();
            }
            // otherwise (admin, printer), show all the PDFs
            else
            {
                Papers = _context.Paper
                    .OrderBy(p => p.DownloadsNum)
                    .ThenBy(p => p.Due)
                    .ToList();
            }

            return View(Papers);
        }

        // GET: Papers/Details/5
        public IActionResult Details(int? id)
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

        // GET: Papers/Create
        [Authorize(Roles = RoleHelper.Teacher)]
        public IActionResult Create()
        {
            return View(new CreateViewModel());
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
                paper.Instructor = model.Instructor;
                paper.Title = model.Title;

                paper.FileName = fileName;
                paper.EncKey = pdf.EncKey;
                paper.Hash = pdf.Hash;
                paper.UploaderId = User.GetUserId();

                _context.Paper.Add(paper);
                _context.SaveChanges();
                return RedirectToAction("Index");

            }

            // if there was some error, store it and return the user to the Upload form
            ModelState.AddModelError("file", pdf.Error);
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
                return RedirectToAction("Index");
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
            return RedirectToAction("Index");
        }

        // GET: Papers/Download/5
        [ActionName("Download")]
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult Download(int? id)
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleHelper.Printer)]
        public IActionResult DownloadConfirmed(int id)
        {
            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            paper.DownloadsNum = paper.DownloadsNum + 1;
            
            var filePath = UploadPath + paper.FileName;

            var pdf = new PDF();
            var fileContents = pdf.download(filePath, User.GetUserId(), paper.DownloadsNum, paper.EncKey);

            _context.Update(paper);
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
    }
}
