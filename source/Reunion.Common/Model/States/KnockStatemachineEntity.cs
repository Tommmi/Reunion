namespace Reunion.Common.Model.States
{
	/// <summary>
	/// current state of the KnockStatemachine in given reunion
	/// </summary>
	public class KnockStatemachineEntity : StatemachineContext
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
		/// if KnockStatemachine has terminated state and needn't to be checked on planning deadline any more
		/// </param>
		public KnockStatemachineEntity(
			ReunionEntity reunion,
			Organizer organizer,
			bool isTerminated)
			: base(
				  currentState:(int)KnockStatusEnum.Idle, 
				  player: organizer, 
				  reunion:reunion,
				  elapseDate:null, 
				  isTerminated: isTerminated, 
				  statemachineTypeId:StatemachineIdEnum.KnockStatemachine)
		{
		}

		public KnockStatemachineEntity()
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
		/// if KnockStatemachine has terminated state and needn't to be checked on planning deadline any more
		/// </param>
		/// <returns></returns>
		public KnockStatemachineEntity Init(
			ReunionEntity reunion,
			Organizer organizer,
			bool isTerminated)
		{
			base.Init(currentState: (int)KnockStatusEnum.Idle,
				  player: organizer,
				  reunion: reunion,
				  elapseDate: null,
				  isTerminated: isTerminated,
				  statemachineTypeId: StatemachineIdEnum.KnockStatemachine);
			return this;
		}

		public new KnockStatusEnum CurrentState
		{
			get { return (KnockStatusEnum)base.CurrentState; }
			set { base.CurrentState = (int)value; }
		}

	}
}