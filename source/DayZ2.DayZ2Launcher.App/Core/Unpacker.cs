using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DayZ2.DayZ2Launcher.App.Core
{
    class Unpacker
    {
        private Dictionary<string, string> _hashes = new Dictionary<string, string>();

        public Unpacker()
        {
            LoadHashes();
        }

        public static string ArchiveName(MetaAddon addOn) =>  $"{addOn.Name}.7z";

        public static string ArchivePath(MetaAddon addOn) => Path.Combine(UserSettings.ContentPackedDataPath, ArchiveName(addOn));

        public void LoadHashes()
        {
            try
            {
                var fileContent = "{}";
                if (File.Exists(UserSettings.ContentLastExtractionHashesPath))
                {
                    fileContent = File.ReadAllText(UserSettings.ContentLastExtractionHashesPath);
                }
                _hashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
            }
            catch (Exception)
            {
                _hashes = new Dictionary<string, string>();
            }
        }

        public void SaveHashes()
        {
            // only call this once the extraction was successful
            try
            {
                var hashesString = JsonConvert.SerializeObject(_hashes);
                File.WriteAllText(UserSettings.ContentLastExtractionHashesPath, hashesString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public bool NeedsExtraction(string archive, string hash)
        {
            return !_hashes.ContainsKey(archive) || hash != _hashes[archive];
        }

        public void MarkExtracted(string archive, string hash)
        {
            _hashes[archive] = hash;
        }

        public string TargetPath { get; set; }

        public void CreateTargetPath()
        {
            if (!Directory.Exists(TargetPath))
            {
                Directory.CreateDirectory(TargetPath);
            }
        }

        public void Unpack(MetaAddon addOn)
        {
            var archive = ArchiveFactory.Open(ArchivePath(addOn));
            var reader = archive.ExtractAllEntries();
            reader.WriteAllToDirectory(
                TargetPath,
                new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
        }

        private static bool IsExtraFile(string fileName)
        {
            foreach (string archivePath in Directory.GetFiles(UserSettings.ContentPackedDataPath))
            {
                var archive = ArchiveFactory.Open(archivePath);
                foreach (IArchiveEntry entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        if (fileName == entry.Key)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void DeleteExtraFiles()
        {
            foreach (string filePath in Directory.EnumerateFiles(UserSettings.ContentDataPath, "*.*", SearchOption.AllDirectories))
            {
                var file = new Uri(TargetPath + "/").MakeRelativeUri(new Uri(filePath));
                if (IsExtraFile(file.ToString()))
                {
                    File.Delete(filePath);
                }
            }
        }

        public static bool IsClear()
        {
            return !Directory.EnumerateDirectories(UserSettings.ContentDataPath).Any() &&
                   !Directory.EnumerateFiles(UserSettings.ContentDataPath).Any();
        }

        public static void Clear()
        {
            DirectoryInfo directory = new DirectoryInfo(UserSettings.ContentDataPath);

            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }

            if (File.Exists(UserSettings.ContentLastExtractionHashesPath))
                File.Delete(UserSettings.ContentLastExtractionHashesPath);
        }
    }
}
