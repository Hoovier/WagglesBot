using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class OpenAICommands : ModuleBase<SocketCommandContext>
    {
        [Command("AIcomplete")]
        [Alias("aicomplete", "aic")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task OpenAiCompleteCommand(string start)
        {
            if(Context.Message.Author.Id != 223651215337193472)
            {
                await ReplyAsync("This command costs Hoovier money! Please dont mess with it!");
                return;
            }
            await ReplyAsync("Thinking...");
            var result = await Global.api.Completions.CreateCompletionAsync(new OpenAI_API.CompletionRequest(start, max_tokens: 400, temperature: .8, frequencyPenalty: .8));
            string response = $"Using Model: {result.Model.EngineName}\nProcessing Time:{result.ProcessingTime}\nResult: {result.ToString()}";
            await ReplyAsync(response);
        }
    }
}
