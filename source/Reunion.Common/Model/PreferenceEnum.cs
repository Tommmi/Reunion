namespace Reunion.Common.Model
{
	public enum PreferenceEnum
	{
		/// <summary>
		/// no selection at all - no meaning - invalid
		/// </summary>
		None = 0,
		/// <summary>
		/// player won't come on that day
		/// </summary>
		NoWay = 1,
		/// <summary>
		/// perfect day
		/// </summary>
		PerfectDay = 2,
		/// <summary>
		/// I don't know if I'll come
		/// </summary>
		MayBe = 4,
		/// <summary>
		/// yes, I have no plans on that day till now
		/// </summary>
		Yes = 8,
	}
}