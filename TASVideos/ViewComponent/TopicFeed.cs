﻿using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using TASVideos.Data.Entity;
using TASVideos.Tasks;

namespace TASVideos.ViewComponents
{
    public class TopicFeed : ViewComponent
	{
		private readonly ForumTasks _forumTasks;

		public TopicFeed(ForumTasks forumTasks)
		{
			_forumTasks = forumTasks;
		}

		public async Task<IViewComponentResult> InvokeAsync(WikiPage pageData, string pp)
		{
			int limit = ParamHelper.GetInt(pp, "l") ?? 5;
			int topicId = ParamHelper.GetInt(pp, "t")
				?? throw new ArgumentException("the parameter t can not be null");

			var model = new TopicFeedModel
			{
				RightAlign = ParamHelper.HasParam(pp, "right"),
				Heading = ParamHelper.GetValueFor(pp, "heading"),
				HideContent = ParamHelper.HasParam(pp, "hidecontent"),
				Posts = await _forumTasks.GetTopicFeed(topicId, limit)
			};

			return View(model);
		}
    }
}
