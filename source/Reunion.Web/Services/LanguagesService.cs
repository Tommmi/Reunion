using System.Collections.Generic;
using Reunion.Web.Common;
using Reunion.Web.Models;

namespace Reunion.Web.Services
{
	public class LanguagesService : ILanguagesService
	{
		IEnumerable<LanguageViewModel> ILanguagesService.GetSupportedLanguages()
		{
			return new[]
			{
				new LanguageViewModel(isoCode: "de", displayname: Resources.Resource1.Language_DE),
				new LanguageViewModel(isoCode: "en", displayname: Resources.Resource1.Language_EN),
			};
		}
	}
}