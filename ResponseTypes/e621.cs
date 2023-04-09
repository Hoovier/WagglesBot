using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles
{
    public class e621
    {
        public class File
        {
            public int width { get; set; }
            public int height { get; set; }
            public string ext { get; set; }
            public int size { get; set; }
            public string md5 { get; set; }
            public string url { get; set; }
        }

        public class Preview
        {
            public int width { get; set; }
            public int height { get; set; }
            public string url { get; set; }
        }

        public class Sample
        {
            public bool? has { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public string url { get; set; }
        }

        public class Score
        {
            public int up { get; set; }
            public int down { get; set; }
            public int total { get; set; }
        }

        public class Tags
        {
            public List<string> general { get; set; }
            public List<string> species { get; set; }
            public List<string> character { get; set; }
            public List<object> copyright { get; set; }
            public List<string> artist { get; set; }
            public List<object> invalid { get; set; }
            public List<object> lore { get; set; }
            public List<object> meta { get; set; }
        }

        public class Flags
        {
            public bool pending { get; set; }
            public bool flagged { get; set; }
            public bool note_locked { get; set; }
            public bool? status_locked { get; set; }
            public bool rating_locked { get; set; }
            public bool deleted { get; set; }
        }

        public class Relationships
        {
            public object parent_id { get; set; }
            public bool has_children { get; set; }
            public bool has_active_children { get; set; }
            public List<object> children { get; set; }
        }

        public class Post
        {
            public int id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime? updated_at { get; set; }
            public File file { get; set; }
            public Preview preview { get; set; }
            public Sample sample { get; set; }
            public Score score { get; set; }
            public Tags tags { get; set; }
            public List<object> locked_tags { get; set; }
            public int change_seq { get; set; }
            public Flags flags { get; set; }
            public string rating { get; set; }
            public int fav_count { get; set; }
            public List<object> sources { get; set; }
            public List<object> pools { get; set; }
            public Relationships relationships { get; set; }
            public int? approver_id { get; set; }
            public int uploader_id { get; set; }
            public string description { get; set; }
            public int comment_count { get; set; }
            public bool is_favorited { get; set; }
        }

        public class RootObject
        {
            public List<Post> posts { get; set; }
        }
        public static async Task<string> getJSON(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                //userAgent info
                string info = "WagglesBot/1.0 (by Hoovier)";
                string type = "application/json";
                client.BaseAddress = new Uri(url);

                //random client things, not super sure if all of it is needed apart from UserAgent stuff.
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.UserAgent.Clear();
                //important useragent things, not allowed through without this
                client.DefaultRequestHeaders.UserAgent.ParseAdd(info);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(type));
                HttpResponseMessage response = await client.GetAsync(String.Empty);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return result;
                }
                else
                {
                    Console.WriteLine(response.StatusCode);
                    return "failure";
                }
            }
        }
    }
}
