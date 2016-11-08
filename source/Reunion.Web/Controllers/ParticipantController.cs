using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MultiSelectionCalendar;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Web.Models;
using TUtils.Common.MVC;

namespace Reunion.Web.Controllers
{
    public class ParticipantController : Controller
    {
	    private readonly IReunionBL _bl;
	    private readonly IIdentityManager _identityManager;

	    public ParticipantController(
			IReunionBL bl,
			IIdentityManager identityManager)
	    {
		    _bl = bl;
		    _identityManager = identityManager;
	    }

		public ActionResult AcceptFinalDate(string id)
		{
			var unguessableParticipantId = id;
			var participant = _bl.GetVerifiedParticipant(unguessableParticipantId);
			if (participant == null)
				return RedirectToAction("Index", "Home");

			_bl.AcceptFinalDate(participantId:participant.Id,reunionId:participant.Reunion.Id);
			return RedirectToAction(actionName: nameof(Edit), routeValues: new { id = unguessableParticipantId });
		}

		public ActionResult RejectFinalDateOnly(string id)
		{
			var unguessableParticipantId = id;
			var participant = _bl.GetVerifiedParticipant(unguessableParticipantId);
			if (participant == null)
				return RedirectToAction("Index", "Home");
			_bl.RejectFinalDateOnly(participantId: participant.Id, reunionId: participant.Reunion.Id);
			return RedirectToAction(actionName: nameof(Edit), routeValues: new { id = unguessableParticipantId });
		}

		public ActionResult RejectCompletely(string id)
		{
			var unguessableParticipantId = id;
			var participant = _bl.GetVerifiedParticipant(unguessableParticipantId);
			if (participant == null)
				return RedirectToAction("Index", "Home");
			_bl.RejectCompletely(participantId: participant.Id, reunionId: participant.Reunion.Id);
			return RedirectToAction("Index","Home");
		}

		// GET: Participant
		[HttpGet]
		public ActionResult Edit(string id)
        {
	        var unguessableParticipantId = id;
			var participant = _bl.GetVerifiedParticipant(unguessableParticipantId);
			if ( participant == null )
				return RedirectToAction("Index", "Home");

			DateTime? finalInvitationdate;
			bool? hasAcceptedFinalInvitationdate;
			IEnumerable<DateTime> daysToBeChecked;
			var preferredDates = _bl.GetTimeRangesOfParticipant(
				reunionId: participant.Reunion.Id,
				participantId:participant.Id,
				finalInvitationdate: out finalInvitationdate,
				hasAcceptedFinalInvitationdate: out hasAcceptedFinalInvitationdate,
				daysToBeChecked: out daysToBeChecked);
			ViewBag.ShowLanguageSwitch = false;
			ViewBag.LanguageIsoCode = participant.LanguageIsoCodeOfPlayer;
			return View("ShowMyCalendar", new ParticipantFeedbackViewModel(
				participant,
				preferredDates: preferredDates,
				timeRangesOfOrganizer: _bl.GetTimeRangesOfReunion(participant.Reunion.Id),
				finalInvitationDate:finalInvitationdate,
				hasAcceptedFinalInvitationdate:hasAcceptedFinalInvitationdate,
				daysToBeChecked: daysToBeChecked));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(ParticipantFeedbackViewModel model)
	    {
			var unguessableParticipantId = model.UnguessableParticipantId;
			var preferredTimeRanges = model.PossibleDates
				.GetDateRangesFromString()
				.Where(r=>r.SelectionIdx!=0)
				.Select(r => new TimeRange(
					start: r.Start,
					end: r.End,
					preference: (PreferenceEnum) r.SelectionIdx,
					player: null,
					reunion: null))
				.ToList();

			_bl.UpdateTimeRangesOfParticipant(preferredTimeRanges, unguessableIdOfParticipant:unguessableParticipantId);

			return RedirectToAction(actionName:nameof(Edit),routeValues:new {id= unguessableParticipantId});
	    }

	}
}