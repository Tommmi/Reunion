using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Reunion.Web.Common;

namespace Reunion.Web.Models
{
	public class LayoutViewModel
	{
		public LayoutViewModel(
			string title
			)
		{
			Title = title;
		}

		public string Title { get; set; }
	}
}