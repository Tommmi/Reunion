using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Serialization;
using System.Text.RegularExpressions;
using AngularJS;
using AngularJS.Cookies;
using AngularJS.UiRouter;
using jQueryApi;
using Reunion.Common.Model;
using Reunion.Web.Models;
using Saltarelle.MultiSelectionCalendar;
using Saltarelle.Utils;

namespace Reunion.Scripts
{
	/// <summary>
	/// participant controller
	/// Represents the angular controller for the editable list of particpants of the reunion.
	/// </summary>
	[Reflectable, Inject("$scope","$cookies","$timeout")]
	public class ParticipantController
	{
		#region fields

		private readonly Timeout _timeoutService;
		private readonly ParticipantsScope _scope;

		#endregion

		#region types

		/// <summary>
		/// represents all editable informations about a participant
		/// </summary>
		public class Participant : ParticipantViewModel
		{
			#region public

			/// <summary>
			/// see ContactPolicyEnum
			/// "0": MayContactByWebservice
			/// "1": ContactFirstTimePersonally
			/// </summary>
			public string contactPolicyValue;
			/// <summary>
			/// localized text of member "contactPolicy"
			/// Automatically updated when changing "contactPolicyValue"
			/// </summary>
			public string contactPolicyDisplayText;

			/// <summary>
			/// localized text of member "playerLanguageIsoCode"
			/// "Deutsch"
			/// "Englisch"
			/// Automatically updated when changing "playerLanguageIsoCode"
			/// </summary>
			public string playerLanguageDisplayText;

			/// <summary>
			/// get localized text of given language. Empty string, if same language as organizer's.
			/// </summary>
			/// <param name="defaultLanguageIsoCode"></param>
			/// <returns></returns>
			public string getLanguageIfNotDefault(string defaultLanguageIsoCode)
			{
				if (defaultLanguageIsoCode == playerLanguageIsoCode)
					return "";
				return playerLanguageDisplayText;
			}

			/// <summary>
			/// gets localized text of participant's contact policy
			/// </summary>
			/// <returns></returns>
			public string getContactPolicyIfNotDefault()
			{
				if (contactPolicy == ContactPolicyEnum.ContactFirstTimePersonally)
					return "";
				return contactPolicyDisplayText;
			}

			/// <summary>
			/// if participant is required this method returns the localized text ("required", "erforderlich")
			/// otherwise an empty string
			/// </summary>
			/// <param name="requiredDisplayText"></param>
			/// <returns></returns>
			public string getLocalizedRequiredStatus(string requiredDisplayText)
			{
				return isRequired ? requiredDisplayText : "";
			}

			#endregion

			#region internal

			public Participant(Participant participant) : this(
				participant.id,
				participant.name,
				participant.mail,
				participant.isRequired,
				participant.contactPolicy,
				participant.playerLanguageIsoCode)
			{
			}

			public Participant(ParticipantViewModel participant) : this(
				participant.id,
				participant.name,
				participant.mail,
				participant.isRequired,
				participant.contactPolicy,
				participant.playerLanguageIsoCode)
			{
			}

			public Participant(
				int id,
				string name,
				string mail,
				bool isRequired,
				ContactPolicyEnum contactPolicy,
				string playerLanguageIsoCode) : base(
					id, name, mail, isRequired, contactPolicy, playerLanguageIsoCode)
			{
				this.HookSetterOfProperty(p => p.contactPolicyValue, OnContactPolicyValueChanged);
				this.HookSetterOfProperty(p => p.playerLanguageIsoCode, OnLanguageIsoCodeChanged);
				this.contactPolicyValue = ((int)contactPolicy).ToString();
				this.playerLanguageIsoCode = playerLanguageIsoCode;
			}

			public void ClearNameAndEmail()
			{
				name = string.Empty;
				mail = string.Empty;
			}

			public void Init(
				int id,
				string name,
				string mail,
				bool isRequired,
				ContactPolicyEnum contactPolicy,
				string playerLanguageIsoCode)
			{
				this.id = id;
				this.name = name;
				this.mail = mail;
				this.isRequired = isRequired;
				this.playerLanguageIsoCode = playerLanguageIsoCode;
				this.HookSetterOfProperty(p => p.contactPolicyValue, OnContactPolicyValueChanged);
				this.HookSetterOfProperty(p => p.playerLanguageIsoCode, OnLanguageIsoCodeChanged);
				this.contactPolicyValue = ((int)contactPolicy).ToString();
			}

			public void Init(Participant participant)
			{
				Init(
					participant.id,
					participant.name,
					participant.mail,
					participant.isRequired,
					participant.contactPolicy,
					participant.playerLanguageIsoCode);
			}

			public bool IsEqual(Participant participant)
			{
				return
					name == participant.name
					&& mail == participant.mail
					&& isRequired == participant.isRequired
					&& contactPolicy == participant.contactPolicy
					&& playerLanguageDisplayText == participant.playerLanguageDisplayText
					&& playerLanguageIsoCode == participant.playerLanguageIsoCode;
			}

			#endregion

			#region private

			/// <summary>
			/// gets name of passed language using the culture of the organizer.
			/// </summary>
			/// <param name="isoCode"></param>
			/// <returns></returns>
			private string GetDisplayTextOfLanguage(string isoCode)
			{
				return jQuery
					.Select(".lang option[value=\"" + isoCode + "\"]")
					.GetText();
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="contactPolicy"></param>
			/// <returns></returns>
			private string GetDisplayTextOfContactPolicy(ContactPolicyEnum contactPolicy)
			{
				return jQuery
					.Select(".contactPolicy option[value=\"" + (int)contactPolicy + "\"]")
					.GetText();
			}

			private void OnLanguageIsoCodeChanged(string newLanguage)
			{
				this.playerLanguageDisplayText = GetDisplayTextOfLanguage(newLanguage);
			}

			private void OnContactPolicyValueChanged(string newConatcPolicyValue)
			{
				this.contactPolicy = (ContactPolicyEnum)int.Parse(newConatcPolicyValue);
				this.contactPolicyDisplayText = GetDisplayTextOfContactPolicy(contactPolicy);
			}

			#endregion
		}

		/// <summary>
		/// the editable list of participants has two different modes:
		/// </summary>
		public enum EditModeEnum
		{
			/// <summary>
			/// the datas of a new participant are beeing entered 
			/// </summary>
			Insert = 0,
			/// <summary>
			/// an allready added participant is being edited
			/// </summary>
			Edit = 1
		}

		/// <summary>
		/// the angular scope of the editable participant list
		/// </summary>
		public class ParticipantsScope : Scope
		{
			/// <summary>
			/// the collection of participants which have been inserted allready
			/// </summary>
			public List<Participant> participants = new List<Participant>();
			/// <summary>
			/// list of invitated friends.
			/// Json encoded string of ParticipantViewModel[]
			/// </summary>
			public string participantsAsJson;
			/// <summary>
			/// The participant who is being edited or inserted by the input row template (see ParticipantsPartial.cshtml #editable-row-pos)
			/// The input row template may be at the bottom of the list (insert mode) or as overlay at the position of an existing participant
			/// (edit mode).
			/// </summary>
			public Participant editrow;
			/// <summary>
			/// EditModeEnum.Edit, if input row template  is editing row currentEditingRowIdx
			/// EditModeEnum.Insert, if input row template contains datas of a new participant
			/// </summary>
			public EditModeEnum editMode = EditModeEnum.Insert;
			/// <summary>
			/// If editMode == EditModeEnum.Edit, currentEditingRowIdx is the index of the participant which is being edited
			/// (participants[currentEditingRowIdx]).
			/// Otherwise currentEditingRowIdx is invalid.
			/// </summary>
			public int currentEditingRowIdx = 0;
		}

		#endregion

		#region constructor

		public ParticipantController(Scope _scope, Cookie _cookies, Timeout timeoutService)
		{
			this._timeoutService = timeoutService;
			var initialParticipantsAsJson = jQuery.Select("#ParticipantsAsJson").GetValue();
			var scope = this._scope = Utils.Enhance<ParticipantsScope, Scope>(_scope); // see TUtils
			scope.WatchPropertyCollection(s=>s.participants,OnParticipantsCollectionChanged);
			var currentLanguage = _cookies["language"] ?? "de";
			scope.editrow = new Participant(
				0, string.Empty, string.Empty,false,ContactPolicyEnum.ContactFirstTimePersonally, currentLanguage);
			scope.On("insertClick", OnInsertParticipant);
			scope.On<string>("editClick", OnEditParticipant);
			scope.On<string>("delClick", OnDeleteParticipant);
			scope.On("saveClick", OnSaveParticipant);

			InitParticipantsByJsonString(initialParticipantsAsJson);
		}

		#endregion

		#region event handler
		/// <summary>
		/// called when clicking on the "Remove"-Button of a participant.
		/// </summary>
		/// <param name="event"></param>
		/// <param name="rowId">'row-'+{row index}</param>
		private void OnDeleteParticipant(Event @event, string rowId)
		{
			var id = GetRowIdxFromId(rowId);
			_scope.participants.RemoveAt(id);
			// update coordinates of input row
			// Deferred because we must wait until DOM has been rebuilt
			_timeoutService.Set(UpdateInputRowCoordinates);
		}

		/// <summary>
		/// Called when clicking on Edit-button of a participant
		/// </summary>
		/// <param name="event"></param>
		/// <param name="rowId">'row-'+{row index}</param>
		private void OnEditParticipant(Event @event, string rowId)
		{
			var editedParticipant = new Participant(_scope.editrow);
			if (IsValid(editedParticipant))
			{
				if (_scope.editMode == EditModeEnum.Edit)
				{
					UpdateParticipant(_scope.currentEditingRowIdx, editedParticipant);
					ShowLastEditedRow();
				}
				else
				{
					InsertOrUpdateParticipant(editedParticipant);
				}
			}
			else if (_scope.editMode == EditModeEnum.Edit)
				return;
			_scope.editMode = EditModeEnum.Edit;
			var rowIdx = HideRow(rowId);
			var participant = _scope.participants[rowIdx];
			_scope.editrow.Init(participant);
			_timeoutService.Set(UpdateInputRowCoordinates);

		}

		/// <summary>
		/// Called when clicking on Insert-Button on bottom of the list
		/// </summary>
		/// <param name="event"></param>
		private void OnInsertParticipant(Event @event)
		{
			var newParticipant = new Participant(_scope.editrow);
			if (!IsValid(newParticipant))
				return;
			if (ExistsParticipant(newParticipant))
			{
				// show warning "mail address allready exists !"
				jQuery.Select("#mailAddressExistsAllreadyDlg").ModalShow();
				return;
			}
			
			if ( InsertOrUpdateParticipant(newParticipant))
				_scope.editrow.ClearNameAndEmail();
		}

		/// <summary>
		/// Called when changing the scope collection property "participants"
		/// </summary>
		/// <param name="newValue"></param>
		/// <param name="oldValue"></param>
		private void OnParticipantsCollectionChanged(List<Participant> newValue, List<Participant> oldValue)
		{
			var json = Json.Stringify(newValue
				.Select(p=>new ParticipantViewModel(
					p.id,
					p.name,
					p.mail,
					p.isRequired,
					p.contactPolicy,
					p.playerLanguageIsoCode))
				.ToArray());
			if (json != _scope.participantsAsJson)
				_scope.participantsAsJson = json;
		}

		/// <summary>
		/// Called when clicking on Save-button of a participant
		/// </summary>
		/// <param name="event"></param>
		private void OnSaveParticipant(Event @event)
		{
			if (_scope.editMode == EditModeEnum.Edit)
			{
				var editedParticipant = new Participant(_scope.editrow);
				if (IsValid(editedParticipant))
					UpdateParticipant(_scope.currentEditingRowIdx, editedParticipant);
				ShowLastEditedRow();
				_scope.editMode = EditModeEnum.Insert;
				_scope.editrow.Init(0, "","",false,ContactPolicyEnum.ContactFirstTimePersonally, _scope.editrow.playerLanguageIsoCode);
				UpdateInputRowCoordinates();
			}
		}

		#endregion

		#region private methods

		/// <summary>
		/// true if participant's mail address allready exists in list _scope.participants
		/// </summary>
		/// <param name="newParticipant"></param>
		/// <returns></returns>
		private bool ExistsParticipant(Participant newParticipant)
		{
			var mail = newParticipant.mail.ToLower();
			return _scope.participants.Any(p => p.mail.ToLower() == mail);
		}

		/// <summary>
		/// Extracts id from "row-{id}"
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		private static int GetRowIdxFromId(string rowId)
		{
			return int.Parse(rowId.Replace("row-", ""));
		}

		/// <summary>
		/// Hides participant's row "rowId"
		/// </summary>
		/// <param name="rowId">"row-{id}"</param>
		/// <returns></returns>
		private int HideRow(string rowId)
		{
			var row = jQuery.Select("#" + rowId);
			var rowIdx = GetRowIdxFromId(rowId);
			_scope.currentEditingRowIdx = rowIdx;
			row.AddClass("transparentColor");
			var bttns = jQuery.Select("#bttns-" + rowIdx);
			bttns.AddClass("hidden");
			return rowIdx;
		}

		/// <summary>
		/// initializes list "participants" by passed json encoded parameter "participantsAsJson"
		/// </summary>
		/// <param name="participantsAsJson">
		/// JSON encoded representation of ParticipantViewModel[]
		/// </param>
		private void InitParticipantsByJsonString(string participantsAsJson)
		{
			if (string.IsNullOrEmpty(participantsAsJson))
				return;
			var newParticipants = Json.Parse<ParticipantViewModel[]>(participantsAsJson);

			var participants = _scope.participants;
			participants.Clear();
			participants.AddRange(newParticipants.Select(p=>new Participant(p)));
		}

		/// <summary>
		/// Depending on if the given participant's mail address allready exists in list "participants" 
		/// allready or not, this method updates the list's item or inserts a new item.
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>true, if inserted</returns>
		private bool InsertOrUpdateParticipant(Participant participant)
		{
			var participantIdx = _scope.participants.IndexOf(p => p.mail == participant.mail);
			if (participantIdx >= 0)
			{
				_scope.participants[participantIdx] = participant;
				return false;
			}
			else
			{
				_scope.participants.Add(participant);
				return true;
			}
		}

		/// <summary>
		/// true, if participants data are valid: Checks
		/// - mail address format
		/// - name length
		/// </summary>
		/// <param name="participant"></param>
		/// <returns></returns>
		private bool IsValid(Participant participant)
		{
			var mailRegex = new Regex(
				@"^([a-z0-9_\.-]+\@[\da-z\.-]+\.[a-z\.]{2,6})$","gm");
			return 
				(participant != null)
				&& (participant.name != null)
				&& (participant.name.Length > 0)
				&& (mailRegex.Test(participant.mail)); // we do need brackets due to a JavaScript bug in Chrome
		}

		/// <summary>
		/// reshow participant row _scope.currentEditingRowIdx
		/// </summary>
		private void ShowLastEditedRow()
		{
			var lastEditedRow = jQuery.Select("#row-" + _scope.currentEditingRowIdx);
			lastEditedRow.RemoveClass("transparentColor");
			var bttns = jQuery.Select("#bttns-" + _scope.currentEditingRowIdx);
			bttns.RemoveClass("hidden");
		}

		/// <summary>
		/// Moves participant form template to the participant's row (edit mode)
		/// or to the bottom of the list (insert mode)
		/// </summary>
		private void UpdateInputRowCoordinates()
		{
			if ( _scope.participants.Count == 0)
				return;
			var inputRow = jQuery.Select("#editable-row");
			if (_scope.editMode == EditModeEnum.Edit)
			{
				var row = jQuery.Select("#row-" + _scope.currentEditingRowIdx);
				var pos = row.GetOffset();
				inputRow.Offset(pos);
			}
			else
			{
				inputRow.CSS("position", "static");
				inputRow.CSS("top", "0");
				inputRow.CSS("left", "0");

			}
		}

		/// <summary>
		/// Copies data from given parameter "participant" to 
		/// the "rowIdx"'th item in participant list.
		/// If "participant" hasn't the same mail address as that item in list and the new address is allready used in list, then 
		/// the method does nothing.
		/// </summary>
		/// <param name="rowIdx">
		/// which row should be updated ?
		/// </param>
		/// <param name="participant"></param>
		private void UpdateParticipant(int rowIdx, Participant participant)
		{
			var participantIdx = _scope.participants.IndexOf(p => p.mail == participant.mail);

			if (rowIdx >= 0 
			    && rowIdx < _scope.participants.Count 
			    && (participantIdx == rowIdx || participantIdx < 0))
			{
				_scope.participants[rowIdx] = participant;
			}
		}

		#endregion
	}
}
