using System.Web;
using System.Web.Optimization;

namespace Reunion.Web
{
	public class BundleConfig
	{
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles)
		{
			//bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
			//			"~/Scripts/jquery-{version}.js"));

			//bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
			//			"~/Scripts/jquery.validate*"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
						"~/Scripts/modernizr-*"));

			//bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
			//		  "~/Scripts/bootstrap.min.js",
			//		  "~/Scripts/respond.js"));

			//bundles.Add(new ScriptBundle("~/bundles/saltarelle").Include(
			//		  "~/Scripts/mscorlib.min.js",
			//		  "~/Scripts/linq.min.js",
			//		  "~/Scripts/saltarelle.angularjs.js",
			//		  "~/Scripts/saltarelle.utils.min.js"));

			//bundles.Add(new ScriptBundle("~/bundles/sitescript").Include(
			//		  "~/Scripts/reunion.scripts.js"));

#if NOT_MINIMIZED
			bundles.Add(new ScriptBundle("~/bundles/calendar").Include(
				"~/Scripts/jquery.mousewheel.js",
				"~/Scripts/multiselectioncalendarresource.js",
				"~/Scripts/multiselectioncalendarresource.es.js",
				"~/Scripts/multiselectioncalendarresource.en.js",
				"~/Scripts/multiselectioncalendar.js"));

			bundles.Add(new ScriptBundle("~/bundles/sitescript").Include(
					  //"~/Scripts/jquery-{version}.js",
					  //"~/Scripts/jquery-ui.js",
					  "~/Scripts/datepicker-de.js",
					  "~/Scripts/datepicker-es.js",
					  "~/Scripts/jquery-validate.js",
					  //"~/Scripts/bootstrap.js",
					  "~/Scripts/respond.js",
					  //"~/Scripts/angular.js",
					  "~/Scripts/angular-cookies.js",
					  "~/Scripts/mscorlib.js",
					  "~/Scripts/linq.js",
					  "~/Scripts/saltarelle.angularjs.js",
					  "~/Scripts/saltarelle.utils.js",
					  "~/Scripts/reunion.scripts.js"));
#else
			bundles.Add(new ScriptBundle("~/bundles/calendar").Include(
				"~/Scripts/jquery.mousewheel.min.js",
				"~/Scripts/multiselectioncalendarresource.min.js",
				"~/Scripts/multiselectioncalendarresource.es.min.js",
				"~/Scripts/multiselectioncalendarresource.en.min.js",
				"~/Scripts/multiselectioncalendar.min.js"));

			bundles.Add(new ScriptBundle("~/bundles/sitescript").Include(
				//"~/Scripts/jquery-{version}.min.js",
				//"~/Scripts/jquery-ui.min.js",
				"~/Scripts/datepicker-de.js",
				"~/Scripts/datepicker-es.js",
				"~/Scripts/jquery-validate.min.js",
				//"~/Scripts/bootstrap.min.js",
				"~/Scripts/respond.js",
				//"~/Scripts/angular.min.js",
				"~/Scripts/angular-cookies.min.js",
				"~/Scripts/mscorlib.min.js",
				"~/Scripts/linq.min.js",
				"~/Scripts/saltarelle.angularjs.js",
				"~/Scripts/saltarelle.utils.min.js",
				"~/Scripts/reunion.scripts.js"));
#endif





			bundles.Add(new StyleBundle("~/Content/css").Include(
					  //"~/Content/bootstrap.css",
					  //"~/Content/jquery-ui.css",
					  "~/Content/multiselectioncalendar.css",
					  "~/Content/Reunion.css"));
		}
	}
}
