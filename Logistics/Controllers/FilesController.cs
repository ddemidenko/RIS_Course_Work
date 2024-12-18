using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FileServer;
using FileServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileServer.TCP;

namespace Employee.Controllers
{
    public class FilesController : Controller
    {
        private readonly ApplicationContext _context;
        IWebHostEnvironment _appEnvironment;

        public FilesController(ApplicationContext context, IWebHostEnvironment appEnvironment)
        {
            _context = context;
            _appEnvironment = appEnvironment;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            if (findUser.RoleModelId == 1)
            {
                var res = _context.Files
                    .Include(x => x.UserModel)
                    .AsNoTracking()
                    .ToList();

                return View(res);
            }

            var acc = _context.Accesses
                .Include(x => x.FileModel)
                .Where(x => x.UserModelId == findUser.Id)
                .Select(x => new FileModel
                {
                    Id = x.FileModel.Id,
                    Name = x.FileModel.Name,
                    UserModelId = x.FileModel.UserModelId,
                    Bytes = x.FileModel.Bytes,
                    ShareToAll = x.FileModel.ShareToAll,
                    Path = x.FileModel.Path,
                    UserModel = x.FileModel.UserModel,
                    SharedUsers = x.FileModel.SharedUsers
                }).AsEnumerable();

            var query = _context.Files
                .Where(x => x.ShareToAll == 2 || x.UserModelId == findUser.Id)
                .Include(x => x.UserModel)
                .AsNoTracking()
                .AsEnumerable();

            var result = acc.Union(query).ToList();

            _context.DetachEntities(result);
            return View(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            var itemToUpdate = await _context.Files
                .Include(x => x.UserModel)
                .FirstAsync(t => t.Id == id);

            _context.DetachEntity(itemToUpdate);

            var findAccess = _context.Accesses.FirstOrDefault(u => u.FileModel.Id == itemToUpdate.Id && u.AccessLevel == 2 );

            if (findAccess is not null || findUser.RoleModelId == 1 || itemToUpdate.UserModelId == findUser.Id)
            {
                var radioTypes = new [] {
                    new {Id = 1, Name = "Приватный"},
                    new {Id = 2, Name = "Общий"}
                };

                ViewBag.Radio = radioTypes;

                return View(itemToUpdate);

            }

            return RedirectToAction("Index", "Files");
        }

        [HttpPost]
        public async Task<IActionResult> Update(FileModel item)
        {
            _context.Files.Update(item);
            await _context.SaveChangesAsync();

            _context.DetachEntity(item);

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            var itemToDelete = await _context.Files
                .FirstOrDefaultAsync(t => t.Id == id);

            var findAccess = _context.Accesses.FirstOrDefault(u => u.FileModel.Id == itemToDelete.Id && u.AccessLevel == 2 );

            if (findAccess is not null || findUser.RoleModelId == 1 || itemToDelete.UserModelId == findUser.Id)
            {
                _context.Files.Remove(itemToDelete);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "Files");
        }

        public async Task<IActionResult> Download(int id)
        {
            var itemToUpdate = await _context.Files
                .Include(x => x.UserModel)
                .FirstAsync(t => t.Id == id);

            _context.DetachEntity(itemToUpdate);

            return File(itemToUpdate.Bytes, "application/pdf", itemToUpdate.Name);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            return View();
        }

        public async Task<IActionResult> Create(IFormFile uploadedFile)
        {
            var user = HttpContext.User.Identity.Name;

            var findUser = _context.Users.FirstOrDefault(u => u.Login == user);

            if (uploadedFile != null)
            {
                string path = "/Files/" + uploadedFile.FileName;
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }
            }

            var a = _appEnvironment.WebRootPath + "/Files/" + uploadedFile.FileName;

            Stream file = System.IO.File.OpenRead(a);
            byte[] bytes = new byte[file.Length];

            file.Read(bytes);

            IPEndPoint ep = new(IPAddress.Loopback, 1488);
            FileServer.TCP.CustomTcpClient oclTcpClient = new();

            await oclTcpClient.ConnectAsync(ep);


            NetworkStream stream = oclTcpClient.GetStream();

            byte[] buffer = BitConverter.GetBytes((int)bytes.Length);

            var code = 3;
            var b = BitConverter.GetBytes(code);
            stream.Write(b, 0, 1);

            var userId = findUser.Id;
            var uI = BitConverter.GetBytes(userId);
            stream.Write(uI, 0, 1);

            var fName = uploadedFile.FileName;
            var fnb = System.Text.Encoding.UTF8.GetBytes(fName);

            var c = BitConverter.GetBytes(fnb.Length);
            stream.Write(c, 0, c.Length);
            stream.Write(fnb, 0, fnb.Length);

            stream.Write(buffer, 0, buffer.Length);
            stream.Write(bytes, 0, bytes.Length);

            return RedirectToAction("Index");
        }
    }
}