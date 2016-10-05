using ShellProgressBar;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;

namespace AlphaCoders_Downloader
{
    class Program
    {
        static Dictionary<string, List<AlphaJson.Wallpaper>> WallPapers;

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);

            var exitCode = result.MapResult(
                options =>
                {
                    WallPapers = new Dictionary<string, List<AlphaJson.Wallpaper>>();
                    if (options.Threads <= 0)
                    {
                        options.Threads = Environment.ProcessorCount;
                    }

                    DoWork(options);
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

        private static string GetDownloadURL(CommandLineOptions options)
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

        private static void DoWork(CommandLineOptions options)
        {
            GetPages(GetDownloadURL(options), 1);

            var maxTicks = WallPapers.Count;
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(options.Threads);
            TaskFactory factory = new TaskFactory(lcts);

            using (var pbar = new ProgressBar(maxTicks, "Starting", ConsoleColor.Cyan))
            {
                pbar.UpdateMessage("Processing " + maxTicks + " pages");

                var currentPage = 0;
                foreach (var page in WallPapers)
                {
                    var tasks = new List<Task>();
                    Task.Run(() =>
                    {
                        pbar.UpdateMessage("Downloading Page " + (currentPage + 1) + "/" + maxTicks);
                        using (var child = pbar.Spawn(page.Value.Count, "Downloading " + page.Value.Count + " items"))
                        {
                            var current = 0;
                            for (var i = 0; i < page.Value.Count - 1; i++)
                            {
                                var temp = i;
                                var t = factory.StartNew(() =>
                                {
                                    using (var dlChild = child.Spawn(100, "Downloading: " + page.Value[temp].id + "." + page.Value[temp].file_type))
                                    {
                                        var prevPerct = 0;
                                        WebDownloader.Download(page.Value[temp].url_image, "output/" + page.Value[temp].id + "." + page.Value[temp].file_type, (perc, cur, total) =>
                                        {
                                            if (perc > prevPerct)
                                            {
                                                dlChild.Tick("Downloading: " + page.Value[temp].id + "." + page.Value[temp].file_type, perc);
                                                prevPerct = perc;
                                            }
                                        });
                                    }
                                }).ContinueWith(action =>
                                {
                                    child.Tick("Processed " + (current = current + 1) + "/" + page.Value.Count);
                                });

                                tasks.Add(t);
                            }

                            Task.WaitAll(tasks.ToArray());
                        }
                    }).ContinueWith(action => pbar.Tick("Finished " + (currentPage = currentPage + 1) + "/" + WallPapers.Count)).Wait();
                }

                pbar.UpdateMessage("Finished...");
            }
        }
    }
}


