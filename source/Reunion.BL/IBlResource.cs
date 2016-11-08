using System.Globalization;

namespace Reunion.BL
{
	/// <summary>
	/// access to resource strings relevant to the business layer
	/// </summary>
	public interface IBlResource
	{
		/// <summary>
		/// subject of mail which invites participant to reunion
		/// {0}: Reunion.Name
		/// </summary>
		string GetInvitationMailSubject(CultureInfo cultureInfo);
		/// <summary>
		/// body of mail for organizer to invite all participants
		/// {0}: Reunion.Name
		/// {1}: Reunion.InvitationText,
		/// {2}: Reunion.Deadline,
		/// {3}: mail address of web service
		/// </summary>
		string InvitationMailBody { get; }
		/// <summary>
		/// body of mail which contains the individual link to the site
		/// {0}: Reunion.Name
		/// {1}: Reunion.InvitationText,
		/// {2}: Reunion.Deadline,
		/// {3}: direct link to web site
		/// </summary>
		string GetInvitationMailBodyParticipant(CultureInfo cultureInfo);

		/// <summary>
		/// subject of mail which awakes organizer
		/// {0}: Reunion.Name
		/// </summary>
		string GetKnockMailSubject(CultureInfo cultureInfo);

		/// <summary>
		/// body of mail which awakes organizer
		/// {0}: Reunion.Name
		/// {1}: link to organizers status page
		/// </summary>
		string GetKnockMailBody(CultureInfo cultureInfo);

		/// <summary>
		/// subject of mail which finally invites participant to reunion
		/// {0}: Reunion.Name
		/// </summary>
		string GetFinalInvitationMailSubject(CultureInfo cultureInfo);

		/// <summary>
		/// body of mail which contains the individual link to the site
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// {2}: direct link to web site
		/// </summary>
		string GetFinalInvitationMailBodyParticipant(CultureInfo cultureInfo);

		/// <summary>
		/// body of mail which informs organizer about final invitation
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// </summary>
		string FinalInvitationMailBody { get; }

		/// <summary>
		/// subject of mail which rejects final invitation to reunion
		/// {0}: Reunion.Name
		/// </summary>
		string GetRejectionMailSubject(CultureInfo cultureInfo);

		/// <summary>
		/// body of mail which contains the individual link to the site and 
		/// the rejection of the final invitation
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// {2}: direct link to web site
		/// </summary>
		string GetRejectionMailBodyParticipant(CultureInfo cultureInfo);
		/// <summary>
		/// subject of mail which informs participant about that he hasn't filled in some important days 
		/// in the calendar.
		/// {0}:reunion name
		/// </summary>
		/// <param name="cultureInfo"></param>
		/// <returns></returns>
		string GetMissingDayNotificationMailSubject(CultureInfo cultureInfo);
		/// <summary>
		/// body of mail which informs participant about that he hasn't filled in some important days 
		/// in the calendar.
		/// {0}:reunion name
		/// {1}:missing days: e.g.: "01.10.2016, 02.10.2016"
		/// {2}:direct link
		/// </summary>
		/// <param name="cultureInfo"></param>
		/// <returns></returns>
		string GetMissingDayNotificationBody(CultureInfo cultureInfo);
	}
}