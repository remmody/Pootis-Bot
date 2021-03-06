﻿using System;
using System.IO;
using Newtonsoft.Json;
using Pootis_Bot.Core.Logging;
using Pootis_Bot.Entities;
using Pootis_Bot.Services.Audio.Music;
using Pootis_Bot.Structs.Config;

namespace Pootis_Bot.Core
{
	/// <summary>
	/// Manages the bot's config
	/// </summary>
	public static class Config
	{
		private const string ConfigFile = "Config.json";

		private const string ConfigVersion = "13";

		/// <summary>
		/// The bot's config
		/// </summary>
		public static readonly ConfigFile bot;

		static Config()
		{
			if (!Directory.Exists(Global.ResourcesDirectory)) //Creates the Resources folder if it doesn't exist.
				Directory.CreateDirectory(Global.ResourcesDirectory);

			//If the config.json file doesn't exist it create a new one.
			if (!File.Exists(Global.ResourcesDirectory + "/" + ConfigFile))
			{
				bot = NewConfig();

				SaveConfig();

				Logger.Warn("Config.json was created. Is this your first time running?");
			}
			else
			{
				string json =
					File.ReadAllText(Global.ResourcesDirectory + "/" + ConfigFile); //If it does exist then it continues like normal.
				bot = JsonConvert.DeserializeObject<ConfigFile>(json);

				if (!string.IsNullOrWhiteSpace(bot.ConfigVersion) && bot.ConfigVersion == ConfigVersion) return;

				bot.ConfigVersion = ConfigVersion;
				SaveConfig();
				Logger.Warn("Updated config to version " + ConfigVersion);
			}
		}

		public static ConfigFile NewConfig()
		{
			ConfigFile newConfig = new ConfigFile
			{
				ConfigVersion = ConfigVersion,
				BotName = "CSharp Bot",
				BotPrefix = "$",
				BotToken = "",
				ResourceFilesFormatting = Formatting.Indented,
				ReportErrorsToOwner = false,
				ReportGuildEventsToOwner = false,
				TwitchStreamingSite = "https://www.twitch.tv/Voltstro",
				LevelUpCooldown = 15,
				CheckConnectionStatus = true,
				CheckConnectionStatusInterval = 60000,
				DefaultGameMessage = "Use $help for help.",
				Apis = new ConfigApis(),
				AudioSettings = new ConfigAudio
				{
					AudioServicesEnabled = false,
					LogPlayStopSongToConsole = true,
					MaxVideoTime = new TimeSpan(0, 7, 0),
					MusicFileFormat = MusicFileFormat.Mp3,
					MusicFolderLocation = "Music/",
					ExternalDirectory = "External/"
				},
				VoteSettings = new VoteSettings
				{
					MaxRunningVotesPerGuild = 3,
					MaxVoteTime = new TimeSpan(7, 0, 0, 0)
				}
			};

			return newConfig;
		}

		/// <summary>
		/// Saves the config, DUH!
		/// </summary>
		public static void SaveConfig()
		{
			string json = JsonConvert.SerializeObject(bot, Formatting.Indented);
			File.WriteAllText(Global.ResourcesDirectory + "/" + ConfigFile, json);
		}
	}
}