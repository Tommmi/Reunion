using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using MultiSelectionCalendar;
using Reunion.Common.Model;
using Reunion.Web.Models;

namespace Reunion.Web.Common
{
	public static class ReunionEntityExtension
	{
		public static ReunionViewModel GetReunionViewModel(
			this ReunionEntity reunionEntity, 
			IEnumerable<Participant> participants, 
			IEnumerable<TimeRange> timeRangesOfOrganizer)
		{
			return new ReunionViewModel(
				id: reunionEntity.Id,
				name: reunionEntity.Name,
				invitationText:reunionEntity.InvitationText,
				participants: participants,
				deadline:reunionEntity.Deadline,
				possibleDates:Calendar.GetStringFromDateRanges(
					timeRangesOfOrganizer
						.Select(r=>new Range(r.Start,r.End,(int)(r.Preference)))
						.ToList()));
		}
	}
}