using System.Collections.Generic;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.Common
{
	/// <summary>
	/// many information associated with a single reunion
	/// </summary>
	public class ReunionInfo
	{
		/// <summary>
		/// reunion
		/// </summary>
		public ReunionEntity ReunionEntity { get; set; }
		/// <summary>
		/// organizer of reunion
		/// </summary>
		public Organizer Organizer { get; set; }
		/// <summary>
		/// statemachine of organizer / reunion
		/// </summary>
		public OrganizerStatemachineEntity OrganizerStatemachineEntity { get; set; }

		/// <summary>
		/// knock statemachine of organizer
		/// </summary>
		public KnockStatemachineEntity KnockStatemachineEntity { get; set; }

		/// <summary>
		/// all invitated participants of reunion
		/// </summary>
		public IList<Participant> Participants { get; set; }

		/// <summary>
		/// all participant statemachines
		/// "Participants" and "ParticipantStatemachines" have a one-to-one association:
		/// eg.: Participants[i] has the statemachine ParticipantStatemachines[i] !
		/// </summary>
		public IList<ParticipantStatemachineEntity> ParticipantStatemachines { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunionEntity">
		/// reunion
		/// </param>
		/// <param name="organizer">
		/// organizer of reunion
		/// </param>
		/// <param name="organizerStatemachineEntity">
		/// statemachine of organizer / reunion
		/// </param>
		/// <param name="knockStatemachineEntity">
		/// knock statemachine of organizer
		/// </param>
		/// <param name="participants">
		/// all invitated participants of reunion
		/// </param>
		/// <param name="participantStatemachines">
		/// all participant statemachines
		/// "Participants" and "ParticipantStatemachines" have a one-to-one association:
		/// eg.: Participants[i] has the statemachine ParticipantStatemachines[i] !
		/// </param>
		public ReunionInfo(
			ReunionEntity reunionEntity,
			Organizer organizer,
			OrganizerStatemachineEntity organizerStatemachineEntity,
			KnockStatemachineEntity knockStatemachineEntity,
			IList<Participant> participants,
			IList<ParticipantStatemachineEntity> participantStatemachines)
		{
			ReunionEntity = reunionEntity;
			Organizer = organizer;
			OrganizerStatemachineEntity = organizerStatemachineEntity;
			KnockStatemachineEntity = knockStatemachineEntity;
			Participants = participants;
			ParticipantStatemachines = participantStatemachines;
		}
	}
}
