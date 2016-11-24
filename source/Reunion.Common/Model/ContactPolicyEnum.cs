namespace Reunion.Common.Model
{
	/// <summary>
	/// what should system consider when contacting a participant
	/// </summary>
	public enum ContactPolicyEnum
	{
		/// <summary>
		/// the participant may be contacted by webservice itself at any time
		/// </summary>
		MayContactByWebservice = 0,
		/// <summary>
		/// the participant may be contacted by webservice, but not the very first time.
		/// </summary>
		ContactFirstTimePersonally = 1,
	}
}