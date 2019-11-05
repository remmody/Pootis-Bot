﻿namespace Pootis_Bot.Structs.Config
{
	public struct ConfigAudio
	{
		public bool AudioServicesEnabled { get; set; }
		public string InitialApplication { get; set; }
		public string PythonArguments { get; set; }
		public bool ShowYoutubeDlWindow { get; set; }
		public bool LogPlayStopSongToConsole { get; set; }
	}
}