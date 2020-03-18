using LibVLCSharp.Shared;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LVST
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var torrentPath = Path.Combine(Environment.CurrentDirectory, "video.torrent");
            var downloadPath = Path.Combine(Environment.CurrentDirectory, "download");

            const string torrentLink = "https://zoink.ch/torrent/Better.Call.Saul.S05E05.480p.x264-mSD[eztv].mkv.torrent";

            var engine = new ClientEngine();

            var torrent = Torrent.Load(new Uri(torrentLink), torrentPath);

            var provider = new StreamProvider(engine, downloadPath, torrent);

            await provider.StartAsync();

            var stream = await provider.CreateStreamAsync(provider.Manager.Torrent.Files[0]);

            Core.Initialize();

            using var libVLC = new LibVLC();
            using var media = new Media(libVLC, new StreamMediaInput(stream));
            using var mp = new MediaPlayer(media);

            mp.Play();

            Console.ReadKey();
        }
    }
}
