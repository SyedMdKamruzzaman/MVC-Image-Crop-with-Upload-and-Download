using AspnetIdentitySample.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Helpers;
using System.Data.Entity;

namespace AspnetIdentitySample.Controllers
{
    public class HomeController : Controller
    {
        private const int AvatarStoredWidth = 100;  // ToDo - Change the size of the stored avatar image
        private const int AvatarStoredHeight = 100; // ToDo - Change the size of the stored avatar image
        private const int AvatarScreenWidth = 400;  // ToDo - Change the value of the width of the image on the screen

        private const string TempFolder = "/Temp";
        private const string MapTempFolder = "~" + TempFolder;
        private const string AvatarPath = "/Avatars";

        private readonly string[] _imageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg" };

        public UserManager<ApplicationUser> UserManager { get; private set; }

          private MyDbContext db;
        private UserManager<ApplicationUser> manager;
        public HomeController()
        {
            db = new MyDbContext();
            manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }
       
        public ActionResult Index()
        {
            return View();
        }

        // Only Authenticated users can access thier profile
        [Authorize]
        public ActionResult Profile()
        {
            // Instantiate the ASP.NET Identity system
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new MyDbContext()));
            
            // Get the current logged in User and look up the user in ASP.NET Identity
            var currentUser = manager.FindById(User.Identity.GetUserId()); 
            
            // Recover the profile information about the logged in user
          
            ViewBag.FirstName = currentUser.MyUserInfo.FirstName;
            if (currentUser.ProfilePic != null)
            {
                ViewBag.ProfilePic = "data:image/jpeg;base64," + Convert.ToBase64String(currentUser.ProfilePic);
                
            }

            

            return View();
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpGet]
        public ActionResult _Profile()
        {
            
            return View();
        }

        [ValidateAntiForgeryToken]
        public ActionResult _Profile(IEnumerable<HttpPostedFileBase> files)
        {
            if (files == null || !files.Any()) return Json(new { success = false, errorMessage = "No file uploaded." });
            var file = files.FirstOrDefault();  // get ONE only
            if (file == null || !IsImage(file)) return Json(new { success = false, errorMessage = "File is of wrong format." });
            if (file.ContentLength <= 0) return Json(new { success = false, errorMessage = "File cannot be zero length." });
            var webPath = GetTempSavedFilePath(file);
            return Json(new { success = true, fileName = webPath.Replace("/", "\\") }); // success
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            if (file == null) return false;
            return file.ContentType.Contains("image") ||
                _imageFileExtensions.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private string GetTempSavedFilePath(HttpPostedFileBase file)
        {
            // Define destination
            var serverPath = HttpContext.Server.MapPath(TempFolder);
            if (Directory.Exists(serverPath) == false)
            {
                Directory.CreateDirectory(serverPath);
            }

            // Generate unique file name
            var fileName = Path.GetFileName(file.FileName);
            fileName = SaveTemporaryAvatarFileImage(file, serverPath, fileName);

            // Clean up old files after every save
            CleanUpTempFolder(1);
            return Path.Combine(TempFolder, fileName);
        }

        [HttpPost]
        public ActionResult SaveUploadedImage(string t, string l, string h, string w, string fileName)
        {
            try
            {
                // Calculate dimensions
                var top = Convert.ToInt32(t.Replace("-", "").Replace("px", ""));
                var left = Convert.ToInt32(l.Replace("-", "").Replace("px", ""));
                var height = Convert.ToInt32(h.Replace("-", "").Replace("px", ""));
                var width = Convert.ToInt32(w.Replace("-", "").Replace("px", ""));

                // Get file from temporary folder
                var fn = Path.Combine(Server.MapPath(MapTempFolder), Path.GetFileName(fileName));
                // ...get image and resize it, ...
                var img = new WebImage(fn);
                img.Resize(width, height);
                // ... crop the part the user selected, ...
                img.Crop(top, left, img.Height - top - AvatarStoredHeight, img.Width - left - AvatarStoredWidth);
                // ... delete the temporary file,...
                System.IO.File.Delete(fn);
                // ... and save the new one.
                var newFileName = Path.Combine(AvatarPath, Path.GetFileName(fn));
                var newFileLocation = HttpContext.Server.MapPath(newFileName);
                if (Directory.Exists(Path.GetDirectoryName(newFileLocation)) == false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileLocation));
                }

                img.Save(newFileLocation);

                var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new MyDbContext()));

                // Get the current logged in User and look up the user in ASP.NET Identity
                var currentUser = manager.FindById(User.Identity.GetUserId());

                var user = new ApplicationUser() { Id = currentUser.Id };

                user.ProfilePic = img.GetBytes();

                using (var db = new MyDbContext())
                {
                    db.Users.Attach(user);
                    db.Entry(user).Property(x => x.ProfilePic).IsModified = true;
                    db.SaveChanges();
                }

                
                

                //db.Entry(user).State = EntityState.Modified;

                 //db.SaveChangesAsync();




                 return Json(new { success = true, avatarFileLocation = newFileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
            }
        }



        [HttpPost]
        public ActionResult DownloadUploadedImage(string t, string l, string h, string w, string fileName)
        {
            try
            {
                // Calculate dimensions
                var top = Convert.ToInt32(t.Replace("-", "").Replace("px", ""));
                var left = Convert.ToInt32(l.Replace("-", "").Replace("px", ""));
                var height = Convert.ToInt32(h.Replace("-", "").Replace("px", ""));
                var width = Convert.ToInt32(w.Replace("-", "").Replace("px", ""));

                // Get file from temporary folder
                var fn = Path.Combine(Server.MapPath(MapTempFolder), Path.GetFileName(fileName));
                // ...get image and resize it, ...
                var img = new WebImage(fn);
                img.Resize(width, height);
                // ... crop the part the user selected, ...
                img.Crop(top, left, img.Height - top - AvatarStoredHeight, img.Width - left - AvatarStoredWidth);
                // ... delete the temporary file,...
                //System.IO.File.Delete(fn);
                // ... and save the new one.
                var newFileName = Path.Combine(AvatarPath, Path.GetFileNameWithoutExtension(fn)+"_new"+Path.GetExtension(fn));
                var newFileLocation = HttpContext.Server.MapPath(newFileName);

                img.Save(newFileLocation);
                
              
                //Response.AddHeader("Content-Disposition", "attachment;filename=ProfilePhoto.png");
                //Response.WriteFile(newFileLocation);
                //Response.End();

                return Json(new { success = true, profilepic = newFileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
            }
        }
      
        private static string SaveTemporaryAvatarFileImage(HttpPostedFileBase file, string serverPath, string fileName)
        {
            var img = new WebImage(file.InputStream);
            var ratio = img.Height / (double)img.Width;
            img.Resize(AvatarScreenWidth, (int)(AvatarScreenWidth * ratio));

            var fullFileName = Path.Combine(serverPath, fileName);
            if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
            }

            img.Save(fullFileName);
            return Path.GetFileName(img.FileName);
        }

        private void CleanUpTempFolder(int hoursOld)
        {
            try
            {
                var currentUtcNow = DateTime.UtcNow;
                var serverPath = HttpContext.Server.MapPath("/Temp");
                if (!Directory.Exists(serverPath)) return;
                var fileEntries = Directory.GetFiles(serverPath);
                foreach (var fileEntry in fileEntries)
                {
                    var fileCreationTime = System.IO.File.GetCreationTimeUtc(fileEntry);
                    var res = currentUtcNow - fileCreationTime;
                    if (res.TotalHours > hoursOld)
                    {
                        System.IO.File.Delete(fileEntry);
                    }
                }
            }
            catch
            {
                // Deliberately empty.
            }
        }
    }
}