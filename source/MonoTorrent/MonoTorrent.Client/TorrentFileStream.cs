using System.IO;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
    internal class TorrentFileStream : FileStream
    {
        private readonly TorrentFile file;

        public TorrentFileStream(TorrentFile file, FileMode mode, FileAccess access, FileShare share)
            : base(file.FullPath, mode, access, share, 1)
        {
            this.file = file;
        }

        public TorrentFile File
        {
            get { return file; }
        }

        public string Path
        {
            get { return file.FullPath; }
        }
    }
}