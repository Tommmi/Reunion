(function() {
	'use strict';
	var $asm = {};
	global.Reunion = global.Reunion || {};
	global.Reunion.Common = global.Reunion.Common || {};
	global.Reunion.Common.Model = global.Reunion.Common.Model || {};
	global.Reunion.Scripts = global.Reunion.Scripts || {};
	global.Reunion.Web = global.Reunion.Web || {};
	global.Reunion.Web.Models = global.Reunion.Web.Models || {};
	ss.initAssembly($asm, 'Reunion.Scripts');
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Common.Model.ContactPolicyEnum
	var $Reunion_Common_Model_ContactPolicyEnum = function() {
	};
	$Reunion_Common_Model_ContactPolicyEnum.__typeName = 'Reunion.Common.Model.ContactPolicyEnum';
	global.Reunion.Common.Model.ContactPolicyEnum = $Reunion_Common_Model_ContactPolicyEnum;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Common.Model.PreferenceEnum
	var $Reunion_Common_Model_PreferenceEnum = function() {
	};
	$Reunion_Common_Model_PreferenceEnum.__typeName = 'Reunion.Common.Model.PreferenceEnum';
	global.Reunion.Common.Model.PreferenceEnum = $Reunion_Common_Model_PreferenceEnum;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.Program
	var $Reunion_Scripts_$Program = function() {
	};
	$Reunion_Scripts_$Program.__typeName = 'Reunion.Scripts.$Program';
	$Reunion_Scripts_$Program.$main = function() {
		var module = angular.module('reunionApp', ['ngCookies']);
		AngularJS.ModuleBuilder.Controller($Reunion_Scripts_ParticipantController).call(null, module, []);
		AngularJS.ModuleBuilder.Controller($Reunion_Scripts_CalendarController).call(null, module, []);
	};
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.CalendarController
	var $Reunion_Scripts_CalendarController = function(_scope) {
		this.$_scope = null;
		var scope = this.$_scope = Saltarelle.Utils.Utils.enhance($Reunion_Scripts_CalendarController$CalendarScope, AngularJS.Scope).call(null, _scope);
		// see TUtils
	};
	$Reunion_Scripts_CalendarController.__typeName = 'Reunion.Scripts.CalendarController';
	global.Reunion.Scripts.CalendarController = $Reunion_Scripts_CalendarController;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.CalendarController.CalendarScope
	var $Reunion_Scripts_CalendarController$CalendarScope = function() {
		this.currentCalendarSelectionIdx = 8;
		AngularJS.Scope.call(this);
	};
	$Reunion_Scripts_CalendarController$CalendarScope.__typeName = 'Reunion.Scripts.CalendarController$CalendarScope';
	global.Reunion.Scripts.CalendarController$CalendarScope = $Reunion_Scripts_CalendarController$CalendarScope;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.ParticipantController
	var $Reunion_Scripts_ParticipantController = function(_scope, _cookies, _timeout) {
		this.$_timeout = null;
		this.$_scope = null;
		this.$_timeout = _timeout;
		var initialParticipantsAsJson = $('#ParticipantsAsJson').val();
		var scope = this.$_scope = Saltarelle.Utils.Utils.enhance($Reunion_Scripts_ParticipantController$ParticipantsScope, AngularJS.Scope).call(null, _scope);
		// see TUtils
		var $t1 = { ntype: 38, type: $Reunion_Scripts_ParticipantController$ParticipantsScope, name: 's' };
		var $t2 = { typeDef: $Reunion_Scripts_ParticipantController$ParticipantsScope, name: 'participants', type: 4, returnType: Array, sname: 'participants' };
		var $t3 = { ntype: 23, type: $t2.returnType, expression: $t1, member: $t2 };
		Saltarelle.Utils.Utils.watchPropertyCollection($Reunion_Scripts_ParticipantController$ParticipantsScope, Array).call(null, scope, { ntype: 18, type: Function, returnType: $t3.type, body: $t3, params: [$t1] }, ss.mkdel(this, this.$onParticipantsCollectionChanged));
		var currentLanguage = ss.coalesce(_cookies['language'], 'de');
		scope.editrow = new $Reunion_Scripts_ParticipantController$Participant.$ctor2(0, '', '', false, 1, currentLanguage);
		scope.$on('insertClick', ss.mkdel(this, this.$onInsertParticipant));
		scope.$on('editClick', ss.mkdel(this, this.$onEditParticipant));
		scope.$on('delClick', ss.mkdel(this, this.$onDeleteParticipant));
		scope.$on('saveClick', ss.mkdel(this, this.$onSaveParticipant));
		this.$initParticipantsByJsonString(initialParticipantsAsJson);
	};
	$Reunion_Scripts_ParticipantController.__typeName = 'Reunion.Scripts.ParticipantController';
	$Reunion_Scripts_ParticipantController.$getRowIdxFromId = function(rowId) {
		return parseInt(ss.replaceAllString(rowId, 'row-', ''));
	};
	global.Reunion.Scripts.ParticipantController = $Reunion_Scripts_ParticipantController;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.ParticipantController.EditModeEnum
	var $Reunion_Scripts_ParticipantController$EditModeEnum = function() {
	};
	$Reunion_Scripts_ParticipantController$EditModeEnum.__typeName = 'Reunion.Scripts.ParticipantController$EditModeEnum';
	global.Reunion.Scripts.ParticipantController$EditModeEnum = $Reunion_Scripts_ParticipantController$EditModeEnum;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.ParticipantController.Participant
	var $Reunion_Scripts_ParticipantController$Participant = function(participant) {
		$Reunion_Scripts_ParticipantController$Participant.$ctor2.call(this, participant.id, participant.name, participant.mail, participant.isRequired, participant.contactPolicy, participant.playerLanguageIsoCode);
	};
	$Reunion_Scripts_ParticipantController$Participant.__typeName = 'Reunion.Scripts.ParticipantController$Participant';
	$Reunion_Scripts_ParticipantController$Participant.$ctor1 = function(participant) {
		$Reunion_Scripts_ParticipantController$Participant.$ctor2.call(this, participant.id, participant.name, participant.mail, participant.isRequired, participant.contactPolicy, participant.playerLanguageIsoCode);
	};
	$Reunion_Scripts_ParticipantController$Participant.$ctor2 = function(id, name, mail, isRequired, contactPolicy, playerLanguageIsoCode) {
		this.contactPolicyValue = null;
		this.contactPolicyDisplayText = null;
		this.playerLanguageDisplayText = null;
		$Reunion_Web_Models_ParticipantViewModel.$ctor1.call(this, id, name, mail, isRequired, contactPolicy, playerLanguageIsoCode);
		var $t1 = { ntype: 38, type: $Reunion_Scripts_ParticipantController$Participant, name: 'p' };
		var $t2 = { typeDef: $Reunion_Scripts_ParticipantController$Participant, name: 'contactPolicyValue', type: 4, returnType: String, sname: 'contactPolicyValue' };
		var $t3 = { ntype: 23, type: $t2.returnType, expression: $t1, member: $t2 };
		Saltarelle.Utils.StaticReflection.hookSetterOfProperty($Reunion_Scripts_ParticipantController$Participant, String).call(null, this, { ntype: 18, type: Function, returnType: $t3.type, body: $t3, params: [$t1] }, ss.mkdel(this, this.$onContactPolicyValueChanged));
		var $t4 = { ntype: 38, type: $Reunion_Scripts_ParticipantController$Participant, name: 'p' };
		var $t5 = { typeDef: $Reunion_Web_Models_ParticipantViewModel, name: 'playerLanguageIsoCode', type: 4, returnType: String, sname: 'playerLanguageIsoCode' };
		var $t6 = { ntype: 23, type: $t5.returnType, expression: $t4, member: $t5 };
		Saltarelle.Utils.StaticReflection.hookSetterOfProperty($Reunion_Scripts_ParticipantController$Participant, String).call(null, this, { ntype: 18, type: Function, returnType: $t6.type, body: $t6, params: [$t4] }, ss.mkdel(this, this.$onLanguageIsoCodeChanged));
		this.contactPolicyValue = contactPolicy.toString();
		this.playerLanguageIsoCode = playerLanguageIsoCode;
	};
	global.Reunion.Scripts.ParticipantController$Participant = $Reunion_Scripts_ParticipantController$Participant;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Scripts.ParticipantController.ParticipantsScope
	var $Reunion_Scripts_ParticipantController$ParticipantsScope = function() {
		this.participants = [];
		this.participantsAsJson = null;
		this.editrow = null;
		this.editMode = 0;
		this.currentEditingRowIdx = 0;
		AngularJS.Scope.call(this);
	};
	$Reunion_Scripts_ParticipantController$ParticipantsScope.__typeName = 'Reunion.Scripts.ParticipantController$ParticipantsScope';
	global.Reunion.Scripts.ParticipantController$ParticipantsScope = $Reunion_Scripts_ParticipantController$ParticipantsScope;
	////////////////////////////////////////////////////////////////////////////////
	// Reunion.Web.Models.ParticipantViewModel
	var $Reunion_Web_Models_ParticipantViewModel = function() {
		this.id = 0;
		this.name = null;
		this.mail = null;
		this.isRequired = false;
		this.contactPolicy = 0;
		this.playerLanguageIsoCode = null;
	};
	$Reunion_Web_Models_ParticipantViewModel.__typeName = 'Reunion.Web.Models.ParticipantViewModel';
	$Reunion_Web_Models_ParticipantViewModel.$ctor1 = function(id, name, mail, isRequired, contactPolicy, playerLanguageIsoCode) {
		this.id = 0;
		this.name = null;
		this.mail = null;
		this.isRequired = false;
		this.contactPolicy = 0;
		this.playerLanguageIsoCode = null;
		this.id = id;
		this.name = name;
		this.mail = mail;
		this.isRequired = isRequired;
		this.contactPolicy = contactPolicy;
		this.playerLanguageIsoCode = playerLanguageIsoCode;
	};
	global.Reunion.Web.Models.ParticipantViewModel = $Reunion_Web_Models_ParticipantViewModel;
	ss.initEnum($Reunion_Common_Model_ContactPolicyEnum, $asm, { mayContactByWebservice: 0, contactFirstTimePersonally: 1 });
	ss.initEnum($Reunion_Common_Model_PreferenceEnum, $asm, { none: 0, noWay: 1, perfectDay: 2, mayBe: 4, yes: 8 });
	ss.initClass($Reunion_Scripts_$Program, $asm, {});
	ss.initClass($Reunion_Scripts_CalendarController, $asm, {});
	ss.initClass($Reunion_Scripts_CalendarController$CalendarScope, $asm, {}, AngularJS.Scope);
	ss.initClass($Reunion_Scripts_ParticipantController, $asm, {
		$onSaveParticipant: function(event) {
			if (this.$_scope.editMode === 1) {
				var editedParticipant = new $Reunion_Scripts_ParticipantController$Participant(this.$_scope.editrow);
				if (this.$isValid(editedParticipant)) {
					this.$updateParticipant(this.$_scope.currentEditingRowIdx, editedParticipant);
				}
				this.$showLastEditedRow();
				this.$_scope.editMode = 0;
				this.$_scope.editrow.init$1(0, '', '', false, 1, this.$_scope.editrow.playerLanguageIsoCode);
				this.$updateInputRowCoordinates();
			}
		},
		$onDeleteParticipant: function(event, rowId) {
			var id = $Reunion_Scripts_ParticipantController.$getRowIdxFromId(rowId);
			ss.removeAt(this.$_scope.participants, id);
			// update coordinates of input row
			// Deferred because we must wait until DOM has been rebuild 
			this.$_timeout(ss.mkdel(this, this.$updateInputRowCoordinates));
		},
		$onEditParticipant: function(event, rowId) {
			var editedParticipant = new $Reunion_Scripts_ParticipantController$Participant(this.$_scope.editrow);
			if (this.$isValid(editedParticipant)) {
				if (this.$_scope.editMode === 1) {
					this.$updateParticipant(this.$_scope.currentEditingRowIdx, editedParticipant);
					this.$showLastEditedRow();
				}
				else {
					this.$insertOrUpdateParticipant(editedParticipant);
				}
			}
			else if (this.$_scope.editMode === 1) {
				return;
			}
			this.$_scope.editMode = 1;
			var rowIdx = this.$hideRow(rowId);
			var participant = this.$_scope.participants[rowIdx];
			this.$_scope.editrow.init(participant);
			this.$_timeout(ss.mkdel(this, this.$updateInputRowCoordinates));
		},
		$hideRow: function(rowId) {
			var row = $('#' + rowId);
			var rowIdx = $Reunion_Scripts_ParticipantController.$getRowIdxFromId(rowId);
			this.$_scope.currentEditingRowIdx = rowIdx;
			row.addClass('transparentColor');
			var bttns = $('#bttns-' + rowIdx);
			bttns.addClass('hidden');
			return rowIdx;
		},
		$showLastEditedRow: function() {
			var lastEditedRow = $('#row-' + this.$_scope.currentEditingRowIdx);
			lastEditedRow.removeClass('transparentColor');
			var bttns = $('#bttns-' + this.$_scope.currentEditingRowIdx);
			bttns.removeClass('hidden');
		},
		$updateInputRowCoordinates: function() {
			if (this.$_scope.participants.length === 0) {
				return;
			}
			var inputRow = $('#editable-row');
			if (this.$_scope.editMode === 1) {
				var row = $('#row-' + this.$_scope.currentEditingRowIdx);
				var pos = row.offset();
				inputRow.offset(pos);
			}
			else {
				inputRow.css('position', 'static');
				inputRow.css('top', '0');
				inputRow.css('left', '0');
			}
		},
		$onInsertParticipant: function(event) {
			var newParticipant = new $Reunion_Scripts_ParticipantController$Participant(this.$_scope.editrow);
			if (!this.$isValid(newParticipant) || this.$existsParticipant(newParticipant)) {
				return;
			}
			if (this.$insertOrUpdateParticipant(newParticipant)) {
				this.$_scope.editrow.clearNameAndEmail();
			}
		},
		$existsParticipant: function(newParticipant) {
			var mail = newParticipant.mail.toLowerCase();
			return Enumerable.from(this.$_scope.participants).any(function(p) {
				return ss.referenceEquals(p.mail.toLowerCase(), mail);
			});
		},
		$isValid: function(participant) {
			var mailRegex = new RegExp('^([a-z0-9_\\.-]+\\@[\\da-z\\.-]+\\.[a-z\\.]{2,6})$', 'gm');
			return ss.isValue(participant) && ss.isValue(participant.name) && participant.name.length > 0 && mailRegex.test(participant.mail);
			// we do need brackets due to a JavaScript bug in Chrome
		},
		$updateParticipant: function(rowIdx, participant) {
			var participantIdx = Enumerable.from(this.$_scope.participants).indexOf(function(p) {
				return ss.referenceEquals(p.mail, participant.mail);
			});
			if (rowIdx >= 0 && rowIdx < this.$_scope.participants.length && (participantIdx === rowIdx || participantIdx < 0)) {
				this.$_scope.participants[rowIdx] = participant;
			}
		},
		$insertOrUpdateParticipant: function(participant) {
			var participantIdx = Enumerable.from(this.$_scope.participants).indexOf(function(p) {
				return ss.referenceEquals(p.mail, participant.mail);
			});
			if (participantIdx >= 0) {
				this.$_scope.participants[participantIdx] = participant;
			}
			else {
				this.$_scope.participants.push(participant);
				return true;
			}
			return false;
		},
		$initParticipantsByJsonString: function(participantsAsJson) {
			if (ss.isNullOrEmptyString(participantsAsJson)) {
				return;
			}
			var newParticipants = JSON.parse(participantsAsJson);
			var participants = this.$_scope.participants;
			ss.clear(participants);
			ss.arrayAddRange(participants, Enumerable.from(newParticipants).select(function(p) {
				return new $Reunion_Scripts_ParticipantController$Participant.$ctor1(p);
			}));
		},
		$onParticipantsCollectionChanged: function(newValue, oldValue) {
			var json = JSON.stringify(Enumerable.from(newValue).select(function(p) {
				return new $Reunion_Web_Models_ParticipantViewModel.$ctor1(p.id, p.name, p.mail, p.isRequired, p.contactPolicy, p.playerLanguageIsoCode);
			}).toArray());
			if (!ss.referenceEquals(json, this.$_scope.participantsAsJson)) {
				this.$_scope.participantsAsJson = json;
			}
		}
	});
	ss.initEnum($Reunion_Scripts_ParticipantController$EditModeEnum, $asm, { insert: 0, edit: 1 });
	ss.initClass($Reunion_Web_Models_ParticipantViewModel, $asm, {
		isEqual: function(other) {
			return ss.referenceEquals(this.mail, other.mail) && ss.referenceEquals(this.name, other.name) && ss.referenceEquals(this.playerLanguageIsoCode, other.playerLanguageIsoCode) && this.contactPolicy === other.contactPolicy && this.isRequired === other.isRequired;
		}
	});
	$Reunion_Web_Models_ParticipantViewModel.$ctor1.prototype = $Reunion_Web_Models_ParticipantViewModel.prototype;
	ss.initClass($Reunion_Scripts_ParticipantController$Participant, $asm, {
		getLanguageIfNotDefault: function(defaultLanguageIsoCode) {
			if (ss.referenceEquals(defaultLanguageIsoCode, this.playerLanguageIsoCode)) {
				return '';
			}
			return this.playerLanguageDisplayText;
		},
		getContactPolicyIfNotDefault: function() {
			if (this.contactPolicy === 1) {
				return '';
			}
			return this.contactPolicyDisplayText;
		},
		$getDisplayTextOfLanguage: function(isoCode) {
			return $('.lang option[value="' + isoCode + '"]').text();
		},
		$getDisplayTextOfContactPolicy: function(contactPolicy) {
			return $('.contactPolicy option[value="' + contactPolicy + '"]').text();
		},
		getLocalizedRequiredStatus: function(requiredDisplayText) {
			return (this.isRequired ? requiredDisplayText : '');
		},
		init$1: function(id, name, mail, isRequired, contactPolicy, playerLanguageIsoCode) {
			this.id = id;
			this.name = name;
			this.mail = mail;
			this.isRequired = isRequired;
			this.playerLanguageIsoCode = playerLanguageIsoCode;
			var $t1 = { ntype: 38, type: $Reunion_Scripts_ParticipantController$Participant, name: 'p' };
			var $t2 = { typeDef: $Reunion_Scripts_ParticipantController$Participant, name: 'contactPolicyValue', type: 4, returnType: String, sname: 'contactPolicyValue' };
			var $t3 = { ntype: 23, type: $t2.returnType, expression: $t1, member: $t2 };
			Saltarelle.Utils.StaticReflection.hookSetterOfProperty($Reunion_Scripts_ParticipantController$Participant, String).call(null, this, { ntype: 18, type: Function, returnType: $t3.type, body: $t3, params: [$t1] }, ss.mkdel(this, this.$onContactPolicyValueChanged));
			var $t4 = { ntype: 38, type: $Reunion_Scripts_ParticipantController$Participant, name: 'p' };
			var $t5 = { typeDef: $Reunion_Web_Models_ParticipantViewModel, name: 'playerLanguageIsoCode', type: 4, returnType: String, sname: 'playerLanguageIsoCode' };
			var $t6 = { ntype: 23, type: $t5.returnType, expression: $t4, member: $t5 };
			Saltarelle.Utils.StaticReflection.hookSetterOfProperty($Reunion_Scripts_ParticipantController$Participant, String).call(null, this, { ntype: 18, type: Function, returnType: $t6.type, body: $t6, params: [$t4] }, ss.mkdel(this, this.$onLanguageIsoCodeChanged));
			this.contactPolicyValue = contactPolicy.toString();
		},
		init: function(participant) {
			this.init$1(participant.id, participant.name, participant.mail, participant.isRequired, participant.contactPolicy, participant.playerLanguageIsoCode);
		},
		$onLanguageIsoCodeChanged: function(newLanguage) {
			this.playerLanguageDisplayText = this.$getDisplayTextOfLanguage(newLanguage);
		},
		$onContactPolicyValueChanged: function(newConatcPolicyValue) {
			this.contactPolicy = parseInt(newConatcPolicyValue);
			this.contactPolicyDisplayText = this.$getDisplayTextOfContactPolicy(this.contactPolicy);
		},
		isEqual$1: function(participant) {
			return ss.referenceEquals(this.name, participant.name) && ss.referenceEquals(this.mail, participant.mail) && this.isRequired === participant.isRequired && this.contactPolicy === participant.contactPolicy && ss.referenceEquals(this.playerLanguageDisplayText, participant.playerLanguageDisplayText) && ss.referenceEquals(this.playerLanguageIsoCode, participant.playerLanguageIsoCode);
		},
		clearNameAndEmail: function() {
			this.name = '';
			this.mail = '';
		}
	}, $Reunion_Web_Models_ParticipantViewModel);
	$Reunion_Scripts_ParticipantController$Participant.$ctor1.prototype = $Reunion_Scripts_ParticipantController$Participant.$ctor2.prototype = $Reunion_Scripts_ParticipantController$Participant.prototype;
	ss.initClass($Reunion_Scripts_ParticipantController$ParticipantsScope, $asm, {}, AngularJS.Scope);
	ss.setMetadata($Reunion_Scripts_CalendarController, { attr: [new AngularJS.InjectAttribute(['$scope'])] });
	ss.setMetadata($Reunion_Scripts_ParticipantController, { attr: [new AngularJS.InjectAttribute(['$scope', '$cookies', '$timeout'])] });
	$Reunion_Scripts_$Program.$main();
})();
