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
    [Command("save")]
    public async Task OatsAsync(string link, string filename)
    {
        WebClient Client = new WebClient();
        if (File.Exists($@"/var/www/waggles.org/html/img/{ filename}.png") || File.Exists($@"/var/www/waggles.org/html/img/{ filename}.jpeg") || File.Exists($@"/var/www/waggles.org/html/img/{ filename}.jpg") || File.Exists($@"/var/www/waggles.org/html/img/{ filename}.gif") || (File.Exists($@"/var/www/waggles.org/html/img/{ filename}.gif")))
        { await ReplyAsync("File already exists try a different name!"); return; }
        if (link.Contains("png"))
        {
            Client.DownloadFile(link, $@"/var/www/waggles.org/html/img/{filename}.png");

            await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}.png>");
        }
        else if (link.Contains(".jpeg"))
        {
            Client.DownloadFile(link, $@"/var/www/waggles.org/html/img/{filename}.jpeg");

            await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}.jpeg>");
        }
        else if (link.Contains(".jpg"))
        {
            Client.DownloadFile(link, $@"/var/www/waggles.org/html/img/{filename}.jpg");

            await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}.jpg>");
        }
        else if (link.Contains(".webm"))
        {
            Client.DownloadFile(link, $@"/var/www/waggles.org/html/img/{filename}.webm");

            await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}.webm>");
        }
        else if (link.Contains(".gif"))
        {
            Client.DownloadFile(link, $@"/var/www/waggles.org/html/img/{filename}.gif");

            await ReplyAsync($"Saved at <http://www.waggles.org/img/{filename}.gif>");
        }
        else { await ReplyAsync("unsupported image type, ask Hoovier to fix this!"); }

    }
    [Command("pick")]
    public async Task OatAsync()
    {
        var rand = new Random();
        var files = Directory.GetFiles(@"/var/www/waggles.org/html/img");

        var ponies = files[rand.Next(files.Length)];

        await ReplyAsync($"http://www.waggles.org/img/{Path.GetFileName(ponies)}");

    }
    [Command("search")]
    public async Task SearcgAsync(string sear)
    {


        var files = Directory.GetFiles(@"/var/www/waggles.org/html/img");

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

        var files = Directory.GetFiles(@"/var/www/waggles.org/html/img/");

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < files.Length; i++)
        {

            sb.Append(Path.GetFileName(files[i]));
            sb.Append(" ");
        }



        await ReplyAsync(sb.ToString());

    }
    
 }
