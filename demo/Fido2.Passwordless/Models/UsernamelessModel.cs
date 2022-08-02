using Fido2NetLib;
using NetDevPack.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fido2.Passwordless.Models
{
    public class UsernamelessModel
    {
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        public string? AttestationResponse { get; set; }

        public Fido2User Get()
        {
            return new Fido2User
            {
                DisplayName = DisplayName,
                Name = DisplayName.Urlize(),
                Id = Encoding.UTF8.GetBytes(DisplayName.Urlize()) // byte representation of userID is required
            };
        }

        public class LoginModel
        {
            [Required]
            public string DisplayName { get; set; }
            [Required]
            public bool RememberMe { get; set; }

            public string? AssertionResponse { get; set; }
        }
    }

}
