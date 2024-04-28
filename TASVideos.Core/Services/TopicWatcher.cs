﻿using TASVideos.Core.Services.Email;
using TASVideos.Core.Settings;

namespace TASVideos.Core.Services;

public interface ITopicWatcher
{
	/// <summary>
	/// Returns all topics the user is currently watching.
	/// </summary>
	Task<ICollection<WatchedTopic>> UserWatches(int userId);

	/// <summary>
	/// Notifies everyone watching a topic (other than the poster)
	/// that a new post has been created.
	/// </summary>
	Task NotifyNewPost(int postId, int topicId, string topicTitle, int posterId);

	/// <summary>
	/// Marks that a user has seen a topic with new posts.
	/// </summary>
	Task MarkSeen(int topicId, int userId);

	/// <summary>
	/// Allows a user to watch a topic.
	/// </summary>
	Task WatchTopic(int topicId, int userId, bool canSeeRestricted);

	/// <summary>
	/// Removes a topic from the user's watched topic list.
	/// </summary>
	Task UnwatchTopic(int topicId, int userId);

	/// <summary>
	/// Removes every topic from the user's watched topic list.
	/// </summary>
	Task UnwatchAllTopics(int userId);

	/// <summary>
	/// Returns whether the user watches a specific topic.
	/// </summary>
	Task<bool> IsWatchingTopic(int topicId, int userId);
}

internal class TopicWatcher(
	IEmailService emailService,
	ApplicationDbContext db,
	AppSettings appSettings)
	: ITopicWatcher
{
	private readonly string _baseUrl = appSettings.BaseUrl;

	public async Task<ICollection<WatchedTopic>> UserWatches(int userId)
	{
		return await db.ForumTopicWatches
			.ForUser(userId)
			.Select(tw => new WatchedTopic(
				tw.ForumTopic!.ForumPosts
					.Where(fp => fp.Id == tw.ForumTopic.ForumPosts.Max(fpp => fpp.Id))
					.Select(fp => fp.CreateTimestamp)
					.First(),
				tw.IsNotified,
				tw.ForumTopic.ForumId,
				tw.ForumTopic!.Forum!.Name,
				tw.ForumTopicId,
				tw.ForumTopic!.Title))
			.ToListAsync();
	}

	public async Task NotifyNewPost(int postId, int topicId, string topicTitle, int posterId)
	{
		var watches = await db.ForumTopicWatches
			.Include(w => w.User)
			.Where(w => w.ForumTopicId == topicId)
			.Where(w => w.UserId != posterId)
			.Where(w => !w.IsNotified)
			.ToListAsync();

		if (watches.Any())
		{
			await emailService
				.TopicReplyNotification(
					watches.Select(w => w.User!.Email),
					new TopicReplyNotificationTemplate(postId, topicId, topicTitle, _baseUrl));

			foreach (var watch in watches)
			{
				watch.IsNotified = true;
			}

			await db.SaveChangesAsync();
		}
	}

	public async Task MarkSeen(int topicId, int userId)
	{
		var watchedTopic = await db.ForumTopicWatches
			.SingleOrDefaultAsync(w => w.UserId == userId && w.ForumTopicId == topicId);

		if (watchedTopic is not null && watchedTopic.IsNotified)
		{
			watchedTopic.IsNotified = false;
			await db.SaveChangesAsync();
		}
	}

	public async Task WatchTopic(int topicId, int userId, bool canSeeRestricted)
	{
		var topic = await db.ForumTopics
			.ExcludeRestricted(canSeeRestricted)
			.SingleOrDefaultAsync(t => t.Id == topicId);

		if (topic is null)
		{
			return;
		}

		var watch = await db.ForumTopicWatches
			.ExcludeRestricted(canSeeRestricted)
			.SingleOrDefaultAsync(w => w.UserId == userId
				&& w.ForumTopicId == topicId);

		if (watch is null)
		{
			db.ForumTopicWatches.Add(new ForumTopicWatch
			{
				UserId = userId,
				ForumTopicId = topicId
			});

			await db.SaveChangesAsync();
		}
	}

	public async Task UnwatchTopic(int topicId, int userId)
	{
		var watch = await db.ForumTopicWatches
			.SingleOrDefaultAsync(w => w.UserId == userId
				&& w.ForumTopicId == topicId);

		if (watch is not null)
		{
			db.ForumTopicWatches.Remove(watch);

			try
			{
				await db.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				// Do nothing
				// 1) if a watch is already removed, we are done
				// 2) if a watch was updated (for instance, someone posted in the topic),
				//        there isn't much we can do other than reload the page anyway with an error.
				//        An error would only be modestly helpful anyway, and wouldn't save clicks
				//        However, this would be a nice to have one day
			}
		}
	}

	public async Task UnwatchAllTopics(int userId)
	{
		var watches = await db.ForumTopicWatches
			.Where(w => w.UserId == userId)
			.ToListAsync();
		db.ForumTopicWatches.RemoveRange(watches);
		try
		{
			await db.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			// Do nothing
			// See UnwatchTopic for why
		}
	}

	public async Task<bool> IsWatchingTopic(int topicId, int userId)
	{
		return (await db.ForumTopicWatches
			.SingleOrDefaultAsync(w => w.UserId == userId && w.ForumTopicId == topicId)) is not null;
	}
}

/// <summary>
/// Represents a watched forum topic
/// </summary>
public record WatchedTopic(
	DateTime LastPostTimestamp,
	bool IsNotified,
	int ForumId,
	string ForumTitle,
	int TopicId,
	string TopicTitle);
