using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Reunion.Web.Startup))]
namespace Reunion.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
