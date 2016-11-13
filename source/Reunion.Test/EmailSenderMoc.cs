using System;
using System.Collections.Generic;
using System.Linq;
using Reunion.Common.Email;

namespace Reunion.Test
{
	public class EmailSenderMoc : IEmailSender
	{
		public class Mail
		{
			public string Subject { get; private set; }
			public string EmailBody { get; private set; }
			public IList<string> Receipients { get; private set; }

			public Mail(string subject, string emailBody, params string[] receipients)
			{
				Subject = subject;
				EmailBody = emailBody;
				Receipients = receipients;
			}

			/// <summary>
			/// gets a substring from "text".
			/// Assumes that "text" has following format:
			/// name={method name}[,0={value1}[,1={value2}[..]]][cultureInfo={two letter iso code}]
			/// {method name}: method of interface IBlResource
			/// {value1}: a string format parameter specified in referenced method 
			/// {two letter iso code}: requested language version of string
			/// example: "name=GetKnockMailSubject,0=reunion name,cultureInfo=de"
			/// example: "name=GetKnockMailBody,0=reunion name,1=statusPage/9718934189894513,cultureInfo=de"
			/// </summary>
			/// <param name="text"></param>
			/// <param name="key"></param>
			/// <returns></returns>
			public string GetMailPart(string text, string key)
			{
				return text.Split(',').First(p => p.StartsWith(key)).Substring(key.Length + 1);
			}
		}

		public IList<Mail> SentMails = new List<Mail>();

		public EmailSenderMoc()
		{
			
		}
		void IEmailSender.SendEmail(string subject, string emailBody, params string[] receipients)
		{
			SentMails.Add(new Mail(subject, emailBody, receipients));
		}
	}
}