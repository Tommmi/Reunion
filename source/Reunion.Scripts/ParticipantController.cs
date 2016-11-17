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
	/// </summary>
	[Reflectable, Inject("$scope","$cookies","$timeout")]
	public class ParticipantController
	{
		private readonly Timeout _timeout;
		private readonly ParticipantsScope _scope;

		public class Participant : ParticipantViewModel
		{
			/// <summary>
			/// "0": MayContactByWebservice
			/// "1": ContactFirstTimePersonally
			/// </summary>
			public string contactPolicyValue;
			/// <summary>
			/// localized text of member "contactPolicy"
			/// </summary>
			public string contactPolicyDisplayText;
			
			/// <summary>
			/// localized text of member "playerLanguageIsoCode"
			/// </summary>
			public string playerLanguageDisplayText;

			public string getLanguageIfNotDefault(string defaultLanguageIsoCode)
			{
				if (defaultLanguageIsoCode == playerLanguageIsoCode)
					return "";
				return playerLanguageDisplayText;
			}

			public string getContactPolicyIfNotDefault()
			{
				if (contactPolicy == ContactPolicyEnum.ContactFirstTimePersonally)
					return "";
				return contactPolicyDisplayText;
			}

			private string GetDisplayTextOfLanguage(string isoCode)
			{
				return jQuery
					.Select(".lang option[value=\"" + isoCode + "\"]")
					.GetText();
			}

			private string GetDisplayTextOfContactPolicy(ContactPolicyEnum contactPolicy)
			{
				return jQuery
					.Select(".contactPolicy option[value=\"" + (int)contactPolicy + "\"]")
					.GetText();
			}

			public string getLocalizedRequiredStatus(string requiredDisplayText)
			{
				return isRequired ? requiredDisplayText : "";
			}

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

			private void OnLanguageIsoCodeChanged(string newLanguage)
			{
				this.playerLanguageDisplayText = GetDisplayTextOfLanguage(newLanguage);
			}

			private void OnContactPolicyValueChanged(string newConatcPolicyValue)
			{
				this.contactPolicy = (ContactPolicyEnum)int.Parse(newConatcPolicyValue);
				this.contactPolicyDisplayText = GetDisplayTextOfContactPolicy(contactPolicy);
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

			public void ClearNameAndEmail()
			{
				name = string.Empty;
				mail = string.Empty;
			}
		}

		public enum EditModeEnum
		{
			Insert = 0,
			Edit = 1
		}

		public class ParticipantsScope : Scope
		{
			/// <summary>
			/// list of invitated friends
			/// </summary>
			public List<Participant> participants = new List<Participant>();
			/// <summary>
			/// list of invitated friends
			/// Json encoded string of ParticipantViewModel[]
			/// </summary>
			public string participantsAsJson;
			/// <summary>
			/// participant datas which are being edited by the input row template (see ParticipantsPartial.cshtml #editable-row-pos)
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
			/// </summary>
			public int currentEditingRowIdx = 0;
		}

		public ParticipantController(Scope _scope, Cookie _cookies, Timeout _timeout)
		{
			this._timeout = _timeout;
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

		private void OnDeleteParticipant(Event @event, string rowId)
		{
			var id = GetRowIdxFromId(rowId);
			_scope.participants.RemoveAt(id);
			// update coordinates of input row
			// Deferred because we must wait until DOM has been rebuild 
			_timeout.Set(UpdateInputRowCoordinates);
		}

		private static int GetRowIdxFromId(string rowId)
		{
			return int.Parse(rowId.Replace("row-", ""));
		}

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
			_timeout.Set(UpdateInputRowCoordinates);

		}

		/// <summary>
		/// 
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

		private void ShowLastEditedRow()
		{
			var lastEditedRow = jQuery.Select("#row-" + _scope.currentEditingRowIdx);
			lastEditedRow.RemoveClass("transparentColor");
			var bttns = jQuery.Select("#bttns-" + _scope.currentEditingRowIdx);
			bttns.RemoveClass("hidden");
		}

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

		private void OnInsertParticipant(Event @event)
		{
			var newParticipant = new Participant(_scope.editrow);
			if (!IsValid(newParticipant) || ExistsParticipant(newParticipant))
				return;
			if ( InsertOrUpdateParticipant(newParticipant))
				_scope.editrow.ClearNameAndEmail();
		}

		private bool ExistsParticipant(Participant newParticipant)
		{
			var mail = newParticipant.mail.ToLower();
			return _scope.participants.Any(p => p.mail.ToLower() == mail);
		}

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
		/// 
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


		/// <summary>
		/// 
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>true, if inserted</returns>
		private bool InsertOrUpdateParticipant(Participant participant)
		{
			var participantIdx = _scope.participants.IndexOf(p => p.mail == participant.mail);
			if (participantIdx >= 0)
			{
				_scope.participants[participantIdx] = participant;
			}
			else
			{
				_scope.participants.Add(participant);
				return true;
			}

			return false;
		}

		private void InitParticipantsByJsonString(string participantsAsJson)
		{
			if (string.IsNullOrEmpty(participantsAsJson))
				return;
			var newParticipants = Json.Parse<ParticipantViewModel[]>(participantsAsJson);

			var participants = _scope.participants;
			participants.Clear();
			participants.AddRange(newParticipants.Select(p=>new  Participant(p)));
		}

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
	}
}
