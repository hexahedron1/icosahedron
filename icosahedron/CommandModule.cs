using Newtonsoft.Json;

namespace Icosahedron;

internal class CommandModule : InteractionModuleBase {
    public CommandModule() { }
    public InteractionService Service { get; set; }
    // {0} = ms
    // {1} = 1000/ms
    // {2} = ms/1000
    // {3} = 50/ms
    private string[] pingMessages = [
        "{0} ms",
        "I'm {0} km away from you",
        "{0} years",
        "{1} kg of hopium left",
        "My brain delay is approximately {0} ms",
        "{2} seconds",
        "Anti-fatigue ration is now {3} mg"
    ];
    [SlashCommand("ping", "latency hopefully")]
    public async Task Ping([Summary("normal", "make it not weird and quirky")]bool normal = false) {
        int ms = ((DiscordSocketClient)Context.Client).Latency;
        await RespondAsync(normal
            ? $"Round-trip latency: {ms} milliseconds" :
            string.Format(pingMessages[new Random().Next(0, pingMessages.Length)], ms, 1000/ms, ms/1000, 50/ms));
    }
    [SlashCommand("generatorismyname", "get a generator is my name 😁🦂😁🦂 image")]
    public async Task GeneratorIsMyName([MinValue(0), Summary("index", "the index of the image, starts at 0")]int? index = null) {
        int lim = Directory.GetFiles($"{datadir}/generatorismyname").Length - 1;
        index ??= new Random().Next(lim+1);
        if (index > lim) {
            await RespondAsync($"❌ Input a smaller number (max is {lim})");
            return;
        }
        string json = await File.ReadAllTextAsync($"{datadir}/generatorismyname/{index}.json");
        var obj = JsonConvert.DeserializeAnonymousType(json, new { 
            Author = ulong.MinValue,
            ImageUrl = "",
            Timestamp = 0 });
        if (obj is null) {
            await RespondAsync("I fucked up\n-# failed to parse json");
            return;
        }
        var embed = new EmbedBuilder {
            ImageUrl = obj.ImageUrl,
            Footer = new EmbedFooterBuilder {
                Text = $"{index+1}/{lim+1}"
            },
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(obj.Timestamp)
        };
        var author = await Context.Client.GetUserAsync(obj.Author);
        if (author is not null) {
            embed.Author = new EmbedAuthorBuilder {
                IconUrl = author.GetAvatarUrl(),
                Name = author.Username
            };
        }
        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("say", "make the bot say shit, owner only")]
    public async Task Say(string text, [Summary("reply", "message to reply to")]string? replystr = null) {
        if (Context.User.Id == SupremeLeader) {
            ulong reply = 0;
            if (replystr is not null) {
                if (!ulong.TryParse(replystr, out reply))
                    await RespondAsync("that aint a number mate", ephemeral: true);
            }
            await Context.Channel.SendMessageAsync(text, messageReference: reply == 0 ? null : new MessageReference(reply));
            await RespondAsync("Roger that", ephemeral: true);
        } else
            await RespondAsync("Go to нахуй", ephemeral: true);
    }

    [SlashCommand("download-ram", "reload config files")]
    public async Task DownloadRam() {
        if (!Semiconductors.Contains(Context.User.Id)) {
            await RespondAsync(IdiNahui());
        }
        long bytes = LoadConfig(out Exception? e);
        if (e != null) {
            await RespondAsync(embed: e.ErrorEmbed());
        }
        await RespondAsync($"Downloaded {bytes} bytes of RAM");
    }

    [SlashCommand("semiconductors", "fetch list of people who have elite privileges")]
    public async Task GetSemiconductors() {
        EmbedBuilder embed = new EmbedBuilder {
            Title = "Semiconductors",
            Description = "This is a list of people who have elevated privileges within the bot (i.e. can execute `sudo` commands)",
            Color = EmbedColor
        };
        string j = "";
        foreach (ulong id in Semiconductors) {
            j += $"<@{id}>\n";
        }
        embed.AddField("The List®", j.Trim());
        await RespondAsync(embed: embed.Build());
    }
    
    [MessageCommand("deутпдшырify")]
    public async Task DeУтпдшырify(IMessage msg) {
        await RespondAsync(msg.CleanContent.DeУтпдшырify());
    }

    [MessageCommand("утпдшырify")]
    public async Task Утпдшырify(IMessage msg) {
        await RespondAsync(msg.CleanContent.Утпдшырify());
    }
    
    [SlashCommand("markov-yourself", "generate a message based on yours (not ai slop)")]
    public async Task MarkovYourself() {
        await RespondAsync(
            $"waiting for Discord.Net to add message search day {Math.Ceiling((DateTime.Now - WaitingForSearchSince).TotalDays)}");
    }
}