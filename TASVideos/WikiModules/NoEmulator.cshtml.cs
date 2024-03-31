﻿using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.NoEmulator)]
public class NoEmulator(ApplicationDbContext db) : WikiViewComponent
{
	public NoGame.MissingModel Missing { get; set; } = new();

	public async Task<IViewComponentResult> InvokeAsync()
	{
		Missing = new NoGame.MissingModel
		{
			Publications = await db.Publications
				.Where(p => string.IsNullOrEmpty(p.EmulatorVersion))
				.OrderBy(p => p.Id)
				.Select(p => new NoGame.MissingModel.Entry(p.Id, p.Title))
				.ToListAsync(),
			Submissions = await db.Submissions
				.Where(s => string.IsNullOrEmpty(s.EmulatorVersion))
				.OrderBy(s => s.Id)
				.Select(s => new NoGame.MissingModel.Entry(s.Id, s.Title))
				.ToListAsync()
		};
		return View();
	}
}