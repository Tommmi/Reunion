using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reunion.Common.Model
{
	/// <summary>
	/// Entity which represents a single REST-task: 
	/// TouchTask is a task which should ping all active statemachines in the system,
	/// so that Reunion can notifiy organizers and participants.
	/// When the task is executed, property "Executed" will be set.
	/// </summary>
	public class TouchTask
	{
		public int Id { get; set; }
		/// <summary>
		/// when task has been created
		/// </summary>
		public DateTime CreationTimestamp { get; set; }
		/// <summary>
		/// true, if 
		/// </summary>
		public bool Executed { get; set; }

		/// <summary>
		/// default constructor
		/// </summary>
		public TouchTask()
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="creationTimestamp">when task has been created</param>
		public TouchTask(
			int id,
			DateTime creationTimestamp)
		{
			Id = id;
			CreationTimestamp = creationTimestamp;
		}
	}
}
