using CommandLine;

namespace AlphaCoders_Downloader
{
    public class CommandLineOptions
    {
        [Option('t', "threads", HelpText = "Number of threads to download images on.")]
        public int Threads { get; set; }

        [Option('a', "auth", Required = true, HelpText = "AlphaCoders API Key")]
        public string AuthCode { get; set; }

        [Option('s', "search", HelpText = "Term at which to search")]
        public string Search { get; set; }

        [Option('i', "id", HelpText = "Category/sub_catagory if mode is sub_category or category")]
        public int ID { get; set; }

        [Option('o', "output", Default = "output", HelpText = "Folder in which to save downloads at")]
        public string Output { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Show debug information")]
        public bool Verbose { get; set; }

        [Option('z', "size", HelpText = "Width x Height to search for (ex: 1920x1080)")]
        public string Size { get; set; }

        [Option('m', "mode", Required = true, HelpText = "Current API Mode (search, sub_category, category)")]
        public Globals.SearchModes SearchMode { get; set; }

        [Option("operator", Default = Globals.Operator.min, HelpText = "Search Operator (max, equal, min)")]
        public Globals.Operator Operator { get; set; }
    }
}
