using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using Paper_Portal.Models;

namespace Portal.Controllers
{
    public class PapersController : Controller
    {
        private ApplicationDbContext _context;

        public PapersController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: Papers
        public IActionResult Index()
        {
            var applicationDbContext = _context.Paper.Include(p => p.Downloader).Include(p => p.Uploader);
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
            ViewData["DownloaderId"] = new SelectList(_context.Users, "Id", "Downloader");
            ViewData["UploaderId"] = new SelectList(_context.Users, "Id", "Uploader");
            return View();
        }

        // POST: Papers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Paper paper)
        {
            if (ModelState.IsValid)
            {
                _context.Paper.Add(paper);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewData["DownloaderId"] = new SelectList(_context.Users, "Id", "Downloader", paper.DownloaderId);
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
            ViewData["DownloaderId"] = new SelectList(_context.Users, "Id", "Downloader", paper.DownloaderId);
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
            ViewData["DownloaderId"] = new SelectList(_context.Users, "Id", "Downloader", paper.DownloaderId);
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
    }
}
