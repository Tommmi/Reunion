using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Reunion.Common.Email;
using Reunion.Web.Models;

namespace Reunion.Web
{
    public class EmailService : IIdentityMessageService
    {
	    private readonly IEmailSender _emailSender;

	    public EmailService(IEmailSender emailSender)
	    {
		    _emailSender = emailSender;
	    }

	    public Task SendAsync(IdentityMessage message)
        {
			_emailSender.SendEmail(message.Subject,message.Body,message.Destination);
			return Task.CompletedTask;
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store, IEmailSender emailSender)
            : base(store)
        {

			// Configure validation logic for usernames
			UserValidator = new UserValidator<ApplicationUser>(this)
			{
				AllowOnlyAlphanumericUserNames = false,
				RequireUniqueEmail = true
			};

			// Configure validation logic for passwords
			PasswordValidator = new PasswordValidator
			{
				RequiredLength = 8,
				RequireNonLetterOrDigit = true,
				RequireDigit = true,
				RequireLowercase = false,
				RequireUppercase = false,
			};

			// Configure user lockout defaults
			UserLockoutEnabledByDefault = true;
			DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
			MaxFailedAccessAttemptsBeforeLockout = 5;


			// Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
			// You can write your own provider and plug it in here.
			//manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
			//{
			//    MessageFormat = "Your security code is {0}"
			//});
			//RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
			//{
			//	Subject = "Security Code",
			//	BodyFormat = "Your security code is {0}"
			//});
			EmailService = new EmailService(emailSender);
			//manager.SmsService = new SmsService();

			var dataProtectionProvider = Startup.DataProtectionProvider;
			if (dataProtectionProvider != null)
			{
				IDataProtector dataProtector = dataProtectionProvider.Create("ASP.NET Identity");

				UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(dataProtector);
			}
		}

	}

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }
    }
}
