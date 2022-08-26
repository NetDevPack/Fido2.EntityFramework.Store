using Fido2.Passwordless.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetDevPack.Fido2.EntityFramework.Store.Store;
using System.Diagnostics;

namespace Fido2.Passwordless.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFido2Store _fido2Store;

        public HomeController(ILogger<HomeController> logger, IFido2Store fido2Store)
        {
            _logger = logger;
            _fido2Store = fido2Store;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize, Route("stored-credentials")]
        public async Task<IActionResult> StoredCredentials()
        {
            var keys = await _fido2Store.ListCredentialDetailsByUser(User.Identity.Name);
            return View(keys);
        }
    }
}