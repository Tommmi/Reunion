using System;

namespace Reunion.Common.Model.States
{
	/// <summary>
	/// current state of the ParticipantStatemachine in given reunion
	/// </summary>
	public class ParticipantStatemachineEntity : StatemachineContext
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="participant">
		/// the participant of the statemachine
		/// </param>
		/// <param name="isTerminated">
		/// true, if ParticipantStatemachine is in terminated state and needn't to be checked on elapse time any more
		/// </param>
		/// <param name="elapseDate">
		/// null, if not set
		/// used by some states to check if a given timestamp elapsed
		/// </param>
		public ParticipantStatemachineEntity(
			ReunionEntity reunion,
			Participant participant,
			DateTime? elapseDate,
			bool isTerminated)
			: base(
				  currentState:(int)ParticipantStatusEnum.Created, 
				  player:participant, 
				  reunion:reunion, 
				  elapseDate:elapseDate, 
				  isTerminated: isTerminated,
				  statemachineTypeId:StatemachineIdEnum.ParticipantStatemachine)
		{
		}

		/// <summary>
		/// default constructor
		/// </summary>
		public ParticipantStatemachineEntity()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="participant">
		/// the participant of the statemachine
		/// </param>
		/// <param name="isTerminated">
		/// true, if ParticipantStatemachine is in terminated state and needn't to be checked on elapse time any more
		/// </param>
		/// <param name="elapseDate">
		/// null, if not set
		/// used by some states to check if a given timestamp elapsed
		/// </param>
		/// <returns>this</returns>
		public ParticipantStatemachineEntity Init(
			ReunionEntity reunion,
			Participant participant,
			DateTime? elapseDate,
			bool isTerminated)
		{
			base.Init(currentState: (int)ParticipantStatusEnum.Created,
				  player: participant,
				  reunion: reunion,
				  elapseDate: elapseDate,
				  isTerminated: isTerminated,
				  statemachineTypeId: StatemachineIdEnum.ParticipantStatemachine);
			return this;
		}

		public new ParticipantStatusEnum CurrentState
		{
			get { return (ParticipantStatusEnum)base.CurrentState; }
			set { base.CurrentState = (int)value; }
		}
	}
}