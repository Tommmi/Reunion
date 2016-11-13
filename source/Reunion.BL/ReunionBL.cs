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
		private static object _sync = new object();
		private readonly IReunionDal _dal;
		private readonly IEmailSender _emailSender;
		private readonly string _mailAddressOfReunion;
		private readonly int _minimumWaitTimeSeconds;
		private readonly IBlResource _resource;
		private ReunionContext _reunionContext;

		/// <summary>
		/// http://findtime.de/participant/ShowMyCalendar/{0}
		/// {0}: URL encoded unguessable id of participant
		/// </summary>
		private readonly string _startPage4Participant;
		/// <summary>
		/// URI to status page of reunion {0}: reunion id
		/// http://findtime.de/reunion/status/{0}
		/// </summary>
		private readonly string _statusPageOfReunion;

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
		/// <param name="minimumWaitTimeSeconds"></param>
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

		private string CreateParticipantDirektLink(Participant participant)
		{
			return string.Format(_startPage4Participant, participant.UnguessableId);
		}

		/// <summary>
		/// randomized string id
		/// Requirement: must be URI encoded
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

		private static IDictionary<DateTime, DateProposal> CreateNewDateProposals(ReunionContext reunionContext)
		{
			IEnumerable<TimeRange> organizersTimeRanges = reunionContext.PossibleDatesOfOrganizer;
			var participantIds = reunionContext.Participants.Select(p => p.Id).ToList();
			return organizersTimeRanges
				.Where(tr => tr.Preference != PreferenceEnum.NoWay && tr.Preference != PreferenceEnum.None)
				.SelectMany(GetDatesOfTimeRange)
				.ToDictionary(date => date, date => new DateProposal(date) {DontKnowParticipantIds = participantIds.ToList()});
		}

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

		private static List<Participant> GetParticipantsToInvite(ReunionContext reunionContext)
		{
			return reunionContext.Participants.Where(p =>
			{
				var participantStatemachine = reunionContext.ParticipantStatemachines[p.Id];
				return participantStatemachine.CurrentState == participantStatemachine.StateCreated;
			}).ToList();
		}

		private static List<int> GetRefusingParticipantIds(ReunionContext reunionContext)
		{
			return reunionContext.ParticipantStatemachines.Values
				.Where(s => s.StateMachineEntity.CurrentState == ParticipantStatusEnum.RejectedInvitation)
				.Select(s => s.StateMachineEntity.PlayerId)
				.ToList();
		}

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

		private void SendInvitationMailsToParticipants(ReunionContext reunionContext)
		{
			var participantsToInvite = GetParticipantsToInvite(reunionContext);
			foreach (var participant in participantsToInvite)
			{
				SendInvitationMailToParticipant(reunionContext, participant);
			}
		}

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

		private static void UpdateDateProposalByAcceptingParticipant(
			DateProposal dateProposal,
			int participantId,
			TimeRange timeRange,
			bool isRequiredParticipant)
		{
			dateProposal.AcceptingParticipantIds.Add(participantId);
			dateProposal.DontKnowParticipantIds.Remove(participantId);
			if (!dateProposal.AcceptingParticipants.IsEmpty())
				dateProposal.AcceptingParticipants += ", ";
			dateProposal.AcceptingParticipants += timeRange.Player.Name;
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

		void IReunionStatemachineBL.ClearFinalInvitationDate(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;
			_dal.SaveFinalInvitationDate(reunionId, null);
			reunionContext.Reunion.FinalInvitationDate = null;
		}

		void IReunionStatemachineBL.DeactivateStatemachines(int reunionId)
		{
			_dal.DeactivateStatemachines(reunionId);
		}

		void IReunionStatemachineBL.ReactivateStatemachines(int reunionId)
		{
			_dal.ReactivateStatemachines(reunionId);
		}

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

		void IReunionStatemachineBL.SendPrimaryInvitationMails(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			SendInvitationMailsToParticipants(reunionContext);
		}

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

		void IReunionStatemachineBL.TriggerSignalFinalInvitationCanceled(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			if (reunionContext == null)
				return;
			foreach (var participantStatemachine in reunionContext.ParticipantStatemachines)
				participantStatemachine.Value.Trigger(new ParticipantStatemachine.SignalFinalInvitationCanceledByOrganizer());
		}

		void IReunionStatemachineBL.WakeOrganizer(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			reunionContext?.KnockStatemachine.Trigger(new KnockStatemachine.SignalKnock());
		}

		bool IReunionStatemachineBL.MissingDaysOfParticipant(int reunionId, int participantId)
		{
			var missingDayInformations = GetMissingDayInformations(reunionId);
			if (missingDayInformations == null)
				return false;
			return missingDayInformations.ContainsKey(participantId);
		}

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

		void IReunionBL.DeleteReunion(int reunionId, int organizerId)
		{
			_dal.DeleteReunion(reunionId: reunionId, organizerId: organizerId);
		}

		void IReunionBL.DoWithSameDbContext(Action action)
		{
			_dal.DoWithSameDbContext(action);
		}

		T IReunionBL.DoWithSameDbContext<T>(Func<T> action)
		{
			return _dal.DoWithSameDbContext(action);
		}

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
									UpdateDateProposalByAcceptingParticipant(dateProposal, participantId, timeRange, isRequiredParticipant);
									dateProposal.CountPerfectDay++;
									break;
								case PreferenceEnum.Yes:
									UpdateDateProposalByAcceptingParticipant(dateProposal, participantId, timeRange, isRequiredParticipant);
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

		IEnumerable<Tuple<Participant, ParticipantStatusEnum>> IReunionBL.GetParticipants(int reunionId, int organizerId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId);
			return reunionContext?.Participants.Select(p => new Tuple<Participant, ParticipantStatusEnum>(p, reunionContext.ParticipantStatemachines[p.Id].StateMachineEntity.CurrentState)).ToList();
		}

		IEnumerable<ReunionEntity> IReunionBL.GetReunionsOfUser(string userId)
		{
			return _dal.GetReunionsOfUser(userId);
		}

		OrganizatorStatusEnum? IReunionBL.GetReunionStatus(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			return reunionContext?.OrganizerStatemachine.StateMachineEntity.CurrentState;
		}

		IEnumerable<TimeRange> IReunionBL.GetTimeRangesOfReunion(int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			return reunionContext?.PossibleDatesOfOrganizer;
		}

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

		void IReunionBL.RejectFinalDateOnly(int participantId, int reunionId)
		{
			var reunionContext = LoadReunion(reunionId, organizerId: null);
			var finalInvitationDate = reunionContext?.Reunion.FinalInvitationDate;
			if (finalInvitationDate == null)
				return;

			// update calendar of participant
			SetDatePreference(participantId, finalInvitationDate.Value, reunionContext, PreferenceEnum.NoWay);
		}

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
