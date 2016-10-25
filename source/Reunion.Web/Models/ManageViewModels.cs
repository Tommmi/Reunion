using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Reunion.Web.Resources;

namespace Reunion.Web.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessageResourceName = "AccountErrorLengthTooShort", ErrorMessageResourceType = typeof(Resource1), MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword", ResourceType = typeof(Resource1))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "AccountConfirmNewPassword", ResourceType = typeof(Resource1))]
        [Compare("NewPassword", ErrorMessageResourceName = "AccountErrNewPasswordAndConfirmationNotMatch", ErrorMessageResourceType = typeof(Resource1))]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "AccountCurrentPassword", ResourceType = typeof(Resource1))]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "AccountErrorLengthTooShort", ErrorMessageResourceType = typeof(Resource1), MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "NewPassword", ResourceType = typeof(Resource1))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "AccountConfirmNewPassword", ResourceType = typeof(Resource1))]
        [Compare("NewPassword", ErrorMessageResourceName = "AccountErrNewPasswordAndConfirmationNotMatch", ErrorMessageResourceType = typeof(Resource1))]
        public string ConfirmPassword { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "PhoneNumber", ResourceType = typeof(Resource1))]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code", ResourceType = typeof(Resource1))]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "PhoneNumber", ResourceType = typeof(Resource1))]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }
}