using System.Web.Http;

public class WebApiConfig
{
	public static void Register(HttpConfiguration configuration)
	{
		// Web API routes
		configuration.MapHttpAttributeRoutes();

		configuration.Routes.MapHttpRoute(
            name: "DefaultApi",
			routeTemplate: "api/{controller}/{id}",
			defaults: new { id = RouteParameter.Optional });


	}
}