using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Reunion.BL;
using Reunion.Web.Resources;

namespace Reunion.Web.Common
{
	public class BLResource : IBlResource
	{
		/// <summary>
		/// {0}: Reunion.Name
		/// </summary>
		string IBlResource.GetInvitationMailSubject(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.InvitationMailSubject), cultureInfo);
		}

		/// <summary>
		/// {0}: Reunion.Name
		/// {1}: Reunion.InvitationText,
		/// {2}: Reunion.Deadline,
		/// {3}: host address of web service
		/// </summary>
		string IBlResource.InvitationMailBody => Resource1.InvitationMailBody;

		/// <summary>
		/// {0}: Reunion.Name
		/// {1}: Reunion.InvitationText,
		/// {2}: Reunion.Deadline,
		/// {3}: direct link to web site
		/// </summary>
		string IBlResource.GetInvitationMailBodyParticipant(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.InvitationMailBodyParticipant), cultureInfo);
		}

		/// <summary>
		/// {0}: Reunion.Name
		/// </summary>
		string IBlResource.GetKnockMailSubject(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.KnockMailSubject), cultureInfo);
		}

		/// <summary>
		/// {0}: Reunion.Name
		/// {1}: link to organizers status page
		/// </summary>
		string IBlResource.GetKnockMailBody(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.KnockMailBody), cultureInfo);
		}

		/// <summary>
		/// subject of mail which finally invites participant to reunion
		/// {0}: Reunion.Name
		/// </summary>
		string IBlResource.GetFinalInvitationMailSubject(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.FinalInvitationMailSubject), cultureInfo);
		}

		/// <summary>
		/// body of mail which contains the individual link to the site
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// {2}: direct link to web site
		/// </summary>
		string IBlResource.GetFinalInvitationMailBodyParticipant(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.FinalInvitationMailBodyParticipant), cultureInfo);
		} 

		/// <summary>
		/// body of mail which informs organizer about final invitation
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// </summary>
		string IBlResource.FinalInvitationMailBody => Resource1.FinalInvitationMailBody;

		/// <summary>
		/// subject of mail which rejects final invitation to reunion
		/// {0}: Reunion.Name
		/// </summary>
		string IBlResource.GetRejectionMailSubject(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.RejectionMailSubject), cultureInfo);
		}

		/// <summary>
		/// body of mail which contains the individual link to the site and 
		/// the rejection of the final invitation
		/// {0}: Reunion.Name
		/// {1}: date of reunion,
		/// {2}: direct link to web site
		/// </summary>
		string IBlResource.GetRejectionMailBodyParticipant(CultureInfo cultureInfo)
		{
			return Resource1.ResourceManager.GetString(nameof(Resource1.RejectionMailBodyParticipant), cultureInfo);
		}
	}
}