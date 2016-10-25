using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reunion.Common.Model
{
	/// <summary>
	/// Entity which represents a friend, who is invitated to the reunion.
	/// A participant isn't associated with a registered user !
	/// Each participant has a unguessable id, which acts as authentification.
	/// </summary>
	public class Participant : Player
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reunion"></param>
		/// <param name="isRequired">
		/// true, if participant is absolutely required for the reunion
		/// </param>
		/// <param name="contactPolicy">
		/// how may Reunion contact the participant ? 
		/// </param>
		/// <param name="mailAddress">
		/// May not be null. Must be unique among email addresses of participants in a reunion
		/// </param>
		/// <param name="name">name of the participant</param>
		/// <param name="languageIsoCodeOfPlayer">two character language iso code (eg.:"de")</param>
		/// <param name="unguessableId">
		/// unguessable id, which acts as authentification and identification of the participant
		/// </param>
		public Participant(
			int id,
			ReunionEntity reunion, 
			bool isRequired, 
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer,
			string unguessableId) : base(
				id: id,
				reunion: reunion,
				isRequired: isRequired,
				contactPolicy: contactPolicy,
				name: name,
				mailAddress: mailAddress,
				languageIsoCodeOfPlayer: languageIsoCodeOfPlayer)
		{
			UnguessableId = unguessableId;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reunion"></param>
		/// <param name="isRequired">
		/// true, if participant is absolutely required for the reunion
		/// </param>
		/// <param name="contactPolicy">
		/// how may Reunion contact the participant ? 
		/// </param>
		/// <param name="mailAddress">
		/// May not be null. Must be unique among email addresses of participants in a reunion
		/// </param>
		/// <param name="name">name of the participant</param>
		/// <param name="languageIsoCodeOfPlayer">two character language iso code (eg.:"de")</param>
		/// <param name="unguessableId">
		/// unguessable id, which acts as authentification and identification of the participant
		/// </param>
		public Participant Init(
			int id,
			ReunionEntity reunion,
			bool isRequired,
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer,
			string unguessableId)
		{
			base.Init(
				id:id,
				reunion: reunion,
				isRequired: isRequired,
				contactPolicy: contactPolicy,
				name: name,
				mailAddress: mailAddress,
				languageIsoCodeOfPlayer: languageIsoCodeOfPlayer);
			UnguessableId = unguessableId;
			return this;
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public Participant()
		{
		}

		/// <summary>
		/// unguessable id, which acts as authentification and identification of the participant
		/// </summary>
		[Index("IX_Participant_Secret", IsClustered = false, IsUnique = false)]
		[Column(TypeName = "VARCHAR")]
		[StringLength(maximumLength: 60)] // 1 <= n <= 450
		public string UnguessableId { get; set; }

	}
}