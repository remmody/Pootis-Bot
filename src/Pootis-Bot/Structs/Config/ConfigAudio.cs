﻿using System;
using Pootis_Bot.Services.Audio.Music;

namespace Pootis_Bot.Structs.Config
{
	/// <summary>
	/// Settings related to audio
	/// </summary>
	public struct ConfigAudio
	{
		/// <summary>
		/// Are the audio services enabled
		/// </summary>
		public bool AudioServicesEnabled { get; set; }

		/// <summary>
		/// Should we log when a song starts or stops on a guild
		/// </summary>
		public bool LogPlayStopSongToConsole { get; set; }

		/// <summary>
		/// The directory where our external applications will live
		/// </summary>
		public string ExternalDirectory { get; set; }

		/// <summary>
		/// Max video time
		/// </summary>
		public TimeSpan MaxVideoTime { get; set; }

		/// <summary>
		/// The location of the music folder
		/// </summary>
		public string MusicFolderLocation { get; set; }

		/// <summary>
		/// The format of the song (.mp3, etc)
		/// </summary>
		public MusicFileFormat MusicFileFormat { get; set; }
	}
}