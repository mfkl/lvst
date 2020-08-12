# LVST

.NET Core CLI app using MonoTorrent and LibVLCSharp.

lvst allows you to stream any media torrent for local or remote (chromecast) playback on Windows, macOS and Linux.

<img src="https://raw.githubusercontent.com/mfkl/lvst/master/lvst.gif"/>

```
 .\LVST.exe --help
LVST 1.0.0
Copyright (C) 2020 LVST

  -v, --verbose    Set output to verbose messages.

  -t, --torrent    The torrent link to download and play

  -c, --cast       Cast to the chromecast

  -s, --save       Whether to save the media file. Defaults to true.

  -p, --path       Set the path where to save the media file.

  --help           Display this help screen.

  --version        Display version information.
```

### [See the related blogpost](https://mfkl.github.io/libvlc/2020/03/23/Torrents-and-multimedia-streaming.html)