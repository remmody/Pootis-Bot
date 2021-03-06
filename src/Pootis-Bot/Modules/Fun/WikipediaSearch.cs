﻿using System.Text;
using System.Threading.Tasks;
using WikiDotNet;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Pootis_Bot.Helpers;
using Pootis_Bot.Preconditions;

namespace Pootis_Bot.Modules.Fun
{
	public class WikipediaSearch : ModuleBase<SocketCommandContext>
	{
		// Module Information
		// Original Author  - Voltstro
		// Description      - Wikipedia search
		// Contributors     - Voltstro, 

		//This uses a library called Wiki.Net
		//It was developed by my good friend EternalClickbait and partially by me(Voltstro).
		//You can get it here: https://github.com/Voltstro-Studios/Wiki.Net

		[Command("wiki", RunMode = RunMode.Async)]
		[Alias("wikipedia")]
		[Summary("Searches Wikipedia")]
		[Cooldown(5)]
		public async Task Wikipedia([Remainder] string search = "")
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				await Context.Channel.SendMessageAsync("A search needs to be entered! E.G: `wiki C#`");
				return;
			}

			await WikiSearch(search, Context.Channel);
		}

		[Command("wiki", RunMode = RunMode.Async)]
		[Alias("wikipedia")]
		[Summary("Searches Wikipedia")]
		[Cooldown(5)]
		public async Task Wikipedia(int maxSearchResults = 15, [Remainder] string search = "")
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				await Context.Channel.SendMessageAsync("A search needs to be entered! E.G: `wiki C#`");
				return;
			}

			if (maxSearchResults > FunCmdsConfig.wikipediaMaxSearches)
			{
				await Context.Channel.SendMessageAsync(
					$"The max search amount you have put in is too high! It has to be below {FunCmdsConfig.wikipediaMaxSearches}.");
				return;
			}

			await WikiSearch(search, Context.Channel, maxSearchResults);
		}

		private async Task WikiSearch(string search, ISocketMessageChannel channel, int maxSearch = 10)
		{
			EmbedBuilder embed = new EmbedBuilder();

			StringBuilder sb = new StringBuilder();
			embed.WithTitle($"Wikipedia Search '{search}'");
			embed.WithColor(FunCmdsConfig.wikipediaSearchColor);
			embed.WithFooter($"Search by {Context.User}", Context.User.GetAvatarUrl());
			embed.WithCurrentTimestamp();
			embed.WithDescription("Searching Wikipedia...");

			RestUserMessage message = await channel.SendMessageAsync("", false, embed.Build());

			WikiSearchResponse response = WikiSearcher.Search(search, new WikiSearchSettings
			{
				ResultLimit = maxSearch
			});

			foreach (WikiSearchResult result in response.Query.SearchResults)
			{
				string link =
					$"**[{result.Title}]({result.ConstantUrl("en")})** (Words: {result.WordCount})\n{result.Preview}\n\n";

				//There is a character limit of 2048, so lets make sure we don't hit that
				if (sb.Length >= 2048) continue;

				if (sb.Length + link.Length >= 2048) continue;

				sb.Append(link);
			}

			embed.WithDescription(sb.ToString());
			embed.WithCurrentTimestamp();

			await MessageUtils.ModifyMessage(message, embed);
		}
	}
}