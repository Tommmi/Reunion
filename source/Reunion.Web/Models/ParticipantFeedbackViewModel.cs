using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web;
using MultiSelectionCalendar;
using Reunion.Common.Model;
using Reunion.Web.Resources;
using Calendar = MultiSelectionCalendar.Calendar;

namespace Reunion.Web.Models
{
	public class ParticipantFeedbackViewModel
	{
		private readonly Participant _participant;

		public ParticipantFeedbackViewModel()
		{
			
		}
		public ParticipantFeedbackViewModel(
			Participant participant,
			IEnumerable<TimeRange> preferredDates,
			IEnumerable<TimeRange> timeRangesOfOrganizer,
			DateTime? finalInvitationDate,
			bool? hasAcceptedFinalInvitationdate)
		{
			TimeRangesOfOrganizer = timeRangesOfOrganizer;
			FinalInvitationDate = finalInvitationDate;
			Culture = new CultureInfo(participant.LanguageIsoCodeOfPlayer);
			HasAcceptedFinalInvitationdate = hasAcceptedFinalInvitationdate;
			_participant = participant;
			PossibleDates = Calendar.GetStringFromDateRanges(
				preferredDates
					.Select(r => new Range(r.Start, r.End, (int)(r.Preference)))
					.ToList());
			UnguessableParticipantId = participant.UnguessableId;
			var setMonths = GetMonths(preferredDates);
			UnsetMonths = GetMonths(timeRangesOfOrganizer)
				.Where(d=>!setMonths.Contains(d))
				.ToList();
		}

		private static List<DateTime> GetMonths(IEnumerable<TimeRange> preferredDates)
		{
			return preferredDates
				.SelectMany(r =>
				{
					var monthsOfRange = new List<DateTime>();
					var startMonth = r.Start;
					startMonth = new DateTime(year: startMonth.Year, month: startMonth.Month, day: 1);
					var endMonth = r.End;
					endMonth = new DateTime(year: endMonth.Year, month: endMonth.Month, day: 1);
					for (var d = startMonth; d <= endMonth; d = d.Date.AddMonths(1).Date)
						monthsOfRange.Add(d);
					return monthsOfRange;
				})
				.Distinct()
				.OrderBy(d => d)
				.ToList();
		}

		public IEnumerable<TimeRange> TimeRangesOfOrganizer { get; private set; }

		public IEnumerable<DateTime>  UnsetMonths { get; private set; }

		public string ParticipantName => _participant.Name;

		/// <summary>
		/// != null, if there is a final invitation date 
		/// </summary>
		public DateTime? FinalInvitationDate { get; private set; }

		public bool? HasAcceptedFinalInvitationdate { get; set; }

		public string GetLocalizedText(string ressourceKey, params object[] args)
		{
			return string.Format(Resource1.ResourceManager.GetString(ressourceKey, Culture), args);
		}

		public CultureInfo Culture { get; private set; }

		public string UnguessableParticipantId { get; set; }
		/// <summary>
		/// example: "2:12.06.2016-20.06.2016;4:25.06.2016-25.06.2016"
		/// Format: {selection index}:{start date}-{end date};{selection index}:{start date}-{end date}
		/// </summary>
		[Required(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof(Resource1))]
		[Display(Name = "ProposedDates", ResourceType = typeof(Resource1))]
		[UIHint("MultiselectionCalendarPartial")]
		public string PossibleDates { get; set; }
	}
}