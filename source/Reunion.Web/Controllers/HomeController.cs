using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Reunion.Web.Models;

namespace Reunion.Web.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}

		public ActionResult ManageExistingReunion()
		{
			return RedirectToAction(
				actionName: "Manage",
				controllerName: "Reunion");
		}

		public ActionResult CreateNewReunion()
		{
			return RedirectToAction(
				actionName: "Create",
				controllerName: "Reunion");
		}

		public ActionResult Vision()
		{
			return View();
		}
	}
}