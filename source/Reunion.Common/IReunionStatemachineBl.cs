using System;
using System.Collections.Generic;

namespace Reunion.Common
{
	/// <summary>
	/// business layer service for statemachines
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public interface IReunionStatemachineBL
	{
		/// <summary>
		/// Triggers signal "SignalFinalDateRejected" to the participant's statemachine,
		/// if participant hasn't marked final invitation date as ok.
		/// If there isn't a final invitation this method doese nothing.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void CheckFinalDateRejected(int reunionId, int participantId);

		/// <summary>
		/// clears the final invitation date of the reunion (sets it to null)
		/// </summary>
		/// <param name="reunionId"></param>
		void ClearFinalInvitationDate(int reunionId);

		/// <summary>
		/// Deactivates statemachines of a reunion
		/// (Sets StatemachineContext.IsTerminated = true)
		/// </summary>
		/// <param name="reunionId"></param>
		void DeactivateStatemachines(int reunionId);

		/// <summary>
		/// Checks if we must send signal "SignalDateFound" or "SignalRequiredRejected" to 
		/// organizer's statemachine.
		/// </summary>
		/// <param name="reunionId"></param>
		void RecheckDateFound(int reunionId);

		/// <summary>
		/// Reactivates statemachines of a reunion after having deactivated them by calling DeactivateStatemachines
		/// (Sets StatemachineContext.IsTerminated = false)
		/// </summary>
		/// <param name="reunionId"></param>
		void ReactivateStatemachines(int reunionId);

		/// <summary>
		/// Sends first invitation mails with personal links to participants, who aren't invitated yet.
		/// </summary>
		/// <param name="reunionId"></param>
		void SendPrimaryInvitationMails(int reunionId);
		/// <summary>
		/// sends notification mail to organizer
		/// </summary>
		/// <param name="reunionId"></param>
		void SendKnockMail2Organizer(int reunionId);
		/// <summary>
		/// sends mail to given participant informing about the cancellation of former final invitation.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void SendRejectionMailToParticipant(int reunionId, int participantId);
		/// <summary>
		/// Triggers signal "SignalFinalInvitationCanceledByOrganizer" to all participant statemachines
		/// </summary>
		/// <param name="reunionId"></param>
		void TriggerSignalFinalInvitationCanceled(int reunionId);

		/// <summary>
		/// Triggers signal "SignalKnock" to knockstatemachine of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		void WakeOrganizer(int reunionId);

		/// <summary>
		/// true, if given participant hasn't set any information regarding the most possible dates of reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		/// <returns></returns>
		bool MissingDaysOfParticipant(int reunionId, int participantId);

		/// <summary>
		/// Sends email to given participant informing about that participant hasn't set any information 
		/// regarding the most possible dates of reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void SendMissingDaysNotification(int reunionId, int participantId);
	}
}