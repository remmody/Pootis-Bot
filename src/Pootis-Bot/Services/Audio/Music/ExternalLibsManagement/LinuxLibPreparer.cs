#if LINUX

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Pootis_Bot.Core;
using Pootis_Bot.Core.Logging;
using Pootis_Bot.Helpers;
using Pootis_Bot.Structs;

namespace Pootis_Bot.Services.Audio.Music.ExternalLibsManagement
{
    public class LinuxLibPreparer : ILibsPreparer
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
                Logger.Info("Downloading files for Linux...");

                //Download all audio service files for Linux
                Logger.Info("Downloading ffmpeg from {@FfmpegDownloadUrl}", libsUrls.FfmpegDownloadUrl);
				WebUtils.DownloadFileAsync(libsUrls.FfmpegDownloadUrl, "Temp/ffmpeg.zip").GetAwaiter().GetResult();
				Logger.Info("Downloading needed DLLs from {@LibsDownloadUrl}", libsUrls.LibsDownloadUrl);
				WebUtils.DownloadFileAsync(libsUrls.LibsDownloadUrl, "Temp/dlls.zip").GetAwaiter().GetResult();

                //Extract required files
                Logger.Info("Extracting files...");
                ZipFile.ExtractToDirectory("Temp/dlls.zip", "./", true);
                ZipFile.ExtractToDirectory("Temp/ffmpeg.zip", "Temp/ffmpeg/", true);

                //Copy the needed parts of ffmpeg to the right directory
                Logger.Info("Setting up ffmpeg");
                Global.DirectoryCopy("Temp/ffmpeg/ffmpeg-linux-64/", "External/", true);
			
                //Because linux, we need the right permissions
                ChmodFile("External/ffmpeg", "700");

                //Delete unnecessary files
                Logger.Info("Cleaning up...");
                File.Delete("Temp/dlls.zip");
                File.Delete("Temp/ffmpeg.zip");
                Directory.Delete("Temp/ffmpeg", true);
            }
            catch (Exception ex)
            {
				Logger.Error("An error occured while preparing music services: {@Exception}\nMusic services has now been disabled!", ex);

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