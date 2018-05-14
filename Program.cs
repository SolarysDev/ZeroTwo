﻿using System;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using Discord;
using Discord.WebSocket;
using DiscordBotsList.Api;

namespace ZeroTwo
{
    class Program
    {
        DiscordSocketClient _client;
        Handler _handler;

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            if(Config.bot.token == null || Config.bot.token == "") return;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            _client.Log += Log;
            _client.JoinedGuild += ClientJoin;
            _client.Ready += Ready;
            
            _client.ReactionAdded += async (m, Channel, Reaction) => {
                if(Reaction.Emote.Name != "⭐") return;
                
                var Message = await m.GetOrDownloadAsync();
                if(Message.Author.IsBot) return;

                if(Message.Channel is IGuildChannel c)
                {
                    if(!Starboard.GuildDir.Exists(s => s.GuildID == c.GuildId)) return;
                    var targetID = Starboard.GuildDir.Single(s => s.GuildID == c.GuildId).ChannelID;
                    var target = await c.Guild.GetTextChannelAsync(targetID);
                    var embed = new EmbedBuilder()
                        .WithColor(new Color(255, 0, 0))
                        .WithTitle($"From {Message.Author.Username}")
                        .WithDescription(Message.Content)
                        .Build();
                    await target.SendMessageAsync("", false, embed);
                }

            };
            

            await _client.LoginAsync(TokenType.Bot, Config.bot.token);
            await _client.StartAsync();

            _handler = new Handler();
                await _handler.InitAsync(_client);

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg.ToString());
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private Task ClientJoin(SocketGuild guild)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Joined {guild.Name}");
            Console.ResetColor();
            return Task.CompletedTask;
        }

        public async Task Ready()
        {
            await _client.SetGameAsync("with my darling", null, ActivityType.Playing);
            var DBLClient = new AuthDiscordBotListApi(424445724348907520, Config.bot.DBLKey);
            await DBLClient.UpdateStats(_client.Guilds.Count);
            var PushTimer = Task.Run(async () => {
                for(;;)
                {
                    await Task.Delay(1800000);
                    await DBLClient.UpdateStats(_client.Guilds.Count);
                }
            });
        }
    }
}