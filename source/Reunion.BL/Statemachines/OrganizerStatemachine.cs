using System;
using System.Collections.Generic;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using TUtils.Common;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// The OrganizerStatemachine. See documentation. 
	/// Calls
	///		IReunionBL.SendPrimaryInvitationMails()
	///		IReunionBL.RecheckDateFound()
	///		IReunionBL.TriggerSignalFinalInvitationCanceled()
	///		IReunionBL.ClearFinalInvitationDate()
	///		IReunionBL.WakeOrganizer()
	///		IReunionBL.DeactivateStatemachines()
	///		IReunionBL.ReactivateStatemachines()
	/// </summary>
	public class OrganizerStatemachine : StateMachine<OrganizerStatemachine, OrganizerStatemachineEntity>
    {
		private readonly ISystemTimeProvider _systemTimeProvider;

		#region states

		public class BaseState : State
		{
			private readonly ISystemTimeProvider _systemTimeProvider;

			public BaseState(
				ISystemTimeProvider systemTimeProvider,
				int currentState, 
				bool isTerminatedState) : base(currentState, isTerminatedState)
			{
				_systemTimeProvider = systemTimeProvider;
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalDeadlineReached)
					SetNewState(Statemachine.StateFinishedFailed);
			}

			public override void Touch()
			{
				base.Touch();
				if ( _systemTimeProvider.LocalTime > Statemachine.StateMachineEntity.ElapseDate)
					Statemachine.Trigger(new SignalDeadlineReached());
			}
		}

		/// <summary>
		/// state Created: reunion has been created, but organizer hasn't started planning process, yet.
		/// </summary>
		public class Created : BaseState
		{
			public Created(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)OrganizatorStatusEnum.Created, isTerminatedState: false)
			{
			}

		    public override void OnSignal(Signal signal)
		    {
				base.OnSignal(signal);
			    if (signal is SignalStartReunion)
			    {
					SetNewState(Statemachine.StateRunning);
					Bl.SendPrimaryInvitationMails(ReunionId);
				}
			}
		}

		/// <summary>
		/// state Created: reunion has been created, but organizer hasn't started planning process, yet.
		/// </summary>
		public Created StateCreated => _stateCreated ?? (_stateCreated = new Created(_systemTimeProvider));
		private Created _stateCreated;


		/// <summary>
		/// state Running: organizer has started planning process. Reunion hasn't found a possible date yet
		/// for all required participants.
		/// </summary>
		public class Running : BaseState
		{
			public Running(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)OrganizatorStatusEnum.Running, isTerminatedState: false)
			{
			}
			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalDateFound)
					SetNewState(Statemachine.StateDateProposal);
				if (signal is SignalDateRangesUpdated)
					Bl.RecheckDateFound(ReunionId);
			}

			protected override void OnEntered(State<OrganizerStatemachine, OrganizerStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				if (oldState != Statemachine.StateCreated)
				{
					var reunionId = ReunionId;
					Bl.TriggerSignalFinalInvitationCanceled(reunionId);
					Bl.ClearFinalInvitationDate(reunionId);
					Bl.WakeOrganizer(reunionId);
					Bl.RecheckDateFound(ReunionId);
				}
			}
		}

		/// <summary>
		/// state Running: organizer has started planning process. Reunion hasn't found a possible date yet
		/// for all required participants.
		/// </summary>
		public Running StateRunning => _stateRunning ?? (_stateRunning = new Running(_systemTimeProvider));
		private Running _stateRunning;

		/// <summary>
		/// state DateProposal: Reunion has found a proper date for all required participants.
		/// Organizer hasn't chooesed a date yet
		/// </summary>
		public class DateProposal : BaseState
		{
			public DateProposal(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)OrganizatorStatusEnum.DateProposal, isTerminatedState: false)
			{
			}

			protected override void OnEntered(State<OrganizerStatemachine, OrganizerStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
			}

			public override void OnSignal(Signal signal)
			{
				base.OnSignal(signal);
				if (signal is SignalRequiredRejected)
					SetNewState(Statemachine.StateRunning);
				else if (signal is SignalFinalInvitationSent)
					SetNewState(Statemachine.StateFinalInvitation);
				else if (signal is SignalDateRangesUpdated)
					Bl.RecheckDateFound(ReunionId);
			}
		}
		/// <summary>
		/// state DateProposal: Reunion has found a proper date for all required participants.
		/// Organizer hasn't chooesed a date yet
		/// </summary>
		public DateProposal StateDateProposal => _stateDateProposal??(_stateDateProposal = new DateProposal(_systemTimeProvider));
		private DateProposal _stateDateProposal;

		/// <summary>
		/// state FinalInvitation: organizer finally has choosen a date for the reunion.
		/// The participants have got the final invitations, but not all of them have answered and 
		/// agreed definitely.
		/// </summary>
		public class FinalInvitation : BaseState
		{
			public FinalInvitation(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider,(int)OrganizatorStatusEnum.FinalInvitation, isTerminatedState: false)
			{
			}
			public override void OnSignal(Signal signal)
			{
				// don't react on deadline reached
				// base.OnSignal(signal);
				if (signal is SignalRequiredRejected)
					SetNewState(Statemachine.StateRunning);
				else if (signal is SignalParticipantsAccepted)
					SetNewState(Statemachine.StateFinishedSucceeded);
				else if (signal is SignalDateRangesUpdated)
					Bl.RecheckDateFound(ReunionId);
			}
		}
		/// <summary>
		/// state FinalInvitation: organizer finally has choosen a date for the reunion.
		/// The participants have got the final invitations, but not all of them have answered and 
		/// agreed definitely.
		/// </summary>
		public FinalInvitation StateFinalInvitation => _stateFinalInvitation??(_stateFinalInvitation=new  FinalInvitation(_systemTimeProvider));
		private FinalInvitation _stateFinalInvitation;

		/// <summary>
		/// state FinishedSucceeded: all participants, who have time that day have accepted the the final invitation.
		/// </summary>
		public class FinishedSucceeded : BaseState
		{
			public FinishedSucceeded(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider,(int)OrganizatorStatusEnum.FinishedSucceeded, isTerminatedState: true)
			{
			}

			protected override void OnEntered(State<OrganizerStatemachine, OrganizerStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
				Bl.DeactivateStatemachines(ReunionId);
			}

			protected override void OnLeaving()
			{
				base.OnLeaving();
				Bl.ReactivateStatemachines(ReunionId);
			}

			public override void OnSignal(Signal signal)
			{
				// don't react on deadline reached
				// base.OnSignal(signal);
				if (signal is SignalRequiredRejected)
					SetNewState(Statemachine.StateRunning);
				else if (signal is SignalDateRangesUpdated)
					Bl.RecheckDateFound(ReunionId);
			}

		}
		/// <summary>
		/// state FinishedSucceeded: all participants, who have time that day have accepted the the final invitation.
		/// </summary>
		public FinishedSucceeded StateFinishedSucceeded => _stateFinishedSucceeded??(_stateFinishedSucceeded=new FinishedSucceeded(_systemTimeProvider));
		private FinishedSucceeded _stateFinishedSucceeded;

		/// <summary>
		/// state FinishedFailed: deadline of reunion planning has reached but we couldn't make a deal.
		/// </summary>
		public class FinishedFailed : BaseState
		{
			public FinishedFailed(ISystemTimeProvider systemTimeProvider) : base(systemTimeProvider, (int)OrganizatorStatusEnum.FinishedFailed, isTerminatedState: true)
			{
			}

			protected override void OnEntered(State<OrganizerStatemachine, OrganizerStatemachineEntity> oldState)
			{
				base.OnEntered(oldState);
				Bl.WakeOrganizer(ReunionId);
				Bl.DeactivateStatemachines(ReunionId);
			}
		}
		/// <summary>
		/// state FinishedFailed: deadline of reunion planning has reached but we couldn't make a deal.
		/// </summary>
		public FinishedFailed StateFinishedFailed => _stateFinishedFailed??(_stateFinishedFailed=new FinishedFailed(_systemTimeProvider));
		private FinishedFailed _stateFinishedFailed;

		#endregion

		#region constructor

		public OrganizerStatemachine(
			OrganizerStatemachineEntity statemachineEntity,
			IReunionDal dal,
			IReunionStatemachineBL bl,
			ISystemTimeProvider systemTimeProvider)
			: base(statemachineEntity, dal, bl)
		{
			_systemTimeProvider = systemTimeProvider;

			Init();
		}

		protected override IEnumerable<State> InitAllStates()
		{
			return new State[]
			{
				StateCreated,
				StateRunning,
				StateDateProposal,
				StateFinalInvitation,
				StateFinishedSucceeded,
				StateFinishedFailed,
			};
		}

		#endregion

		#region signals

		/// <summary>
		/// sent when organizer has started the planning
		/// </summary>
		public class SignalStartReunion : Signal
		{

		}

		/// <summary>
		/// sent when at minimum one proper date has been found
		/// </summary>
		public class SignalDateFound : Signal
		{

		}

		/// <summary>
		/// A required participant has rejected the final invitation
		/// </summary>
		public class SignalRequiredRejected : Signal
		{

		}

		/// <summary>
		/// sent when the deadline has reached
		/// </summary>
		public class SignalDeadlineReached : Signal
		{

		}

		/// <summary>
		/// sent when final invitation has been sent to all participants
		/// </summary>
		public class SignalFinalInvitationSent : Signal
		{

		}

		/// <summary>
		/// sent when all finally invited participants accepted the date.
		/// </summary>
		public class SignalParticipantsAccepted : Signal
		{

		}
		/// <summary>
		/// sent when a participant has changed his date preferences.
		/// </summary>
		public class SignalDateRangesUpdated : Signal
		{

		}

		

		#endregion
	}
}


