
using System.Html;
using AngularJS;
using jQueryApi;
using Saltarelle.Utils;

namespace Reunion.Scripts
{
	class Program
	{
		static void Main()
		{
			var module = new Module("reunionApp", "ngCookies");
			module.Controller<ParticipantController>();
			module.Controller<CalendarController>();
		}
	}
}
