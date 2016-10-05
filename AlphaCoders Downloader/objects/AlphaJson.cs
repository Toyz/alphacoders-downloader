﻿using System.Collections.Generic;

namespace AlphaCoders_Downloader.objects
{
    public class AlphaJson
    {
        public class Wallpaper
        {
            public string id { get; set; }
            public string width { get; set; }
            public string height { get; set; }
            public string file_type { get; set; }
            public string file_size { get; set; }
            public string url_image { get; set; }
            public string url_thumb { get; set; }
            public string url_page { get; set; }
        }

        public class RootObject
        {
            public bool success { get; set; }
            public List<Wallpaper> wallpapers { get; set; }
            public string total_match { get; set; }
            public string error { get; set; }
            public bool is_last { get; set; }
        }

    }
}
