using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reunion.Common.Model
{
	public class ReunionEntity
	{
		public ReunionEntity()
		{
			Participants = new HashSet<Participant>();
		}

		public ReunionEntity Init(
			int reunionId,
			string name,
			string invitationText,
			Organizer organizer,
			DateTime deadline,
			ICollection<Participant> participants)
		{
			Id = reunionId;
			Name = name;
			Organizer = organizer;
			Deadline = deadline;
			Participants = participants;

			return this;
		}

		public int Id { get; set; }
		[MaxLength(50)]
		[Column(TypeName = "VARCHAR")]
		[StringLength(50)]
		[Index("REUNIONNAMEIDX", Order = 1, IsClustered = false, IsUnique = true)]
		public string Name { get; set; }
		[MaxLength(1200)]
		public string InvitationText { get; set; }
		[Required]
		[ForeignKey(nameof(Organizer))]
		[Index("REUNIONNAMEIDX", Order = 2, IsClustered = false, IsUnique = true)]
		public int OrganizerId { get; set; }
		[Required]
		public Organizer Organizer { get; set; }
		public ICollection<Participant> Participants { get; set; }
		[Required]
		public DateTime Deadline { get; set; }
	}
}