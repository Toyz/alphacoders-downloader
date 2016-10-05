using ShellProgressBar;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace AlphaCoders_Downloader
{
    class Program
    {
        static Dictionary<string, List<AlphaJson.Wallpaper>> WallPapers;
        static CommandLineOptions options;

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);

            Console.Title = "AlphaCoders Downloader";

            var exitCode = result.MapResult(
                options =>
                {
                    if (options.AuthCode.Contains("file:"))
                    {
                        options.AuthCode = File.ReadAllText(options.AuthCode.Split(':')[1]);
                    }

                    WallPapers = new Dictionary<string, List<AlphaJson.Wallpaper>>();
                    if (options.Threads <= 0)
                    {
                        options.Threads = Environment.ProcessorCount;
                    }

                    if (options.Output == "output")
                    {
                        options.Output = Path.Combine(Directory.GetCurrentDirectory(), options.Output);
                    }

                    Console.Title += " - " + options.Output;
                    Program.options = options;
                    DoWork();
                    return 0;
                },
                errors =>
                {
                    return 1;
                });

            Console.Write("Press any key to close...");
            Console.ReadKey();
            return exitCode;
        }

        private static void GetPages(string url, int page)
        {
            using (var wc = new WebClient()) {
                var json = JsonConvert.DeserializeObject<AlphaJson.RootObject>(wc.DownloadString(url + "&page=" + page));

                if (!json.success)
                {
                    Console.WriteLine(json.error);
                    Console.Write("Press any key to close...");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                WallPapers.Add(page.ToString(), json.wallpapers);

                if (json.wallpapers.Count < 30) return;

                page = page + 1;
                GetPages(url, page);
             }
        }

        private static string GetDownloadURL()
        {
            string base_url = Globals.base_url + "auth=" + options.AuthCode + "&method=" + options.SearchMode.ToString() + "&" + (options.SearchMode == Globals.SearchModes.search ? "term" : "id") + "=" + options.Search;

            if (!string.IsNullOrEmpty(options.Size))
            {
                string[] sizes = options.Size.ToLower().Split('x');

                if(sizes.Length == 2)
                {
                    base_url += "&width=" + sizes[1] + "&height=" + sizes[0] + "&operator=min";
                }
            }

            return base_url;
        }

        private static void DoWork()
        {
            var url = GetDownloadURL();
            if (options.Verbose)
            {
                Console.WriteLine("Base Download url: " + url);
            }

            Console.Write("Downloading and queuing all pages please wait... ");
            GetPages(url, 1);
            Console.Write("Finished" + Environment.NewLine);

            Console.Write("Setting up workers and task queues... ");

            var maxTicks = WallPapers.Count;
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(options.Threads);
            TaskFactory factory = new TaskFactory(lcts);

            Console.Write("Finished" + Environment.NewLine);

            using (var pbar = new ProgressBar(maxTicks, "Starting", ConsoleColor.Cyan))
            {
                pbar.UpdateMessage("Processing " + maxTicks + " pages");

                var currentPage = 0;
                foreach (var page in WallPapers)
                {
                    var tasks = new List<Task>();
                    Task.Run(() =>
                    {
                        pbar.UpdateMessage("Downloading images from page " + (currentPage + 1) + "/" + maxTicks);
                        using (var child = pbar.Spawn(page.Value.Count, "Downloading " + page.Value.Count + " images from current page", new ProgressBarOptions {
                            ForeGroundColor = ConsoleColor.Green
                        }))
                        {
                            var current = 0;
                            for (var i = 0; i < page.Value.Count - 1; i++)
                            {
                                var temp = i;
                                var t = factory.StartNew(() =>
                                {
                                    var text = "Downloading " + page.Value[temp].id + "." + page.Value[temp].file_type + " from " + page.Value[temp].url_image;
                                    using (var dlChild = child.Spawn(100, text, new ProgressBarOptions { ForeGroundColor = ConsoleColor.Yellow }))
                                    {
                                        var prevPerct = 0;
                                        var folder = Path.Combine(options.Output, options.Search , page.Value[temp].id + "." + page.Value[temp].file_type);
                                        if (!Directory.Exists(Path.GetDirectoryName(folder))) {
                                            Directory.CreateDirectory(Path.GetDirectoryName(folder));
                                        }

                                        WebDownloader.Download(page.Value[temp].url_image, folder , (perc, cur, total) =>
                                        {
                                            if (perc > prevPerct)
                                            {
                                                dlChild.Tick(text, perc);
                                                prevPerct = perc;
                                            }
                                        });
                                    }
                                }).ContinueWith(action =>
                                {
                                    child.Tick("Downloaded " + (current = current + 1) + "/" + page.Value.Count + " images from current page");
                                });

                                tasks.Add(t);
                            }

                            Task.WaitAll(tasks.ToArray());
                        }
                    }).ContinueWith(action => pbar.Tick("Finished Downloading images from " + (currentPage = currentPage + 1) + "/" + WallPapers.Count)).Wait();
                }

                pbar.UpdateMessage("Finished...");
            }
        }
    }
}


