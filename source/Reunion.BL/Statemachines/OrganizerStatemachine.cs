using System;
using System.Collections.Generic;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

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
	    #region states

		public class BaseState : State
		{
			public BaseState(int currentState, bool isTerminatedState) : base(currentState, isTerminatedState)
			{
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
				if ( DateTime.Now > Statemachine.StateMachineEntity.ElapseDate)
					Statemachine.Trigger(new SignalDeadlineReached());
			}
		}

		/// <summary>
		/// state Created: reunion has been created, but organizer hasn't started planning process, yet.
		/// </summary>
		public class Created : BaseState
		{
			public Created() : base((int)OrganizatorStatusEnum.Created, isTerminatedState: false)
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
		public Created StateCreated { get; private set; } = new Created();

		/// <summary>
		/// state Running: organizer has started planning process. Reunion hasn't found a possible date yet
		/// for all required participants.
		/// </summary>
		public class Running : BaseState
		{
			public Running() : base((int)OrganizatorStatusEnum.Running, isTerminatedState: false)
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
		public Running StateRunning { get; private set; } = new Running();

		/// <summary>
		/// state DateProposal: Reunion has found a proper date for all required participants.
		/// Organizer hasn't chooesed a date yet
		/// </summary>
		public class DateProposal : BaseState
		{
			public DateProposal() : base((int)OrganizatorStatusEnum.DateProposal, isTerminatedState: false)
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
		public DateProposal StateDateProposal { get; private set; } = new DateProposal();


		/// <summary>
		/// state FinalInvitation: organizer finally has choosen a date for the reunion.
		/// The participants have got the final invitations, but not all of them have answered and 
		/// agreed definitely.
		/// </summary>
		public class FinalInvitation : BaseState
		{
			public FinalInvitation() : base((int)OrganizatorStatusEnum.FinalInvitation, isTerminatedState: false)
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
		public FinalInvitation StateFinalInvitation { get; private set; } = new FinalInvitation();

		/// <summary>
		/// state FinishedSucceeded: all participants, who have time that day have accepted the the final invitation.
		/// </summary>
		public class FinishedSucceeded : BaseState
		{
			public FinishedSucceeded() : base((int)OrganizatorStatusEnum.FinishedSucceeded, isTerminatedState: true)
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
		public FinishedSucceeded StateFinishedSucceeded { get; private set; } = new FinishedSucceeded();

		/// <summary>
		/// state FinishedFailed: deadline of reunion planning has reached but we couldn't make a deal.
		/// </summary>
		public class FinishedFailed : BaseState
		{
			public FinishedFailed() : base((int)OrganizatorStatusEnum.FinishedFailed, isTerminatedState: true)
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
		public FinishedFailed StateFinishedFailed { get; private set; } = new FinishedFailed();

		#endregion

		#region constructor

		public OrganizerStatemachine(
			OrganizerStatemachineEntity statemachineEntity,
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


