using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Reunion.BL.Statemachines;
using Reunion.Common;
using Reunion.Common.Email;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using TUtils.Common;
using TUtils.Common.Extensions;
using TUtils.Common.Transaction;

namespace Reunion.BL
{
	/// <summary>
	/// Business layer
	/// Instantiated per request !
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public class ReunionBL : IReunionBL, IReunionStatemachineBL
	{
		#region types

		/// <summary>
		/// reunion loaded into RAM
		/// Includes players and statemachines.
		/// </summary>
		private class ReunionContext
		{
			public ReunionContext(
				ReunionEntity reunion,
				Organizer organizer,
				IEnumerable<Participant> participants,
				OrganizerStatemachine organizerStatemachine,
				KnockStatemachine knockStatemachine,
				IEnumerable<TimeRange> possibleDatesOfOrganizer,
				IEnumerable<ParticipantStatemachine> participantStatemachines)
			{
				Reunion = reunion;
				Organizer = organizer;
				Participants = participants;
				OrganizerStatemachine = organizerStatemachine;
				KnockStatemachine = knockStatemachine;
				PossibleDatesOfOrganizer = possibleDatesOfOrganizer;
				ParticipantStatemachines = new Dictionary<int, ParticipantStatemachine>();
				foreach (var statemachine in participantStatemachines)
				{
					ParticipantStatemachines[statemachine.StateMachineEntity.PlayerId] = statemachine;
				}
			}

			/// <summary>
			/// may be null !!
			/// Will be filled only by ReunionBL.GetDateProposals()
			/// </summary>
			public IEnumerable<DateProposal> CachedDateProposals { get; set; }

			/// <summary>
			/// may be null !
			/// Will be filled only by ReunionBl.GetMissingDayInformations()
			/// Map: particpant id -> most possible days, for which participant didn't made a preference.
			/// If a participant has marked all days in the calendar he won't be in this map.
			/// </summary>
			public Dictionary<int,IList<DateTime>> MissingDayInformations { get; set; }

			public ReunionEntity Reunion { get; }
			public Organizer Organizer { get; }
			public IEnumerable<Participant> Participants { get; }
			public OrganizerStatemachine OrganizerStatemachine { get; }
			public KnockStatemachine KnockStatemachine { get; }

			/// <summary>
			/// the date ranges, which have been selected by organizer for the reunion
			/// </summary>
			public IEnumerable<TimeRange> PossibleDatesOfOrganizer { get; }

			/// <summary>
			/// map participant id -> participant statemachine
			/// </summary>
			public IDictionary<int, ParticipantStatemachine> ParticipantStatemachines { get; }
		}

		#endregion

		#region fields

		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// static cache: exists as long as the process exists
		/// map: two letter iso language code -> CultureInfo
		/// </summary>
		private static readonly Dictionary<string, CultureInfo> _cultureCache = new Dictionary<string, CultureInfo>();

		private readonly IReunionDal _dal;
		private readonly IEmailSender _emailSender;
		private readonly string _mailAddressOfReunion;
		/// <summary>
		/// minimum count of seconds the service is waiting for a reaction of a player
		/// </summary>
		private readonly int _minimumWaitTimeSeconds;
		private readonly IBlResource _resource;
		private ReunionContext _reunionContext;

		/// <summary>
		/// e.g.: "https://findtime.de/participant/ShowMyCalendar/{0}"
		/// {0}: URL encoded unguessable id of participant
		/// </summary>
		private readonly string _startPage4Participant;
		/// <summary>
		/// URI to status page of reunion {0}: reunion id
		/// "https://findtime.de/reunion/status/{0}"
		/// </summary>
		private readonly string _statusPageOfReunion;

		private static object _sync = new object();
		private readonly ISystemTimeProvider _systemTimeProvider;
		private readonly ITransactionService _transactionService;

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="systemTimeProvider"></param>
		/// <param name="transactionService"></param>
		/// <param name="dal"></param>
		/// <param name="emailSender"></param>
		/// <param name="resource"></param>
		/// <param name="minimumWaitTimeSeconds">
		/// minimum count of seconds the service is waiting for a reaction of a player
		/// </param>
		/// <param name="startPage4Participant">
		/// http://findtime.de/participant/{0}
		/// {0}: unguessable id of participant
		/// </param>
		/// <param name="statusPageOfReunion">
		/// URI to status page of reunion
		/// http://findtime.de/reunion/status/{0}
		/// </param>
		/// <param name="mailAddressOfReunion">
		/// 
		/// </param>
		public ReunionBL(
			ISystemTimeProvider systemTimeProvider,
			ITransactionService transactionService,
			IReunionDal dal,
			IEmailSender emailSender,
			IBlResource resource,
			int minimumWaitTimeSeconds,
			string startPage4Participant,
			string statusPageOfReunion,
			string mailAddressOfReunion)
		{
			_systemTimeProvider = systemTimeProvider;
			_transactionService = transactionService;
			_dal = dal;
			_emailSender = emailSender;
			_resource = resource;
			_minimumWaitTimeSeconds = minimumWaitTimeSeconds;
			_startPage4Participant = startPage4Participant;
			_statusPageOfReunion = statusPageOfReunion;
			_mailAddressOfReunion = mailAddressOfReunion;
		}

		#endregion

		#region private

		/// <summary>
		/// e.g.: https://findtime.de/Participant/Edit/JuZjOZgZC0GtnwssOiG8gQTxo0Tw
		/// </summary>
		/// <param name="participant"></param>
		/// <returns></returns>
		private string CreateParticipantDirektLink(Participant participant)
		{
			return string.Format(_startPage4Participant, participant.UnguessableId);
		}

		/// <summary>
		/// randomized string id
		/// (URI-able)
		/// </summary>
		/// <returns></returns>
		private string CreateUnguessableId()
		{
			var rnd = ((uint)Math.Abs(new Random().Next())).ToByteArray();
			var guid = Guid.NewGuid().ToByteArray();
			return (guid.ToBase64String() + rnd.ToBase64String())
				.Remove(ignoreCase: false, pattern: "=")
				.Replace('+', '-')
				.Replace('/', '_');
		}

		/// <summary>
		/// map: possible date -> instance of DateProposal.
		/// All instances of DateProposal will be initialized with the information, that no participant has specified anything regarding to that day.
		/// </summary>
		/// <param name="reunionContext"></param>
		/// <returns></returns>
		private static IDictionary<DateTime, DateProposal> CreateNewDateProposals(ReunionContext reunionContext)
		{
			IEnumerable<TimeRange> organizersTimeRanges = reunionContext.PossibleDatesOfOrganizer;
			var participantIds = reunionContext.Participants.Select(p => p.Id).ToList();
			return organizersTimeRanges
				.Where(tr => tr.Preference != PreferenceEnum.NoWay && tr.Preference != PreferenceEnum.None)
				.SelectMany(GetDatesOfTimeRange)
				.ToDictionary(date => date, date => new DateProposal(date) {DontKnowParticipantIds = participantIds.ToList()});
		}

		/// <summary>
		/// performance optimized creation of CultureInfo for a given language iso code
		/// </summary>
		/// <param name="isoCode"></param>
		/// <returns></returns>
		private static CultureInfo GetCulture(string isoCode)
		{
			CultureInfo cultureInfo;

			lock (_sync)
			{
				if (!_cultureCache.TryGetValue(isoCode, out cultureInfo))
				{
					cultureInfo = new CultureInfo(isoCode);
					_cultureCache[isoCode] = cultureInfo;
				}
			}

			return cultureInfo;
		}

		private static IEnumerable<DateTime> GetDatesOfTimeRange(TimeRange timeRange)
		{
			var dates = new List<DateTime>();
			var date = timeRange.Start.Date;
			while (date <= timeRange.End.Date)
			{
				dates.Add(date);
				date = date.AddHours(26).Date;
			}
			return dates;
		}

		/// <summary>
		/// map: participant id -> list of dates, for which participant hasn't given any information
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		private Dictionary<int, IList<DateTime>> GetMissingDayInformations(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId: reunionId, organizerId: null, forceReload: false);
			if (reunionContext == null)
				return null;
			if (reunionContext.MissingDayInformations != null)
				return reunionContext.MissingDayInformations;

			var missingDayInformations = new Dictionary<int, IList<DateTime>>();
			reunionContext.MissingDayInformations = missingDayInformations;
			var dateProposals = ((IReunionBL) this).GetDateProposals(reunionId, organizerId: null);
			if (dateProposals == null)
				return null;
			var mostPossibleDates = dateProposals.Take(3).ToList();
			foreach (var possibleDate in mostPossibleDates)
			{
				foreach (var participantId in possibleDate.DontKnowParticipantIds)
				{
					IList<DateTime> dates;
					if (!missingDayInformations.TryGetValue(participantId, out dates))
					{
						dates = new List<DateTime>();
						missingDayInformations[participantId] = dates;
					}
					dates.Add(possibleDate.Date);
				}
			}

			return missingDayInformations;
		}

		/// <summary>
		/// All participants who haven't been invited yet.
		/// </summary>
		/// <param name="reunionContext"></param>
		/// <returns></returns>
		private static List<Participant> GetParticipantsToInvite(ReunionContext reunionContext)
		{
			return reunionContext.Participants.Where(p =>
			{
				var participantStatemachine = reunionContext.ParticipantStatemachines[p.Id];
				return participantStatemachine.CurrentState == participantStatemachine.StateCreated;
			}).ToList();
		}

		/// <summary>
		/// all ids of particpant who refused invitation at all
		/// </summary>
		/// <param name="reunionContext"></param>
		/// <returns></returns>
		private static List<int> GetRefusingParticipantIds(ReunionContext reunionContext)
		{
			return reunionContext.ParticipantStatemachines.Values
				.Where(s => s.StateMachineEntity.CurrentState == ParticipantStatusEnum.RejectedInvitation)
				.Select(s => s.StateMachineEntity.PlayerId)
				.ToList();
		}

		/// <summary>
		/// Loads all reunion data from database into cache except TimeRanges of participants.
		/// Note ! If this method has been called allready since last creation of this instance
		/// and forceReload==false, it will return the cached instance.
		/// Note! ReunionBl will be recreated on every request. See UnityConfig.cs
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId"></param>
		/// <param name="forceReload">
		/// if true, the method will recreate ReunionContext from scratch.
		/// </param>
		/// <returns></returns>
		private ReunionContext LoadReunion(int reunionId, int? organizerId, bool forceReload = false)
		{
			if (forceReload
				|| _reunionContext == null
				|| _reunionContext.Reunion.Id != reunionId
				|| (organizerId.HasValue && _reunionContext.Organizer.Id != organizerId))
			{
				var reunionInfo = _dal.LoadReunion(reunionId);
				if (reunionInfo == null || (organizerId.HasValue && reunionInfo.Organizer.Id != organizerId))
					return null;

				var organizerStateMachine = new OrganizerStatemachine(reunionInfo.OrganizerStatemachineEntity, _dal, this, _systemTimeProvider);
				var knockStatemachine = new KnockStatemachine(reunionInfo.KnockStatemachineEntity, _dal, this);

				var participantStateMachines = reunionInfo.ParticipantStatemachines.Select(s =>
					new ParticipantStatemachine(s, _minimumWaitTimeSeconds, _dal, this,_systemTimeProvider));

				var timeRangesOfOrganizer = _dal.GetTimeRangesOfOrganizer(reunionId, reunionInfo.Organizer.Id);

				_reunionContext = new ReunionContext(
					reunionInfo.ReunionEntity,
					reunionInfo.Organizer,
					reunionInfo.Participants,
					organizerStateMachine,
					knockStatemachine,
					timeRangesOfOrganizer,
					participantStateMachines
					);
			}

			if (organizerId.HasValue && _reunionContext.Organizer.Id != organizerId.Value)
				return null;

			return _reunionContext;
		}

		/// <summary>
		/// sends invitation mail to all participants who haven't got a primer invitation mail yet.
		/// </summary>
		/// <param name="reunionContext"></param>
		private void SendInvitationMailsToParticipants(ReunionContext reunionContext)
		{
			var participantsToInvite = GetParticipantsToInvite(reunionContext);
			foreach (var participant in participantsToInvite)
			{
				SendInvitationMailToParticipant(reunionContext, participant);
			}
		}

		/// <summary>
		/// sends primer invitation mail to given participant.
		/// </summary>
		/// <param name="reunionContext"></param>
		/// <param name="participant"></param>
		private void SendInvitationMailToParticipant(ReunionContext reunionContext, Participant participant)
		{
			string link = CreateParticipantDirektLink(participant);
			var cultureInfo = GetCulture(participant.LanguageIsoCodeOfPlayer);
			_emailSender.SendEmail(
				subject: string.Format(_resource.GetInvitationMailSubject(cultureInfo),
					reunionContext.Reunion.Name),
				emailBody: string.Format(_resource.GetInvitationMailBodyParticipant(cultureInfo),
					reunionContext.Reunion.Name,
					reunionContext.Reunion.InvitationText.Replace("\n", "<br>"),
					reunionContext.Reunion.Deadline,
					link),
				receipients: new[] { participant.MailAddress });
			reunionContext
				.ParticipantStatemachines[participant.Id]
				.Trigger(new ParticipantStatemachine.SignalPrimerInvitationSent());
		}

		/// <summary>
		/// saves preference of one day in the calendar of a participant
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="date"></param>
		/// <param name="reunionContext"></param>
		/// <param name="preference"></param>
		private void SetDatePreference(
			int participantId,
			DateTime date,
			ReunionContext reunionContext,
			PreferenceEnum preference)
		{
			bool accepted = preference == PreferenceEnum.Yes || preference == PreferenceEnum.PerfectDay;
			var timeRanges = _dal.GetTimeRangesOfParticipant(participantId).ToList();
			var isDateAcceptedCurrently = timeRanges.Any(r => r.IsAcceptedDate(date));
			if ((accepted && !isDateAcceptedCurrently)
				|| (!accepted && isDateAcceptedCurrently))
			{
				var oldTimeRange = timeRanges.FirstOrDefault(r => r.IsInRange(date));
				List<TimeRange> newTimeRanges;
				if (oldTimeRange != null)
				{
					newTimeRanges = timeRanges.Where(r => r != oldTimeRange).ToList();
					if (date != oldTimeRange.Start)
						newTimeRanges.Add(new TimeRange(oldTimeRange.Start, date.AddDays(-0.5).Date, oldTimeRange.Preference, player: null,
							reunion: null));
					if (date != oldTimeRange.End)
						newTimeRanges.Add(new TimeRange(date.AddDays(1.5).Date, oldTimeRange.End, oldTimeRange.Preference, player: null,
							reunion: null));
				}
				else
				{
					newTimeRanges = timeRanges.ToList();
				}
				newTimeRanges.Add(new TimeRange(date, date, preference, player: null, reunion: null));
				var participant = reunionContext.Participants.First(p => p.Id == participantId);
				_dal.UpdateTimeRangesOfParticipant(newTimeRanges, participant.UnguessableId);
				reunionContext.ParticipantStatemachines[participantId].Trigger(new ParticipantStatemachine.SignalDateRangesUpdated());
				var organizerStatemachine = reunionContext.OrganizerStatemachine;
				organizerStatemachine.Trigger(new OrganizerStatemachine.SignalDateRangesUpdated());
			}
		}

		/// <summary>
		/// Modifies the given parameter "dateProposal":
		/// Says that given participant has agrees to the given date.
		/// </summary>
		/// <param name="dateProposal"></param>
		/// <param name="participantId"></param>
		/// <param name="nameOfParticipant"></param>
		/// <param name="isRequiredParticipant"></param>
		private static void UpdateDateProposalByAcceptingParticipant(
			DateProposal dateProposal,
			int participantId,
			string nameOfParticipant,
			bool isRequiredParticipant)
		{
			dateProposal.AcceptingParticipantIds.Add(participantId);
			dateProposal.DontKnowParticipantIds.Remove(participantId);
			if (!dateProposal.AcceptingParticipants.IsEmpty())
				dateProposal.AcceptingParticipants += ", ";
			dateProposal.AcceptingParticipants += nameOfParticipant;
			if (isRequiredParticipant)
				dateProposal.CountAcceptingRequired++;
		}

		/// <summary>
		/// updates collection of dateProposals by knowledge of refusing participants:
		/// Updates members "CountRequiredRefused" and "RefusingParticipantIds" of class DateProposal.
		/// </summary>
		/// <param name="dateProposals"></param>
		/// <param name="refusingParticipantIds"></param>
		/// <param name="reunionContext"></param>
		private static void UpdateDateProposalsByRefusingParticipants(IDictionary<DateTime, DateProposal> dateProposals, List<int> refusingParticipantIds,
			ReunionContext reunionContext)
		{
			foreach (var dateProposal in dateProposals.Values)
			{
				foreach (var refusingParticipantId in refusingParticipantIds)
				{
					var participant = reunionContext.Participants.First(p => p.Id == refusingParticipantId);
					dateProposal.RefusingParticipantIds.Add(refusingParticipantId);
					dateProposal.DontKnowParticipantIds.Remove(refusingParticipantId);
					if (!dateProposal.RefusingParticipants.IsEmpty())
						dateProposal.RefusingParticipants += ", ";
					dateProposal.RefusingParticipants += participant.Name;
					if (participant.IsRequired)
						dateProposal.CountRefusingRequired++;
				}
			}
		}

		#endregion

		#region IReunionStatemachineBL

		/// <summary>
		/// Triggers signal "SignalFinalDateRejected" to the participant's statemachine,
		/// if participant hasn't marked final invitation date as ok.
		/// If there isn't a final invitation this method doese nothing.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void IReunionStatemachineBL.CheckFinalDateRejected(int reunionId, int participantId)
		{
			var bl = this as IReunionBL;

			var reunionContext = LoadReunion(reunionId, organizerId: null);

			var finalDate = reunionContext?.Reunion.FinalInvitationDate;

			if (finalDate != null)
			{
				var dateProposals = bl.GetDateProposals(reunionId, organizerId: null).ToList();
				var statemachine = reunionContext.ParticipantStatemachines[participantId];
				bool participantAcceptsFinalDate =
					dateProposals.Any(d => d.Date == finalDate.Value && d.AcceptingParticipantIds.Contains(participantId));
				if (!participantAcceptsFinalDate)
					statemachine.Trigger(new ParticipantStatemachine.SignalFinalDateRejected());
			}
		}

		/// <summary>
		/// clears the final invitation date of the reunion (sets it to null)
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.ClearFinalInvitationDate(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;
			_dal.SaveFinalInvitationDate(reunionId, null);
			reunionContext.Reunion.FinalInvitationDate = null;
		}

		/// <summary>
		/// Deactivates statemachines of a reunion
		/// (Sets StatemachineContext.IsTerminated = true)
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.DeactivateStatemachines(int reunionId)
		{
			_dal.DeactivateStatemachines(reunionId);
		}

		/// <summary>
		/// Reactivates statemachines of a reunion after having deactivated them by calling DeactivateStatemachines
		/// (Sets StatemachineContext.IsTerminated = false)
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.ReactivateStatemachines(int reunionId)
		{
			_dal.ReactivateStatemachines(reunionId);
		}

		/// <summary>
		/// Checks if we must send signal "SignalDateFound" or "SignalRequiredRejected" to 
		/// organizer's statemachine.
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.RecheckDateFound(int reunionId)
		{
			var bl = this as IReunionBL;

			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;

			var dateProposals = bl.GetDateProposals(reunionId, organizerId: null).ToList();
			var dateProposal = dateProposals.FirstOrDefault();

			// if there is at minimum one date for which all required particants aggree
			if (dateProposal?.AllRequiredAccepted ?? false)
			{
				var finalDate = reunionContext.Reunion.FinalInvitationDate;
				// if there is a final inviation allready and a required participant has rejected final invitation
				if (finalDate.HasValue
				    && dateProposals.All(d => d.Date != finalDate.Value || !d.AllRequiredAccepted))
				{
					// send signal "SignalRequiredRejected" to organizer
					reunionContext.OrganizerStatemachine.Trigger(new OrganizerStatemachine.SignalRequiredRejected());
				}

				// send signal "SignalDateFound" to organizer
				reunionContext.OrganizerStatemachine.Trigger(new OrganizerStatemachine.SignalDateFound());
			}
			else
			{
				// send signal "SignalRequiredRejected" to organizer
				reunionContext.OrganizerStatemachine.Trigger(new OrganizerStatemachine.SignalRequiredRejected());
			}
		}

		/// <summary>
		/// sends notification mail to organizer
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.SendKnockMail2Organizer(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;
			var culture = GetCulture(reunionContext.Organizer.LanguageIsoCodeOfPlayer);

			_emailSender.SendEmail(
				subject: string.Format(
					_resource.GetKnockMailSubject(culture), 
					reunionContext.Reunion.Name), 
				emailBody: string.Format(
					_resource.GetKnockMailBody(culture), 
					reunionContext.Reunion.Name, 
					string.Format(_statusPageOfReunion, reunionId)), 
				receipients: new[] {reunionContext.Organizer.MailAddress});
		}

		/// <summary>
		/// Sends first invitation mails with personal links to participants, who aren't invitated yet.
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.SendPrimaryInvitationMails(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			SendInvitationMailsToParticipants(reunionContext);
		}

		/// <summary>
		/// sends mail to given participant informing about the cancellation of former final invitation.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void IReunionStatemachineBL.SendRejectionMailToParticipant(int reunionId, int participantId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			var participant = reunionContext?.Participants.FirstOrDefault(p => p.Id == participantId);
			if (participant == null)
				return;
			string link = CreateParticipantDirektLink(participant);
			var cultureInfo = GetCulture(participant.LanguageIsoCodeOfPlayer);
			string finalDate = string.Empty;
			if (reunionContext.Reunion.FinalInvitationDate.HasValue)
				finalDate = reunionContext.Reunion.FinalInvitationDate.Value.ToString(cultureInfo.DateTimeFormat.ShortDatePattern);
			_emailSender.SendEmail(
				subject: string.Format(_resource.GetRejectionMailSubject(cultureInfo),
					reunionContext.Reunion.Name),
				emailBody: string.Format(_resource.GetRejectionMailBodyParticipant(cultureInfo),
					reunionContext.Reunion.Name,
					finalDate,
					link),
				receipients: new[] { participant.MailAddress });
		}

		/// <summary>
		/// Triggers signal "SignalFinalInvitationCanceledByOrganizer" to all participant statemachines
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.TriggerSignalFinalInvitationCanceled(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;
			foreach (var participantStatemachine in reunionContext.ParticipantStatemachines)
				participantStatemachine.Value.Trigger(new ParticipantStatemachine.SignalFinalInvitationCanceledByOrganizer());
		}

		/// <summary>
		/// Triggers signal "SignalKnock" to knockstatemachine of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionStatemachineBL.WakeOrganizer(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			reunionContext?.KnockStatemachine.Trigger(new KnockStatemachine.SignalKnock());
		}

		/// <summary>
		/// true, if given participant hasn't set any information regarding the most possible dates of reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		/// <returns></returns>
		bool IReunionStatemachineBL.MissingDaysOfParticipant(int reunionId, int participantId)
		{
			var missingDayInformations = GetMissingDayInformations(reunionId);
			if (missingDayInformations == null)
				return false;
			return missingDayInformations.ContainsKey(participantId);
		}

		/// <summary>
		/// Sends email to given participant informing about that participant hasn't set any information 
		/// regarding the most possible dates of reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void IReunionStatemachineBL.SendMissingDaysNotification(int reunionId, int participantId)
		{
			var missingDayInformations = GetMissingDayInformations(reunionId);
			if (missingDayInformations == null)
				return;
			IList<DateTime> missingDays;
			missingDayInformations.TryGetValue(participantId, out missingDays);


			var reunionContext = _reunionContext;
			var participant = reunionContext.Participants.First(p => p.Id == participantId);
			string link = CreateParticipantDirektLink(participant);
			var cultureInfo = GetCulture(participant.LanguageIsoCodeOfPlayer);
			string missingDaysText = null;
			foreach (var missingDay in missingDays)
			{
				if (missingDaysText == null)
					missingDaysText = string.Empty;
				else
					missingDaysText += ", ";
				missingDaysText += missingDay.ToString("d", cultureInfo);
			}

			_emailSender.SendEmail(
				subject: string.Format(_resource.GetMissingDayNotificationMailSubject(cultureInfo),
					reunionContext.Reunion.Name),
				emailBody: string.Format(_resource.GetMissingDayNotificationBody(cultureInfo),
					reunionContext.Reunion.Name,
					missingDaysText,
					link).Replace("\n", "<br>"),
				receipients: new[] { participant.MailAddress });
		}

		#endregion

		#region IReunionBL

		/// <summary>
		/// Must be called when a participant accepts a final invitation
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void IReunionBL.AcceptFinalDate(int participantId, int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			var finalInvitationDate = reunionContext?.Reunion.FinalInvitationDate;
			if (finalInvitationDate == null)
				return;
			var participantStatemachine = reunionContext.ParticipantStatemachines[participantId];
			// trigger signal "SignalAccepted" to participant statemachine
			participantStatemachine.Trigger(new ParticipantStatemachine.SignalFinalDateAccepted());

			// update calendar of participant
			SetDatePreference(participantId, finalInvitationDate.Value, reunionContext, PreferenceEnum.Yes);

			// send signal "SignalParticipantsAccepted", if all participants who have time, accepted final date
			var haveTimePlayers = _dal.GetAllTimeRanges(reunionId)
				.Where(r => r.IsAcceptedDate(finalInvitationDate.Value))
				.Select(r=>r.Player.Id)
				.ToList();

			var organizerId = reunionContext.Organizer.Id;
			var allParticipantsAccepted = haveTimePlayers.All(playerId =>
				playerId == organizerId
				|| reunionContext.ParticipantStatemachines[playerId].StateMachineEntity.CurrentState == ParticipantStatusEnum.Accepted);
			if (allParticipantsAccepted)
				reunionContext.OrganizerStatemachine.Trigger(new OrganizerStatemachine.SignalParticipantsAccepted());
		}

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
		ReunionCreateResult IReunionBL.CreateReunion(
			ReunionEntity reunion,
			Organizer organizer,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges)
		{
			// ReSharper disable once PossibleMultipleEnumeration
			participants.ForEach(p => p.UnguessableId = CreateUnguessableId());
			// ReSharper disable once PossibleMultipleEnumeration
			return _dal.CreateReunion(reunion, organizer, participants, possibleDateRanges);
		}

		/// <summary>
		/// deletes the given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		void IReunionBL.DeleteReunion(int reunionId, int organizerId)
		{
			_dal.DeleteReunion(reunionId: reunionId, organizerId: organizerId);
		}

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		void IReunionBL.DoWithSameDbContext(Action action)
		{
			_dal.DoWithSameDbContext(action);
		}

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		T IReunionBL.DoWithSameDbContext<T>(Func<T> action)
		{
			return _dal.DoWithSameDbContext(action);
		}

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
		void IReunionBL.FinallyInvite(int reunionId, int organizerId, DateTime date)
		{
			var cultureInfoOrganizer = Thread.CurrentThread.CurrentCulture;
			var reunionContext = LoadReunion(reunionId, organizerId: organizerId);
			if (reunionContext == null)
				return;

			_dal.SaveFinalInvitationDate(reunionId, date);
			reunionContext.Reunion.FinalInvitationDate = date;

			var dateFormatted = date.Date.ToString(cultureInfoOrganizer.DateTimeFormat.ShortDatePattern);

			foreach (var participant in reunionContext.Participants)
			{
				string link = CreateParticipantDirektLink(participant);
				var cultureInfoParticipant = GetCulture(participant.LanguageIsoCodeOfPlayer);
				// send mail to participant
				_emailSender.SendEmail(
					subject: string.Format(
						_resource.GetFinalInvitationMailSubject(cultureInfoParticipant), 
						reunionContext.Reunion.Name), 
					emailBody: string.Format(
						_resource.GetFinalInvitationMailBodyParticipant(cultureInfoParticipant), 
						reunionContext.Reunion.Name, 
						dateFormatted, 
						link).Replace("\n", "<br>"), 
					receipients: new[] { participant.MailAddress });

				// trigger participant statemachine
				reunionContext.ParticipantStatemachines[participant.Id].Trigger(new ParticipantStatemachine.SignalFinalInvitation());
			}

			// send mail to organizer
			_emailSender.SendEmail(
				subject: string.Format(
					_resource.GetFinalInvitationMailSubject(cultureInfoOrganizer),
					reunionContext.Reunion.Name),
				emailBody: string.Format(
					_resource.FinalInvitationMailBody,
					reunionContext.Reunion.Name,
					dateFormatted).Replace("\n", "<br>"),
				receipients: new[] { reunionContext.Organizer.MailAddress });

			// trigger organizer statemachine
			_reunionContext.OrganizerStatemachine.Trigger(new OrganizerStatemachine.SignalFinalInvitationSent());
		}

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
		IEnumerable<DateProposal> IReunionBL.GetDateProposals(int reunionId, int? organizerId)
		{
			ReunionContext reunionContext;

			return _transactionService.DoWithSameDbContext(() =>
			{
				reunionContext = LoadReunion(reunionId, organizerId);
				if (reunionContext == null)
					return null;
				if (!organizerId.HasValue)
					organizerId = reunionContext.Organizer.Id;

				if (reunionContext.CachedDateProposals != null)
					return reunionContext.CachedDateProposals;

				var dateProposals = CreateNewDateProposals(reunionContext);

				var refusingParticipantIds = GetRefusingParticipantIds(reunionContext);

				UpdateDateProposalsByRefusingParticipants(dateProposals, refusingParticipantIds, reunionContext);

				// update date proposals by knowledge of time preferences of participants.
				foreach (var timeRange in _dal.GetAllTimeRanges(reunionId))
				{
					// skip if time range is belonging to organizer
					if (timeRange.Player.Id == organizerId)
						continue;
					var participantId = timeRange.Player.Id;
					if (refusingParticipantIds.Contains(participantId))
						continue;
					var isRequiredParticipant = reunionContext.Participants.Any(p => p.IsRequired && p.Id == participantId);
					foreach (var date in GetDatesOfTimeRange(timeRange))
					{
						DateProposal dateProposal;
						if (dateProposals.TryGetValue(date, out dateProposal))
						{
							switch (timeRange.Preference)
							{
								case PreferenceEnum.None:
									break;
								case PreferenceEnum.NoWay:
									dateProposal.RefusingParticipantIds.Add(participantId);
									dateProposal.DontKnowParticipantIds.Remove(participantId);
									if (!dateProposal.RefusingParticipants.IsEmpty())
										dateProposal.RefusingParticipants += ", ";
									dateProposal.RefusingParticipants += timeRange.Player.Name;
									if (isRequiredParticipant)
										dateProposal.CountRefusingRequired++;
									break;
								case PreferenceEnum.PerfectDay:
									UpdateDateProposalByAcceptingParticipant(dateProposal, participantId, timeRange.Player.Name, isRequiredParticipant);
									dateProposal.CountPerfectDay++;
									break;
								case PreferenceEnum.Yes:
									UpdateDateProposalByAcceptingParticipant(dateProposal, participantId, timeRange.Player.Name, isRequiredParticipant);
									break;
								case PreferenceEnum.MayBe:
									// may not be choosable by organizer
									dateProposal.MayBeParticipantIds.Add(participantId);
									dateProposal.DontKnowParticipantIds.Remove(participantId);
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}
						}
					}
				}

				var countOfRequiredParticipants = reunionContext.Participants.Count(p => p.IsRequired);
				foreach (var dateProposal in dateProposals.Values)
				{
					if (dateProposal.CountAcceptingRequired == countOfRequiredParticipants)
						dateProposal.AllRequiredAccepted = true;
				}

				reunionContext.CachedDateProposals = dateProposals.Values
					.OrderByDescending(d => d.CountAcceptingRequired - d.CountRefusingRequired)
					.ThenByDescending(d => d.AcceptingParticipantIds.Count - d.RefusingParticipantIds.Count)
					.ThenByDescending(d => d.CountPerfectDay)
					.ThenBy(d => d.Date)
					.ToList();

				return reunionContext.CachedDateProposals;
			});
		}

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
		InvitationMailContent IReunionBL.GetInvitationMailContent(int reunionId,int organizerId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: organizerId);
			if (reunionContext == null)
				return null;

			var participantsToInvite = GetParticipantsToInvite(reunionContext)
				.Where(p => p.ContactPolicy != ContactPolicyEnum.MayContactByWebservice)
				.Select(p=>p.MailAddress)
				.ToArray();

			if (participantsToInvite.Any())
			{
				var culture = GetCulture(reunionContext.Organizer.LanguageIsoCodeOfPlayer);

				return new InvitationMailContent(
					subject: string.Format(_resource.GetInvitationMailSubject(culture),
						reunionContext.Reunion.Name),
					body: string.Format(_resource.InvitationMailBody,
						reunionContext.Reunion.Name,
						reunionContext.Reunion.InvitationText,
						reunionContext.Reunion.Deadline,
						_mailAddressOfReunion),
					receipientMailAddresses: participantsToInvite);
			}
			return null;
		}

		/// <summary>
		/// Gets organizer by id of current user and reunion id
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="userId">
		/// user id (Organizer.UserId)
		/// </param>
		/// <returns></returns>
		public Organizer GetOrganizer(int reunionId, string userId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return null;
			var organizer = reunionContext.Organizer;
			if (organizer.UserId != userId)
				return null;
			reunionContext.KnockStatemachine.Trigger(new KnockStatemachine.SignalLoggedin());
			return organizer;
		}

		/// <summary>
		/// Get participants and their status for a given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// The id of the organizer (Organizer.Id)
		/// (must be set to authenticate the caller)
		/// </param>
		/// <returns></returns>
		IEnumerable<Tuple<Participant, ParticipantStatusEnum>> IReunionBL.GetParticipants(int reunionId, int organizerId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId);
			return reunionContext?.Participants.Select(p => new Tuple<Participant, ParticipantStatusEnum>(p, reunionContext.ParticipantStatemachines[p.Id].StateMachineEntity.CurrentState)).ToList();
		}

		/// <summary>
		/// Gets all reunions created by the given user.
		/// </summary>
		/// <param name="userId">
		/// user id (Organizer.UserId)
		/// </param>
		/// <returns></returns>
		IEnumerable<ReunionEntity> IReunionBL.GetReunionsOfUser(string userId)
		{
			return _dal.GetReunionsOfUser(userId);
		}

		/// <summary>
		/// Gets reunion status 
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns>null, if reunion couldn't be found</returns>
		OrganizatorStatusEnum? IReunionBL.GetReunionStatus(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			return reunionContext?.OrganizerStatemachine.StateMachineEntity.CurrentState;
		}

		/// <summary>
		/// time ranges of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> IReunionBL.GetTimeRangesOfReunion(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			return reunionContext?.PossibleDatesOfOrganizer;
		}

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
		IEnumerable<TimeRange> IReunionBL.GetTimeRangesOfParticipant(
			int reunionId, 
			int participantId, 
			out DateTime? finalInvitationdate, 
			out bool? hasAcceptedFinalInvitationdate,
			out IEnumerable<DateTime> daysToBeChecked)
		{
			finalInvitationdate = null;
			hasAcceptedFinalInvitationdate = default(bool?);
			daysToBeChecked = null;
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return null;

			finalInvitationdate = reunionContext.Reunion.FinalInvitationDate;
			var participantStatemachine = reunionContext.ParticipantStatemachines[participantId];
			switch (participantStatemachine.StateMachineEntity.CurrentState)
			{
				case ParticipantStatusEnum.RejectedInvitation:
					hasAcceptedFinalInvitationdate = false;
					break;
				case ParticipantStatusEnum.Accepted:
					hasAcceptedFinalInvitationdate = true;
					break;
				case ParticipantStatusEnum.MissingInformation:
				case ParticipantStatusEnum.ReactionOnFeedbackMissing:
					var missingDayInformations = GetMissingDayInformations(reunionId);
					IList<DateTime> daysToCheck;
					missingDayInformations.TryGetValue(participantId, out daysToCheck);
					daysToBeChecked = daysToCheck;
					break;
			}

			var timeRanges = _dal.GetTimeRangesOfParticipant(participantId);
			return timeRanges;
		}

		/// <summary>
		/// Gets participant by unguessable id of the participant.
		/// </summary>
		/// <param name="unguessableParticipantId"></param>
		/// <returns>null, if not found</returns>
		Participant IReunionBL.GetVerifiedParticipant(string unguessableParticipantId)
		{
			int reunionId = _dal.FindReunionIdOfParticipant(unguessableParticipantId);
			if (reunionId == default(int))
				return null;
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return null;
			foreach (var participant in reunionContext.Participants)
			{
				if (participant.UnguessableId == unguessableParticipantId)
				{
					reunionContext.ParticipantStatemachines[participant.Id].Trigger(new ParticipantStatemachine.SignalLoggedIn());

					return participant;
				}
			}

			return null;
		}

		/// <summary>
		/// If participant has been finally invited, this call marks the invitation date 
		/// as not possible for the participant.
		/// If the participant is required, this will result to a lot of actions and mails.
		/// If the participant hasn't been finally invited, nothing will happen.
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void IReunionBL.RejectFinalDateOnly(int participantId, int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			var finalInvitationDate = reunionContext?.Reunion.FinalInvitationDate;
			if (finalInvitationDate == null)
				return;

			// update calendar of participant
			SetDatePreference(participantId, finalInvitationDate.Value, reunionContext, PreferenceEnum.NoWay);
		}

		/// <summary>
		/// Removes participant from database.
		/// Saves his name in the list of participants, who  have totally rejected invitation.
		/// </summary>
		/// <param name="participantId"></param>
		/// <param name="reunionId"></param>
		void IReunionBL.RejectCompletely(int participantId, int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			var participant = reunionContext?.Participants.FirstOrDefault(p => p.Id == participantId);
			if (participant == null)
				return;
			_dal.RemoveParticipant(reunionId, participantId);
			LoadReunion(reunionId, organizerId: null, forceReload: true);
			(this as IReunionStatemachineBL).RecheckDateFound(reunionId);
		}

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
		ReunionEntity IReunionBL.StartReunion(int reunionId, int organizerId)
		{
			return _transactionService.DoInTransaction(() =>
			{
				var reunionContext = LoadReunion(reunionId, organizerId);
				if (reunionContext == null)
					return null;
				var statemachine = reunionContext.OrganizerStatemachine;
				statemachine.Trigger(new OrganizerStatemachine.SignalStartReunion());
				return reunionContext.Reunion;
			});
		}

		/// <summary>
		/// Loads all active statemachines (StatemachineContext.IsTerminated == false) and calls 
		/// IStateMachine.Touch().
		/// This method must be called time by time to ensure that actions take place when some time markers elapse.
		/// This method may result in notification mails to participants and organizers !
		/// </summary>
		void IReunionBL.TouchAllReunions()
		{
			var statemachineEntities = _dal.GetAllRunningStatemachines();
			foreach (var statemachineEntity in statemachineEntities)
			{
				switch (statemachineEntity.StatemachineTypeId)
				{
					case StatemachineIdEnum.KnockStatemachine:
						new KnockStatemachine(statemachineEntity as KnockStatemachineEntity, _dal, this).Touch();
						break;
					case StatemachineIdEnum.ParticipantStatemachine:
						new ParticipantStatemachine(statemachineEntity as ParticipantStatemachineEntity, _minimumWaitTimeSeconds, _dal, this,_systemTimeProvider).Touch();
						break;
					case StatemachineIdEnum.OrganizerStatemachine:
						new OrganizerStatemachine(statemachineEntity as OrganizerStatemachineEntity, _dal, this,_systemTimeProvider).Touch();
						break;
					default:
						throw new ApplicationException("53guas8924h " + statemachineEntity.StatemachineTypeId);
				}
			}
		}

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
		ReunionUpdateResult IReunionBL.UpdateReunion(ReunionEntity reunion, int verifiedOrganizerId, IEnumerable<Participant> participants, IEnumerable<TimeRange> possibleDateRanges)
		{
			// ReSharper disable once PossibleMultipleEnumeration
			participants.Where(p => p.Id == 0).ForEach(p => p.UnguessableId = CreateUnguessableId());
			// ReSharper disable once PossibleMultipleEnumeration
			var res = _dal.UpdateReunion(reunion, verifiedOrganizerId, participants, possibleDateRanges);
			if (res.ResultCode == ReunionUpdateResult.ResultCodeEnum.Succeeded)
			{
				var reunionContext = LoadReunion(reunion.Id, organizerId: null, forceReload: true);
				if (reunionContext.OrganizerStatemachine.StateMachineEntity.CurrentState != OrganizatorStatusEnum.Created)
				{
					SendInvitationMailsToParticipants(reunionContext);
					(this as IReunionStatemachineBL).RecheckDateFound(reunion.Id);
				}
			}
			return res;
		}

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
		void IReunionBL.UpdateTimeRangesOfParticipant(IEnumerable<TimeRange> timeRanges, string unguessableIdOfParticipant)
		{
			var reunionId = _dal.UpdateTimeRangesOfParticipant(timeRanges, unguessableIdOfParticipant);

			var reunionContext = LoadReunion(reunionId, organizerId: null, forceReload: true);
			if (reunionContext == null)
				return;
			var participantId = reunionContext.Participants.First(p => p.UnguessableId == unguessableIdOfParticipant).Id;

			var participantStateMachine = reunionContext.ParticipantStatemachines[participantId];
			participantStateMachine.Trigger(new ParticipantStatemachine.SignalDateRangesUpdated());
			var organizerStatemachine = reunionContext.OrganizerStatemachine;
			organizerStatemachine.Trigger(new OrganizerStatemachine.SignalDateRangesUpdated());

			// touch statemachines
			var stateMachines = reunionContext.ParticipantStatemachines.Values.Cast<IStateMachine>()
				.Concat(new[] {reunionContext.OrganizerStatemachine});

			foreach (var stateMachine in stateMachines)
				stateMachine.Touch();
		}

		#endregion
	}
}
