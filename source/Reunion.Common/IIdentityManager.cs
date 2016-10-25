using Reunion.Common.Model;

namespace Reunion.Common
{
	/// <summary>
	/// service that gets information about the user of the current request 
	/// </summary>
	public interface IIdentityManager
	{
		IReunionUser GetCurrentUser();
		bool IsUserAuthenticated { get; }
		/// <summary>
		/// two letter iso language code of current user
		/// </summary>
		/// <returns></returns>
		string GetLanguageOfCurrentUser();
		/// <summary>
		/// returns organizer of reunion only if current user is the organizer of the given reunion, too.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns>
		/// null, if current user isn't the organizer of the given reunion
		/// </returns>
		Organizer GetVerifiedOrganizer(int reunionId);
	}
}
