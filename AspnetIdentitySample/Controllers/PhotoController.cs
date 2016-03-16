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
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace AspnetIdentitySample.Controllers
{
    public class PhotoController : Controller
    {
        private const int AvatarStoredWidth = 100;  // ToDo - Change the size of the stored avatar image
        private const int AvatarStoredHeight = 100; // ToDo - Change the size of the stored avatar image
        private const int AvatarScreenWidth = 400;  // ToDo - Change the value of the width of the image on the screen

        private const string TempFolder = "/Temp";
        private const string MapTempFolder = "~" + TempFolder;
        private const string AvatarPath = "/Avatars";

        private readonly string[] _imageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg" };

        public UserManager<ApplicationUser> UserManager { get; private set; }
        //
        // GET: /Photo/
        private MyDbContext db;
        private UserManager<ApplicationUser> manager;
        public PhotoController()
        {
            db = new MyDbContext();
            manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }
           [Authorize]
        public ActionResult ProfilePhoto()
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
           [HttpPost]
           public ActionResult DownloadUploadedImage(string t, string l, string h, string w, string fileName)
           {
               try
               {
                   var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new MyDbContext()));

                  
                   var currentUser = manager.FindById(User.Identity.GetUserId());
                   
                  
                      
                  
                   var stream = new MemoryStream(currentUser.ProfilePic);
                   var imgFile= Image.FromStream(stream);
                   fileName = System.IO.Path.Combine(Server.MapPath("/temp/output.jpg"));
                   Thread.Sleep(100);
                   imgFile.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
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
                   var newFileName = Path.Combine(AvatarPath, Path.GetFileNameWithoutExtension(fn) + "_ppcrop" + Path.GetExtension(fn));
                   var newFileLocation = HttpContext.Server.MapPath(newFileName);

                   img.Save(newFileLocation);


               

                   return Json(new { success = true, profilepic = newFileName });
               }
               catch (Exception ex)
               {
                   return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
               }
           }
	}
}