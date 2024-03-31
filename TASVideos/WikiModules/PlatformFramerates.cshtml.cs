﻿using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.PlatformFramerates)]
public class PlatformFramerates(ApplicationDbContext db) : WikiViewComponent
{
	public List<PlatformFramerateModel> Framerates { get; set; } = [];

	public async Task<IViewComponentResult> InvokeAsync()
	{
		Framerates = await db.GameSystemFrameRates
			.Where(sf => !sf.Obsolete)
			.Select(sf => new PlatformFramerateModel
			{
				SystemCode = sf.System!.Code,
				FrameRate = sf.FrameRate,
				RegionCode = sf.RegionCode,
				Preliminary = sf.Preliminary
			})
			.OrderBy(sf => sf.SystemCode)
			.ThenBy(sf => sf.RegionCode)
			.ToListAsync();
		return View();
	}

	public class PlatformFramerateModel
	{
		[Display(Name = "System")]
		public string SystemCode { get; init; } = "";

		[Display(Name = "Region")]
		public string RegionCode { get; init; } = "";

		[Display(Name = "Framerate")]
		public double FrameRate { get; init; }

		[Display(Name = "Preliminary or approximate")]
		public bool Preliminary { get; init; }
	}
}