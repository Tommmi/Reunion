using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reunion.Web.Common
{
	public interface IAppSettings
	{
		/// <summary>
		/// minimum count of hours the service is waiting for a reaction of a player
		/// </summary>
		int MaxReactionTimeHours { get; }
		string ServiceHost { get; }
		/// <summary>
		/// http://findtime.de/participant?id={0}
		/// {0}: unguessable id of participant
		/// </summary>
		string StartPage4Participant { get; }
		/// <summary>
		/// http://findtime.de/reunion/status/{0}
		/// {0}: id of reunion
		/// </summary>
		string StatusPageOfReunion { get; }
		/// <summary>
		/// account of mail smtp server
		/// </summary>
		string MailAccount_MailAddress { get; }
		string MailAccount_UserName { get; }
		string MailAccount_Password { get; }
		string MailAccount_SmtpHost { get; }
	}
}
