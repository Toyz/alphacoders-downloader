﻿using ShellProgressBar;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommandLine;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Diagnostics;
using AlphaCoders_Downloader.objects;
using AlphaCoders_Downloader.helpers;

namespace AlphaCoders_Downloader
{
    class Program
    {
        static Dictionary<string, List<AlphaJson.Wallpaper>> WallPapers;
        static CommandLineOptions options;

        static int Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.Clear();

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);

            Console.Title = Process.GetCurrentProcess().ProcessName;

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

                    if(options.SearchMode == Globals.SearchModes.search && string.IsNullOrEmpty(options.Search))
                    {
                        Console.WriteLine("Please use --help for proper usage");
                        return 1;
                    }

                    Console.Title += " - " + options.Output;
                    Program.options = options;
                    DoWork();
                    return 0;
                },
                errors =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Example with ID: \"" + Process.GetCurrentProcess().ProcessName + ".exe\" --auth=auth_code --size=1920x1080 --mode=1 --id=173025");
                    Console.WriteLine("Example with Search: \"" + Process.GetCurrentProcess().ProcessName + ".exe\" --auth=auth_code --size=1920x1080 --mode=0 --search=anime");
                    return 1;
                });

            Console.ResetColor();
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
            string base_url = Globals.base_url + "auth=" + options.AuthCode + "&method=" + options.SearchMode.ToString() + "&sort=" + options.SortMode.ToString();

            if(options.SearchMode == Globals.SearchModes.search)
            {
                base_url += "&term=" + options.Search;
            }else
            {
                base_url += "&id=" + options.ID;
            }

            if (!string.IsNullOrEmpty(options.Size))
            {
                string[] sizes = options.Size.ToLower().Split('x');

                if(sizes.Length == 2)
                {
                    base_url += "&height=" + sizes[1] + "&width=" + sizes[0] + "&operator=" + options.Operator.ToString();
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

            if (options.SearchMode != Globals.SearchModes.search)
            {
                if (options.SearchMode != Globals.SearchModes.featured)
                {
                    options.Search = options.ID.ToString();
                }
                else
                {
                    options.Search = Globals.SearchModes.featured.ToString();
                }
            }

            Console.Title += "\\" + options.Search;
            Console.WriteLine("Downloading Images To: " + Path.Combine(options.Output, options.Search));
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
                        using (var child = pbar.Spawn(page.Value.Count, "Downloading " + page.Value.Count + " images from page " + (currentPage + 1), new ProgressBarOptions {
                            ForeGroundColor = ConsoleColor.Green
                        }))
                        {
                            var current = 0;
                            for (var i = 0; i < page.Value.Count; i++)
                            {
                                var temp = i;
                                var t = factory.StartNew(() =>
                                {
                                    var text = "Downloading " + page.Value[temp].id + "." + page.Value[temp].file_type + " from " + page.Value[temp].url_image;
                                    using (var dlChild = child.Spawn(100, text, new ProgressBarOptions {
                                        ForeGroundColor = ConsoleColor.Yellow,
                                        CollapseWhenFinished = true
                                    }))
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
                                    child.Tick("Downloaded " + (current = current + 1) + "/" + page.Value.Count + " images from page " + (currentPage + 1));
                                });

                                tasks.Add(t);
                            }

                            Task.WaitAll(tasks.ToArray());
                            child.UpdateMessage("Downloaded all images from page " + (currentPage + 1));
                        }
                    }).ContinueWith(action => pbar.Tick("Finished Downloading images from " + (currentPage = currentPage + 1) + "/" + WallPapers.Count)).Wait();
                }

                pbar.UpdateMessage("Finished...");
            }
        }
    }
}


