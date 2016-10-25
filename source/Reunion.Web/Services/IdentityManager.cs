using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Web.Controllers;
using Reunion.Web.Models;
using TUtils.Common.DependencyInjection;
using TUtils.Common.Extensions;
using TUtils.Common.Logging;
using TUtils.Common.Logging.Common;

namespace Reunion.Web.Services
{
	public class IdentityManager : IIdentityManager
	{
		private readonly ILazy<ApplicationUserManager> _applicationUserManager;
		private readonly ILazy<IReunionBL> _bl;
		private readonly ITLog _logger;


		public IdentityManager(
			ILazy<ApplicationUserManager> applicationUserManager,
			ILazy<IReunionBL> bl,
			ITLog logger)
		{
			_applicationUserManager = applicationUserManager;
			_bl = bl;
			_logger = logger;
		}

		IReunionUser IIdentityManager.GetCurrentUser()
		{
			var identity = System.Web.HttpContext.Current.User?.Identity;
			if (identity == null)
				return null;
			var userId = identity.GetUserId();
			var user = _applicationUserManager.Value.FindById(userId);
			return user;
		}

		bool IIdentityManager.IsUserAuthenticated => System.Web.HttpContext.Current.User?.Identity.IsAuthenticated??false;


		private static string CreatePassword()
		{
			var rnd = new Random();
			var passwordBuffer = new byte[8];
			rnd.NextBytes(passwordBuffer);
			return Convert.ToBase64String(passwordBuffer).Left(8);
		}

		string IIdentityManager.GetLanguageOfCurrentUser()
		{
			return Thread.CurrentThread.CurrentCulture?.TwoLetterISOLanguageName;
		}

		Organizer IIdentityManager.GetVerifiedOrganizer(int reunionId)
		{
			var currentUser = (this as IIdentityManager).GetCurrentUser();
			if (currentUser == null)
				return null;
			return _bl.Value.GetOrganizer(reunionId,userId:currentUser.GetId());
		}
	}
}