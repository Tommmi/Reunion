using System;
using System.ComponentModel.DataAnnotations;

namespace Reunion.Common.Model
{
	public class TimeRange
	{
		public TimeRange(
			DateTime start,
			DateTime end,
			PreferenceEnum preference,
			Participant participant,
			ReunionEntity reunion)
		{
			Init(
				start,
				end,
				preference,
				participant,
				reunion);
		}

		public TimeRange Init(
			DateTime start,
			DateTime end,
			PreferenceEnum preference,
			Participant participant,
			ReunionEntity reunion)
		{
			Start = start;
			End = end;
			Preference = preference;
			Participant = participant;
			Reunion = reunion;
			return this;
		}

		public TimeRange()
		{
		}

		public int Id { get; set; }
		[Required]
		public ReunionEntity Reunion { get; set; }
		[Required]
		public Participant Participant { get; set; }
		[Required]
		public DateTime Start { get; set; }
		[Required]
		public DateTime End { get; set; }
		[Required]
		public PreferenceEnum Preference { get; set; }

	}
}
