using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles.Commands
{
    public class sentimentAnalysis : ModuleBase<SocketCommandContext>
    {
        [Command("analyze")]
        [Alias("anal", "an")]

        public async Task analyzeAsync(SocketGuildUser name)
        {
            // Create single instance of sample data from first line of dataset for model input
            string response = DBTransaction.pickQuoteRaw(Context.Guild.Id, name.Id);
            SampleClassification.ModelInput sampleData = new SampleClassification.ModelInput()
            {
                Col0 = @response,
            };
            await ReplyAsync("**[" + name.Username + "]:** " + @response);

            var sortedScoresWithLabel = SampleClassification.PredictAllLabels(sampleData);

            Dictionary<string,double> values = new Dictionary<string,double>();

            foreach (var score in sortedScoresWithLabel)
            {
                values.Add(score.Key, score.Value);
            }
            if (values["1"] > values["0"])
            {
                await ReplyAsync(name.Username + " seems positive in this message, I'm " + (int)(values["1"] * 100) + "% sure!");
            }
            else
            {
                await ReplyAsync(name.Username + " seems negative in this message, I'm " + (int)(values["0"] * 100) + "% sure!");
            }

        }
    }
}
