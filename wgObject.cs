using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles
{
    public class wgObject
    {
        public string letters;
        public string Solution;
        public Discord.Rest.RestUserMessage message;
        public Dictionary<ulong, int> scoreCard = new Dictionary<ulong, int>();
        List<string> foundWords = new List<string>();

        private void addPoints(ulong userId, int points)
        {
            if(!scoreCard.ContainsKey(userId))
            {
                scoreCard.Add(userId, points);
            }
            else
            {
                scoreCard[userId] = scoreCard[userId] + points;
            }
        }
        public async Task checkWord(Discord.Commands.SocketCommandContext context, string word)
        {
            if(foundWords.Contains(word))
            {
                await context.Message.AddReactionAsync(new Emoji("🔁"));
                return;
            }
            foreach (char item in word)
            {
                if(!letters.Contains(item))
                {
                    await context.Message.AddReactionAsync(new Emoji("❌"));
                    return;
                }
            }
            using (StreamReader sr = new StreamReader("sowpods.txt"))
            {
                string line = "";
                while((line = sr.ReadLine()) != null)
                {
                    if(line == word)
                    {
                        if(word.Length >= 7)
                        {
                            await context.Channel.SendMessageAsync($"7+ letter word was found, game over!");
                            addPoints(context.Message.Author.Id, word.Length);
                            string scoreboard = "**Scores:**\n";
                            foreach (KeyValuePair<ulong, int> item in Global.wordGameDic[context.Channel.Id].scoreCard)
                            {
                                scoreboard = scoreboard + Commands.DBTransaction.getUserFromID(item.Key) + ": " + item.Value + "\n";
                            }
                            await context.Channel.SendMessageAsync(scoreboard);
                            Global.wordGameDic.Remove(context.Channel.Id);
                            return;
                        }
                        foundWords.Add(word);
                        addPoints(context.Message.Author.Id, word.Length);
                        string newMessage = message.Content + " " + word;
                        await message.ModifyAsync(msg => msg.Content = newMessage);
                        await context.Message.DeleteAsync();
                        return;
                    }
                }
                await context.Message.AddReactionAsync(new Emoji("❌"));
            }
        }
    }
}
