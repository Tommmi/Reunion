using Reunion.Common;
using Reunion.Common.Model.States;

namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// base class for reunion states
	/// </summary>
	/// <typeparam name="TStateMachine"></typeparam>
	/// <typeparam name="TStateMachineEntity"></typeparam>
	public abstract class State<TStateMachine, TStateMachineEntity>
		where TStateMachine : StateMachine<TStateMachine, TStateMachineEntity>
		where TStateMachineEntity : StatemachineContext
	{
		/// <summary>
		/// statemachine of state
		/// </summary>
		public TStateMachine Statemachine { protected get; set; }
		/// <summary>
		/// StatemachineContext.CurrentState
		/// </summary>
		public int CurrentState { get; }
		/// <summary>
		/// true, if state needn't react on Touch()
		/// </summary>
		public bool IsTerminatedState { get; }
		/// <summary>
		/// business layer
		/// </summary>
		public IReunionStatemachineBL Bl { protected get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentState"></param>
		/// <param name="isTerminatedState"></param>
		protected State(
			int currentState,
			bool isTerminatedState)
		{
			CurrentState = currentState;
			IsTerminatedState = isTerminatedState;
		}

		/// <summary>
		/// called before system is trying to make a transition into this state.
		/// </summary>
		/// <returns>false, if transition isn't allowed</returns>
		protected virtual bool MayEnter()
		{
			return true;
		}

		/// <summary>
		/// called when statemachine has switched into this state.
		/// </summary>
		/// <param name="oldState">
		/// the former state
		/// </param>
		protected virtual void OnEntered(State<TStateMachine, TStateMachineEntity> oldState)
		{
			if (IsTerminatedState)
				Statemachine.StateMachineEntity.IsTerminated = true;
		}

		/// <summary>
		/// called before this state will be left and a new state is set
		/// </summary>
		protected virtual void OnLeaving()
		{
			if (IsTerminatedState)
				Statemachine.StateMachineEntity.IsTerminated = false;
		}

		/// <summary>
		/// called when a signal arrives the statemachine AND(!!!) this is the current state
		/// </summary>
		/// <param name="signal"></param>
		public virtual void OnSignal(StateMachine<TStateMachine, TStateMachineEntity>.Signal signal)
		{
			
		}

		/// <summary>
		/// stes the passed state as current state
		/// </summary>
		/// <param name="newState"></param>
		protected void SetNewState(StateMachine<TStateMachine, TStateMachineEntity>.State newState)
		{
			if ( !newState.MayEnter())
				return;
			OnLeaving();
			Statemachine.CurrentState = newState;
			Statemachine.StateMachineEntity.IsDirty = true;
			Statemachine.StateMachineEntity.CurrentState = newState.CurrentState;
			newState.OnEntered(this);
		}

		/// <summary>
		/// This method must be called time by time to ensure that actions take place when some time markers elapsed.
		/// It is implemented by statemachines and states individually to check (by polling) time dependent events.
		/// </summary>
		public virtual void Touch()
		{
			
		}
	}
}