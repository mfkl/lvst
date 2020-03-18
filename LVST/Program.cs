using CommandLine;
using LibVLCSharp.Shared;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Error = CommandLine.Error;
using static System.Console;
using System.Linq;

namespace LVST
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; } = true;

            // TODO: set Required = true
            [Option('t', "torrent", Required = false, HelpText = "The torrent link to download and play")]
            public string Torrent { get; set; } = "https://zoink.ch/torrent/Better.Call.Saul.S05E05.480p.x264-mSD[eztv].mkv.torrent";

            // TODO: If multiple chromecast on the network, allow selecting it interactively via the CLI
            [Option('c', "cast", Required = false, HelpText = "Cast to the chromecast")]
            public bool Chromecast { get; set; }

            [Option('s', "save", Required = false, HelpText = "Whether to save the media file. Defaults to true.")]
            public bool Save { get; set; } = true;

            [Option('p', "path", Required = false, HelpText = "Set the path where to save the media file.")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }
        static async Task Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptions);

            result.WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> error)
        {
            WriteLine($"Error while parsing...");
        }

        private static async Task RunOptions(Options options)
        {
            var engine = new ClientEngine();

            WriteLine("Loading torrent file...");
            var torrent = Torrent.Load(new Uri(options.Torrent),
                Path.Combine(Environment.CurrentDirectory, "video.torrent"));

            WriteLine("Creating a new StreamProvider...");
            var provider = new StreamProvider(engine, options.Path, torrent);

            WriteLine("Starting the StreamProvider...");
            await provider.StartAsync();

            WriteLine("Create a stream from the torrent file...");
            var stream = await provider.CreateStreamAsync(provider.Manager.Torrent.Files[0]);

            WriteLine("Loading LibVLC core library...");

            Core.Initialize();

            var libvlcVerbosity = options.Verbose ? "--verbose=2" : "--quiet";
            using var libVLC = new LibVLC(libvlcVerbosity);

            using var media = new Media(libVLC, new StreamMediaInput(stream));
            using var mp = new MediaPlayer(media);

            if(options.Chromecast)
            {
                using var rendererDiscoverer = new RendererDiscoverer(libVLC);
                rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;
                if(rendererDiscoverer.Start())
                {
                    WriteLine("Searching for chromecasts...");
                }
                else
                {
                    WriteLine("Failed starting the chromecast discovery");
                }

                await Task.Delay(2000);

                if(!renderers.Any())
                {
                    WriteLine("No chromecast found... aborting.");
                    return;
                }

                mp.SetRenderer(renderers.First());
            }

            WriteLine("Starting playback...");
            mp.Play();

            ReadKey();
        }

        static List<RendererItem> renderers = new List<RendererItem>();

        private static void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            WriteLine($"Found a new renderer {e.RendererItem.Name} of type {e.RendererItem.Type}!");
            renderers.Add(e.RendererItem);
        }
    }
}
