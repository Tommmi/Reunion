using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Net.Mail;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Practices.Unity;
using Reunion.BL;
using Reunion.Common;
using Reunion.Common.Email;
using Reunion.DAL;
using Reunion.Web.Common;
using Reunion.Web.Controllers;
using Reunion.Web.Models;
using Reunion.Web.Services;
using TUtils.Common;
using TUtils.Common.DependencyInjection;
using TUtils.Common.EF6.Transaction;
using TUtils.Common.EF6.Transaction.Common;
using TUtils.Common.Logging;
using TUtils.Common.Logging.Common;
using TUtils.Common.Logging.Log4Net;
using TUtils.Common.MVC;
using TUtils.Common.Transaction;

namespace Reunion.Web
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
	        var unityContainer = new UnityContainer();
			IDIContainer container = new ExtendedUnityContainer(unityContainer);



			container.RegisterPerRequest<IUserStore<ApplicationUser>, UserStore<ApplicationUser>>(
						c => new UserStore<ApplicationUser>(c.Get<ApplicationDbContext>()));
			container.RegisterPerRequest<ApplicationUserManager>();
			container.RegisterPerRequest<ILazy<ApplicationUserManager>, TUtils.Common.DependencyInjection.Lazy<ApplicationUserManager>>(di => new TUtils.Common.DependencyInjection.Lazy<ApplicationUserManager>(di));
			container.RegisterPerRequest<IAuthenticationManager>(c => HttpContext.Current.GetOwinContext().Authentication);
			container.RegisterSingleton<IAppSettings, AppSettings>();
			container.RegisterPerRequest<ApplicationSignInManager>();
			container.RegisterSingleton<ILogWriter, Log4NetWriter>(c => new Log4NetWriter());
			container.RegisterSingleton<ITLog, TLog>(c => new TLog(c.Get<ILogWriter>(), isLoggingOfMethodNameActivated: false));
			container.RegisterSingleton<IEmailSender, SmptEmailSender>(c =>
			{
				var appSettings = c.Get<IAppSettings>();
				var mailAddressOfReunion = new MailAddress(appSettings.MailAccount_MailAddress, displayName: "Reunion Webservice");
				return new SmptEmailSender(
					logger: c.Get<ITLog>(),
					fromAddress: mailAddressOfReunion,
					replyToEmailAddress: new MailAddress(appSettings.MailAccount_MailAddress, displayName: "Do Not Respond !"),
					emailAccountUserName: appSettings.MailAccount_UserName,
					emailAccountPassword: appSettings.MailAccount_Password, // TODO secure
					useStarttls: true,
					isBodyHtml: true,
					mailProviderHost: appSettings.MailAccount_SmtpHost,
					mailProviderPort: 587);
			});

			container.Register<TouchController>();
			container.RegisterSingleton<IBlResource, BLResource>();
			container.RegisterPerRequest<IReunionWebservice, ReunionWebservice>();
			container.RegisterSingleton<IDebouncer, Debouncer>(diContainer => new Debouncer(1000));
			container.RegisterSingleton<ILogWriter, Log4NetWriter>();
			container.RegisterSingleton<ITLog, TLog>(diContainer => new TLog(diContainer.Get<ILogWriter>(), isLoggingOfMethodNameActivated: false));
			container.RegisterSingleton<IDbContextFactory<ReunionDbContext>, DbContextFactory<ReunionDbContext>>();
			container.RegisterSingleton<ITransactionService<ReunionDbContext>, TransactionService<ReunionDbContext>>(diContainer =>
				new TransactionService<ReunionDbContext>(
					diContainer.Get<ITLog>(),
					diContainer.Get<IDbContextFactory<ReunionDbContext>>(),
					IsolationLevel.Serializable));
			container.Register<ITransactionService, ITransactionService<ReunionDbContext>>(
				diContainer => diContainer.Get<ITransactionService<ReunionDbContext>>());
			container.RegisterSingleton<IIdentityManager, IdentityManager>();
			container.RegisterSingleton<IReunionDal, ReunionDal>();
			container.RegisterSingleton<ILazy<IReunionBL>, TUtils.Common.DependencyInjection.Lazy<IReunionBL>>(di => new TUtils.Common.DependencyInjection.Lazy<IReunionBL>(di));
			container.Register<IReunionStatemachineBL, ReunionBL>(diContainer => (ReunionBL)diContainer.Get<IReunionBL>());
			container.RegisterPerRequest<IReunionBL, ReunionBL>(diContainer =>
			{
				var appSettings = diContainer.Get<IAppSettings>();
				return new ReunionBL(
					diContainer.Get<ITransactionService<ReunionDbContext>>(),
					diContainer.Get<IReunionDal>(),
					diContainer.Get<IEmailSender>(),
					diContainer.Get<IBlResource>(),
					minimumWaitTimeSeconds: appSettings.MaxReactionTimeHours * 3600,
					startPage4Participant: appSettings.StartPage4Participant,
					statusPageOfReunion: appSettings.StatusPageOfReunion,
					mailAddressOfReunion: appSettings.MailAccount_MailAddress);
			});

			container.RegisterSingleton<ILanguagesService, LanguagesService>();

			DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(unityContainer));
			GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(unityContainer);
		}
	}
}