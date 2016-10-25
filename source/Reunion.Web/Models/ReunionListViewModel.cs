using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Reunion.Web.Models
{
	public class ReunionListViewModel
	{
		public ReunionListViewModel(IEnumerable<ReunionVM> reunions)
		{
			Reunions = reunions;
		}

		public class ReunionVM
		{
			public ReunionVM(string name, int reunionId)
			{
				Name = name;
				ReunionId = reunionId;
			}

			public string Name { get; set; }
			public int ReunionId { get; set; }
		}
		public IEnumerable<ReunionVM> Reunions { get; set; }

	}
}