namespace Reunion.Common
{
	/// <summary>
	/// Data for a mail
	/// </summary>
	public class InvitationMailContent
	{
		/// <summary>
		/// subject of the mail
		/// </summary>
		public string Subject { get; set; }
		/// <summary>
		/// body of the mail - may be HTML too
		/// </summary>
		public string Body { get; set; }
		/// <summary>
		/// mail addresses of receipients
		/// </summary>
		public string[] ReceipientMailAddresses { get; set; }

		public InvitationMailContent(
			string subject,
			string body,
			string[] receipientMailAddresses)
		{
			Subject = subject;
			Body = body;
			ReceipientMailAddresses = receipientMailAddresses;
		}
	}
}