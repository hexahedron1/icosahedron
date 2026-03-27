using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
// добро пожаловать на сервер безумные арбузы
namespace Icosahedron {
    internal class Program {
        string token = "";
        InteractionService interactionService;

        static Task Main(string[] args) => new Program().MainAsync();

        public System.Timers.Timer budilnik = new();

        async Task MainAsync() {
            if (!Directory.Exists(datadir)) Directory.CreateDirectory(datadir);
            token = (await File.ReadAllTextAsync(
#if DEBUG
                "/home/cube/Important/bot tokens/relay.txt"
#else
                "/home/cube/Important/bot tokens/icosahedron.txt"
#endif
            )).Trim();
            client = new DiscordSocketClient(new DiscordSocketConfig() {
                GatewayIntents = GatewayIntents.All,
                UseInteractionSnowflakeDate = false
            });
            interactionService = new InteractionService(client.Rest);
            await interactionService.AddModuleAsync<CommandModule>(null);
            client.Log += Log;
            client.Ready += Ready;
            client.SlashCommandExecuted += SlashCommandExecuted;
            client.MessageCommandExecuted += SlashCommandExecuted;
            client.SelectMenuExecuted += Client_SelectMenuExecuted;
            client.MessageReceived += MessageReceived;
            client.ButtonExecuted += SlashCommandExecuted;
            client.AutocompleteExecuted += SlashCommandExecuted;
            interactionService.Log += Log;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private Queue<ISocketMessageChannel> whopinged = [];
        private const string prefix = "hey icosahedron";

        private async Task<Task> MessageReceived(SocketMessage msg) {
            if (msg.Author.Id == client.CurrentUser.Id) return Task.CompletedTask;
            try {
                if (ServerScopeState.HasValue && !msg.Author.IsWebhook) {
                    if (msg.Channel.Id == ServerScopeState.Value.Item1) {
                        await CopyMessage(msg, (IMessageChannel)await client.GetChannelAsync(ServerScopeState.Value.Item2));
                    } else if (msg.Channel.Id == ServerScopeState.Value.Item2) {
                        await CopyMessage(msg, ServerScopeState.Value.Item3);
                    }
                }
                GeneratorIsMyName(msg);
                string msgContent = msg.Content;
                string msgCleanContent = msg.CleanContent;
                bool? isУтпдшыр = false;
                if (IllegalRussian.Count(msg.Content.ToLower()) >= 2) {
                    await Log("MessageReceived", "Suspected утпдшыр");
                    msgContent = msgContent.DeУтпдшырify();
                    msgCleanContent = msgCleanContent.DeУтпдшырify();
                    isУтпдшыр = true;
                } 
                if (Rand.Next(1000000) == 0) {
                    await Log("MessageReceived", "Suspected english");
                    msgContent = msgContent.Утпдшырify();
                    msgCleanContent = msgCleanContent.Утпдшырify();
                    isУтпдшыр = null;
                }
                if (msgContent.ToLower().StartsWith(prefix)) {
                    string command = msgContent[prefix.Length..].Trim();
                    string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    await Log("Command", $"From {msg.Author.Username}: {command} ({args.Length} args)");
                    if (args.Length == 0) {
                        await msg.SendToNahui(10);
                        return Task.CompletedTask;
                    }

                    if (args[0] == "check") {
                        if (msg.Channel is IGuildChannel guildChannel) {
                            SocketUser user = client.GetUser(args[1]);
                            if (user is null) await msg.SendToNahui();
                            else {
                                await msg.Reply($"can mute: {await CanMute(user.Id, guildChannel)}");
                            }
                        }
                        else await msg.SendToNahui();
                    }

                    if (args[0] == "warn") {
                        if (((SocketGuildUser)msg.Author).GetPermissions((IGuildChannel)msg.Channel).ManageMessages) {
                            if (args.Length < 2) {
                                await msg.Reply("Ping the culprit");
                            }
                            else {
                                Console.WriteLine(args[1]);
                                if (args[1].StartsWith("<@") && args[1].EndsWith(">")) {
                                    ulong id = ulong.Parse(args[1][2..^1]);
                                    IUser user = await client.GetUserAsync(id);
                                    if (args.Length < 3) {
                                        await msg.Reply("Specify at least one warn reason");
                                    }
                                    else {
                                        string arg = "on3 attentionyouhavebeenchargedwith _comma ";
                                        bool success = true;
                                        foreach (var a in args.Skip(2)) {
                                            if (penalCodes.TryGetValue(a, out var code)) {
                                                arg += $"{code} _comma ";
                                            }
                                            else {
                                                await msg.Reply($"Unknown reason '{a}'");
                                                success = false;
                                            }
                                        }

                                        arg += "off2";
                                        if (success) {
                                            Console.WriteLine($"Making sound file: {arg}");
                                            var p = Process.Start(
                                                "/home/cube/RiderProjects/combine_voice_maker/combine_voice_maker/bin/Debug/net9.0/combine_voice_maker",
                                                arg);
                                            await p.WaitForExitAsync();
                                            await user.SendFileAsync(new FileAttachment("out.wav", "warn.wav"));
                                            File.Delete("out.wav");
                                            await msg.AddReactionAsync(Emoji.Parse("✅"));
                                        }
                                    }
                                }
                                else {
                                    await msg.Reply("Ping the culprit");
                                }
                            }
                        }
                        else {
                            await msg.Reply("403 Forbidden");
                        }
                    }
                    else if (args[0] == "list") {
                        if (args.Length == 1) {
                            await msg.Reply("list what gro");
                            return Task.CompletedTask;
                        }
                        if (args[1] == "counters") {
                            string filePath = Path.Join(datadir, "counters.json");
                            if (!File.Exists(filePath)) {
                                await msg.Reply("the file is missing 🐙");
                                return Task.CompletedTask;
                            }
                            string json = await File.ReadAllTextAsync(filePath);
                            var template = new {
                                List = Array.Empty<object>()
                            };
                            var j = JsonConvert.DeserializeAnonymousType(json, template);
                            if (j == null) {
                                await msg.Reply("failed to parse json file 🐙");
                                return Task.CompletedTask;
                            }
                            EmbedBuilder embed = new() {
                                Color = EmbedColor,
                                Footer = new() {
                                    Text = "Last updated"
                                },
                                Timestamp = new DateTimeOffset(File.GetLastWriteTime(filePath)),
                                Description = ""
                            };
                            foreach (var jj in j.List) {
                                if (jj is JObject jjj) {
                                    string? name = null;
                                    if (jjj.TryGetValue("name", out var jname)) name = jname.Value<string>();
                                    int? value = null;
                                    if (jjj.TryGetValue("value", out var jvalue)) value = jvalue.Value<int>();
                                    embed.Description += $"**{name ?? "???"}**: {(value.HasValue ? value.Value : "???")}\n";
                                }
                                else {
                                    embed.Description += "<a:etopizdec:1280870892313907301>\n";
                                }
                            }

                            await msg.Reply(embed: embed.Build());
                        }
                        else {
                            await msg.Reply($"idk what {command[(args[0].Length + 1)..]} means");
                            return Task.CompletedTask;
                        }
                    } 
                    else if (args[0] == "increment") {
                        string filePath = Path.Join(datadir, "counters.json");
                        string cname = command[(args[0].Length + 1)..];
                        string json = await File.ReadAllTextAsync(filePath);
                        var template = new {
                            List = Array.Empty<object>()
                        };
                        var j = JsonConvert.DeserializeAnonymousType(json, template);
                        if (j == null) {
                            await msg.Reply("failed to parse json file 🐙");
                            return Task.CompletedTask;
                        }

                        foreach (var jj in j.List) {
                            if (jj is not JObject jjj) continue;
                            string? name = null;
                            if (jjj.TryGetValue("name", out var jname)) name = jname.Value<string>();
                            int? value = null;
                            if (jjj.TryGetValue("value", out var jvalue)) value = jvalue.Value<int>();
                            if (name != cname || value == null) continue;
                            jjj["value"] = ++value;
                            await msg.Reply($"**{cname}**: {value}");
                            json = JsonConvert.SerializeObject(j);
                            await File.WriteAllTextAsync(filePath, json);
                            return Task.CompletedTask;
                        }
                        await msg.Reply($"what is {cname}");
                    }
                    else if (args[0] == "decrement") {
                        string filePath = Path.Join(datadir, "counters.json");
                        string cname = command[(args[0].Length + 1)..];
                        string json = await File.ReadAllTextAsync(filePath);
                        var template = new {
                            List = Array.Empty<object>()
                        };
                        var j = JsonConvert.DeserializeAnonymousType(json, template);
                        if (j == null) {
                            await msg.Reply("failed to parse json file 🐙");
                            return Task.CompletedTask;
                        }

                        foreach (var jj in j.List) {
                            if (jj is not JObject jjj) continue;
                            string? name = null;
                            if (jjj.TryGetValue("name", out var jname)) name = jname.Value<string>();
                            int? value = null;
                            if (jjj.TryGetValue("value", out var jvalue)) value = jvalue.Value<int>();
                            if (name != cname || value == null) continue;
                            jjj["value"] = --value;
                            await msg.Reply($"**{cname}**: {value}");
                            json = JsonConvert.SerializeObject(j);
                            await File.WriteAllTextAsync(filePath, json);
                            return Task.CompletedTask;
                        }
                        await msg.Reply($"what is {cname}");
                    }
                    else if (args[0] == "throw" && args.Length > 1) {
                        if (args[1] == "up") throw new Exception("up");
                        if (args[1] == "говно") await msg.Channel.SendMessageAsync($"have some fresh говно, <@{msg.Author.Id}>");
                    }
                    else if (args.Length > 2 && ((args[0] is "add" or "remove" or "update" && args[1] == "tag") ||
                                                 (args[0] == "list" && args[1] == "tags")))
                        await msg.AddReactionAsync(Emote.Parse("<:normal:1275453792002773146>"));
                    else if (command == "is this true") await msg.Reply(IsThisTrue.RandomerRandom());
                    else if (command is "how many pixels does this image have" or "how many pixels does this have") {
                        if (msg.Reference == null) {
                            await msg.Reply("how many pixels does WHAT have cuh?????");
                            return Task.CompletedTask;
                        }
                        await Log("MessageReceived", "Searching for image");
                        IMessage iMessage = await msg.Channel.GetMessageAsync(msg.Reference.MessageId.Value);
                        IAttachment? attack = null;
                        foreach (var attahh in iMessage.Attachments) {
                            await Log("MessageReceived", attahh.ContentType);
                            if (ImageTypes.Contains(attahh.ContentType)) {
                                attack = attahh;
                                break;
                            }
                        }

                        if (attack == null) {
                            await msg.Reply("how many pixels does WHAT have cuh?????");
                            return Task.CompletedTask;
                        }
                        if (attack.ContentType == "image/svg+xml") {
                            await msg.Reply("∞x∞ (∞² total)");
                        } else {
                            await Log("MessageReceived", "Downloading image");
                            DownloadImage(attack.Url);
                            using Image img = await Image.LoadAsync("/tmp/icosahedron/image");
                            if (img.Frames.Count == 1) await msg.Reply($"{img.Width}x{img.Height} ({img.Width * img.Height} total)");
                            else await msg.Reply($"{img.Width}x{img.Height} ({img.Width * img.Height * img.Frames.Count} total over {img.Frames.Count} frames)");
                        }
                    } else if (command == "saskjlkjksljad") await msg.Reply(CompletelyRandomResponses.Random());
                    
                }
                else if (msgContent.ToLower().StartsWith("sudo ")) {
                    if (!Semiconductors.Contains(msg.Author.Id)) {
                        await msg.SendToNahui();
                        return Task.CompletedTask;
                    }

                    string command = msgContent[5..];
                    string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (msg.Channel is IGuildChannel guildChannel) {
                        if (command.StartsWith("pacman -Sybau ")) {
                            string thing = msgContent[19..].Trim();
                            if (string.IsNullOrEmpty(thing)) await msg.SendToNahui();
                            else {
                                IGuildUser? victim = await guildChannel.GetUserFromLabel(thing);
                                if (victim == null) await msg.Reply("dunno i can't find that guy");
                                else if (victim.Id == client.CurrentUser.Id) await msg.SendToNahui();
                                else
                                    try {
                                        await victim.SetTimeOutAsync(new TimeSpan(28, 0, 0, 0));
                                        await msg.AddReactionAsync(Emoji.Parse("✅"));
                                        await Log("pacman -Sybau", $"Timed out {victim.Username}");
                                    }
                                    catch (Exception e) {
                                        await msg.Reply("cant 💀");
                                        await Log("pacman -Sybau", $"Failed to timeout {victim.Username}",
                                            exception: e);
                                    }
                            }
                        }
                        else if (args[0] == "usermod" && args.Length >= 2) {
                            IGuildUser? victim = await guildChannel.GetUserFromLabel(args[1]);
                            string nick = command[(args[0].Length + args[1].Length + 2)..];
                            if (victim == null) {
                                await msg.Reply("dunno i can't find that guy");
                                return Task.CompletedTask;
                            }

                            if (string.IsNullOrWhiteSpace(nick)) {
                                await msg.Reply("empty nickname somehow");
                                return Task.CompletedTask;
                            }

                            try {
                                await victim.ModifyAsync(x => x.Nickname = nick == "-r" ? null : nick);
                                await msg.AddReactionAsync(Emoji.Parse("✅"));
                                await Log("usermod", $"Set {victim.Username}'s nickname to {nick}");
                            }
                            catch (Exception e) {
                                await msg.Reply("cant 💀");
                                await Log("usermod", $"Failed to set nickname of {victim.Username} to {nick}",
                                    exception: e);
                            }
                        }
                    }
                    else await msg.SendToNahui();
                }
                else if (msgContent.StartsWith("yay -Sybau ")) {
                    await msg.AddReactionAsync(Emoji.Parse("🥴"));
                }
                else if (msgContent.StartsWith("hey octopusgpt ")) {
                    if (Rand.Next(4) == 0) {
                        await msg.Reply($"octopusgpt isn't here so i asked him and he says {IdiNahui()}");
                    }
                }
                else if (msgContent ==
                         "https://cdn.discordapp.com/attachments/1042064947867287646/1464854577840128247/attachment.gif" &&
                         msg.Author.Id == Unimeter)
                    await msg.DeleteAsync();
                else if (msgContent == "includes unimeter 😎🍘🌾") await msg.SendToNahui(10);
                else if (msgContent == "ico!sex") await msg.Reply("https://cdn.discordapp.com/attachments/1163847466014220339/1474853802699133176/convert.gif");
                else if (msg.Content.EndsWith("kreisicoins just appeared! type 'kreisi' to take them!") &&
                         msg.Author.Id == Unimeter && Rand.Next(20) == 0) await msg.Channel.SendMessageAsync("kreisi");
                else if (KreisicoinMessage.IsMatch(msg.Content) && msg.Author.Id == Unimeter)
                    await msg.Channel.SendMessageAsync("https://cdn.discordapp.com/attachments/1163847466014220339/1479919974868062402/convert.gif?ex=69adca61&is=69ac78e1&hm=1a37bdb8f3d4bd3dbe004d57cfbb8d7c2cc5922cf3c53a22655eb23277a41cd5&");
                else if (PTSD.Contains(msg.Content.ToLower())) await msg.AddReactionAsync(Emote.Parse("<:ptsd:1480100990416982096>"));
                else if (msg.MentionedEveryone) await msg.Reply("big mistake");
                else if ((msg.Channel is IGuildChannel ch &&
                          msg.IsMentioned(await ch.Guild.GetUserAsync(client.CurrentUser.Id)) && !msg.Author.IsBot) ||
                         msg.Channel is IDMChannel) {
                    if (asleep) {
                        if (!whopinged.Contains(msg.Channel)) whopinged.Enqueue(msg.Channel);
                        await Log("Ping", "Asleep");
                    }
                    else if (Rand.Next(10) == 0) await msg.Reply(String.Format(PingMsgs.RandomerRandom(), msg.Author.Id));
                } else if (isУтпдшыр.HasValue && isУтпдшыр.Value) {
                    await msg.Reply($"-# Automatic translation from утпдшыр\n{msgContent}", allowedMentions:  AllowedMentions.None);
                } else if (isУтпдшыр is null) {
                    await msg.Reply($"-# Automatic translation from english\n{msgContent}", allowedMentions:  AllowedMentions.None);
                }
            } catch (Exception e) {
                await client.SetCustomStatusAsync($"Oops! All {e.GetType().Name}s");
                await Log("MessageReceived", "Error", LogSeverity.Error, e);
                if ((DateTime.Now - LastException.Item1).TotalSeconds > 10 || LastException.Item2 != e.GetType().Name) {
                    LastException = (DateTime.Now, e.GetType().Name);
                    await msg.Reply(embed: e.ErrorEmbed());
                } else await msg.AddReactionAsync(Emoji.Parse("💥"));
            }

            return Task.CompletedTask;
        }

        private readonly Dictionary<string, string> penalCodes = new() {
            { "17f", "fugitive17f" },
            { "27", "attemptedcrime27" },
            { "51", "nonsanctionedarson51" },
            { "51b", "threattoproperty51b" },
            { "62", "alarms62" },
            { "63", "criminaltrespass63" },
            { "63s", "illegalinoperation63s" },
            { "69", "posession69" },
            { "94", "weapon94" },
            { "95", "illegalcarrying95" },
            { "99", "recklessoperation99" },
            { "148", "resistingpacification148" },
            { "243", "assault243" },
            { "404", "riot404" },
            { "415", "disturbingunity415" },
            { "507", "publicnoncompliance507" },
            { "647e", "disengaged647e" }
        };

        private Dictionary<string, string> penalCodeNames = new() {
            { "17f", "17f Fugitive detachment" },
            { "27", "attemptedcrime27" },
            { "51", "nonsanctionedarson51" },
            { "51b", "threattoproperty51b" },
            { "62", "alarms62" },
            { "63", "criminaltrespass63" },
            { "63s", "illegalinoperation63s" },
            { "69", "posession69" },
            { "94", "weapon94" },
            { "95", "illegalcarrying95" },
            { "99", "recklessoperation99" },
            { "148", "resistingpacification148" },
            { "243", "assault243" },
            { "404", "riot404" },
            { "415", "disturbingunity415" },
            { "507", "publicnoncompliance507" },
            { "647e", "disengaged647e" }
        };

        async void GeneratorIsMyName(SocketMessage msg) {
            if (msg.Author.Id == 439205512425504771 && msg.Components.Count > 0 && msg.Reference is not null &&
                msg.Reference.MessageId.IsSpecified) {
                await Log(new LogMessage(LogSeverity.Info, "Generator", "NotSoBot message detected"));
                Console.WriteLine(msg.Embeds.Count);
                var msg2 = await msg.Channel.GetMessageAsync(msg.Reference.MessageId.Value);
                if (msg2.Content == ".meme generator is my name 😁🦂😁🦂") {
                    if (!Directory.Exists($"{datadir}/generatorismyname"))
                        Directory.CreateDirectory($"{datadir}/generatorismyname");
                    if (!Directory.Exists($"{datadir}/generatorismyname/archive"))
                        Directory.CreateDirectory($"{datadir}/generatorismyname/archive");
                    if (msg.Components.ElementAt(0).Type == ComponentType.Container &&
                        ((ContainerComponent)(msg.Components.ElementAt(0))).Components.ElementAt(0).Type ==
                        ComponentType.MediaGallery) {
                        await Log(new LogMessage(LogSeverity.Info, "Generator", "Saving image"));
                        string url =
                            ((MediaGalleryComponent)((ContainerComponent)msg.Components.ElementAt(0)).Components
                                .ElementAt(0)).Items.ElementAt(0).Media.Url;
                        var obj = new {
                            Author = msg2.Author.Id,
                            ImageUrl = url,
                            Timestamp = msg.CreatedAt.ToUnixTimeSeconds()
                        };
                        string json = JsonConvert.SerializeObject(obj);
                        File.WriteAllText(
                            $"{datadir}/generatorismyname/{Directory.GetFiles($"{datadir}/generatorismyname").Length}.json",
                            json);
                        ShutTheFuckUpAboutThisBeingDeprecated.DownloadFile(url,
                            $"{datadir}/generatorismyname/archive/{Directory.GetFiles($"{datadir}/generatorismyname").Length}.{url.Split('.').Last().Split('?')[0]}");
                        await msg.AddReactionAsync(Emoji.Parse("💾"));
                        await Log(new LogMessage(LogSeverity.Info, "Generator",
                            $"Saved {Directory.GetFiles($"{datadir}/generatorismyname").Length}"));
                    }
                    else {
                        await Log(new LogMessage(LogSeverity.Info, "Generator", "Oops"));
                    }
                }
            }
        }

        private async Task<Task> Client_SelectMenuExecuted(SocketMessageComponent cmd) {
            await interactionService.ExecuteCommandAsync(new InteractionContext(client, cmd, cmd.Channel), null);
            return Task.CompletedTask;
        }

        private async Task<Task> SlashCommandExecuted(SocketInteraction cmd) {
            try {
#if DEBUG
                if (cmd is IComponentInteraction ci) {
                    await Log("Component interaction", ci.Data.CustomId);
                } else if (cmd is ISlashCommandInteraction sc) {
                    await Log("Interaction", sc.Data.Name);
                }
#endif
                await interactionService.ExecuteCommandAsync(new InteractionContext(client, cmd, cmd.Channel), null);
            } catch (Exception e) {
                if (cmd.HasResponded) await cmd.FollowupAsync(embed: e.ErrorEmbed());
                else await cmd.RespondAsync(embed: e.ErrorEmbed());
            }

            return Task.CompletedTask;
        }

        private bool asleep;

        private async Task<Task> Ready() {
            try {
                LoadConfig(out _);
                await interactionService.RegisterCommandsGloballyAsync();
                StartTime = DateTime.Now;
                await client.SetCustomStatusAsync("Rise and shine, Mr. Freeman");
                budilnik.AutoReset = true;
                budilnik.Interval =
#if DEBUG
                    20000
#else
                    60000
#endif
                    ;
                budilnik.Elapsed += async (_, _) => {
                    bool unstash = Rand.Next(2) == 0;
                    if (Rand.Next(2) == 0) {
                        asleep = !asleep;
                        await client.SetStatusAsync(asleep ? UserStatus.AFK : UserStatus.Online);
                        if (!asleep) unstash = true;
                        else {
                            await Log("Status", "Asleep");
                            await client.SetCustomStatusAsync("");
                        }
                    }

                    if (!asleep && Rand.Next(2) == 0) {
                        var (type, text) = stati.Random();
                        if (type == ActivityType.CustomStatus) await client.SetCustomStatusAsync(text);
                        else await client.SetGameAsync(text, type: type);
                        await ((IMessageChannel)await client.GetChannelAsync(
#if DEBUG
                            1203012662523469824
#else
                            1272557307212857426
#endif
                        )).SendMessageAsync(text);
                        await Log("Status", type.ToString().PadRight(24) + text);
                    }

                    if (unstash && whopinged.Count > 0) {
                        ISocketMessageChannel ch = whopinged.Dequeue();
                        await ch.SendMessageAsync("who pinged");
                        await Log("Ping", "Unsatshed ping");
                    }
                };
                budilnik.Start();
                CommandModule.helpEmbeds.Add("Why is this a thing", new EmbedBuilder {
                    Title = "Why is this a thing",
                    Author = new EmbedAuthorBuilder {
                        IconUrl = client.GetUserAsync(SupremeLeader)
                            .Result.GetAvatarUrl(),
                        Name = $"Small ramble from {client.GetUserAsync(SupremeLeader).Result.Username}, the author"
                    },
                    Description = """
                                  The primary reason for this bot to exist is i'm just bored a lot of the time and code random things into it
                                  It doesn't have a set purpose, doesn't have intended use cases, it just exists as salvation from my eternal boredom
                                  Infact, the bot that's currently running is the 5th or 6th rewrite of it! I did this so many times primarily because the codebase became unmaintainable since i'm very good at writing readable code /s
                                  If you're wondering, no AI code was used in (i think) any of these iterations and it's 100% human-made shitcode
                                  I really advise you to not add this bot to random places that have never heard of it. It contains a LOT of references to niche inside jokes inside my friend group that will require lots of explaining to understand so it's better to not do it to avoid unnecessary confusion
                                  Actually i lied a bit when i said it has no purpose. I *do* have a bit of vision of implementing complex behavior that would mimic a human user without any use of machine learning, just pure algorithms. This includes random status changes that affect the behavior, remembering things that happened, potentially interpreting user's messages to determine their meaning.
                                  The only example of this right now is how the bot periodically jumps between idle and online, and while idle it does not respond to mentions and only acknowledges them when switching to online

                                  I hope this clears out some confusion about the bot's existence and hope you have a great day!
                                  """,
                    Color = EmbedColor,
                    Footer = new EmbedFooterBuilder {
                        Text = "This was written in the middle of the night"
                    }
                });
            } catch (Exception e) {
                await client.GetUser(SupremeLeader).SendMessageAsync(embed: e.ErrorEmbed());
                await client.LogoutAsync();
                Environment.Exit(1);
            }
            return Task.CompletedTask;
        }

        private Task Log(string source, string message, LogSeverity severity = LogSeverity.Info,
            Exception? exception = null) {
            return Log(new LogMessage(severity, source, message, exception));
        }

        private Task Log(LogMessage msg) {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        }
    }
}