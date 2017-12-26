using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TASVideos.Data.Entity;

namespace TASVideos.Models
{
	/// <summary>
	/// Represents a Role entry for the purpose of display
	/// </summary>
	public class RoleDisplayViewModel
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		[Display(Name = "Permissions")]
		public IEnumerable<PermissionTo> Permissions { get; set; } = new List<PermissionTo>();

		[Display(Name = "Related Links")]
		public IEnumerable<string> Links { get; set; } = new List<string>();
	}

	/// <summary>
	/// Represents a Role entry for the purpose of editing
	/// </summary>
	public class RoleEditViewModel
	{
		public int? Id { get; set; }

		[Required]
		[StringLength(50)]
		[Display(Name = "Name")]
		public string Name { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

		[AtLeastOne(ErrorMessage = "At least one permission is required.")]
		[Display(Name = "Selected Permissions")]
		public IEnumerable<int> SelectedPermissions { get; set; } = new List<int>();

		[Display(Name = "Available Permissions")]
		public IEnumerable<SelectListItem> AvailablePermissions { get; set; } = new List<SelectListItem>();

		[Display(Name = "Selected Assignable Permissions")]
		public IEnumerable<int> SelectedAssignablePermissions { get; set; } = new List<int>();

		[Display(Name = "Available Assignable Permissions")]
		public IEnumerable<SelectListItem> AvailableAssignablePermissions { get; set; } = new List<SelectListItem>();

		[Display(Name = "Related Links")]
		public IEnumerable<string> Links { get; set; } = new List<string>();

		public string LinksStr
		{
			get => string.Join(",", Links);
			set => Links = !string.IsNullOrWhiteSpace(value) ? value.Split(',') : new string[] { };
		}
	}

	/// <summary>
	/// Represents a concise view of aRole for the User profile screen
	/// </summary>
	public class RoleBasicDisplay
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}
}