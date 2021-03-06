﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Reunion.Web.Resources;

namespace Reunion.Web.Common
{
	/// <summary>
	/// the assigned property must be a DateTime and younger than current date
	/// </summary>
	public class MustBeInFutureAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null
			    || !(value is DateTime)
			    || ((DateTime) value) <= DateTime.Today)
			{
				return new ValidationResult(Resource1.DeadlineMustBeInFuture);
			}

			return ValidationResult.Success;
		}
	}
}