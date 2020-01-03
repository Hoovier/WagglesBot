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
        public class CreatedAt
        {
            public string json_class { get; set; }
            public int s { get; set; }
            public int n { get; set; }
        }
        public class Image
        {
            public string id { get; set; }
            public string tags { get; set; }
            public string locked_tags { get; set; }
            public string description { get; set; }
            public CreatedAt created_at { get; set; }
            public string creator_id { get; set; }
            public string author { get; set; }
            public string change { get; set; }
            public string source { get; set; }
            public int score { get; set; }
            public int fav_count { get; set; }
            public string md5 { get; set; }
            public int file_size { get; set; }
            public string file_url { get; set; }
            public string file_ext { get; set; }
            public string preview_url { get; set; }
            public int preview_width { get; set; }
            public int preview_height { get; set; }
            public string sample_url { get; set; }
            public int sample_width { get; set; }
            public int sample_height { get; set; }
            public char rating { get; set; }
            public string status { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public bool has_comments { get; set; }
            public bool has_notes { get; set; }
            public bool has_children { get; set; }
            public string children { get; set; }
            public string parent_id { get; set; }
            public string[] artist { get; set; }
            public string[] sources { get; set; }
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
                    return "failure";
                }
            }
        }
    }
}
