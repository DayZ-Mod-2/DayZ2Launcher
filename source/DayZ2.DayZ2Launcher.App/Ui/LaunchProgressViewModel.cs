using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Newtonsoft.Json;
using DayZ2.DayZ2Launcher.App.Core;
using MonoTorrent.Client;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DayZ2.DayZ2Launcher.App.Ui
{
    public class LaunchProgressViewModel : BindableBase
    {
        private readonly IEnumerable<MetaAddon> addOns;
        public Dispatcher Dispatcher = null;
        private bool _closeable = true;
        private int _lowerProgressLimit;
        private string _lowerProgressText;
        private int _lowerProgressValue;
        private int _upperProgressLimit;
        private string _upperProgressText;
        private int _upperProgressValue;
        private MetaGameType gameType;

        public LaunchProgressViewModel(MetaGameType gameType, IEnumerable<MetaAddon> addOns)
        {
            this.gameType = gameType;
            this.addOns = addOns;

            UpperProgressValue = UpperProgressLimit = 0;
            LowerProgressValue = LowerProgressLimit = 0;

            if (TorrentUpdater.CurrentState() != TorrentState.Seeding && TorrentUpdater.CurrentState() != TorrentState.Stopped)
            {
                UpperProgressValue = 0;
                UpperProgressLimit = 100;
                Closeable = true;

                TorrentUpdater.StatusCallbacks += TorrentStatusUpdate;
            }
            else
            {
                ExtractTorrents();
            }
        }

        public string UpperProgressText
        {
            get => _upperProgressText;
            set
            {
                _upperProgressText = value;
                Execute.OnUiThreadSync(() => PropertyHasChanged("UpperProgressText"), Dispatcher, DispatcherPriority.Render);
                if (_upperProgressText != value)
                {
                    _upperProgressText = value;
                    Execute.OnUiThreadSync(() => PropertyHasChanged("UpperProgressText"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public int UpperProgressValue
        {
            get => _upperProgressValue;
            set
            {
                if (_upperProgressValue != value)
                {
                    _upperProgressValue = value;
                    Execute.OnUiThread(() => PropertyHasChanged("UpperProgressValue"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public int UpperProgressLimit
        {
            get => _upperProgressLimit;
            set
            {
                if (_upperProgressLimit != value)
                {
                    _upperProgressLimit = value;
                    Execute.OnUiThread(() => PropertyHasChanged("UpperProgressLimit"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public string LowerProgressText
        {
            get => _lowerProgressText;
            set
            {
                if (_lowerProgressText != value)
                {
                    _lowerProgressText = value;
                    Execute.OnUiThread(() => PropertyHasChanged("LowerProgressText"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public int LowerProgressValue
        {
            get => _lowerProgressValue;
            set
            {
                if (_lowerProgressValue != value)
                {
                    _lowerProgressValue = value;
                    Execute.OnUiThread(() => PropertyHasChanged("LowerProgressValue"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public int LowerProgressLimit
        {
            get => _lowerProgressLimit;
            set
            {
                if (_lowerProgressLimit != value)
                {
                    _lowerProgressLimit = value;
                    Execute.OnUiThread(() => PropertyHasChanged("LowerProgressLimit"), Dispatcher, DispatcherPriority.Render);
                }
            }
        }

        public bool Closeable
        {
            get => _closeable;
            set
            {
                if (_closeable != value)
                {
                    _closeable = value;
                    Execute.OnUiThread(() => PropertyHasChanged("Closeable"), Dispatcher, DispatcherPriority.DataBind);
                }
            }
        }

        private void HandleException(string topText, string txtMsg = null)
        {
            UpperProgressText = topText;
            UpperProgressLimit = UpperProgressValue = 0;
            LowerProgressLimit = LowerProgressValue = 0;
            LowerProgressText = txtMsg;

            Closeable = true;
        }

        private void HandleException(string topText, Exception ex)
        {
            if (ex != null)
                HandleException(topText, ex.Message);
            else
                HandleException(topText);
        }

        private bool HandlePossibleError(string topText, AsyncCompletedEventArgs args)
        {
            string errorMsg = null;
            if (args.Cancelled)
                errorMsg = "Async operation cancelled";
            else if (args.Error != null)
                errorMsg = args.Error.Message;

            if (errorMsg == null)
                return false;

            if (args.Error != null)
                HandleException(topText, args.Error);
            else
                HandleException(topText, errorMsg);

            return true;
        }

        public event EventHandler OnRequestClose;

        private void ExtractTorrents()
        {
            UpperProgressValue = 0;
            Closeable = false;
            var thread = new Thread(() =>
            {
                var targetPath = Path.Combine(UserSettings.ContentDataPath, gameType.Ident);
                var filtered = new List<MetaAddon>();
                bool cleanExtraction = false;

                Unpacker unpacker = new Unpacker
                {
                    TargetPath = targetPath
                };

                try
                {
                    unpacker.CreateTargetPath();
                }
                catch (Exception ex)
                {
                    HandleException(UpperProgressText, $"Failed to create content data directory: {ex}");
                    return;
                }

                // remove extra files
                try
                {
                    unpacker.DeleteExtraFiles();
                }
                catch (Exception ex)
                {
                    HandleException(UpperProgressText, $"Failed to check for unneeded files in content data path: {ex}");
                    return;
                }

                // check if archives need extraction
                try
                {
                    UpperProgressText = "Verifying...";
                    UpperProgressValue = 0;
                    UpperProgressLimit = addOns.Count();

                    foreach (MetaAddon addOn in addOns)
                    {
                        string archiveName = Unpacker.ArchiveName(addOn);
                        string filePath = Unpacker.ArchivePath(addOn);
                        if (!File.Exists(filePath))
                        {
                            HandleException(UpperProgressText, "Archive is missing, please run a full system check");
                            return;
                        }

                        string hash = Hash.Sha256File(filePath);
                        if (unpacker.NeedsExtraction(archiveName, hash))
                        {
                            filtered.Add(addOn);
                            unpacker.MarkExtracted(archiveName, hash);
                        }
                        UpperProgressValue += 1;
                    }
                }
                catch (Exception)
                {
                    cleanExtraction = true;
                }

                // clean the output dir if needed
                if (cleanExtraction)
                {
                    try
                    {
                        filtered = addOns as List<MetaAddon>;
                        Unpacker.Clear();
                        unpacker.CreateTargetPath();
                    }
                    catch (Exception ex)
                    {
                        HandleException(UpperProgressText, $"Failed to clean content data path: {ex}");
                        return;
                    }
                }

                // extract archives
                UpperProgressText = "Extracting...";
                UpperProgressValue = 0;
                UpperProgressLimit = filtered.Count();

                foreach (MetaAddon addOn in filtered)
                {
                    try
                    {
                        unpacker.Unpack(addOn);
                        UpperProgressValue += 1;
                    }
                    catch (Exception ex)
                    {
                        HandleException(UpperProgressText, $"Error extracting '{addOn.Name}' {ex}");
                        return;
                    }

                }

                unpacker.SaveHashes();

                UpperProgressText = "Done.";
                Closeable = true;
                Execute.OnUiThread(() => { OnRequestClose(this, null); }, Dispatcher);
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void TorrentStatusUpdate(TorrentState newState, double newProgress)
        {
            UpperProgressValue = (int)(newProgress * 100.0);

            if (newState == TorrentState.Hashing)
            {
                UpperProgressText = "Verifying...";
            }
            else if (newState == TorrentState.Downloading)
            {
                UpperProgressText = "Downloading...";
            }
            else if (newState == TorrentState.Seeding || newState == TorrentState.Stopped)
            {
                TorrentUpdater.StatusCallbacks -= TorrentStatusUpdate;
                ExtractTorrents();
            }
        }
    }
}