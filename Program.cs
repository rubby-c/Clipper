using System;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace Clipper
{
    internal static class Program
    {
        public static async Task MainAsync(string[] args)
        {
            Console.WriteLine("[-] Clipper - made by rubby");
            Console.WriteLine("\r\n[!] Before using make sure you put the video and the program in an empty folder [!]\r\n");
            if (args.Length <= 0)
            {
                Console.Write("[?] No drag selected videos, enter a path here: ");
                args = new[] { Console.ReadLine() };
            }
            else
            {
                Console.WriteLine($"[~] Drag selected videos: {args.Length}");
            }
            
            Console.Write("[?] Duration of clips [recommended over 10]: ");
            int.TryParse(Console.ReadLine(), out var duration);

            foreach (string vid in args)
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(vid);
                var videoStream =  mediaInfo.VideoStreams.FirstOrDefault();

                Console.WriteLine("\r\n[~] Video file imported!");
                Console.WriteLine("\r\n[~] Video streams: ");
                for (int i = 0; i < mediaInfo.VideoStreams.Count(); i++)
                {
                    var streams = mediaInfo.VideoStreams.ToArray();
                    Console.WriteLine($"{i} | Resolution: {streams[i].Width}x{streams[i].Height}, Bitrate: {streams[i].Bitrate}\r\n");
                }

                Console.WriteLine("\r\n[~] Audio streams: ");
                for (int i = 0; i < mediaInfo.AudioStreams.Count(); i++)
                {
                    var streams = mediaInfo.AudioStreams.ToArray();
                    Console.WriteLine($"{i} | Bitrate: {streams[i].Bitrate}, Sample Rate: {streams[i].SampleRate}, Audio channels: {streams[i].Channels}\r\n");
                }
                
                if (mediaInfo.VideoStreams.Count() > 1 || mediaInfo.AudioStreams.Count() > 1)
                {
                    Console.WriteLine("[!] More than 1 video/audio streams were detected. The default/first ones will be used.");
                }
                
                Console.WriteLine($"\r\n-> Press Enter to begin splicing... [{duration} duration, around {(int)videoStream.Duration.TotalSeconds / duration} clips]\r\n");
                Console.ReadKey();
                
                int part = 0;
                for (int start = 0, end = duration; start < videoStream.Duration.TotalSeconds; start += duration, end += duration)
                {
                    try
                    {
                        Console.WriteLine($"[~] Processing slice {part + 1}... [{start} -> {end}]");

                        if (end > videoStream.Duration.TotalSeconds)
                        {
                            Console.WriteLine("[!] Reached end of video.");
                        }
                        else
                        {
                            var build = await FFmpeg.Conversions.FromSnippet.Split(args[0], $"{mediaInfo.Path}-{part}.mp4", TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(duration));
                            part += 1;
                            build.OnProgress += (sender, vidArgs) =>
                            {
                                var percent = (int)(Math.Round(vidArgs.Duration.TotalSeconds / vidArgs.TotalLength.TotalSeconds, 2) * 100);
                                Console.Title = $"[~] Processing stream cut number {part} | [{vidArgs.Duration} / {vidArgs.TotalLength}] {percent}% ETA";
                                Task.Delay(1000);
                            };
                            await build.Start();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error caught {e.Message}");
                    }
                }
            }

            Console.Title = "[+] Completed!";
            Console.WriteLine("\r\n[+] Splicing completed.");
            Console.ReadLine();
        }

        public static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter();
            Console.ReadLine();
        }
    }
}
