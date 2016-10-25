namespace Reunion.Common
{
	/// <summary>
	/// a registered user
	/// </summary>
	public interface IReunionUser
	{
		string GetEmail();
		/// <summary>
		/// user id 
		/// </summary>
		/// <returns></returns>
		string GetId();
		/// <summary>
		/// user name
		/// </summary>
		/// <returns></returns>
		string GetName();
	}
}