using System.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Paper_Portal.Helpers;
using Paper_Portal.Models;
using Paper_Portal.ViewModels.Papers;
using Microsoft.Net.Http.Headers;

namespace Portal.Controllers
{
    public class PapersController : Controller
    {
        private ApplicationDbContext _context;
        private IApplicationEnvironment _appEnvironment;

        public string UploadPath { get { return _appEnvironment.ApplicationBasePath + "\\Uploads\\"; } }

        public PapersController(ApplicationDbContext context, IApplicationEnvironment environment)
        {
            _appEnvironment = environment;
            _context = context;    
        }

        // GET: Papers
        public IActionResult Index()
        {
            var applicationDbContext = _context.Paper.Include(p => p.Uploader);
            return View(applicationDbContext.ToList());
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
        public IActionResult Create()
        {
            return View(new CreateViewModel());
        }

        // POST: Papers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateViewModel model)
        {
            string filePath = UploadPath + model.FileName;
            var pdf = new PDF();
            pdf.upload(model.File, filePath);

            Paper paper = new Paper();
            paper.Copies = model.Copies;
            paper.Due = model.Due;
            paper.Instructor = model.Instructor;
            paper.FileName = model.FileName;
            paper.EncKey = pdf.EncKey;
            paper.Hash = pdf.Hash;
            paper.UploaderId = User.GetUserId();

            if (ModelState.IsValid)
            {
                _context.Paper.Add(paper);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Uploader", paper.UploaderId);
            return View(paper);
        }

        // GET: Papers/Edit/5
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
            ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Uploader", paper.UploaderId);
            return View(paper);
        }

        // POST: Papers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Paper paper)
        {
            if (ModelState.IsValid)
            {
                _context.Update(paper);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Uploader", paper.UploaderId);
            return View(paper);
        }

        // GET: Papers/Delete/5
        [ActionName("Delete")]
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
        public IActionResult DeleteConfirmed(int id)
        {
            Paper paper = _context.Paper.Single(m => m.PaperId == id);
            _context.Paper.Remove(paper);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Papers/Download/5
        [ActionName("Download")]
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

            if (! verified)
            {
                ViewBag.Verified = "False";
            }
            return View(paper);
        }

        // POST: Papers/Download/5
        [HttpPost, ActionName("Download")]
        [ValidateAntiForgeryToken]
        public IActionResult DownloadConfirmed(int id)
        {
            Paper paper = _context.Paper.Single(m => m.PaperId == id);

            var filePath = UploadPath + paper.FileName;

            var pdf = new PDF();

            var stream = pdf.download(filePath, paper.EncKey);

            var cd = new System.Net.Mime.ContentDisposition
            {
                // for example foo.bak
                FileName = paper.FileName + ".pdf",

                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            return File(stream, "application/pdf");
        }
    }
}
