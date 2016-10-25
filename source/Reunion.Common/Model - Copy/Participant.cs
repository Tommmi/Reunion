using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reunion.Common.Model
{
	public class Participant : Player
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="reunion"></param>
		/// <param name="isRequired"></param>
		/// <param name="contactPolicy"></param>
		/// <param name="mailAddress">may be null</param>
		/// <param name="name"></param>
		/// <param name="languageIsoCodeOfPlayer"></param>
		public Participant(
			string userId,
			ReunionEntity reunion, 
			bool isRequired, 
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer) : base(reunion, userId)
		{
			Init(
				userId,
				reunion,
				isRequired,
				contactPolicy,
				name,
				mailAddress,
				languageIsoCodeOfPlayer);
		}

		public Participant Init(
			string userId,
			ReunionEntity reunion,
			bool isRequired,
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer)
		{
			Init(reunion, userId);
			IsRequired = isRequired;
			ContactPolicy = contactPolicy;
			Name = name;
			LanguageIsoCodeOfPlayer = languageIsoCodeOfPlayer;
			MailAddress = mailAddress;
			return this;
		}

		public Participant()
		{
		}

		public string MailAddress { get; set; }
		[Required]
		[Column(TypeName = "NVARCHAR(128)")]
		[StringLength(128)] // 1 <= n <= 450
		[Index("Name_Reunion_Idx", Order = 1, IsClustered = false, IsUnique = true)]
		public string Name { get; set; }
		[Required]
		public bool IsRequired { get; set; }
		[Required]
		public ContactPolicyEnum ContactPolicy { get; set; }
		[Required]
		public string LanguageIsoCodeOfPlayer { get; set; }
	}
}