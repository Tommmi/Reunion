using System;
using System.Collections.Generic;
using System.Configuration;
using Reunion.Web.Controllers;
using TUtils.Common.Extensions;

namespace Reunion.Web.Common
{
	public class AppSettings : IAppSettings
	{
		int IAppSettings.MaxReactionTimeHours => ConfigurationManager.AppSettings["MaxReactionTimeHours"].ToInt(defaultValue:48);
		string IAppSettings.ServiceHost => ConfigurationManager.AppSettings["serviceHost"];

		string IAppSettings.StartPage4Participant => $"{(this as IAppSettings).ServiceHost}/{nameof(ParticipantController).RemoveController()}/{nameof(ParticipantController.Edit)}/{{0}}";
		string IAppSettings.StatusPageOfReunion => $"{(this as IAppSettings).ServiceHost}/{nameof(ReunionController).RemoveController()}/{nameof(ReunionController.Status)}/{{0}}";

		string IAppSettings.MailAccount_MailAddress => ConfigurationManager.AppSettings["MailAccount_MailAddress"];
		string IAppSettings.MailAccount_UserName => ConfigurationManager.AppSettings["MailAccount_UserName"];
		string IAppSettings.MailAccount_Password => ConfigurationManager.AppSettings["MailAccount_Password"];
		string IAppSettings.MailAccount_SmtpHost => ConfigurationManager.AppSettings["MailAccount_SmtpHost"];

		public static IEnumerable<string> SupportedTwoLetterLanguageIsoCodes => new[] { "de", "en" };
	}
}