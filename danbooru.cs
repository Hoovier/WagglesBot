using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CoreWaggles
{
    public class danbooru
    {
        public class Image
        {
            public int id { get; set; }
            public DateTime created_at { get; set; }
            public int uploader_id { get; set; }
            public int score { get; set; }
            public string source { get; set; }
            public string md5 { get; set; }
            public DateTime? last_comment_bumped_at { get; set; }
            public string rating { get; set; }
            public int image_width { get; set; }
            public int image_height { get; set; }
            public string tag_string { get; set; }
            public bool is_note_locked { get; set; }
            public int fav_count { get; set; }
            public string file_ext { get; set; }
            public DateTime? last_noted_at { get; set; }
            public bool is_rating_locked { get; set; }
            public int? parent_id { get; set; }
            public bool has_children { get; set; }
            public int? approver_id { get; set; }
            public int tag_count_general { get; set; }
            public int tag_count_artist { get; set; }
            public int tag_count_character { get; set; }
            public int tag_count_copyright { get; set; }
            public int file_size { get; set; }
            public bool is_status_locked { get; set; }
            public string pool_string { get; set; }
            public int up_score { get; set; }
            public int down_score { get; set; }
            public bool is_pending { get; set; }
            public bool is_flagged { get; set; }
            public bool is_deleted { get; set; }
            public int tag_count { get; set; }
            public DateTime updated_at { get; set; }
            public bool is_banned { get; set; }
            public int? pixiv_id { get; set; }
            public DateTime? last_commented_at { get; set; }
            public bool has_active_children { get; set; }
            public int bit_flags { get; set; }
            public int tag_count_meta { get; set; }
            public bool has_large { get; set; }
            public bool has_visible_children { get; set; }
            public bool is_favorited { get; set; }
            public string tag_string_general { get; set; }
            public string tag_string_character { get; set; }
            public string tag_string_copyright { get; set; }
            public string tag_string_artist { get; set; }
            public string tag_string_meta { get; set; }
            public string file_url { get; set; }
            public string large_file_url { get; set; }
            public string preview_file_url { get; set; }

        }

        public class RootObject
        {
            public List<Image> ImageList { get; set; }

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
                    if(result.Contains("The database timed out running your query"))
                    {
                        return "failure";
                    }
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
