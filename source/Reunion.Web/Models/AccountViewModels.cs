using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Reunion.Web.Resources;

namespace Reunion.Web.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code", ResourceType = typeof(Resource1))]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Account_RememberThisBrowser", ResourceType = typeof(Resource1))]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resource1))]
        public string Password { get; set; }

        [Display(Name = "Account_RememberMe", ResourceType = typeof(Resource1))]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "AccountErrorLengthTooShort", ErrorMessageResourceType = typeof(Resource1), MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resource1))]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "AccountConfirmPassword", ResourceType = typeof(Resource1))]
        [Compare("Password", ErrorMessageResourceName = "AccountErrPasswordAndConfirmationNotMatch", ErrorMessageResourceType = typeof(Resource1))]
        public string ConfirmPassword { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "AccountErrorLengthTooShort", ErrorMessageResourceType = typeof(Resource1), MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password", ResourceType = typeof(Resource1))]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "AccountConfirmPassword", ResourceType = typeof(Resource1))]
        [Compare("Password", ErrorMessageResourceName = "AccountErrPasswordAndConfirmationNotMatch", ErrorMessageResourceType = typeof(Resource1))]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email", ResourceType = typeof(Resource1))]
        public string Email { get; set; }
    }
}
