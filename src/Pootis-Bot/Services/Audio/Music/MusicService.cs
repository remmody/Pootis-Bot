﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Pootis_Bot.Core;
using Pootis_Bot.Core.Logging;
using Pootis_Bot.Entities;
using Pootis_Bot.Helpers;
using Pootis_Bot.Services.Audio.Music.Playback;
using Pootis_Bot.Services.Google.YouTube;
using Pootis_Bot.Structs.Config;

namespace Pootis_Bot.Services.Audio.Music
{
	/// <summary>
	/// This class handles playing music through a voice chat, it handles connecting and leaving a chat.
	/// <para>It searches and downloads to play music from YouTube, if the song hasn't already been downloaded.</para>
	/// </summary>
	public class MusicService
	{
		public static readonly List<ServerMusicItem> currentChannels = new List<ServerMusicItem>();
		private readonly IYouTubeSearcher youTubeSearcher;

		public MusicService(IYouTubeSearcher searcher)
		{
			youTubeSearcher = searcher;
		}

		/// <summary>
		/// Joins a voice channel
		/// </summary>
		/// <param name="guild">The <see cref="IGuild"/> where the voice channel is in</param>
		/// <param name="target">The target <see cref="IVoiceChannel"/> to join</param>
		/// <param name="channel">The <see cref="IMessageChannel"/> to log messages to</param>
		/// <param name="user">The <see cref="IUser"/> who requested this command</param>
		/// <returns></returns>
		public async Task JoinAudio(IGuild guild, IVoiceChannel target, IMessageChannel channel, IUser user)
		{
			if (target == null)
			{
				await channel.SendMessageAsync(":musical_note: You need to be in a voice channel!");
				return;
			}

			if (CheckIfServerIsPlayingMusic(guild, out ServerMusicItem serverList))
			{
				if (serverList.AudioChannel.GetUser(user.Id) != null)
				{
					await channel.SendMessageAsync(":musical_note: I am already in the same audio channel as you!");
					return;
				}

				await channel.SendMessageAsync(
					":musical_note: Sorry, but I am already playing in a different audio channel at the moment.");

				return;
			}

			IAudioClient audio = await target.ConnectAsync(); //Connect to the voice channel

			ServerMusicItem item = new ServerMusicItem //Add it to the currentChannels list
			{
				GuildId = guild.Id,
				IsPlaying = false,
				AudioClient = audio,
				AudioChannel = (SocketVoiceChannel) target,
				StartChannel = (ISocketMessageChannel) channel,
				Downloader = null,
				CancellationSource = null
			};

			currentChannels.Add(item);
		}

		/// <summary>
		/// Leaves the current voice channel the bot is in
		/// </summary>
		/// <param name="guild">The <see cref="IGuild"/> where the channel to leave is in</param>
		/// <param name="channel">The <see cref="IMessageChannel"/> to log messages to</param>
		/// <param name="user">The <see cref="IUser"/> who requested this command</param>
		/// <returns></returns>
		public async Task LeaveAudio(IGuild guild, IMessageChannel channel, IUser user)
		{
			if (!CheckIfServerIsPlayingMusic(guild, out ServerMusicItem serverList))
			{
				await channel.SendMessageAsync(":musical_note: You are not in any voice channel!");
				return;
			}

			//Check to see if the user is in the playing audio channel
			if (!await CheckIfUserInChat(user, channel, serverList))
				return;

			//If there is already a song playing, cancel it
			await StopPlayingAudioOnServer(serverList);

			await serverList.AudioClient.StopAsync();

			serverList.IsPlaying = false;

			currentChannels.Remove(GetMusicList(guild.Id)); //Remove it from the currentChannels list
		}

		/// <summary>
		/// Stops playing the current song
		/// </summary>
		/// <param name="guild">The guild of <see cref="IMessageChannel"/></param>
		/// <param name="channel">The <see cref="IMessageChannel"/> to use for messages</param>
		/// <param name="user">The <see cref="IUser"/> who requested this command</param>
		/// <returns></returns>
		public async Task StopAudio(IGuild guild, IMessageChannel channel, IUser user)
		{
			if (!CheckIfServerIsPlayingMusic(guild, out ServerMusicItem serverList))
			{
				await channel.SendMessageAsync(":musical_note: Your not in any voice channel!");
				return;
			}

			//Check to see if the user is in the playing audio channel
			if (!await CheckIfUserInChat(user, channel, serverList))
				return;

			//Cancel any downloads first if there is one happening
			if (serverList.Downloader != null)
			{
				serverList.Downloader.CancelTask();
				await Task.Delay(100);

				serverList.Downloader = null;
				await channel.SendMessageAsync(":musical_note: Download canceled.");
				return;
			}

			//There is no music playing
			if (!serverList.IsPlaying)
			{
				await channel.SendMessageAsync(":musical_note: No music is playing!");
				return;
			}

			//Stop the current playing song
			serverList.CancellationSource.Cancel();
			await channel.SendMessageAsync(":musical_note: Stopping current playing song...");
		}

		/// <summary>
		/// Plays a song in a given voice channel
		/// </summary>
		/// <param name="guild">The <see cref="SocketGuild"/> where we are playing in</param>
		/// <param name="channel">The <see cref="IMessageChannel"/> to log messages to</param>
		/// <param name="target">The target <see cref="IVoiceChannel"/> to play music in</param>
		/// <param name="user">The <see cref="IUser"/> who requested this command</param>
		/// <param name="search">The query to search for</param>
		/// <returns></returns>
		public async Task SendAudio(SocketGuild guild, IMessageChannel channel, IVoiceChannel target, IUser user,
			string search)
		{
			//Join the voice channel the user is in if we are already not in a voice channel
			if (!CheckIfServerIsPlayingMusic(guild, out ServerMusicItem serverMusicList))
			{
				await JoinAudio(guild, target, channel, user);

				serverMusicList = GetMusicList(guild.Id);
			}

			//Check to see if the user is in the playing audio channel
			if (!await CheckIfUserInChat(user, channel, serverMusicList))
				return;

			//Make sure the search isn't empty or null
			if (string.IsNullOrWhiteSpace(search))
			{
				await channel.SendMessageAsync("You need to input a search!");
				return;
			}

			IUserMessage message =
				await channel.SendMessageAsync($":musical_note: Preparing to play '{search}'");

			string songFileLocation;
			string songName;

			search.RemoveIllegalChars();

			try
			{
				songFileLocation = await GetOrDownloadSong(search, message, serverMusicList);

				//It failed
				if (songFileLocation == null)
					return;

				Logger.Debug("Playing song from {@SongFileLocation}", songFileLocation);

				//This is so we say "Now playing 'Epic Song'" instead of "Now playing 'Epic Song.mp3'"
				songName = Path.GetFileName(songFileLocation)
					.Replace($".{Config.bot.AudioSettings.MusicFileFormat.GetFormatExtension()}", "");

				//If there is already a song playing, cancel it
				await StopPlayingAudioOnServer(serverMusicList);
			}
			catch (Exception ex)
			{
				Logger.Error("An error occured while trying to get a song! {@Exception}", ex);
				return;
			}

			//Create music playback for our music format
			IMusicPlaybackInterface playbackInterface =
				serverMusicList.MusicPlayback = CreateMusicPlayback(songFileLocation);

			//Log (if enabled) to the console that we are playing a new song
			if (Config.bot.AudioSettings.LogPlayStopSongToConsole)
				Logger.Info("The song {@SongName} on server {@GuildName}({@GuildId}) has started.", songName, guild.Name, guild.Id);

			serverMusicList.CancellationSource = new CancellationTokenSource();
			CancellationToken token = serverMusicList.CancellationSource.Token;

			serverMusicList.IsPlaying = true;

			//Create an outgoing pcm stream
			await using AudioOutStream outStream = serverMusicList.AudioClient.CreatePCMStream(AudioApplication.Music);
			bool fail = false;
			bool exit = false;
			const int bufferSize = 1024;
			byte[] buffer = new byte[bufferSize];

			await MessageUtils.ModifyMessage(message, $":musical_note: Now playing **{songName}**.");

			while (!fail && !exit)
			{
				try
				{
					if (token.IsCancellationRequested)
					{
						exit = true;
						break;
					}

					//Read from stream
					int read = await playbackInterface.ReadAudioStream(buffer, bufferSize, token);
					if (read == 0)
					{
						exit = true;
						break;
					}

					//Flush
					await playbackInterface.Flush();

					//Write it to outgoing pcm stream
					await outStream.WriteAsync(buffer, 0, read, token);

					//If we are still playing
					if (serverMusicList.IsPlaying) continue;

					//For pausing the song
					do
					{
						//Do nothing, wait till is playing is true
						await Task.Delay(100, token);
					} while (serverMusicList.IsPlaying == false);
				}
				catch (OperationCanceledException)
				{
					//User canceled
				}
				catch (Exception ex)
				{
					await channel.SendMessageAsync("Sorry, but an error occured while playing!");

					if (Config.bot.ReportErrorsToOwner)
						await Global.BotOwner.SendMessageAsync(
							$"ERROR: {ex.Message}\nError occured while playing music on guild `{guild.Id}`.");

					fail = true;
				}
			}

			if (Config.bot.AudioSettings.LogPlayStopSongToConsole)
				Logger.Info("The song {@SongName} on server {@GuildName}({@GuildId}) has stopped.", songName, guild.Name, guild.Id);

			//There wasn't a request to cancel
			if (!token.IsCancellationRequested)
				await channel.SendMessageAsync($":musical_note: **{songName}** ended.");

			//Clean up
			// ReSharper disable MethodSupportsCancellation
			await outStream.FlushAsync();
			outStream.Dispose();
			serverMusicList.IsPlaying = false;

			playbackInterface.EndAudioStream();
			serverMusicList.MusicPlayback = null;
			// ReSharper restore MethodSupportsCancellation

			serverMusicList.CancellationSource.Dispose();
			serverMusicList.CancellationSource = null;
		}

		/// <summary>
		/// Pauses the current music playback
		/// </summary>
		/// <param name="guild">The <see cref="IGuild"/> that it is in</param>
		/// <param name="channel">The <see cref="IMessageChannel"/> to log messages to</param>
		/// <param name="user">The <see cref="IUser"/> who requested the command</param>
		/// <returns></returns>
		public async Task PauseAudio(IGuild guild, IMessageChannel channel, IUser user)
		{
			if (guild == null) return;

			ServerMusicItem musicList = GetMusicList(guild.Id);
			if (musicList == null) //The bot isn't in any voice channels
			{
				await channel.SendMessageAsync(":musical_note: There is no music being played!");
				return;
			}

			//Check to see if the user is in the playing audio channel
			if (musicList.AudioChannel.GetUser(user.Id) == null)
			{
				await channel.SendMessageAsync(":musical_note: You are not in the current playing channel!");
				return;
			}

			//Toggle pause status
			musicList.IsPlaying = !musicList.IsPlaying;

			if (musicList.IsPlaying) await channel.SendMessageAsync(":musical_note: Current song has been un-paused.");
			else await channel.SendMessageAsync(":musical_note: Current song has been paused.");
		}

		/// <summary>
		/// Creates a <see cref="IMusicPlaybackInterface"/>, depending on the audio extension selected
		/// </summary>
		/// <param name="fileLocation"></param>
		/// <returns></returns>
		private IMusicPlaybackInterface CreateMusicPlayback(string fileLocation)
		{
			return Config.bot.AudioSettings.MusicFileFormat switch
			{
				MusicFileFormat.Mp3 => new MusicMp3Playback(fileLocation),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		#region List Fuctions

		/// <summary>
		/// Gets a <see cref="ServerMusicItem"/>
		/// </summary>
		/// <param name="guildId"></param>
		/// <returns></returns>
		public static ServerMusicItem GetMusicList(ulong guildId)
		{
			IEnumerable<ServerMusicItem> result = from a in currentChannels
				where a.GuildId == guildId
				select a;

			ServerMusicItem list = result.FirstOrDefault();
			return list;
		}

		#endregion

		#region Inital User Checking

		private async Task<bool> CheckIfUserInChat(IUser user, IMessageChannel channel, ServerMusicItem serverMusic)
		{
			//Check to see if the user is in the playing voice channel
			if (serverMusic.AudioChannel.GetUser(user.Id) == null)
			{
				await channel.SendMessageAsync(":musical_note: You are not in the current playing channel!");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if server is currently already playing music
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="serverMusic"></param>
		/// <returns></returns>
		private bool CheckIfServerIsPlayingMusic(IGuild guild, out ServerMusicItem serverMusic)
		{
			ServerMusicItem serverMusicList = GetMusicList(guild.Id);
			if (serverMusicList == null)
			{
				serverMusic = null;
				return false;
			}

			serverMusic = serverMusicList;
			return true;
		}

		#endregion

		#region Additional Music Functions

		/// <summary>
		/// Searches music folder for similar or same results to <see cref="search"/>
		/// </summary>
		/// <param name="search">The name of the song to search for</param>
		/// <param name="fileFormat"></param>
		/// <returns>Returns the first found similar or matching result</returns>
		public static string SearchMusicDirectory(string search, MusicFileFormat fileFormat)
		{
			string musicDir = Config.bot.AudioSettings.MusicFolderLocation;
			if (!Directory.Exists(musicDir)) Directory.CreateDirectory(musicDir);

			DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(musicDir);
			FileInfo[] filesInDir =
				hdDirectoryInWhichToSearch.GetFiles($"*{search}*.{fileFormat.GetFormatExtension()}");

			return filesInDir.Select(foundFile => foundFile.FullName).FirstOrDefault();
		}

		/// <summary>
		/// Gets, or downloads (if necessary) a song
		/// </summary>
		/// <param name="search"></param>
		/// <param name="message"></param>
		/// <param name="musicList"></param>
		/// <returns>Returns a path to the song, or null if it failed</returns>
		private async Task<string> GetOrDownloadSong(string search, IUserMessage message, ServerMusicItem musicList)
		{
			string songFileLocation;

			if (musicList.Downloader != null)
			{
				musicList.Downloader.CancelTask();
				await Task.Delay(100);

				musicList.Downloader = null;
			}

			ConfigAudio audioCfg = Config.bot.AudioSettings;

			musicList.Downloader = new StandardMusicDownloader(audioCfg.MusicFolderLocation, audioCfg.MusicFileFormat,
				Global.HttpClient, new CancellationTokenSource(), youTubeSearcher);
			if (WebUtils.IsStringValidUrl(search))
				songFileLocation = await musicList.Downloader.GetSongViaYouTubeUrl(search, message);
			else
				songFileLocation = await musicList.Downloader.GetOrDownloadSong(search, message);

			return songFileLocation;
		}

		/// <summary>
		/// Stops a song playing on a guild
		/// </summary>
		/// <param name="serverMusic"></param>
		/// <returns></returns>
		public static async Task StopPlayingAudioOnServer(ServerMusicItem serverMusic)
		{
			if (serverMusic.IsPlaying)
			{
				serverMusic.CancellationSource.Cancel();

				while (serverMusic.CancellationSource != null)
				{
					//Wait until CancellationSource is null
					await Task.Delay(100);
				}
			}
		}

		#endregion
	}
}