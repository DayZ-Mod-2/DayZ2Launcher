using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace DayZ2.DayZ2Launcher.App.Core
{
    public class GameVersion
    {
        public string DirPath;
        public string ExePath;
        public Version ExeVersion;

        public GameVersion(string gameDir)
        {
            var dirInfo = new DirectoryInfo(gameDir);
            if (!dirInfo.Exists)
                return;

            DirPath = dirInfo.FullName;
            ExePath = Path.Combine(gameDir, "arma2oa.exe");
            if (!File.Exists(ExePath))
                ExePath = null;
            else
                ExeVersion = GetFileVersion(ExePath);
        }

        public int? BuildNo
        {
            get
            {
                if (ExeVersion != null)
                    return ExeVersion.Revision;

                return null;
            }
        }

        private static Version GetFileVersion(string arma2OAExePath)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(arma2OAExePath);
                return Version.Parse(versionInfo.ProductVersion);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class GameVersions
    {
        public GameVersion Beta;
        public GameVersion Retail;

        public GameVersions(string oaDir)
        {
            Retail = new GameVersion(oaDir);
            if (Retail.DirPath == null)
                return;

            Beta = new GameVersion(Path.Combine(oaDir, "Expansion\\beta"));
            if (Beta.DirPath == null)
                return;
        }

        public GameVersion BestVersion
        {
            get
            {
                if (Equals(Retail.BuildNo, Beta.BuildNo))
                    return Retail;

                if ((Retail.BuildNo ?? 0) > (Beta.BuildNo ?? 0))
                    return Retail;

                if ((Beta.BuildNo ?? 0) > 0)
                    return Beta;

                if ((Retail.BuildNo ?? 0) > 0)
                    return Retail;

                return null;
            }
        }
    }
}
