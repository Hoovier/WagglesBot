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
    public class DerpibooruResponse
    {
        public class Intensities
        {
            public double ne { get; set; }
            public double nw { get; set; }
            public double se { get; set; }
            public double sw { get; set; }
        }

        public class Representations
        {
            public string full { get; set; }
            public string large { get; set; }
            public string medium { get; set; }
            public string small { get; set; }
            public string tall { get; set; }
            public string thumb { get; set; }
            public string thumb_small { get; set; }
            public string thumb_tiny { get; set; }
            public string mp4 { get; set; }
            public string webm { get; set; }
        }

        public class Image
        {
            public string format { get; set; }
            public Intensities intensities { get; set; }
            public int upvotes { get; set; }
            public Representations representations { get; set; }
            public DateTime updated_at { get; set; }
            public string description { get; set; }
            public int height { get; set; }
            public string uploader { get; set; }
            public List<string> tags { get; set; }
            public List<int> tag_ids { get; set; }
            public bool hidden_from_users { get; set; }
            public int faves { get; set; }
            public string orig_sha512_hash { get; set; }
            public DateTime first_seen_at { get; set; }
            public int tag_count { get; set; }
            public double aspect_ratio { get; set; }
            public double wilson_score { get; set; }
            public int score { get; set; }
            public bool spoilered { get; set; }
            public string source_url { get; set; }
            public object duplicate_of { get; set; }
            public int downvotes { get; set; }
            public string sha512_hash { get; set; }
            public int id { get; set; }
            public bool thumbnails_generated { get; set; }
            public string mime_type { get; set; }
            public string view_url { get; set; }
            public bool processed { get; set; }
            public int comment_count { get; set; }
            public int width { get; set; }
            public int? uploader_id { get; set; }
            public object deletion_reason { get; set; }
            public string name { get; set; }
            public DateTime created_at { get; set; }
        }

        public class Rootobject
        {
            public List<Image> images { get; set; }
            public List<object> interactions { get; set; }
            public int total { get; set; }
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

