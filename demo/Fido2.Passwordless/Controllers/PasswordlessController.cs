using Fido2.Passwordless.Models;
using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NetDevPack.Fido2.EntityFramework.Store.Store;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Fido2.Passwordless.Controllers
{
    public class PasswordlessController : Controller
    {
        private UserManager<IdentityUser> _userManager;
        private SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<PasswordlessController> _logger;
        private readonly IFido2Store _fido2Store;
        private readonly IFido2 _fido2;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IEmailSender _emailSender;
        private readonly IDataProtector _protector;

        public PasswordlessController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<PasswordlessController> logger,
            IFido2Store fido2Store,
            IFido2 fido2,
            IHttpContextAccessor httpContext,
            IDataProtectionProvider provider,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _fido2Store = fido2Store;
            _fido2 = fido2;
            _httpContext = httpContext;
            _emailSender = emailSender;
            _protector = provider.CreateProtector("Passwordless");
        }
        public IActionResult Index(string returnUrl)
        {

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(PasswordlessModel model, string returnUrl)
        {
            var response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(model.AttestationResponse);
            // 1. get the options we sent the client
            if (string.IsNullOrEmpty(_httpContext?.HttpContext?.Request.Cookies["fido2.attestationOptions"]))
                return NotFound();

            var jsonOptions = _protector.Unprotect(_httpContext?.HttpContext?.Request.Cookies["fido2.attestationOptions"]);
            var options = CredentialCreateOptions.FromJson(jsonOptions);

            // 2. Create callback so that lib can verify credential id is unique to this user
            IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
            {
                var users = await _fido2Store.ListCredentialsByPublicKeyIdAsync(args.CredentialId);
                if (users.Count() > 0)
                    return false;

                return true;
            };

            // 2. Verify and make the credentials
            var success = await _fido2.MakeNewCredentialAsync(response, options, callback);

            // 3. Store the credentials in db
            var user = new IdentityUser(options.User.Name)
            {
                Email = options.User.Name
            };
            _fido2Store.Store(user.UserName, new StoredCredential
            {
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignatureCounter = success.Result.Counter,
                CredType = success.Result.CredType,
                RegDate = DateTime.Now,
                AaGuid = success.Result.Aaguid
            });

            // 4. Create user at ASP.NET Identity
            var result = await _userManager.CreateAsync(user);

            // 5. Default ASP.NET Identity flow. (e-mail confirmation, ReturnUrl, etc.)
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account without password.");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.Username, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("/Account/RegisterConfirmation", new { email = model.Username, returnUrl = returnUrl, area = "Identity" });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            // 6. In case of errors 
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }


            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(PasswordlessModel.LoginModel login, string returnUrl)
        {

            var clientResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(login.AssertionResponse);

            // 1. Get the assertion options we sent the client
            if (string.IsNullOrEmpty(_httpContext?.HttpContext?.Request.Cookies["fido2.assertionOptions"]))
                return NotFound();

            var jsonOptions = _protector.Unprotect(_httpContext?.HttpContext?.Request.Cookies["fido2.assertionOptions"]);
            var options = AssertionOptions.FromJson(jsonOptions);

            // 2. Get registered credential from database
            var creds = await _fido2Store.GetCredentialByPublicKeyIdAsync(clientResponse.Id);
            if (creds is null)
                return Json(new AssertionVerificationResult { Status = "error", ErrorMessage = "Unknown Credentials" });

            // 3. Get credential counter from database
            var storedCounter = creds.SignatureCounter;

            // 4. Create callback to check if userhandle owns the credentialId
            IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
            {
                var storedCreds = await _fido2Store.GetCredentialsByUserHandleAsync(args.UserHandle);
                return storedCreds.ToList().Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
            };

            // 5. Make the assertion
            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter, callback);

            // 6. Store the updated counter
            await _fido2Store.UpdateCounter(res.CredentialId, res.Counter);

            // Login at ASP.NET Identity
            if (res.Status == "ok")
            {

                returnUrl ??= Url.Content("~/");
                var userName = await _fido2Store.GetUsernameByIdAsync(creds.UserId);
                var user = await _userManager.FindByNameAsync(userName);

                var accountLockout = await _userManager.FindByNameAsync(userName);
                if (accountLockout.LockoutEnabled && accountLockout.LockoutEnd < DateTime.UtcNow)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("Account/Lockout", new { area = "Identity" });
                }

                await _signInManager.SignInAsync(user, login.RememberMe);
                
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(login);
            }

        }

        [HttpGet]
        public async Task<ActionResult> GetAssertionOptions([FromQuery] PasswordlessModel.LoginModel query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingCredentials = new List<PublicKeyCredentialDescriptor>();

                if (!string.IsNullOrEmpty(query.Username))
                {

                    // 1. Get registered credentials from database
                    existingCredentials = (await _fido2Store.ListPublicKeysByUser(query.Username)).ToList();
                }

                var exts = new AuthenticationExtensionsClientInputs()
                {
                    UserVerificationMethod = true
                };

                // 2. Create options
                var options = _fido2.GetAssertionOptions(
                    existingCredentials,
                    UserVerificationRequirement.Discouraged,
                    exts
                );

                // 3. Temporarily store options, session/in-memory cache/redis/db
                var cookieOptions = new CookieOptions()
                {
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(2),
                    HttpOnly = true,
                };
                var content = _protector.Protect(options.ToJson());
                _httpContext?.HttpContext?.Response.Cookies.Append("fido2.assertionOptions", content, cookieOptions);

                // 5. Return options to client
                return Json(options);
            }

            catch (Exception e)
            {
                return Json(new AssertionOptions { Status = "error", ErrorMessage = FormatException(e) });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttestationOptions([FromQuery] PasswordlessModel query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var fidoUser = query.Get();
            // 1. Get user existing keys by username
            var existingKeys = await _fido2Store.ListPublicKeysByUser(fidoUser.Name);


            // 2. Create options
            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = false,
                UserVerification = UserVerificationRequirement.Preferred
            };

            var exts = new AuthenticationExtensionsClientInputs()
            {
                Extensions = true,
                UserVerificationMethod = true,
            };

            var options = _fido2.RequestNewCredential(fidoUser, existingKeys.ToList(), authenticatorSelection, AttestationConveyancePreference.None, exts);

            // 3. Temporarily store options, session/in-memory cache/redis/db 
            var cookieOptions = new CookieOptions()
            {
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(2),
                HttpOnly = true,
            };
            var content = _protector.Protect(options.ToJson());
            _httpContext?.HttpContext?.Response.Cookies.Append("fido2.attestationOptions", content, cookieOptions);

            // 4. return options to client
            return Json(options);
        }


        private string FormatException(Exception e)
        {
            return string.Format("{0}{1}", e.Message, e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
        }

    }
}
