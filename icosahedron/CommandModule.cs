using System.Diagnostics;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using Image = SixLabors.ImageSharp.Image;
using System.IO.Ports;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization.Json;
using Discord.Webhook;
using Newtonsoft.Json.Linq;

namespace Icosahedron;

internal class CommandModule : InteractionModuleBase {
    public CommandModule() { }

    public static Dictionary<string, EmbedBuilder> helpEmbeds = new() {
        { "Message commands", new EmbedBuilder {
            Title = "Message commands",
            Description = "these commands are triggered by messages instead of bot interactions. The prefix is **hey icosahedron**",
            Color = EmbedColor,
            Fields = [
                new EmbedFieldBuilder {
                    Name = "Command list",
                    Value = """
                            **list counters** - list all counters the bot knows
                            **increment [counter]** - increments a counter
                            **decrement [counter]** - decrements a counter
                            **is this true** (must reply to something) - essentially 8 ball
                            **how many pixels does this have** (also must reply to something) - tells you how many pixels an image has
                            """,
                }
            ]
        } },
        { "Sudo", new EmbedBuilder {
            Title = "Sudo",
            Description = "these commands are similar to message commands, but have a different prefix mimicing a unix shell, only work in servers and are trusted-person only. See </semiconductors:1471952442680934466> for more info",
            Color = EmbedColor,
            Fields = [
                new EmbedFieldBuilder {
                    Name = "Command list",
                    Value = """
                            **pacman -Sybau [person]** - mute someone for 28 days
                            **usermod [person] [nickname]** - change someone's nickname
                            """,
                }
            ]
        } },
        { "Утпдшыр", new EmbedBuilder {
            Title = "Утпдшыр",
            Description = "This bot can translate between утпдшыр (english with a russian keyboard layout) and english. If it detect a message to be утпдшыр, it will automatically translate it, although this does not occur always. You can always manually translate it with the relevant message command",
            Color = EmbedColor
        } },
        { "Semiconductors", new EmbedBuilder {
            Title = "Semiconductors",
            Description = "The trusted list in this bot is called semiconductors. These people have access to `sudo` commands as well as other things",
            Color = EmbedColor,
            Fields = [
                new EmbedFieldBuilder {
                    Name = "Why?",
                    Value = "Science isn't about why, it's about why not! Why is so much of our science dangerous? Why not marry safe science if you love it so much! In fact, why not invent a special safety door that won't hit you on the butt on the way out, because you are fired!"
                }
            ]
        } },
        { "Serverscope", new EmbedBuilder {
            Title = "Serverscope",
            Description = "Owner-only tool for checking where this bot is added to\nIf you saw the bot talk on its own, this was probably used to do that",
            Color = EmbedColor,
            Footer = new EmbedFooterBuilder {
                Text = "I SWEAR THIS ISN'T SPY TECH"
            }
        } }
    };

    public InteractionService Service { get; set; }

    // {0} = ms
    // {1} = 1000/ms
    // {2} = ms/1000
    // {3} = 50/ms
    private readonly string[] pingMessages = [
        "{0} ms",
        "I'm {0} km away from you",
        "{0} years",
        "{1} kg of hopium left",
        "My brain delay is approximately {0} ms",
        "{2} seconds",
        "Anti-fatigue ration is now {3} mg"
    ];

    

    [SlashCommand("info", "general info about the bot")]
    public async Task Info() {
        try {
            var author = await client.GetUserAsync(SupremeLeader);
            var dotnetver = Environment.Version;
            var discordver = Assembly.GetAssembly(typeof(DiscordSocketClient))!.GetName().Version!;
            var embed = new EmbedBuilder {
                Title = "Icosahedron",
                Author = new EmbedAuthorBuilder {
                    Name = $"by {author.Username}",
                    IconUrl = author.GetAvatarUrl()
                },
                Description =
                    "Bot with highly specific inside jokes to a random friend group that won't make sense to anyone outside\nMore in-depth help can be accessed in the menu below",
                Color = EmbedColor,
                ThumbnailUrl = Memes.Random(),
                Fields = [
                    new EmbedFieldBuilder {
                        IsInline = false,
                        Name = "Software",
                        Value =
                            $"Written in C# (.NET {dotnetver.Major}.{dotnetver.Minor}.{dotnetver.Build}, Discord.Net {discordver.Major}.{discordver.Minor}.{discordver.Build}); running on Linux {Environment.OSVersion.Version}"
                    },
                    new EmbedFieldBuilder {
                        IsInline = true,
                        Name = "Source code",
                        Value = "[Github](https://github.com/hexahedron1/icosahedron)"
                    },
                    new EmbedFieldBuilder {
                        IsInline = true,
                        Name = "\"Support\" server",
                        Value = "https://discord.gg/vGyZmXGnea\nin reality you shouldn't even use this bot unless you know what you're doing"
                    }
                ],
                Footer = new EmbedFooterBuilder {
                    Text = "Online since",
                    IconUrl = "https://cdn.discordapp.com/attachments/1176906188613488692/1486085830161465506/etopizdec.gif"
                },
                Timestamp = StartTime
            };
            var menu = new SelectMenuBuilder {
                Options = (from x in helpEmbeds select new SelectMenuOptionBuilder(x.Key, x.Key)).ToList(),
                CustomId = "help-embed-select",
                Placeholder = "Select a help topic"
            };
            await RespondAsync(embed: embed.Build(), components: new ComponentBuilder { ActionRows = [ new ActionRowBuilder { Components = [ menu ] } ] }.Build());
        }
        catch (Exception e) {
            await ShowError(Context.Interaction, e);
        }
    }

    [ComponentInteraction("help-embed-select")]
    public async Task HelpEmbedSelect(string[] selections) => await RespondAsync(ephemeral: true, embeds: (from x in selections select helpEmbeds[x].Build()).ToArray());
    
    [ComponentInteraction("show-stack_*")]
    public async Task SendStackTrace(string id) {
        string path = Path.Join(datadir, "errorlogs", $"log_{id}.txt");
        if (File.Exists(path)) {
            await RespondWithFileAsync(path, ephemeral: true);
        }
        else await RespondAsync($"log {id} not found");
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
        [MinValue(0), Summary("index", "the index of the image, starts at 0")]
        int? index = null) {
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

    [MessageCommand("crush")]
    public async Task Crush(IMessage msg) {
        try {
            if (msg.Attachments.Count == 0) {
                await RespondAsync("where image");
                return;
            }

            IAttachment? attack = msg.Attachments.FirstOrDefault(attahh => ImageTypes.Contains(attahh.ContentType));

            if (attack == null) {
                await RespondAsync("where image");
                return;
            }

            await DeferAsync();
            DownloadImage(attack.Url);
            var img = Image.Load<Rgba32>("/tmp/icosahedron/image");
            for (int x = 0; x < img.Width; x++) {
                for (int y = 0; y < img.Height; y++) {
                    var pix = img[x, y].ToScaledVector4();
                    Vector4 bot = new(Cram(pix.X, 4, -1), Cram(pix.Y, 4, -1), Cram(pix.Z, 4, -1), Cram(pix.W, 4, -1));
                    Vector4 top = new(Cram(pix.X, 4, 1), Cram(pix.Y, 4, 1), Cram(pix.Z, 4, 1), Cram(pix.W, 4, 1));
                    float map = bayer[x % 4 + y % 4 * 4]/16f;
                    var outCol = new Rgba32();
                    outCol.FromScaledVector4(new(Dither(pix.X, bot.X, top.X, map), Dither(pix.Y, bot.Y, top.Y, map),
                        Dither(pix.Z, bot.Z, top.Z, map), pix.W));
                    img[x, y] = outCol;
                }
            }
            await img.SaveAsPngAsync("/tmp/icosahedron/crushed.png");
            await FollowupWithFileAsync("/tmp/icosahedron/crushed.png");
        } catch (Exception e) {
            await ShowError(Context.Interaction, e);
        }
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

    [Group("serverscope", "I SWEAR THIS IS NOT SPY TECH also owner only")]
    internal class ServerScopeGroupModule : InteractionModuleBase {
        public static async Task ServerAutocomplete(IInteractionContext Context) {
            if (Context.User.Id != SupremeLeader) {
                await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync([]);
                return;
            }

            var userInput = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = client.Guilds.Select(guild => new AutocompleteResult(guild.Name, guild.Id.ToString()))
                .Where(x => string.IsNullOrWhiteSpace(userInput) ||
                            x.Name.Contains(userInput, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(25));
        }

        public static async Task UserAutocomplete(IInteractionContext Context) {
            if (Context.User.Id != SupremeLeader) {
                await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync([]);
                return;
            }

            var userInput = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            List<AutocompleteResult> results = [];
            foreach (var guild in client.Guilds) {
                if (results.Count > 25) break;
                results.AddRange(from x in await guild.GetUsersAsync().FlattenAsync()
                    where string.IsNullOrWhiteSpace(userInput) ||
                          x.Username.Contains(userInput, StringComparison.InvariantCultureIgnoreCase)
                    where results.FirstOrDefault(y => (string)y.Value == x.Id.ToString()) == null
                    select new AutocompleteResult(x.Username, x.Id.ToString()));
            }

            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(25));
        }

        public static async Task ChannelAutocomplete(IInteractionContext Context) {
            if (Context.User.Id != SupremeLeader) {
                await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync([]);
                return;
            }

            var userInput = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            List<AutocompleteResult> results = [];
            foreach (var guild in client.Guilds) {
                if (results.Count > 25) break;
                results.AddRange(from x in guild.TextChannels
                    where string.IsNullOrWhiteSpace(userInput) ||
                          x.Name.Contains(userInput, StringComparison.InvariantCultureIgnoreCase)
                    select new AutocompleteResult($"{guild.Name} - {x.Name}", x.Id.ToString())
                );
            }

            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(25));
        }

        [Group("server", "subcommands for servers")]
        internal class ServerSubCommandModule : InteractionModuleBase {
            [SlashCommand("list", "lists all server the bot is in")]
            public async Task List() {
                if (Context.User.Id != SupremeLeader) {
                    await RespondAsync(IdiNahui(100));
                    return;
                }

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
                if (Context.User.Id != SupremeLeader) {
                    await RespondAsync(IdiNahui(100));
                    return;
                }

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

            [AutocompleteCommand("server", "info")]
            public async Task InfoAutocomplete() => await ServerAutocomplete(Context);

            [SlashCommand("info", "fetch server info")]
            public async Task Info([Summary("server"), Autocomplete] string serverId) {
                try {
                    if (Context.User.Id != SupremeLeader) {
                        await RespondAsync(IdiNahui(100));
                        return;
                    }

                    await DeferAsync();
                    ulong id = ulong.Parse(serverId);
                    var guild = client.GetGuild(id);
                    EmbedBuilder embed = new EmbedBuilder {
                        Author = new EmbedAuthorBuilder {
                            Name = guild.Owner.Username,
                            IconUrl = guild.Owner.GetAvatarUrl()
                        },
                        Title = guild.Name,
                        Description = guild.Description,
                        Timestamp = guild.CreatedAt,
                        ThumbnailUrl = guild.IconUrl,
                        Color = EmbedColor,
                        Fields = [
                            new() {
                                IsInline = true,
                                Name = "Members",
                                Value = $"{guild.MemberCount}"
                            },
                            new() {
                                IsInline = true,
                                Name = "Verification level",
                                Value = $"{guild.VerificationLevel}"
                            },
                            new() {
                                IsInline = true,
                                Name = "Boost level",
                                Value = $"{(int)guild.PremiumTier}"
                            }
                        ],
                        Footer = new EmbedFooterBuilder {
                            Text = serverId
                        }
                    };
                    await FollowupAsync(embed: embed.Build());
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
        }

        [Group("channel", "subcommands for channels")]
        internal class ChannelSubCommandModule : InteractionModuleBase {
            [AutocompleteCommand("server", "list")]
            public async Task ListAutocomplete() => await ServerAutocomplete(Context);

            [SlashCommand("list", "lists all channels the bot is in")]
            public async Task List([Summary("server"), Autocomplete] string serverId) {
                if (Context.User.Id != SupremeLeader) {
                    await RespondAsync(IdiNahui(100));
                    return;
                }

                try {
                    await DeferAsync();
                    var guild = ((DiscordSocketClient)Context.Client).GetGuild(ulong.Parse(serverId));
                    MakePageEmbed($"{guild.Name} servers", $"serverscope-channel-list-{serverId}",
                        guild.Channels.Where(x => x is IMessageChannel),
                        x => x.Name, 0, out var embed, out var components);
                    await FollowupAsync(embed: embed, components: components);
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }

            [ComponentInteraction("serverscope-channel-list-*-*", true)]
            public async Task ListButton(string serverId, int id) {
                if (Context.User.Id != SupremeLeader) {
                    await RespondAsync(IdiNahui(100));
                    return;
                }

                try {
                    await DeferAsync();
                    var guild = ((DiscordSocketClient)Context.Client).GetGuild(ulong.Parse(serverId));
                    MakePageEmbed($"{guild.Name} servers", $"serverscope-channel-list-{serverId}",
                        guild.Channels.Where(x => x is IMessageChannel),
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

            [AutocompleteCommand("channel", "info")]
            public async Task InfoAutocomplete() => await ChannelAutocomplete(Context);

            [SlashCommand("info", "fetch channel info")]
            public async Task Info([Summary("channel"), Autocomplete] string channelId) {
                try {
                    if (Context.User.Id != SupremeLeader) {
                        await RespondAsync(IdiNahui(100));
                        return;
                    }

                    await DeferAsync();
                    if (await client.GetChannelAsync(ulong.Parse(channelId)) is not ITextChannel channel) {
                        throw new Exception("Invald channel type");
                    }

                    EmbedBuilder embed = new EmbedBuilder {
                        Title = channel.Name,
                        Description = channel.Topic,
                        Color = EmbedColor,
                        Author = new() {
                            Name = $"{(await channel.GetCategoryAsync()).Name}"
                        },
                        Fields = [
                            new() {
                                IsInline = true,
                                Name = "Slowmode",
                                Value = $"{(channel.SlowModeInterval == 0 ? "None" : channel.SlowModeInterval)}"
                            },
                            new() {
                                IsInline = true,
                                Name = "Is NSFW?",
                                Value = $"{channel.IsNsfw}"
                            }
                        ],
                        Footer = new() {
                            Text = channelId
                        },
                        Timestamp = channel.CreatedAt,
                    };
                    
                    await FollowupAsync(embed: embed.Build(), components: new ComponentBuilder {
                        ActionRows = [
                            new() {
                                Components = [
                                    new ButtonBuilder("Invite", $"create-invite-{channelId}", ButtonStyle.Secondary)
                                ]
                            }
                        ]
                    }.Build());
                } catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }

            [ComponentInteraction("create-invite-*", true)]
            public async Task CreateInvite(ulong channel) {
                try {
                    if (Context.User.Id != SupremeLeader) {
                        await RespondAsync(IdiNahui(100));
                        return;
                    }
                    if (await client.GetChannelAsync(channel) is not ITextChannel ch) {
                        throw new Exception("Invald channel type");
                    }

                    var invite = await ch.CreateInviteAsync();
                    await RespondAsync(invite.Url, ephemeral: true);
                } catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
            [AutocompleteCommand("channel", "connect")]
            public async Task ConnectAutocomplete() => await ChannelAutocomplete(Context);
            [SlashCommand("connect", "starts a serverscope session")]
            public async Task Connect([Summary("channel"), Autocomplete] string channelId, [Summary("preload", "how many messages to load on start"), MinValue(0), MaxValue(25)]int preload = 5) {
                try {
                    if (Context.User.Id != SupremeLeader) {
                        await RespondAsync(IdiNahui(100));
                        return;
                    }
                    if (ServerScopeState is not null) {
                        await RespondAsync("the serverscope is already running");
                        return;
                    }
                    await DeferAsync();
                    if (await client.GetChannelAsync(ulong.Parse(channelId)) is not ITextChannel target || Context.Channel is not ITextChannel source) {
                        throw new Exception("Invald channel type");
                    }
                    var srcHook = await source.TryGetWebhook();
                    DiscordWebhookClient hookClient = new(srcHook);
                    ServerScopeState = (source.Id, target.Id, hookClient);
                    await FollowupAsync($"connected to <#{channelId}>");
                    var msgs = (await target.GetMessagesAsync(preload).FlattenAsync()).Reverse().ToArray();
                    foreach (var msg in msgs) {
                        if (msg is null) continue;
                        await CopyMessage(msg, hookClient);
                    }
                } catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }

            [SlashCommand("disconnect", "stops the running session")]
            public async Task Disconnect() {
                ServerScopeState = null;
                await RespondAsync("disconnected");
            }
        }
            
        [AutocompleteCommand("user", "userinfo")]
        public async Task UserInfoAutocomplete() => await UserAutocomplete(Context);

        [SlashCommand("userinfo", "fetch user info")]
        public async Task UserInfo([Summary("user"), Autocomplete] string userId) {
            try {

                if (Context.User.Id != SupremeLeader) {
                    await RespondAsync(IdiNahui(100));
                    return;
                }

                await DeferAsync();
                ulong id = ulong.Parse(userId);
                var user = await client.GetUserAsync(id);
                EmbedBuilder embed = new EmbedBuilder {
                    Title = user.Username,
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = EmbedColor,
                    Description = $"<@{userId}>",
                    Fields = [
                        new() {
                            Name = "Status",
                            Value = $"{user.Status}"
                        },
                    ],
                    Timestamp = user.CreatedAt
                };
                if (user.IsWebhook)
                    embed.Footer = new EmbedFooterBuilder {
                        Text = $"Webhook | {userId}"
                    };
                else if (user.IsBot)
                    embed.Footer = new EmbedFooterBuilder {
                        Text = $"Bot | {userId}"
                    };
                else 
                    embed.Footer = new EmbedFooterBuilder {
                        Text = userId
                    };
                await FollowupAsync(embed: embed.Build());
            }
            catch (Exception e) {
                await ShowError(Context.Interaction, e);
            }
        }

        [SlashCommand("status", "shows serverscope status")]
        public async Task Status() {
            
            if (Context.User.Id != SupremeLeader) {
                await RespondAsync(IdiNahui(100));
                return;
            }

            if (ServerScopeState == null) await RespondAsync("serverscope is offline");
            else await RespondAsync($"<#{ServerScopeState.Value.Item1}> <-> <#{ServerScopeState.Value.Item2}>");
        }
    }

    [Group("config", "confi gurations")]
    internal class ConfigGroupModule : InteractionModuleBase {
        [SlashCommand("autodeутпдшырification", "toggle automatically translating to the english keyboard layout")]
        public async Task AutoDeУтпдшырification([Summary("thing", "what to do"), Choice("toggle myself", "user"), Choice("toggle this channel", "channel"), Choice("toggle this server", "guild"), Choice("view status", "status")] string what) {
            switch (what) {
                case "user":
                    if (NoAutoDeУтпдшырify.Users.Contains(Context.User.Id)) NoAutoDeУтпдшырify.Users.Remove(Context.User.Id);
                    else NoAutoDeУтпдшырify.Users.Add(Context.User.Id);
                    await RespondAsync(NoAutoDeУтпдшырify.Users.Contains(Context.User.Id)
                        ? "You have opted out"
                        : "you have opted in");
                    break;
                case "channel":
                    if (NoAutoDeУтпдшырify.Channels.Contains(Context.Channel.Id)) NoAutoDeУтпдшырify.Channels.Remove(Context.Channel.Id);
                    else NoAutoDeУтпдшырify.Channels.Add(Context.Channel.Id);
                    await RespondAsync(NoAutoDeУтпдшырify.Channels.Contains(Context.Channel.Id)
                        ? "disabled for this channel"
                        : "enabled for this channel");
                    break;
                case "guild":
                    if (Context.Guild is null) {
                        await RespondAsync("this ain't a server mate");
                        break;
                    }
                    if (NoAutoDeУтпдшырify.Guilds.Contains(Context.Guild.Id)) NoAutoDeУтпдшырify.Guilds.Remove(Context.Guild.Id);
                    else NoAutoDeУтпдшырify.Guilds.Add(Context.Guild.Id);
                    await RespondAsync(NoAutoDeУтпдшырify.Guilds.Contains(Context.Guild.Id)
                        ? "disabled for this server"
                        : "enabled for this server");
                    break;
                case "status":
                    await RespondAsync($"{(NoAutoDeУтпдшырify.Users.Contains(Context.User.Id) ? "❌ you are opted out" : "✅ you are opted in")}\n{(NoAutoDeУтпдшырify.Channels.Contains(Context.Channel.Id) ? "❌ this channel is disabled" : "✅ this channel is enabled")}\n{(NoAutoDeУтпдшырify.Guilds.Contains(Context.Guild.Id) ? "❌ this server is disabled" : "✅ this server is enabled")}");
                    break;
                default: 
                    await RespondAsync("this was not supposed to happen");
                    break;
            }
            SaveConfig();
        }
    }
    
    [Group("prism", "polyhedron database explorer")]
    internal class PrismGroupModule : InteractionModuleBase {
        [SlashCommand("info", "info about the database")]
        public async Task Info() {
            EmbedBuilder embed = new() {
                Title = "Prism",
                Description = "Info database for polyhedra, polygons and tilings",
                Fields = [
                    new EmbedFieldBuilder {
                        IsInline = true, Name = "Credits",
                        Value = """
                                Images of polyhedra and vertex figures taken from [Wikipedia](https://en.wikipedia.org/) under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) or public domain
                                OBJ files taken from [polyhedra.tessera.li](https://polyhedra.tessera.li/) under the MIT License
                                """
                    }
                ],
                Color = EmbedColor,
                Footer = new EmbedFooterBuilder {
                    Text = $"{Polyhedra.UniqueKeys.Count} polyhedra, {Polyhedra.Groups.Count} groups"
                }
            };
            await RespondAsync(embed: embed.Build());
        }
        public static async Task GroupAutocomplete(IInteractionContext Context) {
            var userInput = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = (from x in Polyhedra.Groups
                where string.IsNullOrWhiteSpace(userInput) || x.Key.Contains(userInput, StringComparison.InvariantCultureIgnoreCase)
                select new AutocompleteResult(x.Key, x.Value)).ToList();
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(25));
        }
        
        public static async Task PolyhedronAutocomplete(IInteractionContext Context) {
            var userInput = (Context.Interaction as SocketAutocompleteInteraction)!.Data.Current.Value.ToString();
            var results = (from x in Polyhedra.UniqueKeys 
                where string.IsNullOrWhiteSpace(userInput) || x.Contains(userInput, StringComparison.InvariantCultureIgnoreCase)
                select new AutocompleteResult(x, x)).ToList();
            await (Context.Interaction as SocketAutocompleteInteraction)!.RespondAsync(results.Take(25));
        }

        [Group("group", "subcommands for groups")]
        internal class PrismGroupGroupModule : InteractionModuleBase {
            [SlashCommand("list", "list all available polyhedron groups")]
            public async Task List() {
                try {
                    await DeferAsync();
                    MakePageEmbed("Polyhedron groups", "prism-group-list",
                        Polyhedra.Groups,
                        x => x.Key, 0, out var embed, out var components);
                    await FollowupAsync(embed: embed, components: components);
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
            [ComponentInteraction("prism-group-list-*", true)]
            public async Task ListButton(int id) {
                try {
                    await DeferAsync();
                    MakePageEmbed("Polyhedron groups", "prism-group-list",
                        Polyhedra.Groups,
                        x => x.Key, id, out var embed, out var components);
                    await ModifyOriginalResponseAsync(x => {
                        x.Embed = embed;
                        x.Components = components;
                    });
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
            [AutocompleteCommand("group", "info")]
            public async Task InfoAutocomplete() => await GroupAutocomplete(Context);
            [SlashCommand("info", "show info about a group")]
            public async Task Info([Summary("group"), Autocomplete] string groupPath) {
                try {
                    string json = await File.ReadAllTextAsync(Path.Join(groupPath, ".group.json"));
                    var template = new {
                        name = "",
                        description = "",
                        thumbnail = "",
                        sections = new JObject(),
                        trivia = Array.Empty<string>()
                    };
                    var groupData = JsonConvert.DeserializeAnonymousType(json, template);
                    if (groupData is null) throw new JsonSerializationException("Failed to parse group data");
                    EmbedBuilder embed = new EmbedBuilder {
                        Title = groupData.name,
                        Description = groupData.description,
                        ThumbnailUrl = groupData.thumbnail,
                        Footer = new EmbedFooterBuilder {
                            Text = groupData.trivia.Random()
                        },
                        Color = EmbedColor
                    };
                    foreach (var pair in groupData.sections) {
                        if (pair.Value is null) continue;
                        embed.AddField(pair.Key, pair.Value.ToString(), true);
                    }
                    await RespondAsync(embed: embed.Build());
                } catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
            [AutocompleteCommand("group", "items")]
            public async Task ListAutocomplete() => await GroupAutocomplete(Context);

            [SlashCommand("items", "list all items in a group")]
            public async Task ListItems([Summary("group"), Autocomplete] string groupPath) {
                try {
                    string groupConfigFile = Path.Join(groupPath, ".group.json");
                    string groupConfigJson = await File.ReadAllTextAsync(groupConfigFile);
                    var template = new {
                        name = ""
                    };
                    var groupConfig = JsonConvert.DeserializeAnonymousType(groupConfigJson, template);
                    if (groupConfig == null) {
                        await Log("prism group items", $"Failed to parse {groupPath}", LogSeverity.Warning);
                        throw new JsonException($"Failed to parse {groupConfigFile}");
                    }
                    await DeferAsync();
                    MakePageEmbed(groupConfig.name, "prism-group-poly-list",
                        Polyhedra.GroupCache[groupConfig.name], 0, out var embed, out var components);
                    await FollowupAsync(embed: embed, components: components);
                }
                catch (Exception e) {
                    await ShowError(Context.Interaction, e);
                }
            }
            [ComponentInteraction("prism-group-poly-list-*", true)]
            public async Task ListItemsButton(int id) {
                try {
                    await DeferAsync();
                    string gid = (await Context.Interaction.GetOriginalResponseAsync()).Embeds.First().Title;
                    MakePageEmbed(gid, "prism-group-poly-list",
                        Polyhedra.GroupCache[gid],  id, out var embed, out var components);
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

        [ComponentInteraction("prism-select-menu", true)]

        public async Task PrismSelectMenu(string[] selections) => await RespondAsync(
            embed: Polyhedra.CreatePolyhedronEmbed(selections.First(), out var component), components: component);
        [AutocompleteCommand("polyhedron", "view")]
        public async Task ViewAutocomplete() => await PolyhedronAutocomplete(Context);
        [SlashCommand("view", "view the data of a polyhedron")]
        public async Task View([Summary("polyhedron"), Autocomplete] string poly) =>
            await RespondAsync(embed: Polyhedra.CreatePolyhedronEmbed(poly, out var component), components: component);
    }
}
