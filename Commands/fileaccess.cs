using CoreWaggles;
using Discord.Commands;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

public class Vtick : ModuleBase<SocketCommandContext>
{
    // Server Image Directory.
    // TODO: Make this a config file or environment variable option.
    private readonly string imgDirectory = @"/var/www/waggles.org/html/img/";
    private readonly string notFoundImage = "404notfound.png";

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
        int howManyFiles = Directory.GetFiles(this.imgDirectory, $"{filename}.*").Length;
        if (howManyFiles > 0) {
            await ReplyAsync("File already exists try a different name!"); 
            return;
        }

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
            case null:
            case "":
                // Path.GetExtension() can return `null` or an empty string if a file extension or path failed.
                // @see: https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getextension
                await ReplyAsync("Link missing extension, cannot detect file type! Let Hoovier know if I messed up.");
                break;
            default:
                await ReplyAsync($"Unsupported image type ('{linkExt}'), ask Hoovier to fix this!");
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
    public async Task SearchAsync(string searchTerm)
    {
        // Get all image files, translate to file names without extensions.
        var files = Directory.GetFiles(this.imgDirectory);

        // Set default result to a 404 image. Will be overwritten if a match is found.
        string linkResult = this.notFoundImage;
        // Our fuzzing threshold, find file names within X edits of the search term.
        int fuzz = 3;

        foreach (string filepath in files)
        {
            // Get searchable name only for exact matches or levenstein. No extension.
            string bareName = Path.GetFileNameWithoutExtension(filepath);

            // Automatically return and end function if exact match found.
            if (searchTerm == bareName) {
                await ReplyAsync($"https://www.waggles.org/img/{Path.GetFileName(filepath)}");
                return;
            }

            // Store closest match (minimum edit distance) link and distance.
            if (levenshtein.Compute(bareName, searchTerm) < fuzz) {
                fuzz = levenshtein.Compute(bareName, searchTerm);
                linkResult = Path.GetFileName(filepath);
            }
        }

        // Default URL result return.
        string message = $"https://www.waggles.org/img/{Path.GetFileName(linkResult)}";

        // If it ended up being a close match and NOT a 404, give a friendly little preamble. :)
        if (!linkResult.Equals(this.notFoundImage)) {
            message = "Couldn't find an exact match, is this what you meant?\n" + message;
        }

        // Return our message, be it 404 or a close match.
        await ReplyAsync(message);
    }
    [Command("list")]
    public async Task ListAsync()
    {
        var files = Directory.GetFiles(this.imgDirectory);
        // Map file names instead of full paths.
        files = files.Select(file => Path.GetFileNameWithoutExtension(file)).ToArray();
        // Sort filenames alphabetically!
        Array.Sort(files, StringComparer.InvariantCulture);
        // TODO: Save above results to a TXT file, and serve the file instead if the results get too large (above 2000 characters).
        await ReplyAsync(String.Join(" | ", files));
    }
 }
