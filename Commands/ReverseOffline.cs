using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
   public  class ReverseOffline : ModuleBase<SocketCommandContext>
    {
        [Command("revOff")]
        public async Task RevAsync()
        {
            string output = "/home/hoovier/derpibooruDB/cli_intensities-master/image-intensities /home/hoovier/Mona/mona_by_partylikeanartist-dd9tryx.png".Bash();
            string myString = Regex.Replace(output, @"\s+", ",");
            string[] stuff = myString.Split(',');
            await ReplyAsync(output);
            await ReplyAsync(stuff.Length + $" results: {stuff[0]} {stuff[1]} {stuff[2]} {stuff[3]}" );

        }
    }
}
