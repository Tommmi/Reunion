using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Reunion.Web.Startup))]
namespace Reunion.Web
{
	/// <summary>
	/// nextgen
	/// </summary>
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
