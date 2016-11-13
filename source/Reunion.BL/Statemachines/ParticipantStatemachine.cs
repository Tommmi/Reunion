using System;
using System.Collections.Generic;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using TUtils.Common;
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
			protected readonly ISystemTimeProvider _systemTimeProvider;

			public BaseState(
				ISystemTimeProvider systemTimeProvider,
				int currentState, 
				bool isTerminated) : base(currentState, isTerminated)
			{
				_systemTimeProvider = systemTimeProvider;
			}

			public override void Touch()
			{
				base.Touch();
				if (_systemTimeProvider.LocalTime > Statemachine.StateMachineEntity.ElapseDate)
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
			public Created(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)ParticipantStatusEnum.Created, isTerminated:false)
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
		public Created StateCreated => _stateCreated??(_stateCreated= new Created(_systemTimeProvider));
		private Created _stateCreated;


		/// <summary>
		/// state WaitOnLoginForInvitation: participant has got primer invitation mail but didn't 
		/// visit the page yet.
		/// </summary>
		public class WaitOnLoginForInvitation : BaseState
		{
			public WaitOnLoginForInvitation(ISystemTimeProvider systemTimeProvider) 
				: base(systemTimeProvider, (int)ParticipantStatusEnum.WaitOnLoginForInvitation, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Statemachine.StateMachineEntity.ElapseDate = _systemTimeProvider.LocalTime.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
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
		public WaitOnLoginForInvitation StateWaitOnLoginForInvitation 
			=> _stateWaitOnLoginForInvitation ?? (_stateWaitOnLoginForInvitation = new WaitOnLoginForInvitation(_systemTimeProvider));
		private WaitOnLoginForInvitation _stateWaitOnLoginForInvitation;

		/// <summary>
		/// state ReactionOnInvitationMissing:  participant has got primer invitation mail but didn't 
		/// visit the page for a long time.
		/// </summary>
		public class ReactionOnInvitationMissing : BaseState
		{
			public ReactionOnInvitationMissing(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)ParticipantStatusEnum.ReactionOnInvitationMissing, isTerminated: false)
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
		public ReactionOnInvitationMissing StateReactionOnInvitationMissing => _stateReactionOnInvitationMissing??(_stateReactionOnInvitationMissing= new ReactionOnInvitationMissing(_systemTimeProvider));
		private ReactionOnInvitationMissing _stateReactionOnInvitationMissing;

		/// <summary>
		///  state Invitated: participant has reacted at minimum one time on the primaer invitation mail,
		/// but hasn't git a final invitation mail, yet.
		/// </summary>
		public class Invitated : BaseState
		{
			public Invitated(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)ParticipantStatusEnum.Invitated, isTerminated: false)
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
		public Invitated StateInvitated => _stateInvitated??(_stateInvitated = new Invitated(_systemTimeProvider));
		private Invitated _stateInvitated;

		/// <summary>
		/// state FinallyInvitated: participant has got a final invitation mail, but hasn't reacted on that mail yet.
		/// </summary>
		public class FinallyInvitated : BaseState
		{
			public FinallyInvitated(ISystemTimeProvider systemTimeProvider) 
				: base(systemTimeProvider, (int) ParticipantStatusEnum.FinallyInvitated, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Statemachine.StateMachineEntity.ElapseDate = _systemTimeProvider.LocalTime.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
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
		public FinallyInvitated StateFinallyInvitated => _stateFinallyInvitated??(_stateFinallyInvitated=new FinallyInvitated(_systemTimeProvider));
		private FinallyInvitated _stateFinallyInvitated;

		/// <summary>
		/// state ReactionOnFinalInvitationMissing: participant has got a final invitation mail, 
		/// but hasn't reacted on that mail for a long time.
		/// </summary>
		public class ReactionOnFinalInvitationMissing : BaseState
		{
			public ReactionOnFinalInvitationMissing(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int) ParticipantStatusEnum.ReactionOnFinalInvitationMissing, isTerminated: false)
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
		public ReactionOnFinalInvitationMissing StateReactionOnFinalInvitationMissing => _stateReactionOnFinalInvitationMissing??(_stateReactionOnFinalInvitationMissing=new ReactionOnFinalInvitationMissing(_systemTimeProvider));
		private ReactionOnFinalInvitationMissing _stateReactionOnFinalInvitationMissing;

		/// <summary>
		/// state RejectedInvitation: participant has rejected the final invitation.
		/// </summary>
		public class RejectedInvitation : BaseState
		{
			public RejectedInvitation(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int) ParticipantStatusEnum.RejectedInvitation, isTerminated: false)
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
		public RejectedInvitation StateRejectedInvitation => _stateRejectedInvitation??(_stateRejectedInvitation=new RejectedInvitation(_systemTimeProvider));
		private RejectedInvitation _stateRejectedInvitation;

		/// <summary>
		/// state Accepted: participant has accepted final invitation
		/// </summary>
		public class Accepted : BaseState
		{
			public Accepted(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int) ParticipantStatusEnum.Accepted, isTerminated: true)
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
		public Accepted StateAccepted => _stateAccepted??(_stateAccepted=new Accepted(_systemTimeProvider));
		private Accepted _stateAccepted;

		/// <summary>
		/// state MissingInformation: The participant has so far not given any information on the most possible days.
		/// </summary>
		public class MissingInformation : BaseState
		{
			public MissingInformation(ISystemTimeProvider systemTimeProvider) 
				: base(systemTimeProvider, (int) ParticipantStatusEnum.MissingInformation, isTerminated: false)
			{
			}

			protected override void OnEntered(State<ParticipantStatemachine, ParticipantStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.SendMissingDaysNotification(ReunionId, ParticipantId);
				Statemachine.StateMachineEntity.ElapseDate = _systemTimeProvider.LocalTime.AddSeconds(Statemachine.MinimumWaitTimeSeconds);
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
		public MissingInformation StateMissingInformation => _stateMissingInformation??(_stateMissingInformation=new MissingInformation(_systemTimeProvider));
		private MissingInformation _stateMissingInformation;

		/// <summary>
		/// state ReactionOnFeedbackMissing: The participant has so far not given any information on the most possible days
		/// for a long time.
		/// </summary>
		public class ReactionOnFeedbackMissing : BaseState
		{
			public ReactionOnFeedbackMissing(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int) ParticipantStatusEnum.ReactionOnFeedbackMissing, isTerminated: false)
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
		public ReactionOnFeedbackMissing StateReactionOnFeedbackMissing => _stateReactionOnFeedbackMissing??(_stateReactionOnFeedbackMissing=new ReactionOnFeedbackMissing(_systemTimeProvider));
		private ReactionOnFeedbackMissing _stateReactionOnFeedbackMissing;

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
		/// <param name="systemTimeProvider"></param>
		public ParticipantStatemachine(
			ParticipantStatemachineEntity statemachineEntity,
			int minimumWaitTimeSeconds, 
			IReunionDal dal, 
			IReunionStatemachineBL bl,
			ISystemTimeProvider systemTimeProvider) : base(statemachineEntity, dal, bl)
		{
			MinimumWaitTimeSeconds = minimumWaitTimeSeconds;
			_systemTimeProvider = systemTimeProvider;
			Init();
		}

		protected override IEnumerable<State> InitAllStates()
		{
			return new State[]
			{
				StateCreated,
				StateFinallyInvitated,
				StateInvitated,
				StateReactionOnFinalInvitationMissing,
				StateWaitOnLoginForInvitation,
				StateReactionOnInvitationMissing,
				StateAccepted,
				StateRejectedInvitation,
				StateMissingInformation,
				StateReactionOnFeedbackMissing,
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
		private readonly ISystemTimeProvider _systemTimeProvider;
		public int ParticipantId => StateMachineEntity.PlayerId;

		#endregion
	}
}