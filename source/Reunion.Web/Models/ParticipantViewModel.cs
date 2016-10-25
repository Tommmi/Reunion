using Reunion.Common.Model;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBeProtected.Global

namespace Reunion.Web.Models
{
	public class ParticipantViewModel
	{
		/// <summary>
		/// may be 0 if it's new
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public int id;
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public string name;
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public string mail;
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public bool isRequired;
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public ContactPolicyEnum contactPolicy;
		/// <summary>
		/// Two letter ISO code
		/// </summary>
		/// <remarks>
		/// lower case - used as JavaScript, too
		/// </remarks>
		public string playerLanguageIsoCode;

		public ParticipantViewModel()
		{
			
		}

		public ParticipantViewModel(
			int id,
			string name,
			string mail,
			bool isRequired,
			ContactPolicyEnum contactPolicy,
			string playerLanguageIsoCode)
		{
			this.id = id;
			this.name = name;
			this.mail = mail;
			this.isRequired = isRequired;
			this.contactPolicy = contactPolicy;
			this.playerLanguageIsoCode = playerLanguageIsoCode;
		}

		public bool IsEqual(ParticipantViewModel other)
		{
			return
				this.mail == other.mail
				&& this.name == other.name
				&& this.playerLanguageIsoCode == other.playerLanguageIsoCode
				&& this.contactPolicy == other.contactPolicy
				&& this.isRequired == other.isRequired;
		}
	}
}