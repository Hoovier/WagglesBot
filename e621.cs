using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
