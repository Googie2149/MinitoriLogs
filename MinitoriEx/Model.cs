using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;

namespace MinitoriEx
{
    public class MessageContext : DbContext
    {
        public DbSet<UserMessage> Messages { get; set; }

        public string DbPath { get; }

        public MessageContext()
        {
            DbPath = "messages.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserMessage>().Property(m => m.Id).ValueGeneratedNever();
            modelBuilder.Entity<UserMessage>().ToTable("Messages");
            modelBuilder.Entity<UserMessage>().Property(x => x.Attachments)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<string>>(v),
                    new ValueComparer<List<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()
                        )
                );
        }
    }

    public class UserMessage
    {
        public UserMessage()
        {

        }

        public UserMessage(MessageCreateEventArgs e, bool isBot = false)
        {
            Id = e.Message.Id;
            UserId = e.Author.Id;
            ChannelId = e.Channel.Id;
            GuildId = e.Guild.Id;
            botMessage = isBot;

            if (!isBot)
            {
                Content = e.Message.Content;
                Attachments = e.Message.Attachments.Select(x => x.Url).ToList();
            }
        }

        public UserMessage(MessageUpdateEventArgs e, DiscordMessage m)
        {
            Id = m.Id;
            UserId = e.Author.Id;
            ChannelId = e.Channel.Id;
            GuildId = e.Guild.Id;
            Content = m.Content;
            Attachments = m.Attachments.Select(x => x.Url).ToList();
            EditedTime = m.EditedTimestamp;
        }

        public UserMessage(MessageDeleteEventArgs e)
        {
            Id = e.Message.Id;
            UserId = e.Message.Author.Id;
            ChannelId = e.Channel.Id;
            GuildId = e.Guild.Id;
            Content = e.Message.Content;
            Attachments = e.Message.Attachments.Select(x => x.Url).ToList();
        }

        [Required]
        public ulong Id { get; set; }
        public bool botMessage { get; set; } = false;
        [Required]
        public ulong UserId { get; set; }
        [Required]
        public ulong ChannelId { get; set; }
        [Required]
        public ulong GuildId { get; set; }

        public string Content { get; set; }
        public List<string> Attachments { get; set; }

        public DateTimeOffset? DeletedTime { get; set; }
        public DateTimeOffset? EditedTime { get; set; }
    }
}
