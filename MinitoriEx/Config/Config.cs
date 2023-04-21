using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MinitoriEx
{
    public class Config
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("twitter_key")]
        public string TwitterKey { get; set; }
        [JsonProperty("twitter_secret")]
        public string TwitterSecret { get; set; }
        [JsonProperty("twitter_token")]
        public string TwitterToken { get; set; }
        
        [JsonProperty("guild_id")]
        public ulong HomeGuildId { get; set; }

        [JsonProperty("log_channel")]
        public ulong LogChannelId { get; set; }

        [JsonProperty("ignored_channels")]
        public List<ulong> IgnoredChannelIds { get; set; }

        [JsonProperty("watched_feeds")]
        public Dictionary<string, List<ulong>> WatchedTwitterFeeds { get; set; } = new Dictionary<string, List<ulong>>();

        [JsonProperty("webhook_tokens")]
        public Dictionary<ulong, string> WebHooks { get; set; } = new Dictionary<ulong, string>();

        public async static Task<Config> Load()
        {
            if (File.Exists("config.json"))
            {
                var json = File.ReadAllText("config.json");
                return JsonConvert.DeserializeObject<Config>(json);
            }
            var config = new Config();
            await config.Save();
            throw new InvalidOperationException("configuration file created; insert token and restart.");
        }

        public async Task Save()
        {
            //var json = JsonConvert.SerializeObject(this);
            //File.WriteAllText("config.json", json);
            await JsonStorage.SerializeObjectToFile(this, "config.json");
        }
    }
}
