namespace Reunion.Common.Email
{
	public interface IEmailSender
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="emailBody"></param>
		/// <param name="receipients">
		/// email addresses
		/// </param>
		void SendEmail(string subject, string emailBody, params string[] receipients);
	}
}
