using System.Collections.Generic;
using System.Linq;
using Reunion.Common;
using Reunion.Common.Model.States;
using TUtils.Common.Extensions;

namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// base class of all reunion statemachines
	/// <example><code><![CDATA[
	/// public class MyStatemachine : StateMachine<MyStatemachine, MyStatemachineContext>
	/// {
	///		public MyStatemachine(
	/// 		MyStatemachineContext statemachineEntity,
	/// 		IReunionDal dal,
	/// 		IReunionStatemachineBL bl)
	/// 		: base(statemachineEntity, dal, bl)
	/// 	{
	///			// Must be called here !!!
	/// 		Init();
	/// 	}
	/// 
	///		protected override IEnumerable<State> InitAllStates()
	/// 	{
	/// 		return new State[]
	/// 		{
	/// 			MyState1,
	/// 			MyState2,...
	/// 		};
	/// 	}
	/// }
	/// ]]></code></example>
	/// </summary>
	/// <typeparam name="TStatemachine"></typeparam>
	/// <typeparam name="TStateMachineEntity"></typeparam>
	public abstract class StateMachine<TStatemachine, TStateMachineEntity> : IStateMachine 
		where TStateMachineEntity : StatemachineContext
		where TStatemachine : StateMachine<TStatemachine, TStateMachineEntity>
	{
		#region types

		/// <summary>
		///  base class of all states in this statemachine
		/// </summary>
		public abstract class State : State<TStatemachine, TStateMachineEntity>
		{
			public State(int currentState, bool isTerminatedState) : base(currentState, isTerminatedState)
			{
			}

			public int ReunionId => Statemachine.ReunionId;
		}

		/// <summary>
		/// base class of all signals in this statemachine
		/// </summary>
		public abstract class Signal
		{

		}

		#endregion

		#region fields

		private readonly IReunionDal _dal;
		private readonly IReunionStatemachineBL _bl;
		private IEnumerable<State> _allStates;

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stateMachineEntity"></param>
		/// <param name="dal"></param>
		/// <param name="bl"></param>
		protected StateMachine(
			TStateMachineEntity stateMachineEntity,
			IReunionDal dal,
			IReunionStatemachineBL bl)
		{
			_dal = dal;
			_bl = bl;
			StateMachineEntity = stateMachineEntity;
		}

		#endregion

		#region protected

		/// <summary>
		/// returns enumeration of all state instances !
		/// </summary>
		/// <returns></returns>
		protected abstract IEnumerable<State> InitAllStates();

		/// <summary>
		/// must be called in derived constructor !!!!!!!!!!!!!!!
		/// </summary>
		protected void Init()
		{
			_allStates = InitAllStates();
			// ReSharper disable once PossibleMultipleEnumeration
			_allStates.ForEach(s =>
			{
				s.Statemachine = (TStatemachine) this;
				s.Bl = _bl;
			});
			var currentState = StateMachineEntity.CurrentState;
			// ReSharper disable once PossibleMultipleEnumeration
			var initialState = _allStates.First(s => s.CurrentState == currentState);
			CurrentState = initialState;
		}

		#endregion

		#region public

		/// <summary>
		/// persisted statemachine context
		/// </summary>
		public TStateMachineEntity StateMachineEntity { get; set; }
		/// <summary>
		/// current state
		/// </summary>
		public State CurrentState { get; set; }

		/// <summary>
		/// Triggers stetmachine and calls IReunionDal.UpdateState() 
		/// </summary>
		/// <param name="signal"></param>
		public void Trigger(Signal signal)
		{
			CurrentState.OnSignal(signal);
			_dal.UpdateState(StateMachineEntity);
		}

		#region IStateMachine

		/// <summary>
		/// id of associated reunion
		/// </summary>
		public int ReunionId => StateMachineEntity.ReunionId;

		/// <summary>
		/// This method must be called time by time to ensure that actions take place when some time markers elapsed.
		/// It is implemented by statemachines and states individually to check (by polling) time dependent events.
		/// Note !!!!!!!!!!!!!!
		/// This method persists changes to the statemachine's context
		/// </summary>
		public void Touch()
		{
			CurrentState.Touch();
			_dal.UpdateState(StateMachineEntity);
		}

		#endregion

		#endregion
	}
}