using System;
using System.Threading.Tasks;
using DSharpPlus;
using Newtonsoft.Json;
using System.Linq;
using DSharpPlus.Entities;
using System.Text;

namespace MinitoriEx
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().MainAsync().GetAwaiter().GetResult();

        static string SanitizeNames(string input)
        {
            return input.Replace("*", "\\*").Replace("`", "\\`").Replace("\\", "\\\\").Replace("_", "\\_").Replace("~~", "\\~\\~").Replace("||", "\\|\\|");
        }

        private static void Log(Exception ex)
        {
            string exMessage;

            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = $"{ex.Message}";
                if (exMessage != "Reconnect failed: HTTP/1.1 503 Service Unavailable")
                    exMessage += $"\n{ex.StackTrace}";
            }
            else
                exMessage = null;

            string sourceName = ex.Source?.ToString();

            string text;
            if (ex.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = ex.Message;

            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            builder.Append($"[{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}] ");
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (c == '\n' || !char.IsControl(c) || c != (char)8226)
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();

            Console.WriteLine(text);
        }

        private Config config;
        private DiscordClient socketClient;
        private DiscordChannel logChannel = null;

        public async Task MainAsync()
        {
            config = await Config.Load();

            socketClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.GuildMessages | DiscordIntents.Guilds,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
            });

            socketClient.GuildAvailable += (s, e) =>
            {
                if (logChannel == null && e.Guild.Id == config.HomeGuildId)
                {
                    logChannel = e.Guild.Channels[config.LogChannelId];
                }

                return Task.CompletedTask;
            };

            socketClient.GuildDownloadCompleted += (s, e) =>
            {
                socketClient.MessageCreated += Client_MessageCreated;
                socketClient.MessageUpdated += Client_MessageUpdated;
                socketClient.MessageDeleted += Client_MessageDeleted;
                socketClient.MessagesBulkDeleted += Client_MessagesBulkDeleted;

                return Task.CompletedTask;
            };

            await socketClient.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Guild.Id == config.HomeGuildId && !e.Author.IsBot)
                {
                    if ((!e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Id)) ||
                        (e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Parent.Id)))
                    {
                        return;
                    }

                    try
                    {
                        using (var db = new MessageContext())
                        {
                            await db.AddAsync(new UserMessage(e));
                            await db.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            });

            return Task.CompletedTask;
        }

        private Task Client_MessageUpdated(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Guild.Id == config.HomeGuildId && e.Author != null && e.Author.IsBot == false)
                {
                    if ((!e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Id)) ||
                        (e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Parent.Id)))
                    {
                        return;
                    }

                    if (e.Message.Content == e.MessageBefore?.Content && e.Message.Attachments.Count == e.MessageBefore?.Attachments?.Count)
                    {
                        // No actual change, don't log anything
                        return;
                    }

                    try
                    {
                        using (var db = new MessageContext())
                        {
                            UserMessage oldMessage = db.Messages.FirstOrDefault(x => x.Id == e.Message.Id);


                            if (oldMessage == null && e.MessageBefore != null)
                            {
                                // Message was not logged before now, but we have it cached.
                                oldMessage = new UserMessage(e, e.MessageBefore);

                                // Insert it into the db, but we'll edit it before saving. Pretty sure that works.
                                await db.AddAsync(oldMessage);
                            }

                            //UserMessage newMessage = new UserMessage(e, e.Message);
                            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

                            // I am absolutely certain eventually I will need to account for 2k messages pushing this over some limit.
                            await logChannel.SendMessageAsync
                            ($"[<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:T>] ⚠️ **{SanitizeNames(e.Author.Username)}**#{e.Author.Discriminator} (ID:{e.Author.Id}) edited a message in {e.Channel.Mention}:", builder.WithDescription(
                                    $"{(oldMessage != null ? $"**From:**\n{oldMessage.Content}{(oldMessage.Attachments.Count > 0 ? "\n" : "")}{string.Join("\n", oldMessage.Attachments)}" : "**Original message not logged!**")}\n" +
                                    $"**To:**\n{e.Message.Content}{(e.Message.Attachments.Count > 0 ? "\n" : "")}{string.Join("\n", e.Message.Attachments.Select(x => x.Url))}")
                            .WithColor(DiscordColor.Yellow)
                            .WithTitle("Jump to message").WithUrl(e.Message.JumpLink).Build());

                            if (oldMessage == null)
                            {
                                // Never had a copy of the message, store the new copy.
                                await db.AddAsync(new UserMessage(e, e.Message));
                            }
                            else
                            {
                                // Edit the stored copy
                                oldMessage.EditedTime = e.Message.EditedTimestamp;
                                oldMessage.Content = e.Message.Content;
                                oldMessage.Attachments = e.Message.Attachments.Select(x => x.Url).ToList();
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            });

            return Task.CompletedTask;
        }

        private Task Client_MessageDeleted(DiscordClient sender, DSharpPlus.EventArgs.MessageDeleteEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Guild.Id == config.HomeGuildId)
                {
                    if ((!e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Id)) ||
                        (e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Parent.Id)))
                    {
                        return;
                    }

                    if (e.Message.Author?.IsBot == true)
                    {
                        return;
                    }

                    try
                    {
                        using (var db = new MessageContext())
                        {
                            UserMessage oldMessage = db.Messages.FirstOrDefault(x => x.Id == e.Message.Id);
                            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

                            if (oldMessage != null)
                            {
                                if (oldMessage.botMessage)
                                {
                                    return;
                                }

                                // Deleted message was logged
                                var user = await socketClient.GetUserAsync(oldMessage.UserId);

                                await logChannel.SendMessageAsync
                            ($"[<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:T>] ❌ **{SanitizeNames(user.Username)}**#{user.Discriminator} (ID:{user.Id})'s messasge has been deleted from {e.Channel.Mention}:", builder.WithDescription(
                                    $"{oldMessage.Content}{(oldMessage.Attachments.Count > 0 ? "\n" : "")}{string.Join("\n", oldMessage.Attachments)}")
                            .WithColor(DiscordColor.Red)
                            .WithTitle("Jump to message (approximate location)").WithUrl(e.Message.JumpLink).Build());

                                // With it logged on Discord, don't keep a local copy.
                                // Essentially locally we just keep a mirror of what is publicly available.
                                oldMessage.DeletedTime = DateTimeOffset.Now;
                                oldMessage.Content = null;

                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                // Deleted message was not logged, ignore.
                                // Potentially change this to log that an un-logged message in a channel was deleted.
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            });

            return Task.CompletedTask;
        }

        private Task Client_MessagesBulkDeleted(DiscordClient sender, DSharpPlus.EventArgs.MessageBulkDeleteEventArgs e)
        {
            // generate an HTML file for this maybe?
            // Might be able to get away with just a txt file. Sucks for mobile, would be fine as an attachment for desktop.
            // Maybe get my own web server?

            //Task.Run(async () =>
            //{
            //    if (e.Guild.Id == config.HomeGuildId)
            //    {
            //        if ((!e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Id)) ||
            //            (e.Channel.IsThread && config.IgnoredChannelIds.Contains(e.Channel.Parent.Id)))
            //        {
            //            return;
            //        }

            //        if (e.Message.Author?.IsBot == true)
            //        {
            //            return;
            //        }

            //        try
            //        {
            //            using (var db = new MessageContext())
            //            {
            //                var oldMessages = db.Messages.Where(x => e.Messages.Select(x => x.Id).ToList().Contains(x.Id)).ToList();

            //                StringBuilder output = new StringBuilder();
            //                Dictionary<ulong, DiscordUser> users = new Dictionary<ulong, DiscordUser>();

            //                foreach (var m in oldMessages)
            //                {
            //                    if (m != null)
            //                    {
            //                        if (!users.ContainsKey(m.UserId))
            //                        {
            //                            users[m.UserId] = await client.GetUserAsync(m.UserId);
            //                        }
            //                    }
            //                    else
            //                    {
            //                        // message wasn't logged
            //                    }
            //                }


            //                UserMessage oldMessage = db.Messages.FirstOrDefault(x => x.Id == e.Message.Id);
            //                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            //                if (oldMessage != null)
            //                {
            //                    if (oldMessage.botMessage)
            //                    {
            //                        return;
            //                    }

            //                    // Deleted message was logged
            //                    var user = await client.GetUserAsync(oldMessage.UserId);

            //                    await logChannel.SendMessageAsync
            //                ($"[<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:T>] ❌ **{SanitizeNames(user.Username)}**#{user.Discriminator} (ID:{user.Id})'s messasge has been deleted from {e.Channel.Mention}:", builder.WithDescription(
            //                        $"{oldMessage.Content}{(oldMessage.Attachments.Count > 0 ? "\n" : "")}{string.Join("\n", oldMessage.Attachments)}")
            //                .WithColor(DiscordColor.Red)
            //                .WithTitle("Jump to message (approximate location)").WithUrl(e.Message.JumpLink).Build());

            //                    // With it logged on Discord, don't keep a local copy.
            //                    // Essentially locally we just keep a mirror of what is publicly available.
            //                    oldMessage.DeletedTime = DateTimeOffset.Now;
            //                    oldMessage.Content = null;

            //                    await db.SaveChangesAsync();
            //                }
            //                else
            //                {
            //                    // Deleted message was not logged, ignore.
            //                    // Potentially change this to log that an un-logged message in a channel was deleted.
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Log(ex);
            //        }
            //    }
            //});

            return Task.CompletedTask;
        }
    }
}