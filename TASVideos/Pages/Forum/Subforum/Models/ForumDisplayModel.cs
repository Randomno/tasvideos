﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TASVideos.Core;
using TASVideos.Data.Entity.Forum;

namespace TASVideos.Pages.Forum.Subforum.Models;

public class ForumDisplayModel
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string? Description { get; set; }

	public PageOf<ForumTopicEntry> Topics { get; set; } = PageOf<ForumTopicEntry>.Empty();

	public class ForumTopicEntry
	{
		[TableIgnore]
		public int Id { get; set; }

		[DisplayName("Topics")]
		public string Title { get; set; } = "";

		[MobileHide]
		[DisplayName("Replies")]
		public int PostCount { get; set; }

		[MobileHide]
		[DisplayName("Author")]
		public string? CreateUserName { get; set; }

		[TableIgnore]
		public DateTime CreateTimestamp { get; set; }

		[TableIgnore]
		public ForumTopicType Type { get; set; }

		[TableIgnore]
		public bool IsLocked { get; set; }

		[TableIgnore]
		public Todo? LastPost { get; set; }

		[DisplayName("Last Post")]
		public string? Dummy { get; set; }
	}

	public class Todo
	{
		public int Id { get; set; }
		public string? PosterName { get; set; }
		public DateTime CreateTimestamp { get; set; }
	}
}
