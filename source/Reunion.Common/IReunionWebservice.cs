using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reunion.Common.Model;

namespace Reunion.Common
{
	/// <summary>
	/// all REST funktions for resource Touch.
	/// Touch is the function complex, which reawakes all statemachines for checking elapsed times and so on.
	/// </summary>
	public interface ITouch
	{
		/// <summary>
		/// gets latest touch task
		/// </summary>
		/// <returns></returns>
		TouchTask Get();
		/// <summary>
		/// gets concrete touch task 
		/// </summary>
		/// <param name="id">
		/// id of touch task resource
		/// </param>
		/// <returns></returns>
		TouchTask Get(int id);
		/// <summary>
		/// Upserts touch task. This method is idempotent.
		/// If touch task was found by creationTimestamp,
		/// the existing entity will be updated, other wise it will create a new touch task.
		/// </summary>
		/// <param name="creationTimestamp"></param>
		/// <returns>id of the upserted touch task</returns>
		int Put(DateTime creationTimestamp);
		/// <summary>
		/// creates and inserts a new touch task.
		/// </summary>
		/// <returns>id of the created touch task</returns>
		int Post();
		/// <summary>
		/// delets the touch task with the passed id.
		/// This method is idempotent.
		/// This method deletes the task-resource for the touch process only.
		/// It won't rollback any actions resulting from a former touch task.
		/// </summary>
		/// <param name="id"></param>
		void Delete(int id);
	}

	/// <summary>
	/// all REST functions, grouped by resources
	/// </summary>
	public interface IReunionWebservice
	{
		/// <summary>
		/// all REST functions for resource Touch.
		/// Touch is the function complex, which reawakes all statemachines for checking elapsed times and so on.
		/// </summary>
		ITouch Touch { get; }
	}
}
