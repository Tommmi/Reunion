using System;
using System.Net.Mail;
using TUtils.Common.Logging;

namespace Reunion.Common.Email
{
	public class SmptEmailSender : IEmailSender
	{
		#region fields

		private readonly ITLog _logger;
		private readonly MailAddress _fromAddress;
		private readonly MailAddress _replyToEmailAddress;
		private readonly string _emailAccountUserName;
		private readonly string _emailAccountPassword;
		private readonly bool _useStarttls;
		private readonly bool _isBodyHtml;
		private readonly string _mailProviderHost;
		private readonly int _mailProviderPort;

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="fromAddress">
		/// it must be the email address of the email account which is being used for sending this mail.
		/// </param>
		/// <param name="replyToEmailAddress"></param>
		/// <param name="emailAccountUserName">
		/// login user name of the smtp account
		/// </param>
		/// <param name="emailAccountPassword">
		/// login password of the smtp account
		/// </param>
		/// <param name="useStarttls">
		/// true if email account / provider needs STARTTLS for sending email
		/// </param>
		/// <param name="isBodyHtml">
		/// true, if email body will be HTML 
		/// </param>
		/// <param name="mailProviderHost">
		/// host of the email provider. E.g. smtp.1und1.de
		/// </param>
		/// <param name="mailProviderPort">
		/// Port of the smptp server, Default: 587 (STARTTLS)
		/// 
		/// </param>
		public SmptEmailSender(
			ITLog logger,
			MailAddress fromAddress,
			MailAddress replyToEmailAddress,
			string emailAccountUserName,
			string emailAccountPassword,
			bool useStarttls,
			bool isBodyHtml,
			string mailProviderHost,
			int mailProviderPort = 587)
		{
			_logger = logger;
			_fromAddress = fromAddress;
			_replyToEmailAddress = replyToEmailAddress;
			_emailAccountUserName = emailAccountUserName;
			_emailAccountPassword = emailAccountPassword;
			_useStarttls = useStarttls;
			_isBodyHtml = isBodyHtml;
			_mailProviderHost = mailProviderHost;
			_mailProviderPort = mailProviderPort;
		}

		#endregion

		#region IEmailSender

		void IEmailSender.SendEmail(string subject, string emailBody, params string[] receipients)
		{
			SmtpClient smtpClient = new SmtpClient();
			MailMessage message = new MailMessage();

			try
			{
				smtpClient.Host = _mailProviderHost;
				smtpClient.Port = _mailProviderPort;

				System.Net.NetworkCredential myCred;
				myCred = new System.Net.NetworkCredential(_emailAccountUserName, _emailAccountPassword);
				smtpClient.Credentials = myCred;
				smtpClient.EnableSsl = _useStarttls;

				message.From = _fromAddress;
				foreach (var receipient in receipients)
				{
					message.To.Add(receipient);
				}

				message.Subject = subject;
				message.IsBodyHtml = _isBodyHtml;
				message.Body = emailBody;
				message.ReplyToList.Add(_replyToEmailAddress);

				smtpClient.Send(message);
			}
			catch (Exception e)
			{
				_logger.LogException(e);
			}
		}

		#endregion
	}
}