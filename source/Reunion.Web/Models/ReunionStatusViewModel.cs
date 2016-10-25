using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using Reunion.Web.Resources;

namespace Reunion.Web.Models
{
	public class ReunionStatusViewModel
	{
		private readonly IEnumerable<Tuple<Participant, ParticipantStatusEnum>> _participants;
		private readonly string _startPage4Participant;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunion"></param>
		/// <param name="organizatorStatus"></param>
		/// <param name="participants"></param>
		/// <param name="dateProposals"></param>
		/// <param name="startPage4Participant">
		/// http://findtime.de/participant?id={0}
		/// {0}: unguessable id of participant
		/// </param>
		/// <param name="invitationMailContent"></param>
		public ReunionStatusViewModel(
			ReunionEntity reunion,
			OrganizatorStatusEnum organizatorStatus,
			IEnumerable<Tuple<Participant,ParticipantStatusEnum>> participants,
			IEnumerable<DateProposal> dateProposals,
			string startPage4Participant,
			InvitationMailContent invitationMailContent)
		{
			_participants = participants;
			_startPage4Participant = startPage4Participant;
			Reunion = reunion;
			OrganizatorStatus = organizatorStatus;
			DateProposals = dateProposals;
			InvitationMailContent = invitationMailContent;
		}

		public ReunionEntity Reunion { get; set; }
		public IEnumerable<DateProposal> DateProposals { get; set; }
		public InvitationMailContent InvitationMailContent { get; set; }

		public OrganizatorStatusEnum OrganizatorStatus { get; set; }


		public IEnumerable<ParticipantStatusViewModel> Participants
		{
			get { return _participants.Select(p => new ParticipantStatusViewModel(p.Item1,p.Item2, _startPage4Participant)); }
		}

		public string StatusDisplayText
		{
			get
			{
				switch (OrganizatorStatus)
				{
					case OrganizatorStatusEnum.Created:
						return Resource1.StatusCreated;
					case OrganizatorStatusEnum.Running:
						return Resource1.StatusRunning;
					case OrganizatorStatusEnum.DateProposal:
						return Resource1.StatusProposingDate;
					case OrganizatorStatusEnum.FinalInvitation:
						return Resource1.StatusFinallyInvitating;
					case OrganizatorStatusEnum.FinishedSucceeded:
						return Resource1.StatusFinishedSucceeded;
					case OrganizatorStatusEnum.FinishedFailed:
						return Resource1.StatusFinishedFailed;
					default:
						throw new ArgumentOutOfRangeException("gfd67132dhwe " + OrganizatorStatus);
				}
			}
		}
	}
}

