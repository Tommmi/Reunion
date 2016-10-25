using System;
using System.ComponentModel.DataAnnotations;

namespace Reunion.Common.Model
{
	/// <summary>
	/// Entity which represents a date range intervall, when the given player can come or not.
	/// TimeRange is associated with exactly one reunion and one player
	/// </summary>
	public class TimeRange
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="preference"></param>
		/// <param name="player"></param>
		/// <param name="reunion"></param>
		public TimeRange(
			DateTime start,
			DateTime end,
			PreferenceEnum preference,
			Player player,
			ReunionEntity reunion)
		{
			Init(
				start,
				end,
				preference,
				player,
				reunion);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="preference"></param>
		/// <param name="player"></param>
		/// <param name="reunion"></param>
		/// <returns></returns>
		public TimeRange Init(
			DateTime start,
			DateTime end,
			PreferenceEnum preference,
			Player player,
			ReunionEntity reunion)
		{
			Start = start;
			End = end;
			Preference = preference;
			Player = player;
			Reunion = reunion;
			return this;
		}

		public TimeRange()
		{
		}

		public int Id { get; set; }
		public ReunionEntity Reunion { get; set; }
		public Player Player { get; set; }
		[Required]
		public DateTime Start { get; set; }
		[Required]
		public DateTime End { get; set; }
		[Required]
		public PreferenceEnum Preference { get; set; }

		/// <summary>
		/// true if "date" is in date range [Start,End]
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public bool IsInRange(DateTime date)
		{
			return date.Date >= Start.Date && date.Date <= End.Date;
		}

		/// <summary>
		/// true, if date is in this date range and the date range is marked as a time, 
		/// when the player can come to the meeting
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		public bool IsAcceptedDate(DateTime date)
		{
			return IsInRange(date)
				   && (Preference == PreferenceEnum.Yes || Preference == PreferenceEnum.PerfectDay);
		}
	}
}
