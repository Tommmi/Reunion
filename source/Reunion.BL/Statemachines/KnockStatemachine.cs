using System.Collections.Generic;
using Reunion.Common;
using Reunion.Common.Model.States;

namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// The KnockStatemachine. See documentation. 
	/// Calls IReunionBL.SendKnockMail2Organizer(), when statemachine gets signal "SignalKnock".
	/// But: if organizer hasn't react on last notification mail, the statemachine won't call SendKnockMail2Organizer() again.
	/// </summary>
	public class KnockStatemachine : StateMachine<KnockStatemachine, KnockStatemachineEntity>
	{
		#region states

		/// <summary>
		/// state Idle: organizer is waiting for notification
		/// </summary>
		public class Idle : State
		{
			public Idle() : base((int)KnockStatusEnum.Idle, isTerminatedState:false)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if ( signal is SignalKnock)
					SetNewState(Statemachine.StateWaitForLogin);
			}
		}
		/// <summary>
		/// state Idle: organizer is waiting for notification
		/// </summary>
		public Idle StateIdle { get; private set; } = new Idle();

		/// <summary>
		/// state WaitForLogin: organizer hasn't react on last notification mail, yet.
		/// </summary>
		public class WaitForLogin : State
		{
			public WaitForLogin() : base((int)KnockStatusEnum.WaitForLogin, isTerminatedState: false)
			{
			}

			protected override void OnEntered(State<KnockStatemachine, KnockStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.SendKnockMail2Organizer(ReunionId);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalLoggedin)
					SetNewState(Statemachine.StateIdle);
			}
		}

		/// <summary>
		/// state WaitForLogin: organizer hasn't react on last notification mail, yet.
		/// </summary>
		public WaitForLogin StateWaitForLogin { get; private set; } = new WaitForLogin();

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="statemachineEntity">
		/// current persisted context of statemachine
		/// </param>
		/// <param name="dal">data access layer</param>
		/// <param name="bl"></param>
		public KnockStatemachine(
			KnockStatemachineEntity statemachineEntity,
			IReunionDal dal,
			IReunionStatemachineBL bl)
			: base(statemachineEntity, dal, bl)
		{
			Init();
		}

		protected override IEnumerable<State> InitAllStates()
		{
			return new State[]
			{
				StateIdle,
				StateWaitForLogin,
			};
		}

		#endregion

		#region signals

		/// <summary>
		/// sent when organizer should be notified
		/// </summary>
		public class SignalKnock : Signal
		{
		}

		/// <summary>
		/// sent when registered user of organizer has logged-in 
		/// </summary>
		public class SignalLoggedin : Signal
		{
		}


		#endregion
	}
}