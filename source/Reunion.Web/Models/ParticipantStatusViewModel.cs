using System;
using System.Globalization;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using Reunion.Web.Common;
using Reunion.Web.Resources;

namespace Reunion.Web.Models
{
	public class ParticipantStatusViewModel
	{
		private readonly Participant _participant;
		private readonly string _startPage4Participant;

		public ParticipantStatusViewModel()
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="participant"></param>
		/// <param name="participantStatus"></param>
		/// <param name="startPage4Participant">
		/// http://findtime.de/participant?id={0}
		/// {0}: unguessable id of participant
		/// </param>
		public ParticipantStatusViewModel(
			Participant participant,
			ParticipantStatusEnum participantStatus,
			string startPage4Participant)
		{
			_participant = participant;
			_startPage4Participant = startPage4Participant;
			CurrentState = participantStatus;
			Culture = new CultureInfo(_participant.LanguageIsoCodeOfPlayer);
		}

		public string Name => _participant.Name;
		public string UnguessableParticipantId => _participant.UnguessableId;
		public ParticipantStatusEnum CurrentState { get; set; }
		public bool IsRequired => _participant.IsRequired;

		public string Mail => _participant.MailAddress;
		public string DirectLink => string.Format(_startPage4Participant, _participant.UnguessableId);

		public CultureInfo Culture { get; private set; }

		public string GetLocalizedText(string ressourceKey,params object[] args)
		{
			return string.Format(Resource1.ResourceManager.GetString(ressourceKey, Culture), args);
		}

		public bool IsWarningStatus
		{
			get
			{
				switch (CurrentState)
				{
					case ParticipantStatusEnum.Created:
					case ParticipantStatusEnum.ReactionOnInvitationMissing:
					case ParticipantStatusEnum.ReactionOnFinalInvitationMissing:
					case ParticipantStatusEnum.RejectedInvitation:
						return true;
					case ParticipantStatusEnum.WaitOnLoginForInvitation:
					case ParticipantStatusEnum.Invitated:
					case ParticipantStatusEnum.FinallyInvitated:
					case ParticipantStatusEnum.Accepted:
						return false;
					default:
						throw new ArgumentOutOfRangeException("hsahdfd723jkafv " + CurrentState);
				}
			}
		}

		public string CurrentStateText
		{
			get
			{
				switch (CurrentState)
				{
					case ParticipantStatusEnum.Created:
						return Resource1.NotInvitatedYet;
					case ParticipantStatusEnum.WaitOnLoginForInvitation:
						return Resource1.PrimaryInvitationSent;
					case ParticipantStatusEnum.Invitated:
						return Resource1.Answered;
					case ParticipantStatusEnum.ReactionOnInvitationMissing:
						return Resource1.NotAnswered1;
					case ParticipantStatusEnum.FinallyInvitated:
						return Resource1.FinallyInvitated;
					case ParticipantStatusEnum.ReactionOnFinalInvitationMissing:
						return Resource1.NotAnswered2;
					case ParticipantStatusEnum.RejectedInvitation:
						return Resource1.RejectedFinallyInvitation;
					case ParticipantStatusEnum.Accepted:
						return Resource1.Accepted;
					default:
						throw new ArgumentOutOfRangeException("hsahdfd723jkafv " + CurrentState);
				}
			}
		}
	}
}