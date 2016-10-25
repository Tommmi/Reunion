using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Web.Common;
using Reunion.Web.Models;
using Reunion.Web.Resources;
using TUtils.Common.Extensions;
using TUtils.Common.MVC;
using MultiSelectionCalendar;
using Reunion.Common.Model.States;
using TUtils.Common.EF6.Transaction;
using TUtils.Common.Transaction;

namespace Reunion.Web.Controllers
{
	[MustBeAuthorized]
    public class ReunionController : Controller
    {
		private readonly IReunionBL _bl;
		private readonly IIdentityManager _identityManager;
		private readonly IAppSettings _appSettings;

		public ReunionController(
			IReunionBL bl,
			IIdentityManager identityManager,
			IAppSettings appSettings)
		{
			_bl = bl;
			_identityManager = identityManager;
			_appSettings = appSettings;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">reunion id</param>
		/// <param name="date"></param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult FinalInvite(int id, string date)
		{
			var dateAsDate = DateTime.ParseExact(date,format:"dd.MM.yyyy",provider:CultureInfo.InvariantCulture).Date;
			var reunionId = id;
			var organizer = _identityManager.GetVerifiedOrganizer(reunionId);
			if (organizer == null)
				return RedirectToAction("Index", "Home");
			_bl.FinallyInvite(reunionId, organizerId: organizer.Id, date: dateAsDate);
			return RedirectToAction("Status", new {id = reunionId});
		}

		[HttpGet]
		public ActionResult Manage()
		{
			var userId = _identityManager.GetCurrentUser()?.GetId();
			if (userId == null)
				return RedirectToAction("index", "Home");
			var reunions = _bl.GetReunionsOfUser(userId);
			if ( reunions == null)
				return RedirectToAction("index", "Home");
			return View(new ReunionListViewModel(
				reunions:reunions
					.Select(r=>new ReunionListViewModel.ReunionVM(name:r.Name,reunionId:r.Id))
					.ToList()));
		}

		[HttpGet]
		public ActionResult Edit(int id)
		{
			var reunionId = id;
			var organizer = _identityManager.GetVerifiedOrganizer(reunionId);
			if (organizer == null)
				return RedirectToAction("Index", "Home");
			var timeRangesOfOrganizer = _bl.GetTimeRangesOfReunion(reunionId);
			var participants = _bl.GetParticipants(reunionId, organizer.Id);
			var reunionViewModel = organizer.Reunion.GetReunionViewModel(participants.Select(p=>p.Item1),timeRangesOfOrganizer);
			return View("Details",reunionViewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(ReunionViewModel model)
		{
			return CreateOrEdit(model);
		}



		[HttpGet]
		public ActionResult Delete(int id)
		{
			var reunionId = id;
			var organizer = _identityManager.GetVerifiedOrganizer(reunionId);
			if (organizer == null)
				return RedirectToAction("Index", "Home");
			_bl.DeleteReunion(reunionId:reunionId,organizerId:organizer.Id);
			return RedirectToAction("Manage");
		}

		[HttpGet]
		public ActionResult StartPlanning(int id)
		{
			var reunionId = id;
			var organizer = _identityManager.GetVerifiedOrganizer(reunionId);
			if (organizer == null)
				return RedirectToAction("Index", "Home");
			var reunion = _bl.StartReunion(reunionId: reunionId, organizerId: organizer.Id);
			if (reunion == null)
				return RedirectToAction("Index", "Home");
			return RedirectToAction("Status", new {id= reunionId});
		}

		/// <summary>
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Status(int id)
		{
			var reunionId = id;
			var organizer = _identityManager.GetVerifiedOrganizer(reunionId);
			if (organizer == null)
				return RedirectToAction("Index", "Home");

			var reunionStatus = _bl.GetReunionStatus(reunionId).Value;
			var participants = _bl.GetParticipants(reunionId, organizer.Id);
			var dateProposals = _bl.GetDateProposals(reunionId: reunionId, organizerId: organizer.Id);
			var invitationMailContent  = reunionStatus == OrganizatorStatusEnum.Created ? _bl.GetInvitationMailContent(reunionId, organizer.Id) : null;
			var reunionStatusViewModel = new ReunionStatusViewModel(
				organizer.Reunion,
				reunionStatus,
				participants,
				dateProposals,
				_appSettings.StartPage4Participant,
				invitationMailContent);

			return View(reunionStatusViewModel);
		}



		[HttpGet]
		public ActionResult Create()
		{
			return View("Details",new ReunionViewModel());
		}

		[HttpPost]
		public ActionResult Create(ReunionViewModel model)
		{
			return CreateOrEdit(model);
		}

		private ActionResult CreateOrEdit(ReunionViewModel model)
		{
			if ( !ModelState.IsValid)
				return View("Details", model);

			var participantsAsJson = model.ParticipantsAsJson;
			if (participantsAsJson.IsNullOrEmpty() || participantsAsJson=="[]")
			{
				ModelState.AddModelError(key:nameof(ReunionViewModel.ParticipantsAsJson),errorMessage:Resource1.ErrEnterParticipants);
				return View("Details", model);
			}

			var participants = System.Web.Helpers.Json
				.Decode<ParticipantViewModel[]>(participantsAsJson)
				.Select(p => new Participant(
					id:p.id,
					reunion: null,
					isRequired: p.isRequired,
					contactPolicy: p.contactPolicy,
					name: p.name,
					mailAddress: p.mail,
					languageIsoCodeOfPlayer: p.playerLanguageIsoCode,
					unguessableId: null))
				.ToList();

			if (participants.GroupBy(p => p.MailAddress).Where(g => g.Count() > 1).Any())
			{
				ModelState.AddModelError(key: nameof(ReunionViewModel.ParticipantsAsJson), errorMessage: Resource1.ErrAmbigiousParticipantEmail);
				return View("Details", model);
			}

			var possibleDatesOfOrganizer =
				model.PossibleDates
					.GetDateRangesFromString()
					.Where(range => range.SelectionIdx != 0)
					.Select(range => new TimeRange(
						start: range.Start,
						end: range.End,
						preference: (PreferenceEnum)range.SelectionIdx,
						player: null,
						reunion: null))
					.ToList();

			if (!possibleDatesOfOrganizer.Any())
			{
				ModelState.AddModelError(key: nameof(ReunionViewModel.PossibleDates), errorMessage: Resource1.ErrPossibleDatesMissing);
				return View("Details", model);
			}


			return _bl.DoWithSameDbContext<ActionResult>(() =>
			{
				var currentuser = _identityManager.GetCurrentUser();
				var organizer = new Organizer(
					id:0,
					userId: currentuser.GetId(),
					reunion: null,
					languageIsoCodeOfPlayer: _identityManager.GetLanguageOfCurrentUser(),
					name: currentuser.GetName(),
					mailAddress: currentuser.GetEmail());
				var reunion = new ReunionEntity().Init(
					reunionId: model.Id,
					name: model.Name,
					invitationText: model.InvitationText,
					deadline: model.Deadline,
					maxReactionTimeHours:_appSettings.MaxReactionTimeHours,
					deactivatedParticipants:null,
					finalInvitationDate:null);

				// if new reunion
				if (model.Id == 0)
				{
					var result = _bl.CreateReunion(reunion,organizer,participants,possibleDatesOfOrganizer);
					switch (result.ResultCode)
					{
						case ReunionCreateResult.ResultCodeEnum.Succeeded:
							reunion = result.Entity;
							model.Id = reunion.Id;
							return RedirectToAction("Status", new {id = reunion.Id});
						case ReunionCreateResult.ResultCodeEnum.NameAllreadyExists:
							ModelState.AddModelError(key: nameof(ReunionViewModel.Name), errorMessage: Resource1.ErrReunionNameAllreadyExist);
							return View("Details", model);
						default:
							throw new ArgumentOutOfRangeException("87ehfo40324gfj");
					}
				}
				else // if edit existing reunion
				{
					organizer = _identityManager.GetVerifiedOrganizer(reunion.Id);
					var result = _bl.UpdateReunion(reunion, organizer.Id,participants, possibleDatesOfOrganizer);
					switch (result.ResultCode)
					{
						case ReunionUpdateResult.ResultCodeEnum.Succeeded:
							return RedirectToAction(nameof(Status), new { id = reunion.Id });
						case ReunionUpdateResult.ResultCodeEnum.ReunionNotExists:
						case ReunionUpdateResult.ResultCodeEnum.NotAuthorized:
							return RedirectToAction("Index", "Home");
						case ReunionUpdateResult.ResultCodeEnum.NameAllreadyExists:
							ModelState.AddModelError(key: nameof(ReunionViewModel.Name), errorMessage: Resource1.ErrReunionNameAllreadyExist);
							return View("Details", model);
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			});
		}
    }
}