using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.RegularExpressions;
using Discord.Webhook;
using Newtonsoft.Json;

namespace Icosahedron;

internal static partial class Data {
    public static DiscordSocketClient client;
    public static DateTime StartTime = DateTime.MinValue;
    public static string datadir =
#if DEBUG
        "/home/cube/.local/share/relaybot";
#else
        "/home/cube/.local/share/icosahedron";
#endif
    #region Peoples
    public const ulong SupremeLeader = 801078409076670494;
    public const ulong Unimeter = 1295364360419541125;
    public static ulong[] Semiconductors = [];
    #endregion

    public static Color EmbedColor =
#if DEBUG
        0x63B2F5;
#else
        0x715497;
#endif
    public static DateTime WaitingForSearchSince = new DateTime(2026, 2, 19, 18, 0, 0);
    public static async Task<IUserMessage> Reply(this IMessage message, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageComponent components = null, ISticker[] stickers = null, Embed[] embeds = null, MessageFlags flags = MessageFlags.None) {
        return await message.Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, new MessageReference(message.Id), components, stickers, embeds, flags);
    }

    public static string[] ErrorMsgs = [
    ];

    public static string[] PingMsgs = [];

    public static string[] IsThisTrue = [];

    public static string[] CompletelyRandomResponses = [];
    public static string[] Memes = [];

    public static string[] PTSD = [
        "g bad",
        "h good"
    ];
    public static T Random<T>(this IEnumerable<T> list) {
        var enumerable = list as T[] ?? list.ToArray();
        return enumerable.ElementAt(Rand.Next(enumerable.Count()));
    }
    public static string RandomerRandom(this IEnumerable<string> list, int chanceOfSilly = 100) {
        return Rand.Next(chanceOfSilly) == 0 ? CompletelyRandomResponses.Random() : list.Random();
    }

    public static byte[] bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];

    public static float Cram(float x, float num, int type = 0) => (float)(type switch {
        -1 => Math.Floor(x*num)/num,
        1 => Math.Ceiling(x*num)/num,
        _ => Math.Round(x*num)/num
    });
    

    public static float Dither(float v, float b, float t, float m) => (v - b) / (t - b) >= m ? t : b;
    

    public static readonly string[] ImageTypes = [
        "image/bmp",
        "image/png",
        "image/gif",
        "image/jpeg",
        "image/tiff",
        "image/webp",
        "image/svg+xml"
    ];

    
    public static void MakePageEmbed<T>(string title, string id, IEnumerable<T> list, Func<T, string> nameGetter, int page,
        out Embed embed,
        out MessageComponent components) {
        var enumerable = list as T[] ?? list.ToArray();
        switch (page) {
            case int.MinValue:
                MakePageEmbed(title, id, enumerable, nameGetter, 0, out embed, out components);
                return;
            case int.MaxValue:
                MakePageEmbed(title, id, enumerable, nameGetter, (int)Math.Ceiling(enumerable.Length / 10.0) - 1, out embed, out components);
                return;
        }

        EmbedBuilder embedBuilder = new() {
            Color = EmbedColor,
            Title = title,
            Footer = new EmbedFooterBuilder {
                Text = $"Page {page + 1}/{Math.Ceiling(enumerable.Length / 10.0)}"
            }
        };
        
        for (int i = 0; i < 10; i++) {
            int idx = i + page * 10;
            if (idx >= enumerable.Length) break;
            embedBuilder.Description += $"**{idx + 1}**. {nameGetter(enumerable[idx])}\n";
        }
        embed = embedBuilder.Build();
        components = new ComponentBuilder {
            ActionRows = [
                new() {
                    Components = [
                        new ButtonBuilder("<<", $"{id}-{int.MinValue}", ButtonStyle.Secondary, null, null, page == 0),
                        new ButtonBuilder("<", $"{id}-{page-1}", ButtonStyle.Primary, null, null, page == 0),
                        new ButtonBuilder(">", $"{id}-{page+1}", ButtonStyle.Primary, null, null, page == (int)Math.Ceiling(enumerable.Length / 10.0) - 1),
                        new ButtonBuilder(">>", $"{id}-{int.MaxValue}", ButtonStyle.Secondary, null, null, page == (int)Math.Ceiling(enumerable.Length / 10.0) - 1),
                    ]
                }
            ]
        }.Build();
    }
    // source, target, target webhook
    public static (ulong, ulong, DiscordWebhookClient)? ServerScopeState = null;

    public static HttpClient HttpClient = new();
    // source, target
    private static Dictionary<ulong, ulong> MessageCache = new();
    public static async Task CopyMessage(IMessage message, DiscordWebhookClient webhook) {
        if (string.IsNullOrEmpty(message.Content) && message.Embeds.Count == 0 && message.Attachments.Count == 0) return;
        ulong newmsg;
        string content = message.Reference is not null && message.Reference.MessageId.IsSpecified ?
            $"-# - **{(await message.Channel.GetMessageAsync(message.Reference.MessageId.Value)).Author.Username}**: {(await message.Channel.GetMessageAsync(message.Reference.MessageId.Value)).CleanContent.Ellipsis(50)}\n{message.Content}".Ellipsis(2000)
            : message.Content;
        if (message.Attachments.Count == 0) {
            newmsg = await webhook.SendMessageAsync(content, false, from x in message.Embeds select (Embed)x,
                message.Author.Username, message.Author.GetAvatarUrl());
        }
        else {
            List<IMessageComponentBuilder> comps = [];
            if (message.CleanContent.Length > 0) comps.Add(new TextDisplayBuilder(content));
            var sanitized = (from x in message.Attachments
                where x.ContentType.Split('/')[0] is "image" or "video"
                select new MediaGalleryItemProperties(new UnfurledMediaItemProperties(x.Url))).ToArray();
            if (sanitized.Length > 0) comps.Add(new MediaGalleryBuilder(sanitized));
            if (comps.Count == 0) return;
            ComponentBuilderV2 builder = new(comps);
            newmsg = await webhook.SendMessageAsync(components: builder.Build(), username: message.Author.Username, avatarUrl: message.Author.GetAvatarUrl());
        }
        MessageCache.Add(newmsg, message.Id);
    }
    public static async Task CopyMessage(IMessage message, IMessageChannel channel) {
        if (string.IsNullOrEmpty(message.Content) && message.Embeds.Count == 0 && message.Attachments.Count == 0) return;
        MessageReference? refer = message.Reference is not null && message.Reference.MessageId.IsSpecified ? 
            MessageCache.TryGetValue(message.Reference.MessageId.Value, out var id) ? new MessageReference(id, channel.Id) : null
            : null;
        if (message.Attachments.Count == 0) {
            await channel.SendMessageAsync(message.Content, false, embeds: (from x in message.Embeds select (Embed)x).ToArray(), messageReference: refer);
        }
        else {
            List<IMessageComponentBuilder> comps = [];
            if (message.CleanContent.Length > 0) comps.Add(new TextDisplayBuilder(message.CleanContent));
            var sanitized = (from x in message.Attachments
                where x.ContentType.Split('/')[0] is "image" or "video"
                select new MediaGalleryItemProperties(new UnfurledMediaItemProperties(x.Url))).ToArray();
            if (sanitized.Length > 0) comps.Add(new MediaGalleryBuilder(sanitized));
            if (comps.Count == 0) return;
            ComponentBuilderV2 builder = new(comps);
            await channel.SendMessageAsync(components: builder.Build(), messageReference: refer);
        }
    }
    
    public static void MakePageEmbed(string title, string id, IEnumerable<string> list, int page, out Embed embed,
        out MessageComponent component) {
        MakePageEmbed(title, id, list, x => x, page, out embed, out component);
    }

    [Obsolete("i don't fucking care that it's obsolote fuck off")] public static readonly WebClient ShutTheFuckUpAboutThisBeingDeprecated = new();
    public static Embed ErrorEmbed(this Exception e, bool dmCube = false) {
        try {
            EmbedBuilder embed = new() {
                Color = 0xAF2D2A,
                Footer = new EmbedFooterBuilder {
                    Text = ErrorMsgs.Random()
                }
            };
            if (e is NotImplementedException) {
                embed.Title = "Not implemented";
                embed.Description = "This feature has not been fully implemented yet";
                if (!string.IsNullOrEmpty(e.Message)) {
                    embed.AddField("Developer message", e.Message);
                }
            } else {
                embed.Title = e.GetType().Name;
                embed.Description = e.Message;
            }
            if (e.StackTrace != null) {
                embed.AddField("Stack trace", e.StackTrace.Length <= EmbedFieldBuilder.MaxFieldValueLength
                    ? $"```\n{e.StackTrace}\n```"
                    : "Too long to show immediately"
                );
            }
            if (dmCube) client.GetUser(SupremeLeader).SendMessageAsync(embed: e.ErrorEmbed());
            return embed.Build();
        }
        catch (Exception ee) {
            EmbedBuilder embed = new EmbedBuilder {
                Color = 0xAF2D2A,
                Title = "The error embed generator had an error <:normal:1275453792002773146>",
                Description = e.Message
            };
            
            if (ee.StackTrace != null) {
                embed.Fields.Add(new() {
                    IsInline = true,
                    Name = "Stack trace",
                    Value = ee.StackTrace.Length <= EmbedFieldBuilder.MaxFieldValueLength
                        ? $"```\n{ee.StackTrace}\n```"
                        : "Too long to show immediately"
                });
            }
            if (dmCube) client.GetUser(SupremeLeader).SendMessageAsync(embed: e.ErrorEmbed());
            return embed.Build();
        }
    }

    public static (DateTime, string?) LastException = (new DateTime(2000, 1, 1), null);
    public static long LoadConfig(out Exception? e) {
        string DJson = File.ReadAllText(Path.Join(datadir, "things.json"));
        long bytes = new FileInfo(Path.Join(datadir, "things.json")).Length;
        
        var template = new {
            Semiconductors = Array.Empty<ulong>()
        };
        var jayson = JsonConvert.DeserializeAnonymousType(DJson, template);
        if (jayson is null) {
            e = new Exception("Couldn't deserialize things.json");
            return 0;
        }
        Semiconductors = jayson.Semiconductors;
        DJson = File.ReadAllText(Path.Join(datadir, "random_lines.json"));
        bytes += new FileInfo(Path.Join(datadir, "random_lines.json")).Length;
        var templateToo = new {
            errormsgs = Array.Empty<string>(),
            pingmsgs = Array.Empty<string>(),
            isthistrue = Array.Empty<string>(),
            completelyrandomresponses = Array.Empty<string>(),
            memes = Array.Empty<string>()
        };
        var jaysonToo = JsonConvert.DeserializeAnonymousType(DJson, templateToo);
        if (jaysonToo is null) {
            e = new Exception("Couldn't deserialize random_lines.json");
            return 0;
        }
        ErrorMsgs = jaysonToo.errormsgs;
        PingMsgs = jaysonToo.pingmsgs;
        IsThisTrue = jaysonToo.isthistrue;
        CompletelyRandomResponses = jaysonToo.completelyrandomresponses;
        Memes = jaysonToo.memes;
        e = null;
        return bytes;
    }

    public static async Task<IGuildUser?> GetUserFromLabel(this IGuildChannel guildChannel, string thing) {
        if (ulong.TryParse(thing, out ulong id) || thing.StartsWith("<@") && thing.EndsWith(">") &&
            ulong.TryParse(thing.Substring(2, thing.Length - 3), out id))
            return await guildChannel.GetUserAsync(id);
        if (client.GetUser(thing) is not null)
            return await guildChannel.GetUserAsync(client.GetUser(thing).Id);
        return null;
    }

    public static Random Rand = new Random();

    public static (ActivityType, string)[] stati = [
        (ActivityType.Watching, "you"),
        (ActivityType.CustomStatus, "woozy face"),
        (ActivityType.Competing, "shitting"),
        (ActivityType.Listening, "Core - Zef"),
        (ActivityType.Playing, "Pikuniku"),
        (ActivityType.Playing, "Geometry Dash"),
        (ActivityType.Playing, "Minecraft"),
        (ActivityType.Playing, "Noita"),
        (ActivityType.Playing, "Baba is you"),
        (ActivityType.Playing, "Team Fortress 2"),
        (ActivityType.Playing, "Garry's Mod"),
        (ActivityType.Playing, "Just Shapes & Beats"),
        (ActivityType.Listening, "Noita OST"),
        (ActivityType.Listening, "JSaB OST"),
        (ActivityType.Listening, "Baba Is You OST"),
        (ActivityType.Listening, "atrovillage.wav"),
        (ActivityType.Listening, "Basis Point - xetto"),
        (ActivityType.Listening, "Electrical Whisk - Slinx92 & Dtpls"),
        (ActivityType.CustomStatus, "Making up stories"),
        (ActivityType.CustomStatus, "How do i make music"),
        (ActivityType.Playing, "with electricity"),
        (ActivityType.CustomStatus, "Engineering"),
        (ActivityType.Watching, "paint dry")
    ];

    public static async Task<IMessage> SendToNahui(this IMessage message, int? chanceOfSilly = null) => await message.Reply(IdiNahui(chanceOfSilly));

    public static async Task<bool> CanMute(ulong id, IGuildChannel channel) {
        IGuildUser user = await channel.Guild.GetUserAsync(id);
        return user.GuildPermissions.MuteMembers || user.GetPermissions(channel).MuteMembers ||
               user.GuildPermissions.Administrator;
    }

    public static Dictionary<char, char> Утпдшыр = new() {
        { 'ф', 'a' },
        { 'и', 'b' },
        { 'с', 'c' },
        { 'в', 'd' },
        { 'у', 'e' },
        { 'а', 'f' },
        { 'п', 'g' },
        { 'р', 'h' },
        { 'ш', 'i' },
        { 'о', 'j' },
        { 'л', 'k' },
        { 'д', 'l' },
        { 'ь', 'm' },
        { 'т', 'n' },
        { 'щ', 'o' },
        { 'з', 'p' },
        { 'й', 'q' },
        { 'к', 'r' },
        { 'ы', 's' },
        { 'е', 't' },
        { 'г', 'u' },
        { 'м', 'v' },
        { 'ц', 'w' },
        { 'ч', 'x' },
        { 'н', 'y' },
        { 'я', 'z' },
    };

    public static Dictionary<char, char> УтпдшырChars = new() {
        { 'ё', '`' },
        { 'х', '[' },
        { 'ъ', ']' },
        { 'ж', ';' },
        { 'э', '\'' },
        { 'б', ',' },
        { 'ю', '.' },
        { 'Ё', '~' },
        { 'Х', '{' },
        { 'Ъ', '}' },
        { 'Ж', ':' },
        { 'Э', '"' },
        { 'Б', '<' },
        { 'Ю', '>' }
    };

    public static readonly Regex IllegalRussian = IllegalRussianRegex();
    public static readonly Regex KreisicoinMessage = KreisicoinRegex();

    public static string DeУтпдшырify(this string str) {
        foreach (var pair in Утпдшыр) {
            str = str.Replace(pair.Key, pair.Value);
            str = str.Replace(char.ToUpper(pair.Key), char.ToUpper(pair.Value));
        }
        foreach (var pair in УтпдшырChars) {
            str = str.Replace(pair.Key, pair.Value);
        }
        return str;
    }
    public static string Утпдшырify(this string str) {
        foreach (var pair in Утпдшыр) {
            str = str.Replace(pair.Value, pair.Key);
            str = str.Replace(char.ToUpper(pair.Value), char.ToUpper(pair.Key));
        }
        foreach (var pair in УтпдшырChars) {
            str = str.Replace(pair.Value, pair.Key);
        }
        return str;
    }
    public static string IdiNahui(int? chanceOfSilly = null) {
        if (chanceOfSilly.HasValue && Rand.Next(chanceOfSilly.Value) == 0) return CompletelyRandomResponses.Random();
        int rand = Rand.Next(100);
        return rand switch {
            0 => "НАПИШИ СВОЕМУ ДРУГУ БОТА КОТОРЫЙ ПОСЫЛАЕТ ЕГО НАХУЙ",
            1 =>
                "Every day, миша☘️go over 5000 to The чо Rats за I фигню ты нисёш hear FitnessGram™ 🙄💅s Pacer have нахуй to type a пажалуста удали ето Test 😨 and lose is their 4wte sanity. Make that радители 😾собирайся в a 5wre make multistage комноте 👨‍👩‍👧 aerobic M4 less capacity and УДОЛЯЙ ЭТО СЕЙЧАЗ 💀 ААААААААА c4wsu test I 😊 that was    в  crazy progressively   gets садик🏡идидиди  once   they put 😭misha me  on a  room more get   difficult  a   round up room  5lunr  quickly as  it 🥺ДА ИДЕ rubber  room  НАХУУУУУ 🐀😅   continues.  заткнись курица😤shut  up The with rats  round     rats 20   4kunf   4yvvet  chicken😡пи$да4ек tkwte 34ll     прикрыла😋(я абослют)😈зткнс 6ha5e   likeitr meter  крца🤐зоткися pacer   I'd",
            2 => "славянский зажим яйцами",
            3 => "НЕ СТОЙТИ И НИ ПРЫГАЙТИ 🧍‍♂️ НЕ ПОЙТЕ НИ КРИЧИТЕ 😱 ТАМ ГДЕ ИДЕТ СТРАИТИЛЬСТВА 🏗️ ИЛИ ПАДВЕШИН ГРУЗ 🏀",
            4 => "А ТЫ СМОТРЕЛ УРАЛЬСКИЕ ПЕЛЬМЕНИ",
            5 => "четоwrong пирог無しだ_ARGS",
            6 => "yo mama so FAT32 she triggered an integer overflow 😂😂🔧⁉️",
            7 => "пиздуй к офтальмологу",
            8 => "пиздуй в пизду",
            9 => "заткнись курица",
            _ => "иди нахуй"
        };
    }

    public static void DownloadImage(string url) {
        if (!Directory.Exists("/tmp/icosahedron")) Directory.CreateDirectory("/tmp/icosahedron");
                            
        ShutTheFuckUpAboutThisBeingDeprecated.DownloadFile(url,
            $"/tmp/icosahedron/image");
    }
    public static bool IsMentioned(this IMessage msg, IGuildUser user) {
        return msg.MentionedEveryone || msg.MentionedUserIds.Contains(user.Id) || (from x in user.RoleIds where msg.MentionedRoleIds.Contains(x) select x).Any();
    }

    public static string Ellipsis(this string str, int maxLength) {
        if (maxLength <= 3) throw new ArgumentException("Maximum length must be equal to or bigger than 4", nameof(maxLength));
        return str.Length <= maxLength ? str : str[..(maxLength-3)]+"..."; 
    }

    [GeneratedRegex("[a-z][абвгдеёжзийклмнопрстуфхцчшщъыьэюя]|[абвгдеёжзийклмнопрстуфхцчшщъыьэюя][a-z]|(\\n|^| )(ь|ъ|ы)[абвгдеёжзийклмнопрстуфхцчшщъыьэюя]|ёё|ёщ|ыё|ёу|йэ|гъ|кщ|щф|щз|эщ|щк|гщ|щп|щт|щш|щг|щм|фщ|щл|щд|дщ|ьэ|чц|вй|ёц|ёэ|ёа|йа|шя|шы|ёе|йё|гю|хя|йы|ця|гь|сй|хю|хё|ёи|ёо|яё|ёя|ёь|ёэ|ъж|эё|ъд|цё|уь|щч|чй|шй|шз|ыф|жщ|жш|жц|ыъ|ыэ|ыю|ыь|жй|ыы|жъ|жы|ъш|пй|ъщ|зщ|ъч|ъц|ъу|ъф|ъх|ъъ|ъы|ыо|жя|зй|ъь|ъэ|ыа|нй|еь|цй|ьй|ьл|ьр|пъ|еы|еъ|ьа|шъ|ёы|ёъ|ът|щс|оь|къ|оы|щх|щщ|щъ|щц|кй|оъ|цщ|лъ|мй|шщ|ць|цъ|щй|йь|ъг|иъ|ъб|ъв|ъи|ъй|ъп|ър|ъс|ъо|ън|ък|ъл|ъм|иы|иь|йу|щэ|йы|йъ|щы|щю|щя|ъа|мъ|йй|йж|ьу|гй|эъ|уъ|аь|чъ|хй|тй|чщ|ръ|юъ|фъ|уы|аъ|юь|аы|юы|эь|эы|бй|яь|ьы|ьь|ьъ|яъ|яы|хщ|дй|фй")]
    private static partial Regex IllegalRussianRegex();
    [GeneratedRegex(@"\*\*icosahedron\*\* just got \*\*[0-9]+\*\* kreisicoins")]
    private static partial Regex KreisicoinRegex();
}