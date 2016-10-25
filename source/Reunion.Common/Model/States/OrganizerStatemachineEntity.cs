using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reunion.Common.Model.States
{
	/// <summary>
	/// current state of the OrganizerStatemachine in given reunion
	/// </summary>
	public class OrganizerStatemachineEntity : StatemachineContext
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="organizer">
		/// the organizer of the reunion
		/// </param>
		/// <param name="isTerminated">
		/// if OrganizerStatemachine is in terminated state and needn't to be checked on planning deadline any more
		/// </param>
		/// <param name="deadline">
		/// deadline of reunion planning
		/// </param>
		public OrganizerStatemachineEntity(
			ReunionEntity reunion,
			Organizer organizer,
			bool isTerminated,
			DateTime deadline)
			: base(
				  currentState:(int)OrganizatorStatusEnum.Created, 
				  player: organizer, 
				  reunion:reunion, 
				  elapseDate: deadline, 
				  isTerminated: isTerminated,
				  statemachineTypeId:StatemachineIdEnum.OrganizerStatemachine)
		{
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public OrganizerStatemachineEntity()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="organizer">
		/// the organizer of the reunion
		/// </param>
		/// <param name="isTerminated">
		/// if OrganizerStatemachine is in terminated state and needn't to be checked on planning deadline any more
		/// </param>
		/// <param name="deadline">
		/// </param>
		/// <returns>this</returns>
		public OrganizerStatemachineEntity Init(
			ReunionEntity reunion,
			Organizer organizer,
			bool isTerminated,
			DateTime deadline)
		{
			base.Init(currentState: (int)OrganizatorStatusEnum.Created,
				  player: organizer,
				  reunion: reunion,
				  elapseDate: deadline,
				  isTerminated: isTerminated,
				  statemachineTypeId: StatemachineIdEnum.OrganizerStatemachine);
			return this;
		}


		public new OrganizatorStatusEnum CurrentState
	    {
		    get { return (OrganizatorStatusEnum) base.CurrentState; }
		    set { base.CurrentState = (int)value; }
	    }

	}
}
