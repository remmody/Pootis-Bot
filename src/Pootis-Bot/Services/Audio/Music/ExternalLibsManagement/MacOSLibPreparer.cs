#if OSX

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Pootis_Bot.Core;
using Pootis_Bot.Helpers;
using Pootis_Bot.Core.Logging;
using Pootis_Bot.Structs;

namespace Pootis_Bot.Services.Audio.Music.ExternalLibsManagement
{
    public class MacOSLibPreparer : ILibsPreparer
    {
        public bool CheckLibFiles()
        {
            string externalDirectory = Config.bot.AudioSettings.ExternalDirectory;

            //If either ffmpeg, opus or libsodium doesn't exist, we need to download them
            return File.Exists($"{externalDirectory}ffmpeg") && File.Exists("opus.so") && File.Exists("libsodium.so");
        }

        public void DownloadFiles(AudioExternalLibFiles libsUrls)
        {
            try
            {
                Logger.Log("Downloading files for Linux...");

                //Download all audio service files for Linux
                Logger.Log($"Downloading ffmpeg from {libsUrls.FfmpegDownloadUrl}");
                WebUtils.DownloadFileAsync(libsUrls.FfmpegDownloadUrl, "Temp/ffmpeg.zip").GetAwaiter().GetResult();
                Logger.Log($"Downloading needed DLLs from {libsUrls.LibsDownloadUrl}");
                WebUtils.DownloadFileAsync(libsUrls.LibsDownloadUrl, "Temp/dlls.zip").GetAwaiter().GetResult();

                //Extract required files
                Logger.Log("Extracting files...");
                ZipFile.ExtractToDirectory("Temp/dlls.zip", "./", true);
                ZipFile.ExtractToDirectory("Temp/ffmpeg.zip", "Temp/ffmpeg/", true);

                //Copy the needed parts of ffmpeg to the right directory
                Logger.Log("Setting up ffmpeg");
                Global.DirectoryCopy("Temp/ffmpeg/", "External/", true);
			
                //Because macos, we need the right permissions
                ChmodFile("External/ffmpeg", "700");

                //Delete unnecessary files
                Logger.Log("Cleaning up...");
                File.Delete("Temp/dlls.zip");
                File.Delete("Temp/ffmpeg.zip");
                Directory.Delete("Temp/ffmpeg", true);
            }
            catch (Exception ex)
            {
#if DEBUG
                Logger.Log(
                    $"An error occured while preparing music services: {ex}\nMusic services has now been disabled!",
                    LogVerbosity.Error);
#else
				Logger.Log($"An error occured while preparing music services: {ex.Message}\nMusic services has now been disabled!", LogVerbosity.Error);
#endif

                Config.bot.AudioSettings.AudioServicesEnabled = false;
                Config.SaveConfig();
            }
        }

		public void DeleteFiles()
		{
			string externalDir = Config.bot.AudioSettings.ExternalDirectory;

			//Delete ffmpeg
			if(File.Exists($"{externalDir}ffmpeg"))
				File.Delete($"{externalDir}ffmpeg");

			//Delete so
			if(File.Exists("opus.so"))
				File.Delete("opus.so");

			if(File.Exists("libsodium.so"))
				File.Delete("libsodium.so");
		}
        
        private static void ChmodFile(string file, string flag)
        {
            Process process = new Process{StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/bin/bash",
                Arguments = $"-c \"chmod {flag} {file}\""
            }};

            process.Start();
            process.WaitForExit();
        }
    }
}

#endif