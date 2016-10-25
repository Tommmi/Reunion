namespace Reunion.Common.Model
{
	public class Organizer : Participant
	{
		public Organizer(
			string userId,
			ReunionEntity reunion, 
			string languageIsoCodeOfPlayer,
			string name,
			string mailAddress) 
			: base(
				  userId: userId,
				  reunion: reunion, 
				  isRequired: true,
				  contactPolicy:ContactPolicyEnum.MayContactByWebservice, 
				  languageIsoCodeOfPlayer:languageIsoCodeOfPlayer,
				  name: name,
				  mailAddress: mailAddress)
		{
		}

		public Organizer() : base()
		{
		}
	}
}