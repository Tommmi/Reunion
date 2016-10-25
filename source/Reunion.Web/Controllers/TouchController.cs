using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Reunion.Common;
using Reunion.Common.Model;

namespace Reunion.Web.Controllers
{
	public class TouchController : ApiController
	{
		private readonly IReunionWebservice _service;

		public TouchController(
			IReunionWebservice service)
		{
			_service = service;
		}

		/// <summary>
		/// get latest touch task
		/// GET "api/Touch"
		/// </summary>
		/// <returns></returns>
		public TouchTask Get()
		{
			return _service.Touch.Get();
		}

		/// <summary>
		/// GET "api/Touch/5"
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public TouchTask Get(int id)
		{
			return _service.Touch.Get(id);
		}

		/// <summary>
		/// PUT "api/Touch"
		/// body: 
		/// </summary>
		/// <param name="timestamp">
		/// timestamp of Touch-task
		/// </param>
		/// <returns></returns>
		public int Put([FromBody]DateTime timestamp)
		{
			return _service.Touch.Put(timestamp);
		}

		/// <summary>
		/// POST "api/Touch"
		/// </summary>
		/// <returns></returns>
		public int Post()
		{
			return _service.Touch.Post();
		}

		/// <summary>
		/// DELETE api/Touch/5
		/// </summary>
		/// <param name="id"></param>
		public void Delete(int id)
		{
			_service.Touch.Delete(id);
		}
	}
}