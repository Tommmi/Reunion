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

		public ParticipantFeedbackViewModel()
		{
			
		}
		public ParticipantFeedbackViewModel(
			Participant participant,
			IEnumerable<TimeRange> preferredDates,
			IEnumerable<TimeRange> timeRangesOfOrganizer,
			DateTime? finalInvitationDate,
			bool? hasAcceptedFinalInvitationdate,
			IEnumerable<DateTime> daysToBeChecked)
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
			if (daysToBeChecked != null)
			{
				UnsetDaysFormattedText = null;
				foreach (var day in daysToBeChecked)
				{
					if (UnsetDaysFormattedText == null)
						UnsetDaysFormattedText = string.Empty;
					else
						UnsetDaysFormattedText += ", ";

					UnsetDaysFormattedText += day.ToString("d", Culture);
				}
			}
		}

		/// <summary>
		/// culture of participant
		/// </summary>
		public CultureInfo Culture { get; private set; }

		/// <summary>
		/// HasValue, if there is a final invitation date.
		/// (In this case final invitaion mail has been sent to participant)
		/// </summary>
		public DateTime? FinalInvitationDate { get; private set; }

		/// <summary>
		/// true, if participant has accepted final invitation
		/// </summary>
		public bool? HasAcceptedFinalInvitationdate { get; set; }

		/// <summary>
		/// name of participant
		/// </summary>
		public string ParticipantName => _participant.Name;

		/// <summary>
		/// date preferences of participant.
		/// example: "2:12.06.2016-20.06.2016;4:25.06.2016-25.06.2016"
		/// Format: {selection index}:{start date}-{end date};{selection index}:{start date}-{end date}
		/// {selection index}: see PreferenceEnum
		/// </summary>
		[Required(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof(Resource1))]
		[Display(Name = "ProposedDates", ResourceType = typeof(Resource1))]
		[UIHint("MultiselectionCalendarPartial")]
		public string PossibleDates { get; set; }

		/// <summary>
		/// if any which days may be choosed by participant ?
		/// </summary>
		public IEnumerable<TimeRange> TimeRangesOfOrganizer { get; private set; }

		/// <summary>
		/// which months participant forget to fill in ?
		/// </summary>
		public IEnumerable<DateTime>  UnsetMonths { get; private set; }

		/// <summary>
		/// which days should participant check ?
		/// May be null.
		/// </summary>
		public string UnsetDaysFormattedText { get; set; }

		public string UnguessableParticipantId { get; set; }

		/// <summary>
		/// Gets a string by ressource key and Format parameters.
		/// Uses the participant's culture to localize it.
		/// </summary>
		/// <param name="ressourceKey"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public string GetLocalizedText(string ressourceKey, params object[] args)
		{
			return string.Format(Resource1.ResourceManager.GetString(ressourceKey, Culture), args);
		}
	}
}