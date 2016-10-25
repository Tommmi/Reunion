using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Reunion.Common.Model.States;

namespace Reunion.Common.Model
{
	/// <summary>
	/// Entity which represents the reunion
	/// </summary>
	public class ReunionEntity
	{
		/// <summary>
		/// defualt constructor
		/// </summary>
		public ReunionEntity()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="name">name of reunion</param>
		/// <param name="invitationText"> multiline invitation text. </param>
		/// <param name="deadline">Deadline of planning. If there couldn't be found a date till that day, the planning fails.</param>
		/// <param name="maxReactionTimeHours">
		/// How much time (hours) doese a participant has to react on an email, untill Reunion will escalate it ?
		/// </param>
		/// <param name="deactivatedParticipants">
		/// comma seperated list of participants, which denied to come to the reunion in any case</param>
		/// <param name="finalInvitationDate">
		/// !=null: Found date for the reunion. Participants have got final invitation mails.
		/// ==null: No final date has been determined
		/// </param>
		/// <returns>this</returns>
		public ReunionEntity Init(
			int reunionId,
			string name,
			string invitationText,
			DateTime deadline,
			int maxReactionTimeHours,
			string deactivatedParticipants,
			DateTime? finalInvitationDate)
		{
			Id = reunionId;
			Name = name;
			Deadline = deadline;
			InvitationText = invitationText;
			MaxReactionTimeHours = maxReactionTimeHours;
			DeactivatedParticipants = deactivatedParticipants;
			FinalInvitationDate = finalInvitationDate;
			return this;
		}

		/// <summary>
		/// Id of the reunion
		/// </summary>
		public int Id { get; set; }
		/// <summary>
		/// name of reunion
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// multiline invitation text.
		/// </summary>
		public string InvitationText { get; set; }
		/// <summary>
		/// Deadline of planning. If there couldn't be found a date till that day, the planning fails.
		/// </summary>
		public DateTime Deadline { get; set; }
		/// <summary>
		/// comma seperated list of participants, which denied to come to the reunion in any case
		/// </summary>
		public string DeactivatedParticipants { get; set; }
		/// <summary>
		/// How much time doese a participant has to react on an email, utill Reunion will escalate it ?
		/// </summary>
		public int MaxReactionTimeHours { get; set; }
		/// <summary>
		/// !=null: Found date for the reunion. Participants have got final invitation mails.
		/// ==null: No final date has been determined
		/// </summary>
		public DateTime? FinalInvitationDate { get; set; }
	}
}