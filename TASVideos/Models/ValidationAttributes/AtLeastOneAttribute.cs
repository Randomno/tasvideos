﻿using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace TASVideos.Models
{
	public class AtLeastOneAttribute : ValidationAttribute
	{
		public override bool IsValid(object value)
		{
			if (value is IEnumerable list)
			{
				foreach (var item in list)
				{
					return true;
				}
			}

			return false;
		}
	}
}
