using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Reunion.Common.Model.States;

namespace Reunion.Common.Model
{
	/// <summary>
	/// entity for player in Reunion.
	/// A player is associated exactly to one reunion.
	/// A reunion may have many players.
	/// </summary>
	public abstract class Player
	{
		protected Player()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reunion"></param>
		/// <param name="isRequired">
		/// true, if player is a participant and required for the meeting
		/// </param>
		/// <param name="contactPolicy">
		/// in case of participant: how may Reunion contact the player ?
		/// </param>
		/// <param name="name">
		/// name of player
		/// </param>
		/// <param name="mailAddress">
		/// email of player
		/// </param>
		/// <param name="languageIsoCodeOfPlayer">
		/// in case of participant: two caharcter language iso code (eg.:"de")
		/// </param>
		protected Player(
			int id,
			ReunionEntity reunion,
			bool isRequired,
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer)
		{
			Init(
				id:id,
				reunion: reunion,
				isRequired: isRequired,
				contactPolicy: contactPolicy,
				name: name,
				mailAddress: mailAddress,
				languageIsoCodeOfPlayer: languageIsoCodeOfPlayer);
		}

		public int Id { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reunion"></param>
		/// <param name="isRequired">
		/// true, if player is a participant and required for the meeting
		/// </param>
		/// <param name="contactPolicy">
		/// in case of participant: how may Reunion contact the player ?
		/// </param>
		/// <param name="name">
		/// name of player
		/// </param>
		/// <param name="mailAddress">
		/// email of player
		/// </param>
		/// <param name="languageIsoCodeOfPlayer">
		/// in case of participant: two character language iso code (eg.:"de")
		/// </param>
		protected void Init(
			int id,
			ReunionEntity reunion,
			bool isRequired,
			ContactPolicyEnum contactPolicy,
			string name,
			string mailAddress,
			string languageIsoCodeOfPlayer)
		{
			Id = id;
			Reunion = reunion;
			IsRequired = isRequired;
			ContactPolicy = contactPolicy;
			Name = name;
			LanguageIsoCodeOfPlayer = languageIsoCodeOfPlayer;
			MailAddress = mailAddress;
		}

		/// <summary>
		/// 
		/// </summary>
		[Required]
		public virtual ReunionEntity Reunion { get; set; }
		/// <summary>
		/// email of player
		/// </summary>
		public string MailAddress { get; set; }
		/// <summary>
		/// name of player
		/// </summary>
		[Required]
		public string Name { get; set; }
		/// <summary>
		/// true, if player is a participant and required for the meeting
		/// </summary>
		[Required]
		public bool IsRequired { get; set; }
		/// <summary>
		/// in case of participant: how may Reunion contact the player ?
		/// </summary>
		[Required]
		public ContactPolicyEnum ContactPolicy { get; set; }
		/// <summary>
		/// in case of participant: two character language iso code (eg.:"de")
		/// </summary>
		[Required]
		public string LanguageIsoCodeOfPlayer { get; set; }
	}
}