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

            [Option('t', "torrent", Required = false, HelpText = "The torrent link to download and play")]
            public string Torrent { get; set; } = "http://www.publicdomaintorrents.com/bt/btdownload.php?type=torrent&file=Charlie_Chaplin_Mabels_Strange_Predicament.avi.torrent";

            // TODO: If multiple chromecast on the network, allow selecting it interactively via the CLI
            [Option('c', "cast", Required = false, HelpText = "Cast to the chromecast")]
            public bool Chromecast { get; set; }

            [Option('s', "save", Required = false, HelpText = "Whether to save the media file. Defaults to true.")]
            public bool Save { get; set; } = true;

            [Option('p', "path", Required = false, HelpText = "Set the path where to save the media file.")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }

        static LibVLC libVLC;
        static MediaPlayer mediaPlayer;
        static List<RendererItem> renderers = new List<RendererItem>();

        static async Task Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptions);

            result.WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> error)
        {
            WriteLine($"Error while parsing...");
        }

        private static async Task RunOptions(Options cliOptions)
        {
            var stream = await StartTorrenting(cliOptions);

            await StartPlayback(stream, cliOptions);

            ReadKey();
        }

        private static async Task StartPlayback(Stream stream, Options cliOptions)
        {
            WriteLine("LibVLCSharp -> Loading LibVLC core library...");

            Core.Initialize();

            libVLC = new LibVLC();
            if(cliOptions.Verbose)
                libVLC.Log += (s, e) => WriteLine($"LibVLC -> {e.FormattedLog}");

            using var media = new Media(libVLC, new StreamMediaInput(stream));
            mediaPlayer = new MediaPlayer(media);

            if (cliOptions.Chromecast)
            {
                var result = await FindAndUseChromecast();
                if (!result)
                    return;
            }

            WriteLine("LibVLCSharp -> Starting playback...");
            mediaPlayer.Play();
        }

        private static async Task<bool> FindAndUseChromecast()
        {
            using var rendererDiscoverer = new RendererDiscoverer(libVLC);
            rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;
            if (rendererDiscoverer.Start())
            {
                WriteLine("LibVLCSharp -> Searching for chromecasts...");
                // give it some time...
                await Task.Delay(2000);
            }
            else
            {
                WriteLine("LibVLCSharp -> Failed starting the chromecast discovery");
            }

            rendererDiscoverer.ItemAdded -= RendererDiscoverer_ItemAdded;

            if (!renderers.Any())
            {
                WriteLine("LibVLCSharp -> No chromecast found... aborting.");
                return false;
            }

            mediaPlayer.SetRenderer(renderers.First());
            return true;
        }

        private static async Task<Stream> StartTorrenting(Options cliOptions)
        {
            var engine = new ClientEngine();

            WriteLine("MonoTorrent -> Loading torrent file...");
            var torrent = Torrent.Load(new Uri(cliOptions.Torrent),
                Path.Combine(Environment.CurrentDirectory, "video.torrent"));

            WriteLine("MonoTorrent -> Creating a new StreamProvider...");
            var provider = new StreamProvider(engine, cliOptions.Path, torrent);

            if (cliOptions.Verbose)
            {
                provider.Manager.PeerConnected += (o, e) => WriteLine($"MonoTorrent -> Connection succeeded: {e.Peer.Uri}");
                provider.Manager.ConnectionAttemptFailed += (o, e) => WriteLine($"MonoTorrent -> Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer.AllowedEncryption}");
            }

            WriteLine("MonoTorrent -> Starting the StreamProvider...");
            await provider.StartAsync();

            WriteLine("MonoTorrent -> Creating a stream from the torrent file...");
            var stream = await provider.CreateStreamAsync(provider.Manager.Torrent.Files[0]);

            return stream;
        }

        private static void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            WriteLine($"LibVLCSharp -> Found a new renderer {e.RendererItem.Name} of type {e.RendererItem.Type}!");
            renderers.Add(e.RendererItem);
        }
    }
}
