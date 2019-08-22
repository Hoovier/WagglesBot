using CoreWaggles;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WagglesBot.Modules;

public class Vtick : ModuleBase<SocketCommandContext>
{
    // Server Image Directory.
    // TODO: Make this a config file or environment variable option.
    private readonly string imgDirectory = @"/var/www/waggles.org/html/img/";

    [Command("save")]
    public async Task OatsAsync(string link, string filename)
    {
        // Get the file extension from the link.
        //   First, remove any URL query parameters. (Such as https://www.example.com/image.png?v=2)
        //   Second, grab the file extension if one exists.
        // @see: https://stackoverflow.com/a/23229959
        Uri linkUri = new Uri(link);
        string linkPath = linkUri.GetLeftPart(UriPartial.Path);
        string linkExt = Path.GetExtension(linkPath).ToLower();

        // Test if target filename already exists on the server first!
        //   Implication: No files can share a base name, regardless of extension.
        //   Example: Cat.png and Cat.jpg cannot coexist, for example.
        var howManyFiles = Directory.GetFiles(this.imgDirectory, $@"{filename}.*").Length;
        if (howManyFiles > 1)
        { await ReplyAsync("File already exists try a different name!"); return; }

        switch (linkExt) {
            case ".png":
            case ".jpg":
            case ".jpeg":
            case ".webm":
            case ".gif":
                // Instantiate a WebClient, download the file, then automatically dispose of the client when finished.
                using (WebClient client = new WebClient()) {
                    client.DownloadFile(link, $@"{this.imgDirectory}{filename}{linkExt}");
                }
                await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}{linkExt}>");
                break;
            default:
                await ReplyAsync("unsupported image type, ask Hoovier to fix this!");
                break;
        }
    }
    [Command("pick")]
    public async Task OatAsync()
    {
        var rand = new Random();
        var files = Directory.GetFiles(this.imgDirectory);

        var ponies = files[rand.Next(files.Length)];

        await ReplyAsync($"http://www.waggles.org/img/{Path.GetFileName(ponies)}");

    }
    [Command("search")]
    public async Task SearcgAsync(string sear)
    {
        var files = Directory.GetFiles(this.imgDirectory);

        bool foundflag = false;
        string lonk = "404notfound.png";
        int fuzz = 3;
        foreach (string filename in files)
        {
            if (sear == Path.GetFileNameWithoutExtension(filename))
            {
                foundflag = true;
                await ReplyAsync($"https://www.waggles.org/img/{Path.GetFileName(filename)}");
                break;
            }
            else if (levenshtein.Compute(Path.GetFileNameWithoutExtension(filename), sear) < fuzz)
            {
                fuzz = levenshtein.Compute(Path.GetFileNameWithoutExtension(filename), sear);
                lonk = Path.GetFileName(filename);
            }


        }
        if (!foundflag)
        {

            await ReplyAsync($"https://www.waggles.org/img/{Path.GetFileName(lonk)}");
        }



    }
    [Command("list")]
    public async Task ListAsync()
    {
        var files = Directory.GetFiles(this.imgDirectory);
        await ReplyAsync(String.Join(" ", files));
    }
    
 }
