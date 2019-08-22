using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WagglesBot.Modules
{
    class DerpibooruResponse
    {
        public class Rootobject
        {
            public Search[] Search { get; set; }
            public int Total { get; set; }
            public object[] Interactions { get; set; }
        }

        public class Search
        {
            public string id { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public Duplicate_Reports[] duplicate_reports { get; set; }
            public DateTime first_seen_at { get; set; }
            public string uploader_id { get; set; }
            public int score { get; set; }
            public int comment_count { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string file_name { get; set; }
            public string description { get; set; }
            public string uploader { get; set; }
            public string image { get; set; }
            public int upvotes { get; set; }
            public int downvotes { get; set; }
            public int faves { get; set; }
            public string tags { get; set; }
            public string[] tag_ids { get; set; }
            public float aspect_ratio { get; set; }
            public string original_format { get; set; }
            public string mime_type { get; set; }
            public string sha512_hash { get; set; }
            public string orig_sha512_hash { get; set; }
            public string source_url { get; set; }
            public Representations representations { get; set; }
            public bool is_rendered { get; set; }
            public bool is_optimized { get; set; }
        }

        public class Representations
        {
            public string thumb_tiny { get; set; }
            public string thumb_small { get; set; }
            public string thumb { get; set; }
            public string small { get; set; }
            public string medium { get; set; }
            public string large { get; set; }
            public string tall { get; set; }
            public string full { get; set; }
        }

        public class Duplicate_Reports
        {
            public int id { get; set; }
            public string state { get; set; }
            public string reason { get; set; }
            public int image_id { get; set; }
            public int duplicate_of_image_id { get; set; }
            public object user_id { get; set; }
            public Modifier modifier { get; set; }
            public DateTime created_at { get; set; }
        }

        public class Modifier
        {
            public int id { get; set; }
            public string name { get; set; }
            public string slug { get; set; }
            public string role { get; set; }
            public string description { get; set; }
            public string avatar_url { get; set; }
            public DateTime created_at { get; set; }
            public int comment_count { get; set; }
            public int uploads_count { get; set; }
            public int post_count { get; set; }
            public int topic_count { get; set; }
            public Link[] links { get; set; }
            public Award[] awards { get; set; }
        }

        public class Link
        {
            public int user_id { get; set; }
            public DateTime created_at { get; set; }
            public string state { get; set; }
            public int[] tag_ids { get; set; }
        }

        public class Award
        {
            public string image_url { get; set; }
            public string title { get; set; }
            public int id { get; set; }
            public string label { get; set; }
            public DateTime awarded_on { get; set; }
        }

    }
    public class Get
    {
        private static string _cDir;

        public static void DownloadImage(string url, string id)
        {
            Console.Write($"Downloading {id}...");
            string extension = Path.GetExtension(url);
            string fileName = $"{id}{extension}";

            string downloadPath = IsMono() ? $@"{_cDir}/{fileName}" : $@"{_cDir}\{fileName}";
            if (!File.Exists(downloadPath))
            {
                using (WebClient webConnection = new WebClient())
                {
                    AutoResetEvent notifier = new AutoResetEvent(false);
                    webConnection.DownloadFileCompleted += delegate
                    {
                        Console.WriteLine("Done!");
                        notifier.Set();
                    };

                    webConnection.DownloadFileAsync(new Uri($"https:{url}"), downloadPath);
                    notifier.WaitOne();
                }
            }
            else
            {
                Console.WriteLine("Already exists!");
            }
        }

        public static async Task<string> Derpibooru(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                string type = "application/json";
                client.BaseAddress = new Uri(url);

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(type));
                HttpResponseMessage response = await client.GetAsync(String.Empty);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return string.Empty;
            }
        }

        public static bool IsMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public static void SetDownloadFolder(string c)
        {
            _cDir = c;
        }
    }
}

