using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using Image = SixLabors.ImageSharp.Image;
using System.IO.Ports;

namespace Icosahedron;

internal class CommandModule : InteractionModuleBase {
    public CommandModule() {
    }

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

    public static async Task ShowError(IDiscordInteraction interaction, Exception e) {
        try {
            if (interaction.HasResponded) await interaction.FollowupAsync(embed: e.ErrorEmbed());
            else await interaction.RespondAsync(embed: e.ErrorEmbed());
        }
        catch (Discord.Net.HttpException) {
            await (await client.GetChannelAsync(interaction.ChannelId!.Value) as IMessageChannel)!.SendMessageAsync(
                embed: e.ErrorEmbed());
        }
    }

    [SlashCommand("ping", "latency hopefully")]
    public async Task Ping([Summary("normal", "make it not weird and quirky")] bool normal = false) {
        int ms = ((DiscordSocketClient)Context.Client).Latency;
        await RespondAsync(normal
            ? $"Round-trip latency: {ms} milliseconds"
            : string.Format(pingMessages[new Random().Next(0, pingMessages.Length)], ms, 1000 / ms, ms / 1000,
                50 / ms));
    }

    [SlashCommand("generatorismyname", "get a generator is my name 😁🦂😁🦂 image")]
    public async Task GeneratorIsMyName(
        [MinValue(0), Summary("index", "the index of the image, starts at 0")] int? index = null) {
        int lim = Directory.GetFiles($"{datadir}/generatorismyname").Length - 1;
        index ??= new Random().Next(lim + 1);
        if (index > lim) {
            await RespondAsync($"❌ Input a smaller number (max is {lim})");
            return;
        }

        string json = await File.ReadAllTextAsync($"{datadir}/generatorismyname/{index}.json");
        var obj = JsonConvert.DeserializeAnonymousType(json, new {
            Author = ulong.MinValue,
            ImageUrl = "",
            Timestamp = 0
        });
        if (obj is null) {
            await RespondAsync("I fucked up\n-# failed to parse json");
            return;
        }

        var embed = new EmbedBuilder {
            ImageUrl = obj.ImageUrl,
            Footer = new EmbedFooterBuilder {
                Text = $"{index + 1}/{lim + 1}"
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
    public async Task Say(string text, [Summary("reply", "message to reply to")] string? replystr = null) {
        if (Context.User.Id == SupremeLeader) {
            ulong reply = 0;
            if (replystr is not null) {
                if (!ulong.TryParse(replystr, out reply))
                    await RespondAsync("that aint a number mate", ephemeral: true);
            }

            await Context.Channel.SendMessageAsync(text,
                messageReference: reply == 0 ? null : new MessageReference(reply));
            await RespondAsync("Roger that", ephemeral: true);
        }
        else
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
            Description =
                "This is a list of people who have elevated privileges within the bot (i.e. can execute `sudo` commands)",
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

    [Group("arduino", "commands for cube's arduino")]
    internal class ArduinoGroupModule : InteractionModuleBase {
        [SlashCommand("show-image", "show an image on an OLED screen")]
        public async Task ShowImage(IAttachment attachment) {
            try {
                await DeferAsync();
                throw new NotImplementedException("The arduino does not comply for some reason");
                if (!ImageTypes.Contains(attachment.ContentType)) {
                    await FollowupAsync("this is either not an image or not an image format that's supported");
                    return;
                }

                DownloadImage(attachment.Url);
                using Image<Rgb24> img =
                    await Image.LoadAsync<Rgb24>("/tmp/icosahedron/image");
                if (img.Frames.Count > 1) {
                    await FollowupAsync("the image must not be animated");
                    return;
                }

                double aspect = (double)img.Width / img.Height;
                if (aspect >= 2.67) {
                    double scalar = img.Width / 128.0;
                    img.Mutate(x => x.Resize(128, (int)(img.Height / scalar)));
                }
                else {
                    double scalar = img.Height / 48.0;
                    img.Mutate(x => x.Resize((int)(img.Width / scalar), 48));
                }

                img.Mutate(x => x.Grayscale().Pad(128, 48).BinaryDither(OrderedDither.Bayer4x4));
                //using SixLabors.ImageSharp.Image properImg = new Image<Rgb24>(128, 48);
                //properImg.Mutate(x => x.);
                await img.SaveAsPngAsync("/tmp/icosahedron/converted.png");
                byte[] raw = new byte[768];
                for (int i = 0; i < 768; i++) {
                    int ox = i % 16 * 8;
                    int oy = i / 16;
                    byte b = 0;
                    for (int j = 0; j < 8; j++) {
                        b |= (byte)(img[ox + j, oy].R == 255 ? 1 : 0);
                        b <<= 1;
                    }

                    raw[i] = b;
                }

                await File.WriteAllBytesAsync("/tmp/icosahedron/raw", raw);
                SerialPort port;
                try {
                    port = new SerialPort("/dev/ttyUSB0", 9600);
                }
                catch (IOException) {
                    await FollowupAsync(
                        "could not open serial port\n-# this is likely due to another program using the arduino or it is not plugged into the computer");
                    return;
                }

                port.Open();
                for (var i = 0; i < 12; i++) {
                    port.Write(raw, i * 64, 64);
                    Thread.Sleep(200);
                }

                port.Close();
                await FollowupWithFileAsync(new FileAttachment("/tmp/icosahedron/converted.png"));
            }
            catch (Exception e) {
                await ShowError(Context.Interaction, e);
            }
        }
    }

    [Group("serverscope",
        "I SWEAR THIS IS NOT SPY TECH also owner only")]
    internal class ServerScopeGroupModule : InteractionModuleBase {
        [Group("server", "subcommands for servers")]
        internal class ServerSubCommandModule : InteractionModuleBase {
            [SlashCommand("list", "lists all server the bot is in")]
            public async Task List() {
                try {
                    await DeferAsync();
                    MakePageEmbed("Available servers", "serverscope-server-list",
                        ((DiscordSocketClient)Context.Client).Guilds,
                        x => x.Name, 0, out var embed, out var components);
                    await FollowupAsync(embed: embed, components: components);
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }

            [ComponentInteraction("serverscope-server-list-*", true)]
            public async Task ListButton(int id) {
                try {
                    await DeferAsync();
                    MakePageEmbed("Available servers", "serverscope-server-list",
                        ((DiscordSocketClient)Context.Client).Guilds,
                        x => x.Name, id, out var embed, out var components);
                    await ModifyOriginalResponseAsync(x => {
                        x.Embed = embed;
                        x.Components = components;
                    });
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
        }
    }
}
