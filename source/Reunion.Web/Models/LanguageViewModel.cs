using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Reunion.Web.Resources;

namespace Reunion.Web.Models
{
	public class LanguageViewModel
	{
		/// <summary>
		/// two letter ISO Code
		/// </summary>
		public string IsoCode { get; set; }
		public string Displayname { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="isoCode">two letter ISO Code</param>
		/// <param name="displayname"></param>
		public LanguageViewModel(string isoCode, string displayname)
		{
			IsoCode = isoCode;
			Displayname = displayname;
		}
	}
}