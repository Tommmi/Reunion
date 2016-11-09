using System;
using System.Collections.Generic;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using TUtils.Common.Extensions;

namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// statemachine ParticipantStatemachine. see documentation.
	/// Calls
	///		- IReunionBL.WakeOrganizer()
	///		- IReunionBL.SendRejectionMailToParticipant()
	///		- IReunionBL.CheckFinalDateRejected()
	/// </summary>
	public class ParticipantStatemachine : StateMachine<ParticipantStatemachine, ParticipantStatemachineEntity>
	{
		#region states

		public class BaseState : State
		{
			public BaseState(int currentState, bool isTerminated) : base(currentState, isTerminated)
			{
			}

			public override void Touch()
			{
				base.Touch();
				if (DateTime.Now > Statemachine.StateMachineEntity.ElapseDate)
					Statemachine.Trigger(new SignalTimeElapsed());
			}

			protected int ParticipantId => Statemachine.ParticipantId;
		}


		/// <summary>
		/// state Created: participant has been added to reunion, but did not received 
		/// invitation mail yet
		/// </summary>
		public class Created : BaseState
		{
			public Created() : base((int)ParticipantStatusEnum.Created, isTerminated:false)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalPrimerInvitationSent)
					SetNewState(Statemachine.StateWaitOnLoginForInvitation);
			}
		}
		/// <summary>
		/// state Created: participant has been added to reunion, but did not received 
		/// invitation mail yet
		/// </summary>
		public Created StateCreated { get; private set; } = new Created();


		/// <summary>
		/// state WaitOnLoginForInvitation: participant has got primer invitation mail but didn't 
		/// visit the page yet.
		/// </summary>
		public class WaitOnLoginForInvitation : BaseState
		{
			public WaitOnLoginForInvitation() : base((int)ParticipantStatusEnum.WaitOnLoginForInvitation, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Statemachine.StateMachineEntity.ElapseDate = DateTime.Now.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalLoggedIn)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalTimeElapsed)
					SetNewState(Statemachine.StateReactionOnInvitationMissing);
			}
		}
		/// <summary>
		/// state WaitOnLoginForInvitation: participant has got primer invitation mail but didn't 
		/// visit the page yet.
		/// </summary>
		public WaitOnLoginForInvitation StateWaitOnLoginForInvitation { get; private set; } = new WaitOnLoginForInvitation();

		/// <summary>
		/// state ReactionOnInvitationMissing:  participant has got primer invitation mail but didn't 
		/// visit the page for a long time.
		/// </summary>
		public class ReactionOnInvitationMissing : BaseState
		{
			public ReactionOnInvitationMissing() : base((int)ParticipantStatusEnum.ReactionOnInvitationMissing, isTerminated: false)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalLoggedIn)
					SetNewState(Statemachine.StateInvitated);
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
			}
		}
		/// <summary>
		/// state ReactionOnInvitationMissing:  participant has got primer invitation mail but didn't 
		/// visit the page for a long time.
		/// </summary>
		public ReactionOnInvitationMissing StateReactionOnInvitationMissing { get; private set; } = new ReactionOnInvitationMissing();

		/// <summary>
		///  state Invitated: participant has reacted at minimum one time on the primaer invitation mail,
		/// but hasn't git a final invitation mail, yet.
		/// </summary>
		public class Invitated : BaseState
		{
			public Invitated() : base((int)ParticipantStatusEnum.Invitated, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				switch ((ParticipantStatusEnum)oldState.CurrentState)
				{
					case ParticipantStatusEnum.ReactionOnInvitationMissing:
					case ParticipantStatusEnum.WaitOnLoginForInvitation:
					case ParticipantStatusEnum.MissingInformation:
					case ParticipantStatusEnum.ReactionOnFeedbackMissing:
						break;
					case ParticipantStatusEnum.Created:
					case ParticipantStatusEnum.Invitated:
					case ParticipantStatusEnum.FinallyInvitated:
					case ParticipantStatusEnum.ReactionOnFinalInvitationMissing:
					case ParticipantStatusEnum.RejectedInvitation:
					case ParticipantStatusEnum.Accepted:
						Bl.SendRejectionMailToParticipant(reunionId: ReunionId, participantId: ParticipantId);
						break;
					default:
						throw new ApplicationException("651re1qzdq73:" + oldState.CurrentState);
				}
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalInvitation)
					SetNewState(Statemachine.StateFinallyInvitated);
				else if (signal is SignalInformationMissing)
					SetNewState(Statemachine.StateMissingInformation);
			}

			public override void Touch()
			{
				base.Touch();
				if (Statemachine.CurrentState == this)
				{
					if (Bl.MissingDaysOfParticipant(ReunionId, ParticipantId))
						Statemachine.Trigger(new SignalInformationMissing());
				}
			}
		}

		/// <summary>
		///  state Invitated: participant has reacted at minimum one time on the primaer invitation mail,
		/// but hasn't git a final invitation mail, yet.
		/// </summary>
		public Invitated StateInvitated { get; private set; } = new Invitated();

		/// <summary>
		/// state FinallyInvitated: participant has got a final invitation mail, but hasn't reacted on that mail yet.
		/// </summary>
		public class FinallyInvitated : BaseState
		{
			public FinallyInvitated() : base((int) ParticipantStatusEnum.FinallyInvitated, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Statemachine.StateMachineEntity.ElapseDate = DateTime.Now.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalDateRejected)
					SetNewState(Statemachine.StateRejectedInvitation);
				else if (signal is SignalFinalDateAccepted)
					SetNewState(Statemachine.StateAccepted);
				else if (signal is SignalTimeElapsed)
					SetNewState(Statemachine.StateReactionOnFinalInvitationMissing);
				else if (signal is SignalFinalInvitationCanceledByOrganizer)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalDateRangesUpdated)
					Bl.CheckFinalDateRejected(ReunionId, ParticipantId);
			}
		}

		/// <summary>
		/// state FinallyInvitated: participant has got a final invitation mail, but hasn't reacted on that mail yet.
		/// </summary>
		public FinallyInvitated StateFinallyInvitated { get; private set; } = new FinallyInvitated();

		/// <summary>
		/// state ReactionOnFinalInvitationMissing: participant has got a final invitation mail, 
		/// but hasn't reacted on that mail for a long time.
		/// </summary>
		public class ReactionOnFinalInvitationMissing : BaseState
		{
			public ReactionOnFinalInvitationMissing() : base((int) ParticipantStatusEnum.ReactionOnFinalInvitationMissing, isTerminated: false)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalDateRejected)
					SetNewState(Statemachine.StateRejectedInvitation);
				else if (signal is SignalFinalDateAccepted)
					SetNewState(Statemachine.StateAccepted);
				else if (signal is SignalFinalInvitationCanceledByOrganizer)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalDateRangesUpdated)
					Bl.CheckFinalDateRejected(ReunionId, ParticipantId);
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
			}
		}

		/// <summary>
		/// state ReactionOnFinalInvitationMissing: participant has got a final invitation mail, 
		/// but hasn't reacted on that mail for a long time.
		/// </summary>
		public ReactionOnFinalInvitationMissing StateReactionOnFinalInvitationMissing { get; private set; } = new ReactionOnFinalInvitationMissing();

		/// <summary>
		/// state RejectedInvitation: participant has rejected the final invitation.
		/// </summary>
		public class RejectedInvitation : BaseState
		{
			public RejectedInvitation() : base((int) ParticipantStatusEnum.RejectedInvitation, isTerminated: false)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalDateAccepted)
					SetNewState(Statemachine.StateAccepted);
				else if (signal is SignalFinalInvitationCanceledByOrganizer)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalDateRangesUpdated)
					Bl.CheckFinalDateRejected(ReunionId, ParticipantId);
			}
		}

		/// <summary>
		/// state RejectedInvitation: participant has rejected the final invitation.
		/// </summary>
		public RejectedInvitation StateRejectedInvitation { get; private set; } = new RejectedInvitation();

		/// <summary>
		/// state Accepted: participant has accepted final invitation
		/// </summary>
		public class Accepted : BaseState
		{
			public Accepted() : base((int) ParticipantStatusEnum.Accepted, isTerminated: true)
			{
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalDateRejected)
					SetNewState(Statemachine.StateRejectedInvitation);
				else if (signal is SignalFinalInvitationCanceledByOrganizer)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalDateRangesUpdated)
					Bl.CheckFinalDateRejected(ReunionId, ParticipantId);
			}
		}

		/// <summary>
		/// state Accepted: participant has accepted final invitation
		/// </summary>
		public Accepted StateAccepted { get; private set; } = new Accepted();

		/// <summary>
		/// state MissingInformation: The participant has so far not given any information on the most possible days.
		/// </summary>
		public class MissingInformation : BaseState
		{
			public MissingInformation() : base((int) ParticipantStatusEnum.MissingInformation, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.SendMissingDaysNotification(ReunionId, ParticipantId);
				Statemachine.StateMachineEntity.ElapseDate = DateTime.Now.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalInvitation)
					SetNewState(Statemachine.StateFinallyInvitated);
				else if (signal is SignalNoInformationMissing)
					SetNewState(Statemachine.StateInvitated);
				else if (signal is SignalTimeElapsed)
					SetNewState(Statemachine.StateReactionOnFeedbackMissing);
			}

			public override void Touch()
			{
				base.Touch();
				if (Statemachine.CurrentState == this)
				{
					if (!Bl.MissingDaysOfParticipant(ReunionId, ParticipantId))
						Statemachine.Trigger(new SignalNoInformationMissing());
				}
			}
		}

		/// <summary>
		/// state MissingInformation: The participant has so far not given any information on the most possible days.
		/// </summary>
		public MissingInformation StateMissingInformation { get; private set; } = new MissingInformation();

		/// <summary>
		/// state ReactionOnFeedbackMissing: The participant has so far not given any information on the most possible days
		/// for a long time.
		/// </summary>
		public class ReactionOnFeedbackMissing : BaseState
		{
			public ReactionOnFeedbackMissing() : base((int) ParticipantStatusEnum.ReactionOnFeedbackMissing, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalFinalInvitation)
					SetNewState(Statemachine.StateFinallyInvitated);
				else if (signal is SignalNoInformationMissing)
					SetNewState(Statemachine.StateInvitated);
			}

			public override void Touch()
			{
				base.Touch();
				if (Statemachine.CurrentState == this)
				{
					if (!Bl.MissingDaysOfParticipant(ReunionId, ParticipantId))
						Statemachine.Trigger(new SignalNoInformationMissing());
				}
			}
		}

		/// <summary>
		/// state ReactionOnFeedbackMissing: The participant has so far not given any information on the most possible days
		/// for a long time.
		/// </summary>
		public ReactionOnFeedbackMissing StateReactionOnFeedbackMissing { get; private set; } = new ReactionOnFeedbackMissing();

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="statemachineEntity"></param>
		/// <param name="minimumWaitTimeSeconds">
		/// how many seconds do we give the particpant to react on a notification before escalate
		/// it ?
		/// </param>
		/// <param name="dal"></param>
		/// <param name="bl"></param>
		public ParticipantStatemachine(ParticipantStatemachineEntity statemachineEntity, int minimumWaitTimeSeconds, IReunionDal dal, IReunionStatemachineBL bl) : base(statemachineEntity, dal, bl)
		{
			MinimumWaitTimeSeconds = minimumWaitTimeSeconds;
			Init();
		}

		protected override IEnumerable<State> InitAllStates()
		{
			return new State[]
			{
				StateCreated, StateFinallyInvitated, StateInvitated, StateReactionOnFinalInvitationMissing, StateWaitOnLoginForInvitation, StateReactionOnInvitationMissing, StateAccepted, StateRejectedInvitation, StateMissingInformation, StateReactionOnFeedbackMissing,
			};
		}

		#endregion

		#region signals

		/// <summary>
		/// sent when timestamp this.Statemachine.StateMachineEntity.ElapseDate is reached.
		/// </summary>
		private class SignalTimeElapsed : Signal
		{
		}

		/// <summary>
		/// sent when participant visits the page
		/// </summary>
		public class SignalLoggedIn : Signal
		{
		}

		/// <summary>
		/// participant rejected final invitation
		/// </summary>
		public class SignalFinalDateRejected : Signal
		{
		}

		/// <summary>
		/// sent when participant has got the first invitation mail
		/// </summary>
		public class SignalPrimerInvitationSent : Signal
		{
		}

		/// <summary>
		/// sent when participant has got the final invitation mail.
		/// </summary>
		public class SignalFinalInvitation : Signal
		{
		}

		/// <summary>
		/// sent when participant accepts the final invitation
		/// </summary>
		public class SignalFinalDateAccepted : Signal
		{
		}

		/// <summary>
		/// sent when the given final invitation was canceled by organizer
		/// </summary>
		public class SignalFinalInvitationCanceledByOrganizer : Signal
		{
		}

		/// <summary>
		/// sent when particpant updates his date preferences.
		/// </summary>
		public class SignalDateRangesUpdated : Signal
		{
		}

		/// <summary>
		/// sent when the participant has so far not given any information on the most possible days.
		/// </summary>
		public class SignalInformationMissing : Signal
		{
		}

		/// <summary>
		/// sent when the participant has given all information on the very most possible days.
		/// </summary>
		public class SignalNoInformationMissing : Signal
		{
		}

		#endregion

		#region public

		public readonly int MinimumWaitTimeSeconds;
		public int ParticipantId => StateMachineEntity.PlayerId;

		#endregion
	}
}