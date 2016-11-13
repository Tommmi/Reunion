using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiSelectionCalendar;
using Reunion.BL;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using Reunion.Web.Models;
using TUtils.Common;

namespace Reunion.Test
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class ComponentTest1
	{
		#region helper

		private static ActionResult CreateReunion(
			Environment env, 
			out int reunionId, 
			params Participant[] participants)
		{
			reunionId = 0;
			var res = env.ReunionController.Create(new ReunionViewModel(
				id: 0,
				name: "testReunion",
				invitationText: "welcome",
				participants: participants,
				deadline: env.SystemTimeProvider.LocalTime.AddDays(1),
				possibleDates:
					Calendar.GetStringFromDateRanges(new List<Range>
					{
						new Range(
							start: env.SystemTimeProvider.LocalTime.AddDays(2), 
							end: env.SystemTimeProvider.LocalTime.AddDays(10), 
							selectionIdx: (int) PreferenceEnum.Yes)
					})));
			var redirectResult = res as RedirectToRouteResult;
			if (redirectResult != null)
				reunionId = (int) redirectResult.RouteValues["id"];
			return res;
		}

		private Environment CreateReunionEnvironment(string testMethodName, out int reunionId, out List<Participant> participants)
		{
			var env = new Environment(
				testMethodName: testMethodName,
				minimumWaitTimeSeconds: 2*24*3600,
				isUserAuthenticated: true,
				startTime:new DateTime(year:2017,month:1,day:1));

			// create reunion ******************************************************
			participants = new List<Participant>();
			for (int i = 0; i < 2; i++)
			{
				participants.Add(new Participant(
					id: 0,
					reunion: null,
					isRequired: true,
					contactPolicy: ContactPolicyEnum.ContactFirstTimePersonally,
					name: $"participant {i}",
					mailAddress: $"address{i}@mail.de",
					languageIsoCodeOfPlayer: "de",
					unguessableId: null));
			}
			var createResult = CreateReunion(env, out reunionId,
				participants.ToArray()) as RedirectToRouteResult;
			env.ClearRequestCache();
			Assert.IsTrue(reunionId != 0);
			return env;
		}

		#endregion

		#region tests

		/// <summary>
		/// Create reunion - simple test
		/// </summary>
		[TestMethod]
		public void CreateReunion()
		{
			var env = new Environment(
				testMethodName: nameof(CreateReunion), 
				minimumWaitTimeSeconds: 1000, 
				isUserAuthenticated: true,
				startTime:new DateTime(year:2017,month:1,day:1));
			int reunionId;
			CreateReunion(env,out reunionId,
				new Participant(
					id: 0,
					reunion: null,
					isRequired: true,
					contactPolicy: ContactPolicyEnum.ContactFirstTimePersonally,
					name: "participant 1",
					mailAddress: "address1@mail.de",
					languageIsoCodeOfPlayer: "de",
					unguessableId: null));
		}

		/// <summary>
		/// create reunion without any participants
		/// </summary>
		[TestMethod]
		public void CreateReunionFailed_NoParticipantsEntered()
		{
			var env = new Environment(
				testMethodName: nameof(CreateReunionFailed_NoParticipantsEntered), 
				minimumWaitTimeSeconds: 1000, 
				isUserAuthenticated: true,
				startTime: new DateTime(year: 2017, month: 1, day: 1));
			int reunionId;
			var res = CreateReunion(env, out reunionId) as ViewResult;
			Assert.IsTrue(res != null);
			Assert.IsTrue(res.ViewName=="Details");
			Assert.IsTrue(res.Model is ReunionViewModel);
			Assert.IsTrue(((ReunionViewModel)res.Model).Id == 0);
			Assert.IsTrue(((ReunionViewModel)res.Model).InvitationText == "welcome");
			Assert.IsTrue(((ReunionViewModel)res.Model).PossibleDates == "8:03.01.2017-11.01.2017");
		}

		/// <summary>
		/// Create reunion - simple test
		/// </summary>
		[TestMethod]
		public void StartReunion()
		{
			int reunionId;
			List<Participant> participants;
			var env = CreateReunionEnvironment(
				testMethodName:nameof(StartReunion),
				reunionId: out reunionId, 
				participants: out participants);

			// get status ******************************************************
			var statusResult = env.ReunionController.Status(reunionId) as ViewResult;
			Assert.IsTrue(statusResult!=null);
			var statusViewModel = statusResult.Model as ReunionStatusViewModel;
			Assert.IsTrue(statusViewModel != null);
			Assert.IsTrue(statusViewModel.OrganizatorStatus == OrganizatorStatusEnum.Created);
			Assert.IsTrue(statusViewModel.Reunion.InvitationText == "welcome");
			foreach (var participant in statusViewModel.Participants)
			{
				Assert.IsTrue(participant.CurrentState==ParticipantStatusEnum.Created);
			}

			// start planning ******************************************************
			var startResult = env.ReunionController.StartPlanning(reunionId) as RedirectToRouteResult;
			Assert.IsTrue(startResult != null);
			Assert.IsTrue((string)startResult.RouteValues["action"] == "Status");
			Assert.IsTrue((int)startResult.RouteValues["id"] == reunionId);

			// get status ******************************************************
			statusResult = env.ReunionController.Status(reunionId) as ViewResult;
			Assert.IsTrue(statusResult != null);
			statusViewModel = statusResult.Model as ReunionStatusViewModel;
			Assert.IsTrue(statusViewModel.OrganizatorStatus == OrganizatorStatusEnum.Running);
			foreach (var participant in statusViewModel.Participants)
			{
				Assert.IsTrue(participant.CurrentState == ParticipantStatusEnum.WaitOnLoginForInvitation);
			}
			// check mail
			var mails = env.EmailSender.SentMails;
			Assert.IsTrue(mails.Count == 2);
			for (int i = 0; i < participants.Count; i++)
			{
				var participant = statusViewModel.Participants.ToList()[i];
				Assert.IsTrue(mails[i].Receipients[0] == participants[i].MailAddress);
				Assert.IsTrue(mails[i].GetMailPart(mails[i].Subject, key: "name") == nameof(IBlResource.GetInvitationMailSubject));
				Assert.IsTrue(mails[i].GetMailPart(mails[i].Subject, key: "0") == "testReunion");
				Assert.IsTrue(mails[i].GetMailPart(mails[i].EmailBody, key: "3") == $"StartPage4Participant/{participant.UnguessableParticipantId}");
			}
		}

		[TestMethod]
		public void RunningTest1()
		{
			int reunionId;
			List<Participant> participants;
			var env = CreateReunionEnvironment(
				testMethodName:nameof(RunningTest1),
				reunionId: out reunionId,
				participants: out participants);

			// start planning ******************************************************
			env.ReunionController.StartPlanning(reunionId);
			env.ClearRequestCache();

			var organizer = env.Bl.GetOrganizer(reunionId, env.CurrentUser.GetId());
			var participantUnguessableIds =
				env.Bl.GetParticipants(reunionId, organizer.Id).Select(p => p.Item1.UnguessableId).ToList();

			// first visit of first participant's calendar ************************************
			var editResult = env.ParticipantController.Edit(participantUnguessableIds[0]) as ViewResult;
			env.ClearRequestCache();
			Assert.IsTrue(editResult.ViewName == "ShowMyCalendar");
			Assert.IsTrue(editResult != null);
			var participantModel = editResult.Model as ParticipantFeedbackViewModel;
			Assert.IsTrue(participantModel != null);
			Assert.IsTrue(participantModel.Culture.TwoLetterISOLanguageName == participants[0].LanguageIsoCodeOfPlayer);
			Assert.IsTrue(participantModel.PossibleDates == string.Empty);
			Assert.IsTrue(participantModel.FinalInvitationDate == null);
			Assert.IsTrue(participantModel.UnsetDaysFormattedText == null);
			// get reunion status ******************************************************
			var statusResult = env.ReunionController.Status(reunionId) as ViewResult;
			env.ClearRequestCache();
			Assert.IsTrue(statusResult != null);
			var reunionStatusViewModel = statusResult.Model as ReunionStatusViewModel;
			Assert.IsTrue(reunionStatusViewModel != null);
			// first participant has status Invitated now 
			var participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[0]);
			Assert.IsTrue(participantStatusViewModel != null);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.Invitated);
			// second participant has still status WaitOnLoginForInvitation
			participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[1]);
			Assert.IsTrue(participantStatusViewModel != null);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.WaitOnLoginForInvitation);
			// touch all ***************************************************************
			env.Bl.TouchAllReunions();
			env.ClearRequestCache();
			// get reunion status ******************************************************
			statusResult = env.ReunionController.Status(reunionId) as ViewResult;
			env.ClearRequestCache();
			reunionStatusViewModel = statusResult.Model as ReunionStatusViewModel;
			// first participant has status MissingInformation now 
			participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[0]);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.MissingInformation);
			// second participant has still status WaitOnLoginForInvitation
			participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[1]);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.WaitOnLoginForInvitation);
			// second visit of first participant's calendar ************************************
			editResult = env.ParticipantController.Edit(participantUnguessableIds[0]) as ViewResult;
			env.ClearRequestCache();
			participantModel = editResult.Model as ParticipantFeedbackViewModel;
			// first participant has now warning that days are missing
			Assert.IsTrue(participantModel.UnsetDaysFormattedText!=null);
			
			// wait some days
			env.SystemTimeProvider.MakeTimeJump(new TimeSpan(days:3,hours:0,minutes:0,seconds:0));
			// touch all ***************************************************************
			env.Bl.TouchAllReunions();
			env.ClearRequestCache();
			// get reunion status ******************************************************
			statusResult = env.ReunionController.Status(reunionId) as ViewResult;
			env.ClearRequestCache();
			reunionStatusViewModel = statusResult.Model as ReunionStatusViewModel;
			// first participant has status ReactionOnFeedbackMissing now
			participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[0]);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.ReactionOnFeedbackMissing);
			// second participant has status ReactionOnInvitationMissing now
			participantStatusViewModel = reunionStatusViewModel.Participants.First(p => p.UnguessableParticipantId == participantUnguessableIds[1]);
			Assert.IsTrue(participantStatusViewModel.CurrentState == ParticipantStatusEnum.ReactionOnInvitationMissing);

			// participant 1 enters his preferences ******************************
			participants = env.Bl.GetParticipants(reunionId, organizerId: organizer.Id).Select(p=>p.Item1).ToList();
			var editResult2 = env.ParticipantController.Edit(new ParticipantFeedbackViewModel(
				participant: participants[0],
				preferredDates:
					new[]
					{
						new TimeRange(
							start: new DateTime(2017, 1, 3),
							end: new DateTime(2017, 1, 10),
							preference: PreferenceEnum.Yes,
							player: null, reunion: null),
						new TimeRange(
							start: new DateTime(2017, 1, 11),
							end: new DateTime(2017, 1, 11),
							preference: PreferenceEnum.NoWay,
							player: null, reunion: null),
						new TimeRange(
							start: new DateTime(2017, 1, 11),
							end: new DateTime(2017, 1, 11),
							preference: PreferenceEnum.MayBe,
							player: null, reunion: null),
					},
				// ignorable
				timeRangesOfOrganizer: new TimeRange[0],
				// ignorable
				finalInvitationDate: null,
				// ignorable
				hasAcceptedFinalInvitationdate: null,
				// ignorable
				daysToBeChecked: null)) as RedirectToRouteResult;
			env.ClearRequestCache();
			Assert.IsTrue(editResult2 != null );
			Assert.IsTrue((string)editResult2.RouteValues["action"]=="Edit");
			// visit of first participant's calendar ************************************
			editResult = env.ParticipantController.Edit(participantUnguessableIds[0]) as ViewResult;
			env.ClearRequestCache();
			participantModel = editResult.Model as ParticipantFeedbackViewModel;
			Assert.IsTrue(participantModel.UnsetDaysFormattedText == null);
			// participant 2 enters his preferences ******************************
			editResult2 = env.ParticipantController.Edit(new ParticipantFeedbackViewModel(
				participant: participants[1],
				preferredDates:
					new[]
					{
						new TimeRange(
							start: new DateTime(2017, 1, 10),
							end: new DateTime(2017, 1, 12),
							preference: PreferenceEnum.Yes,
							player: null, reunion: null),
						new TimeRange(
							start: new DateTime(2017, 1, 3),
							end: new DateTime(2017, 1, 9),
							preference: PreferenceEnum.MayBe,
							player: null, reunion: null),
					},
				// ignorable
				timeRangesOfOrganizer: new TimeRange[0],
				// ignorable
				finalInvitationDate: null,
				// ignorable
				hasAcceptedFinalInvitationdate: null,
				// ignorable
				daysToBeChecked: null)) as RedirectToRouteResult;
			env.ClearRequestCache();

		}

		#endregion
	}
}
