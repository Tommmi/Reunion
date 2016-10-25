using System.Collections.Generic;
using Reunion.Web.Models;

namespace Reunion.Web.Common
{
	public interface ILanguagesService
	{
		IEnumerable<LanguageViewModel> GetSupportedLanguages();
	}
}
