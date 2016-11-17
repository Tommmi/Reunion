using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AngularJS;
using jQueryApi;
using Reunion.Common.Model;
using Saltarelle.MultiSelectionCalendar;
using Saltarelle.Utils;

namespace Reunion.Scripts
{
	/// <summary>
	/// Angular controller for the multiselection calendar in Reunion
	/// </summary>
	[Reflectable, Inject("$scope")]
	public class CalendarController
	{
		private readonly CalendarScope _scope;

		public class CalendarScope : Scope
		{
			/// <summary>
			/// when marking some dates in the multiselection calendar, this index specifies the
			/// meaning of the selected dates.
			/// </summary>
			public PreferenceEnum currentCalendarSelectionIdx = PreferenceEnum.Yes;
		}


		public CalendarController(Scope _scope)
		{
			var scope = this._scope = Utils.Enhance<CalendarScope, Scope>(_scope); // see TUtils
		}

	}
}
