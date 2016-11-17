using System;
using System.Collections.Generic;

namespace Reunion.Common
{
	/// <summary>
	/// All planning information for a single date:
	/// - day
	/// - who agreed to come
	/// - who may come
	/// - who won't come
	/// - who hasn't specified anything relating the given date
	/// - how many required participants may come
	/// - how many required participants won't come
	/// </summary>
	public class DateProposal
	{
		/// <summary>
		/// The date, to which these informations belong to
		/// </summary>
		public DateTime Date { get; set; }
		/// <summary>
		/// all participant ids of those who have time on that day
		/// </summary>
		public IList<int> AcceptingParticipantIds { get; private set; } =  new List<int>();
		/// <summary>
		/// all participant ids of those who won't come
		/// </summary>
		public IList<int> RefusingParticipantIds { get; private set; } = new List<int>();
		/// <summary>
		/// all participant ids of those who may come on that date
		/// </summary>
		public IList<int> MayBeParticipantIds { get; private set; } = new List<int>();
		/// <summary>
		/// all participants who didn't give an information for that day
		/// </summary>
		public IList<int> DontKnowParticipantIds { get; set; } = new List<int>();
		/// <summary>
		/// comma seperated list of all participants, who accept this date
		/// </summary>
		public string AcceptingParticipants { get; set; } = "";
		/// <summary>
		/// comma seperated list of all participants, who refuse this date
		/// </summary>
		public string RefusingParticipants { get; set; } = "";

		/// <summary>
		/// true, if all required participants can come
		/// </summary>
		public bool AllRequiredAccepted { get; set; }

		/// <summary>
		/// count of required participants, who may come
		/// </summary>
		public int CountAcceptingRequired { get; set; }
		/// <summary>
		/// count of required participants, who won't come
		/// </summary>
		public int CountRefusingRequired { get; set; }
		/// <summary>
		/// count of participants, who say that is a perfect day for date
		/// </summary>
		public int CountPerfectDay { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="date">
		/// The date, to which these informations belong to
		/// </param>
		public DateProposal(DateTime date)
		{
			Date = date;
		}
	}
}
