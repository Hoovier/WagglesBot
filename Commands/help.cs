using CoreWaggles;
using Discord;
using Discord.Commands;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

[Group("help")]
public class Repsonsetest : ModuleBase<SocketCommandContext>
{
    [Command]
    public async Task PingAsync()
    {
        RestUserMessage msg = await Context.Channel.
            SendMessageAsync("**__Hi there! I'm Waggles, let me show you what I can do!__** \n I don't wanna cloud your view, so I made a machine to help me help you. All you have to do is react with one of the following emojis to get its corresponding page. \n :wrench: - My admin tools! \n :tada: - The funnest commands! \n :house: - Takes you to the homepage. \n :horse: - Derpibooru commands!");
        await msg.AddReactionAsync(new Emoji("🔧"));
        await msg.AddReactionAsync(new Emoji("🎉"));
        await msg.AddReactionAsync(new Emoji("🏠"));
        await msg.AddReactionAsync(new Emoji("🐴"));
        Global.MessageIdToTrack[Context.Channel.Id] = msg.Id;
        Global.MessageTotrack[Context.Channel.Id] = msg;
    }
    [Command]
    public async Task PingAsync(string command)
    {
        string commandLower = command.ToLower();
        switch (commandLower)
        {
            case "cookie":
                {
                    await ReplyAsync("The easter egg youre looking for isn't here!");
                    break;
                }
            case "boop":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Boop Command");
                    builder.AddField("Usage", "```~boop <user>```");
                    builder.AddField("Command Description", "This allows you to boop another user! The speed at which my hoof moves is determined by just how badly you want to break their nose!");
                    builder.AddField("Optional Args", "You can leave out a user and I'll pick a random nose to break.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/11/19/1589790.png");
                    builder.WithTitle("Boop Command");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "pony":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Pony Command");
                    builder.AddField("Usage", "```~pony```");
                    builder.AddField("Command Description", "This brings up a picture of me! I'm pretty sure its illegal to keep all this cuteness to myself.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2018/10/28/1868266.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "joke":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Joke Command");
                    builder.AddField("Usage", "```~joke```");
                    builder.AddField("Command Description", "This makes me tell you a joke. I cant guarantee any laughs but I'll try!");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2018/2/12/1654588.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "click":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Click Command");
                    builder.AddField("Usage", "```~click```");
                    builder.AddField("Command Description", "This __o__ne makes me count. Its re__a__lly not that in__t__eresting, try __s__omething else. Something to do with noses, maybe.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2014/1/18/527524.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "is":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Is Command");
                    builder.AddField("Usage", "```~is <question>```");
                    builder.AddField("Command Description", "This allows you to ask me a question! I'll try my best to guess the right answer. If I get it wrong, its your fault though.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2016/7/1/1191169.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "say":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Say Command");
                    builder.AddField("Usage", "```~say <words>```");
                    builder.AddField("Command Description", "Make me say something. If its dirty I'm telling!");
                    builder.AddField("<words>", "The sentence or phrase you want me to parrot");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2018/2/6/1649896.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }

            case "derpist":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Derpist Command");
                    builder.AddField("Usage", "```~derpi tag AND tag2 AND NOT tag3```");
                    builder.AddField("Command Description", "Returns the amount of results for a certain derpibooru query.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2018/6/22/1763520.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "derpi":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Derpi Command");
                    builder.AddField("Usage", "```~derpi tag AND tag2 AND NOT tag3```");
                    builder.AddField("Command Description", "This makes a derpibooru search for a certain combo of tags, posting 1 out of the top 50 results randomly.");
                    builder.AddField("Complex Queries", "Derpi allows for very complicated queries, the one mentioned above is the basic one. You can also do '~derpi id:imageidHere' or '~derpi faved_by:username' in tandem with the basic tags. ");
                    builder.AddField("Adding before the query allows you to change the sorting.", "0:Created at, 1:wilson, 2:relevance, 3:random");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/8/17/1513108.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "next":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Next Command");
                    builder.AddField("Usage", "```~next```");
                    builder.AddField("Command Description", "This reads the last derpibooru search made in this channel and returns another image of the collection.");
                    builder.AddField("Randomness", "The image chosen is not perfectly random, so you may receive duplicates from earlier ~nexts");
                    builder.AddField("Can I spam it?", "You can run this command as much as you want, the last search is saved locally so it does not contact Derpibooru at all.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/8/17/1513108.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "derpitags":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Derpitags Command");
                    builder.AddField("Usage", "```~derpitags <optional link>```");
                    builder.AddField("Command Description", "This command returns the tags of the image. If the linked image is blocked by her filter this may fail.");
                    builder.AddField("Empty Args", "You can run `~next` with no arguments to tell Waggles to find the last derpi-link posted in the channel and post the tags.");
                    builder.AddField("Combo Commands?", "You can run this command right after a `~derpi` or `~next` search, with no args, to post the tags of that very image!");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/8/17/1513108.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "artist":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Artist Command");
                    builder.AddField("Usage", "```~artist <optional link>```");
                    builder.AddField("Command Description", "This command returns the artist(s) of the image as well as links to their derpibooru page(s). If the linked image is blocked by her filter this may fail.");
                    builder.AddField("Empty Args", "You can run `~artist` with no arguments to tell Waggles to find the last derpi-link posted in the channel and post the artist(s).");
                    builder.AddField("Combo Commands?", "You can run this command right after a `~derpi` or `~next` search, with no args, to post the artist(s) of that very image!");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/8/17/1513108.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "wlist":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Whitelist Command");
                    builder.AddField("Usage", "```~wlist <list/remove/add> #channel```");
                    builder.AddField("Command Description", "This command whitelists a channel so that NSFW derpi results can be posted. If a channel is not in the whitelist, the search will simply be filtered to exclude those results.");
                    builder.AddField("Argument", "Pretty straightforward, add a channel to the whitelist, or remove one added earlier.");
                    builder.AddField("No Arg Commands?", "use '~wlist all' to whitelist every channel in the server. Use '~wlist list' to list all whitelisted channels.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2017/12/27/1616987.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "alias":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Alias Command");
                    builder.AddField("Usage", "```~alias <list/remove/add/edit/get> <New command name> <command and arguments> ```");
                    builder.AddField("Command Description", "This command provides a way to alias commands. Ex. ```alias add dailywaifu derpi female AND solo``` this allows you to run `dailywaifu` as if it was a command and then the rest of the command will be run for you.");
                    builder.AddField("Argument", "Pretty straightforward, add, remove, edit, or get an alias. Getting an alias returns the command associated with that alias.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2015/2/5/821737.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            case "witty":
                {
                    var builder = new EmbedBuilder();
                    builder.WithTitle("Witty Command");
                    builder.AddField("Usage", "```~witty <list/remove/add/edit/get> <WittyName> <Regex String> <0.0-1.0 chance> <'Response 1' 'Response 2' 'Response X'> ```");
                    builder.AddField("Command Description", "This command allows one to add witties, or responses to a certain message matching a Regex string");
                    builder.AddField("Argument", "Pretty straightforward, add, remove, or get a witty.");
                    builder.WithThumbnailUrl("https://derpicdn.net/img/view/2015/2/5/821737.png");
                    builder.WithColor(66, 244, 238);
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    break;
                }
            default:
                {
                    await Context.Channel.SendMessageAsync($"{command} is not a command I know. I triple checked!");
                    break;
                }
        }
    }

}
public class aliases : ModuleBase<SocketCommandContext>
{
    [Command("about")]
    [Alias("info", "?")]


    public async Task aboutAsync()
    {
        var builder = new EmbedBuilder();
        builder.AddField("Author", "Hoovier#4192");
        builder.AddField("Library", "[Discord.net](https://github.com/discord-net/Discord.Net)");
        builder.AddField("Owner ID", "223651215337193472");
        builder.AddField("Contributor", "LazyReader (MaxR#4813)");
        builder.AddField("Website", "https://Waggles.org/");
        builder.WithAuthor(Context.Client.CurrentUser);
        builder.Author.Name = "WagglesBot";
        builder.Author.IconUrl = "https://derpicdn.net/img/view/2019/8/24/2126246.png";
        builder.WithThumbnailUrl("https://www.waggles.org/public/images/logo.png");
        builder.WithColor(66, 244, 238);
        await Context.Channel.SendMessageAsync("", false, builder.Build());
    }
}