using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using Reunion.Common.Model;
using Reunion.Web.Common;
using Reunion.Web.Resources;
using TUtils.Common.MVC;

namespace Reunion.Web.Models
{
	public class ReunionViewModel
	{
		public int Id { get; set; } = 0;

		[Required(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof (Resource1))]
		[Display(Name = "ReunionNameLabel", ResourceType = typeof (Resource1))]
		[StringLength(maximumLength: 40, ErrorMessageResourceName = "ErrTextTooLong",
			ErrorMessageResourceType = typeof (Resource1))]
		public string Name { get; set; }

		[Required(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof (Resource1))]
		[Display(Name = "InvitationTextLabel", ResourceType = typeof (Resource1))]
		[DataType(DataType.MultilineText)]
		[StringLength(maximumLength: 1000, ErrorMessageResourceName = "ErrTextTooLong",
			ErrorMessageResourceType = typeof (Resource1))]
		public string InvitationText { get; set; }

		/// <summary>
		/// Json-encoded string of "ParticipantViewModel[]"
		/// </summary>
		[Display(Name = "Participants", ResourceType = typeof (Resource1))]
		[UIHint("ParticipantsPartial")]
		[Required(ErrorMessageResourceName = "ErrEnterParticipants", ErrorMessageResourceType = typeof (Resource1))]
		public string ParticipantsAsJson { get; set; }

		[Display(Name = "Deadline", ResourceType = typeof(Resource1))]
		[Required(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof(Resource1))]
		[MustBeInFuture]
		[DataType(dataType: DataType.Date)]
//		[MustBeDate(ErrorMessageResourceName = "ErrRequired", ErrorMessageResourceType = typeof(Resource1))]
		public DateTime Deadline { get; set; }

		/// <summary>
		/// example: "2:12.06.2016-20.06.2016;4:25.06.2016-25.06.2016"
		/// Format: {selection index}:{start date}-{end date};{selection index}:{start date}-{end date}
		/// </summary>
		[Display(Name = "WhenDoYouCome", ResourceType = typeof (Resource1))]
		[UIHint("MultiselectionCalendarPartial")]
		public string PossibleDates { get; set; }

		public ReunionViewModel()
		{
			Deadline = DateTime.Now.Date;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="invitationText"></param>
		/// <param name="participants">
		/// </param>
		/// <param name="deadline"></param>
		/// <param name="possibleDates"></param>
		public ReunionViewModel(
			int id,
			string name,
			string invitationText,
			IEnumerable<Participant> participants,
			DateTime deadline,
			string possibleDates)
		{
			Id = id;
			Name = name;
			InvitationText = invitationText;
			Deadline = deadline;
			PossibleDates = possibleDates;
			ParticipantsAsJson = Json.Encode(participants
				.Select(p => new ParticipantViewModel(
					id: p.Id,
					name: p.Name,
					mail: p.MailAddress,
					isRequired: p.IsRequired,
					contactPolicy: p.ContactPolicy,
					playerLanguageIsoCode: p.LanguageIsoCodeOfPlayer)).ToArray());
		}
	}
}