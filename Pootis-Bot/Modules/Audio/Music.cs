﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Pootis_Bot.Modules.Audio
{
    public class MusicModule : ModuleBase<ICommandContext>
    {
        private readonly AudioService _service;

        // Remember to add an instance of the AudioService
        // to your IServiceCollection when you initialize your bot
        public MusicModule()
        {
            AudioService service = new AudioService();
            _service = service;
        }

        // You *MUST* mark these commands with 'RunMode.Async'
        // otherwise the bot will not respond until the Task times out.
        [Command("join", RunMode = RunMode.Async)]
        [Summary("Joins in the current voice channel you are in")]
        public async Task JoinCmd()
        {
            await _service.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        // Remember to add preconditions to your commands,
        // this is merely the minimal amount necessary.
        // Adding more commands of your own is also encouraged.
        [Command("leave", RunMode = RunMode.Async)]
        [Summary("Leaves the current voice channel thats it in")]
        public async Task LeaveCmd()
        {
            await _service.LeaveAudio(Context.Guild);
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays a song")]
        public async Task PlayCmd([Remainder] string song)
        {
            await _service.SendAudioAsync(Context.Guild, Context.Channel, song);
        }

        [Command("pause", RunMode = RunMode.Async)]
        [Summary("Pauses the current song")]
        public async Task PauseCmd()
        {
            await _service.PauseAudio(Context.Guild, Context.Channel);
        }
    }
}