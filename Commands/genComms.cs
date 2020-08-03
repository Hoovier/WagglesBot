using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GeneralCOmmands : ModuleBase<SocketCommandContext>
{
    [Command("is")]
    public async Task PingAsync([Remainder] string kink)
    {
        Random rand = new Random();
        int pick = rand.Next(4);
        switch (pick)
        {
            case 3:
                await ReplyAsync($" Is {kink}? Of course!");
                break;
            case 1:
                await ReplyAsync($" Is {kink}? Of course not!");
                break;
            case 2:
                await ReplyAsync($" Is {kink}? Maybe! I'm not totally sure, sorry!");
                break;
            default:
                await ReplyAsync($" I asked Hoovier and he told me not to answer! \n I wonder why? ");
                break;


        }



    }

    [Command("pony")]
    public async Task PonyAsync()
    {
        string[] freshboops;

        freshboops = new string[]
        {
                "https://derpicdn.net/img/view/2018/5/12/1731029.png", "https://derpicdn.net/img/view/2018/10/28/1868277.png", "https://derpicdn.net/img/view/2018/4/26/1717588.jpeg", "https://i.imgur.com/EtaUkvP.png","https://derpicdn.net/img/view/2018/10/28/1868281.jpeg","https://derpicdn.net/img/view/2018/8/24/1814559.png","https://derpicdn.net/img/view/2018/8/23/1814438.png"
        };
        Random rand = new Random();

        int pick = rand.Next(freshboops.Length);

        await ReplyAsync(freshboops[pick]);

    }


    [Command("say")]
    public async Task SayAsync([Remainder] string kink)
    {
        await ReplyAsync(kink);



    }
    [Command("convert")]
    public async Task ConvertAsync(string type, Double inout)
    {
        Double result;
        switch (type)
        {
            case "c":
                result = ((inout * 9) / 5) + 32;
                await ReplyAsync($"{inout} celsius is equal to {result.ToString("#.##")} farenheit.");
                break;
            case "f":
                result = ((inout - 32) * 5) / 9;
                await ReplyAsync($"{inout} farenheit is equal to {result.ToString("#.##")} celsius.");
                break;

            default: await ReplyAsync("Unsupported temperature type!"); break;
        }
    }
    [Command("shutdown")]
    public async Task ShutdownAsync()
    {
        // await _command.ExecuteAsync(Context, "help");




        IEmote emote = Context.Guild.Emotes.First(e => e.Name == "rymwave");


        await ReplyAsync($"{emote}");
    }

    [Command("aliases")]
    public async Task AliasAsync()
    {
        // await _command.ExecuteAsync(Context, "help");

        var builder = new EmbedBuilder();

        builder.WithTitle("Aliases");
        builder.AddField("~Derpitags", "```~dt```");
        builder.AddField("~derpist", "```~st```");
        builder.AddField("~derpi", "```~d```");
        builder.AddField("~artist", "```~a```");
        builder.AddField("~next", "```~n```");
        builder.WithThumbnailUrl("https://derpicdn.net/img/view/2019/1/10/1931169.png");

        builder.WithTitle("Aliases");
        builder.WithColor(66, 244, 238);
        await Context.Channel.SendMessageAsync("", false, builder.Build());


        IEmote emote = Context.Guild.Emotes.First(e => e.Name == "rymwave");


        await ReplyAsync($"{emote}");
    }
    [Command("joke")]
    public async Task PlinkAsync()
    {
        string[] freshjokes;
        freshjokes = new string[]
            {
                "Why was the blue star bear not allowed into the R-rated movie? \n Because he was an Ursa Minor.","What do you call it when your sister refuses to lower the moon? Lunacy.", "What do you call it when a unicorn studies fashion? I don't know, but it sure is a rarity.", " How do unicorns signal impatience with slow ponies in front of them? They honk their horns."
        };
        Random rand = new Random();

        int pick = rand.Next(freshjokes.Length);

        await ReplyAsync(freshjokes[pick]);


    }
}

