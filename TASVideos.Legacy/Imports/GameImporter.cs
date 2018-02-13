﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using FastMember;
using Microsoft.EntityFrameworkCore;

using TASVideos.Data;
using TASVideos.Data.Entity.Game;
using TASVideos.Legacy.Data.Site;

namespace TASVideos.Legacy.Imports
{
	public static class GameImporter
	{
		public static void Import(
			ApplicationDbContext context,
			NesVideosSiteContext legacySiteContext)
		{
			var legacyGameNames = legacySiteContext.GameNames.ToList();

			var games = new List<Game>();
			foreach (var legacyGameName in legacyGameNames)
			{
				var game = new Game
				{
					Id = legacyGameName.Id,
					SystemId =  legacyGameName.SystemId,
					GoodName = legacyGameName.GoodName,
					DisplayName = legacyGameName.DisplayName,
					Abbreviation = legacyGameName.Abbreviation,
					SearchKey = legacyGameName.SearchKey,
					YoutubeTags = legacyGameName.YoutubeTags,
					CreateTimeStamp = DateTime.UtcNow,
					LastUpdateTimeStamp = DateTime.UtcNow
				};

				games.Add(game);
			}

			var copyParams = new[]
			{
				nameof(Game.Id),
				nameof(Game.SystemId),
				nameof(Game.GoodName),
				nameof(Game.DisplayName),
				nameof(Game.Abbreviation),
				nameof(Game.SearchKey),
				nameof(Game.YoutubeTags),
				nameof(Game.CreateTimeStamp),
				nameof(Game.LastUpdateTimeStamp)
			};

			using (var sqlCopy = new SqlBulkCopy(context.Database.GetDbConnection().ConnectionString, SqlBulkCopyOptions.KeepIdentity))
			{
				sqlCopy.DestinationTableName = $"[{nameof(ApplicationDbContext.Games)}]";
				sqlCopy.BatchSize = 10000;

				foreach (var param in copyParams)
				{
					sqlCopy.ColumnMappings.Add(param, param);
				}

				using (var reader = ObjectReader.Create(games, copyParams))
				{
					sqlCopy.WriteToServer(reader);
				}
			}
		}
	}
}