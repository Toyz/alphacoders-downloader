using CommandLine;

namespace AlphaCoders_Downloader
{
    public class CommandLineOptions
    {
        [Option('t', "threads", HelpText = "Number of threads to download images on.")]
        public int Threads { get; set; }

        [Option('a', "auth", Required = true, HelpText = "AlphaCoders API Key")]
        public string AuthCode { get; set; }

        [Option('s', "search", Required = true, HelpText = "Term at which to search")]
        public string Search { get; set; }

        [Option('o', "output", Default = "output", HelpText = "Folder in which to save downloads at")]
        public string Output { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Show debug information")]
        public bool Verbose { get; set; }

        [Option(longName: "size", HelpText = "Width x Height to search for (ex: 1920x1080)")]
        public string Size { get; set; }

        [Option(longName: "mode", Required = true, HelpText = "Current API Mode (search, sub_category, category)")]
        public Globals.SearchModes SearchMode { get; set; }
    }
}
