﻿using TASVideos.WikiEngine;

namespace TASVideos.WikiModules;

[WikiModule(ModuleNames.NoGameName)]
public class NoGame(ApplicationDbContext db) : WikiViewComponent
{
	public MissingModel Missing { get; set; } = new();

	public async Task<IViewComponentResult> InvokeAsync()
	{
		Missing = new MissingModel
		{
			Publications = await db.Publications
				.Where(p => p.GameId == -1)
				.OrderBy(p => p.Id)
				.Select(p => new MissingModel.Entry(p.Id, p.Title))
				.ToListAsync(),
			Submissions = await db.Submissions
				.Where(s => s.GameId == null || s.GameId < 1)
				.ThatAreInActive()
				.OrderBy(p => p.Id)
				.Select(s => new MissingModel.Entry(s.Id, s.Title))
				.ToListAsync()
		};

		return View();
	}

	public class MissingModel
	{
		public IReadOnlyCollection<Entry> Publications { get; init; } = [];
		public IReadOnlyCollection<Entry> Submissions { get; init; } = [];
		public record Entry(int Id, string Title);
	}
}