﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Pootis_Bot.Core;
using Pootis_Bot.Core.Managers;
using Pootis_Bot.Entities;
using Pootis_Bot.Preconditions;

namespace Pootis_Bot.Modules.Basic
{
	public class BasicCommands : ModuleBase<SocketCommandContext>
	{
		// Module Information
		// Original Author  - Creepysin
		// Description      - Basic, simple commands
		// Contributors     - Creepysin, HelloHowIsItGoing

		[Command("hello")]
		[Summary("Displays the 'hello' message")]
		[Cooldown(5)]
		public async Task Hello()
		{
			EmbedBuilder embed = new EmbedBuilder();
			embed.WithTitle("Hello!");
			embed.WithDescription("Hello! My name is " + Config.bot.BotName + "!\n\n**__Links__**" +
			                      $"\n:computer: [Commands]({Global.websiteCommands})" +
			                      $"\n<:GitHub:529571722991763456> [Github Page]({Global.githubPage})" +
			                      $"\n:bookmark: [Documentation]({Global.websiteHome})" +
			                      $"\n<:Discord:529572497130127360> [Creepysin Server]({Global.discordServers[0]})" +
			                      "\n\nRunning Pootis-Bot version: " + Global.version +
			                      $"\nThis project is licensed under the [MIT license]({Global.githubPage}/blob/master/LICENSE.md)");
			embed.WithColor(new Color(241, 196, 15));

			await Context.Channel.SendMessageAsync("", false, embed.Build());
		}

		[Command("server")]
		[Summary("Gets details about the server you are in")]
		[Cooldown(5)]
		public async Task ServerGuild()
		{
			SocketGuildUser guildUser = (SocketGuildUser) Context.User;

			EmbedBuilder embed = new EmbedBuilder();
			embed.WithTitle("Server Details");
			embed.WithDescription("**__Server__**" +
			                      $"\n**Server Name:** {guildUser.Guild}" +
			                      $"\n**Server Id:** {guildUser.Guild.Id}" +
			                      $"\n**Server Member Count:** {guildUser.Guild.MemberCount}" +
			                      "\n\n**__Server Owner__**" +
			                      $"\n**Owner Name: **{guildUser.Guild.Owner.Username}" +
			                      $"\n**Owner Id: ** {guildUser.Guild.OwnerId}");
			embed.WithThumbnailUrl(guildUser.Guild.IconUrl);
			embed.WithColor(new Color(241, 196, 15));

			await Context.Channel.SendMessageAsync("", false, embed.Build());
		}

		[Command("top10")]
		[Summary("Get the top 10 users in the server")]
		[Cooldown(5)]
		public async Task Top10()
		{
			List<UserAccount> serverUsers = new List<UserAccount>();
			foreach (SocketGuildUser user in Context.Guild.Users)
				if (!user.IsBot && !user.IsWebhook)
					serverUsers.Add(UserAccountsManager.GetAccount(user));

			serverUsers.Sort(new SortUserAccount());
			serverUsers.Reverse();

			StringBuilder format = new StringBuilder();
			format.Append("```csharp\n 📋 Top 10 Server User Position\n ========================\n");

			int count = 1;
			foreach (UserAccount user in serverUsers.Where(user => count <= 10))
			{
				format.Append(
					$"\n [{count}] -- # {Context.Client.GetUser(user.Id)}\n         └ Level: {user.LevelNumber}\n         └ Xp: {user.Xp}");
				count++;
			}

			UserAccount userAccount = UserAccountsManager.GetAccount((SocketGuildUser) Context.User);
			format.Append(
				$"\n------------------------\n 😊 Your Position: {serverUsers.IndexOf(userAccount) + 1}      Your Level: {userAccount.LevelNumber}      Your Xp: {userAccount.Xp}```");

			await Context.Channel.SendMessageAsync(format.ToString());
		}

		[Command("top10total", RunMode = RunMode.Async)]
		[Summary("Gets the top user in Pootis-Bot")]
		[Cooldown(10)]
		public async Task Top10Total()
		{
			//Get all accounts Pootis-Bot has and sort them
			List<UserAccount> totalUsers = UserAccountsManager.GetAllUserAccounts().ToList();
			totalUsers.Sort(new SortUserAccount());
			totalUsers.Reverse();

			StringBuilder format = new StringBuilder();
			format.Append("```csharp\n 📋 Top 10 Pootis-Bot Accounts\n ========================\n");

			int count = 1;
			foreach (UserAccount user in totalUsers.Where(user => count <= 10))
			{
				format.Append(
					$"\n [{count}] -- # {Context.Client.GetUser(user.Id)}\n         └ Level: {user.LevelNumber}\n         └ Xp: {user.Xp}");
				count++;
			}

			UserAccount userAccount = UserAccountsManager.GetAccount((SocketGuildUser) Context.User);
			format.Append(
				$"\n------------------------\n 😊 Your Position: {totalUsers.IndexOf(userAccount) + 1}      Your Level: {userAccount.LevelNumber}      Your Xp: {userAccount.Xp}```");

			await Context.Channel.SendMessageAsync(format.ToString());
		}

		private class SortUserAccount : IComparer<UserAccount>
		{
			public int Compare(UserAccount x, UserAccount y)
			{
				if ((y != null) && (x != null) && (x.LevelNumber > y.LevelNumber))
					return 1;
				if ((y != null) && (x != null) && (x.LevelNumber < y.LevelNumber))
					return -1;
				return 0;
			}
		}
	}
}