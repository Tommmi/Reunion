using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Reunion.Web.Common;
using TUtils.Common.Extensions;
using TUtils.Common.MVC;

namespace Reunion.Web
{
    public class MvcApplication : HttpApplication
    {
	    public void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
			UnityConfig.RegisterComponents();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
			DefaultModelBinder.ResourceClassKey = "MVCDefaultStrings";

			var appSettings = new AppSettings() as IAppSettings;
	        if (appSettings.MailAccount_MailAddress.IsNullOrEmpty())
	        {
		        throw new ApplicationException("please fill keys MailAccount_* in web.config !");
	        }
		}

	    protected void Application_AcquireRequestState(object sender, EventArgs args)
		{
			var language = HttpContext.Current.Request.Url.GetQueryParameter("language");
			CultureInfo culture;

			if (language == null)
			{
				language = HttpContext.Current.Request.Cookies["language"]?.Value;

				if (language == null)
				{
					language = GetBrowserLanguage();
				}
			}
			else
			{
				language = language.RemoveWhitespaces().ToLowerInvariant();
			}

			culture = new CultureInfo(language);
			HttpContext.Current.Response.Cookies.Set(new HttpCookie(name: "language", value: culture.TwoLetterISOLanguageName));
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
			CultureInfo.CurrentCulture = culture;
		}

	    private static string GetBrowserLanguage()
	    {
		    var supportedLanguages2LetterISOCodes = AppSettings.SupportedTwoLetterLanguageIsoCodes;


		    try
		    {
				return HttpContext.Current.Request.UserLanguages.FirstOrDefault(lang =>
			    {
				    var userCulture = new CultureInfo(lang).TwoLetterISOLanguageName;
				    return supportedLanguages2LetterISOCodes.Contains(userCulture);
			    }) ?? "en";
				
		    }
		    catch
		    {
			    return "en";
		    }
	    }
    }
}
