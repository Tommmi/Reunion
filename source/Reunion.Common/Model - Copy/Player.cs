using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reunion.Common.Model
{
	public abstract class Player
	{
		protected Player()
		{
		}

		protected Player(ReunionEntity reunion, string userId)
		{
			Init(reunion, userId);
		}

		public int Id { get; set; }

		protected void Init(ReunionEntity reunion, string userId)
		{
			Reunion = reunion;
			UserId = userId;
		}

		[Required]
		[Column(TypeName = "NVARCHAR(128)")]
		[StringLength(128)] // 1 <= n <= 450
		[Index("UserId_Reunion_Idx", Order=1, IsClustered = false,IsUnique = true)]
		public string UserId { get; set; }

		[Required]
		[ForeignKey(nameof(Reunion))]
		[Index("UserId_Reunion_Idx", Order = 2, IsClustered = false, IsUnique = true)]
		[Index("Name_Reunion_Idx", Order = 2, IsClustered = false, IsUnique = true)]
		public int ReunionId { get; set; }

		[Required]
		public ReunionEntity Reunion { get; set; }
	}
}