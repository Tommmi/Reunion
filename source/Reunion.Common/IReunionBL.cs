using System;
using System.Collections.Generic;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.Common
{
	// ReSharper disable once InconsistentNaming
	/// <summary>
	/// business layer of reunion
	/// </summary>
	public interface IReunionBL
	{
		#region for participants
		/// <summary>
		/// Must be called when a participant accepts a final invitation
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void AcceptFinalDate(int participantId, int reunionId);

		/// <summary>
		/// Gets date ranges of the given participant
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		/// <param name="finalInvitationdate">
		/// OUT: final date of reuion, if set allready, otherwise null
		/// </param>
		/// <param name="hasAcceptedFinalInvitationdate">
		/// OUT: true, if participant has accepted a given final invitation
		/// OUT: false, if participant has rejected a given final invitation
		/// OUT: null, if participant hasn't accepted nor rejected a given final invitation
		/// </param>
		/// <param name="daysToBeChecked">
		/// days, which has to be checked by participant. 
		/// Normally these days are the most possible dates for the event.
		/// May be null.
		/// </param>
		/// <returns></returns>
		IEnumerable<TimeRange> GetTimeRangesOfParticipant(
			int reunionId, 
			int participantId, 
			out DateTime? finalInvitationdate, 
			out bool? hasAcceptedFinalInvitationdate,
			out IEnumerable<DateTime> daysToBeChecked);

		/// <summary>
		/// Gets participant by unguessable id of the participant.
		/// </summary>
		/// <param name="unguessableParticipantId"></param>
		/// <returns>null, if not found</returns>
		Participant GetVerifiedParticipant(string unguessableParticipantId);

		/// <summary>
		/// Removes participant from database.
		/// Saves his name in the list of participants, who  have totally rejected invitation.
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void RejectCompletely(int participantId, int reunionId);

		/// <summary>
		/// If participant has been finally invited, this call marks the invitation date 
		/// as not possible for the participant.
		/// If the participant is required, this will result to a lot of actions and mails.
		/// If the participant hasn't been finally invited, nothing will happen.
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void RejectFinalDateOnly(int participantId, int reunionId);

		///  <summary>
		///  Replaces the time ranges of the given participant by the given enumeration.
		///  </summary>
		///  <param name="timeRanges">
		///  you needn't fill in
		/// 		TimeRange.Id
		/// 		TimeRange.Player
		/// 		TimeRange.Reunion
		///  </param>
		/// <param name="unguessableIdOfParticipant">
		/// authentication id of participant
		/// </param>
		void UpdateTimeRangesOfParticipant(IEnumerable<TimeRange> timeRanges, string unguessableIdOfParticipant);

		#endregion

		#region for organizers

		/// <summary>
		/// Creates 
		///		- new reunion in database.
		/// 	- organizer and participants.
		/// 	- statemachines of organizer.
		/// 	- possible date ranges of organizer.
		/// 	- statemachines of participants.
		/// </summary>
		/// <param name="reunion">
		/// Following members of reunion will be ignored and needn't to be filled:
		/// - Id
		/// - DeactivatedParticipants
		/// - FinalInvitationDate
		/// </param>
		/// <param name="organizer">
		/// Following members of Organizer will be ignored and needn't to be filled:
		/// - Id
		/// - Reunion
		/// </param>
		/// <param name="participants">
		/// Following members of Participant will be ignored and needn't to be filled:
		/// - Id
		/// - Reunion
		/// - UnguessableId
		/// </param>
		/// <param name="possibleDateRanges">
		/// Date ranges of organizer.
		/// Following members needn't to be filled  and will be ignored:
		/// - Id
		/// - Player
		/// - Reunion
		/// </param>
		/// <returns></returns>
		ReunionCreateResult CreateReunion(
			ReunionEntity reunion,
			Organizer organizer,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges);

		/// <summary>
		/// deletes the given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		void DeleteReunion(int reunionId, int organizerId);

		/// <summary>
		/// Sets the date on which the reunion should take place now.
		/// Results in sending invitation mails and much more.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		/// <param name="date">
		/// the date which is chossed for the final reunion
		/// </param>
		void FinallyInvite(int reunionId, int organizerId, DateTime date);

		/// <summary>
		/// Gets the planning information for each day, sorted by recommendation (best days on top)
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// May be null.
		/// </param>
		/// <returns>date proposals</returns>
		IEnumerable<DateProposal> GetDateProposals(int reunionId, int? organizerId);

		/// <summary>
		/// Gets the mail content of the first overall invitation mail, which is to be sent from
		/// organizer to all participants manually.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		/// <returns>
		/// null, if all participants have got invitation mails allready or if reunion id is invalid
		/// </returns>
		InvitationMailContent GetInvitationMailContent(int reunionId, int organizerId);

		/// <summary>
		/// Gets organizer by id of current user and reunion id
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="userId">
		/// user id (Organizer.UserId)
		/// </param>
		/// <returns></returns>
		Organizer GetOrganizer(int reunionId, string userId);

		/// <summary>
		/// Get participants and their status for a given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		/// <returns></returns>
		IEnumerable<Tuple<Participant, ParticipantStatusEnum>> GetParticipants(int reunionId, int organizerId);

		/// <summary>
		/// Gets reunion status 
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns>null, if reunion couldn't be found</returns>
		OrganizatorStatusEnum? GetReunionStatus(int reunionId);

		/// <summary>
		/// Gets all reunions created by the given user.
		/// </summary>
		/// <param name="userId">
		/// user id (Organizer.UserId)
		/// </param>
		/// <returns></returns>
		IEnumerable<ReunionEntity> GetReunionsOfUser(string userId);

		/// <summary>
		/// time ranges of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> GetTimeRangesOfReunion(int reunionId);

		/// <summary>
		/// starts or resume reunion planning
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		/// <returns>
		/// null, if there is no reunion with given id
		/// </returns>
		ReunionEntity StartReunion(int reunionId, int organizerId);

		/// <summary>
		/// Updates reunion.
		/// </summary>
		/// <param name="reunion">
		/// Following members needn't to be filled and will be ignored:
		/// - DeactivatedParticipants
		/// - FinalInvitationDate
		/// </param>
		/// <param name="verifiedOrganizerId">
		/// the id of the organizer
		/// </param>
		/// <param name="participants">
		/// Following members needn't to be filled and will be ignored:
		/// - Id: must be 0 if it's new !
		/// - Reunion
		/// - UnguessableId
		/// </param>
		/// <param name="possibleDateRanges">
		/// Date ranges of organizer.
		/// Following members needn't to be filled  and will be ignored:
		/// - Player
		/// - Reunion
		/// - Id:  must be 0, if it's new !
		/// </param>
		ReunionUpdateResult UpdateReunion(
			ReunionEntity reunion,
			int verifiedOrganizerId,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges);

		#endregion

		#region Common

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		void DoWithSameDbContext(Action action);
		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		T DoWithSameDbContext<T>(Func<T> action);

		/// <summary>
		/// Loads all active statemachines (StatemachineContext.IsTerminated == false) and calls 
		/// IStateMachine.Touch().
		/// This method must be called time by time to ensure that actions take place when some time markers elapse.
		/// This method may result in notification mails to participants and organizers !
		/// </summary>
		void TouchAllReunions();

		#endregion

	}
}