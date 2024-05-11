﻿using TASVideos.Data.Entity.Forum;

namespace TASVideos.Pages.Forum.Topics;

[RequirePermission(PermissionTo.SetTopicType)]
public class SetTypeModel(ApplicationDbContext db) : BaseForumModel
{
	[FromRoute]
	public int TopicId { get; set; }

	[BindProperty]
	public ForumTopicType Type { get; set; }

	[BindProperty]
	public string TopicTitle { get; set; } = "";

	[BindProperty]
	public int ForumId { get; set; }

	[BindProperty]
	public string ForumName { get; set; } = "";

	public async Task<IActionResult> OnGet()
	{
		var seeRestricted = User.Has(PermissionTo.SeeRestrictedForums);

		var topic = await db.ForumTopics
			.Include(t => t.Forum)
			.ExcludeRestricted(seeRestricted)
			.Where(t => t.Id == TopicId)
			.SingleOrDefaultAsync();

		if (topic is null)
		{
			return NotFound();
		}

		TopicTitle = topic.Title;
		Type = topic.Type;
		ForumId = topic.ForumId;
		ForumName = topic.Forum!.Name;
		return Page();
	}

	public async Task<IActionResult> OnPost()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var topic = await db.ForumTopics
			.ExcludeRestricted(UserCanSeeRestricted)
			.Where(t => t.Id == TopicId)
			.SingleOrDefaultAsync();

		if (topic is null)
		{
			return NotFound();
		}

		topic.Type = Type;

		await ConcurrentSave(db, $"Topic set to {Type}", "Unable to set the topic type");
		return RedirectToPage("Index", new { topic.Id });
	}
}
