using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reunion.Common.Model
{
	/// <summary>
	/// Entity which represents the organizer of the reunion.
	/// A reunion has exactly one organizer.
	/// The organizer is associated with exactly one reunion and belongs to
	/// exactly one registered user of the website.
	/// On the other side a registered user may manange different reunions
	/// and for this is associated with different organizers.
	/// </summary>
	public class Organizer : Player
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">
		/// the id of the organizer
		/// </param>
		/// <param name="userId">
		/// The id of the registered user
		/// </param>
		/// <param name="reunion"></param>
		/// <param name="languageIsoCodeOfPlayer">
		/// in case of participant: two character language iso code (eg.:"de")
		/// </param>
		/// <param name="name">name of organizer</param>
		/// <param name="mailAddress">email address of organizer</param>
		public Organizer(
			int id,
			string userId,
			ReunionEntity reunion, 
			string languageIsoCodeOfPlayer,
			string name,
			string mailAddress) 
			: base(
				id:id,
				reunion: reunion,
				isRequired: true,
				contactPolicy: ContactPolicyEnum.MayContactByWebservice,
				name: name,
				mailAddress: mailAddress,
				languageIsoCodeOfPlayer: languageIsoCodeOfPlayer)
		{
			UserId = userId;
		}

		public Organizer() : base()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">
		/// the id of the organizer
		/// </param>
		/// <param name="userId">
		/// The id of the registered user
		/// </param>
		/// <param name="reunion"></param>
		/// <param name="languageIsoCodeOfPlayer">
		/// in case of participant: two character language iso code (eg.:"de")
		/// </param>
		/// <param name="name">name of organizer</param>
		/// <param name="mailAddress">email address of organizer</param>
		/// <returns>this</returns>
		public Organizer Init(
			int id,
			string userId,
			ReunionEntity reunion,
			string languageIsoCodeOfPlayer,
			string name,
			string mailAddress)
		{
			base.Init(
				id: id,
				reunion: reunion,
				isRequired: true,
				contactPolicy: ContactPolicyEnum.MayContactByWebservice,
				name: name,
				mailAddress: mailAddress,
				languageIsoCodeOfPlayer: languageIsoCodeOfPlayer);
			UserId = userId;
			return this;
		}


		/// <summary>
		/// the id of the registered user.
		/// A registered user may manange different reunions
		/// and for this is associated with different organizers.
		/// </summary>
		[Required]
		[Index("IX_UserId",IsClustered = false,IsUnique = false)]
		[Column(TypeName = "VARCHAR")]
		[StringLength(maximumLength:40)] // 1 <= n <= 450
		public string UserId { get; set; }
	}
}