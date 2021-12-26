using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SteamKit2;
using DayZ2.DayZ2Launcher.App.Ui;
using DayZ2.DayZ2Launcher.App.Ui.Controls;

namespace DayZ2.DayZ2Launcher.App.Core
{
    public class Arma2Installer : BindableBase
    {
        private bool _isRunning;

        private string _status;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                PropertyHasChanged("IsRunning");
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                Execute.OnUiThread(() => PropertyHasChanged("Status"));
            }
        }

        public void DownloadAndInstall(int revision, bool steamBeta,
            string steamBuild, UpdatesView view)
        {
            const int appId = 33930;
            string gameName = "ArmA 2: Operation Arrowhead";
            DirectoryInfo steamPath;

            try
            {
                steamPath = new DirectoryInfo(LocalMachineInfo.Current.SteamPath);
            }
            catch (ArgumentException)
            {
                Execute.OnUiThreadSync(() =>
                {
                    var popup = new InfoPopup("Invalid path", MainWindow.GetWindow(view));
                    popup.Headline.Content = "Steam could not be found";
                    popup.SetMessage("Are you sure you have Steam installed?");
                    popup.Show();
                }, null, DispatcherPriority.Input);

                return;
            }

            if (steamPath.Exists)
            {
                string steamAppsDir = Path.Combine(steamPath.FullName, "steamapps");
                string manifestName = "appmanifest_33930.acf";

                string fullManifestPath = Path.Combine(steamAppsDir, manifestName);

                if (File.Exists(fullManifestPath))
                {
                    // Kill Steam so we can edit the game configuration.
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                    {
                        var perm = RegistryKeyPermissionCheck.Default;
                        var rights = RegistryRights.QueryValues;
                        int steamPid;

                        try
                        {
                            using (RegistryKey steamKey = baseKey.OpenSubKey("SOFTWARE\\Valve\\Steam", perm, rights))
                            {
                                steamPid = (int)steamKey.GetValue("SteamPID");
                                steamKey.Close();
                                steamKey.Dispose();
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Unable to find Steam Process ID.",
                                "Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        } //no steam pid key found

                        if (steamPid != 0)
                        {
                            try
                            {
                                Process steam = Process.GetProcessById(steamPid);
                                steam.Kill();
                                steam.WaitForExit();
                            }
                            catch (Exception) { }
                        }

                        Thread.Sleep(250);
                    }

                    var acfKeys = new KeyValue();
                    using (var reader = new StreamReader(fullManifestPath))
                    {
                        var acfReader = new KVTextReader(acfKeys, reader.BaseStream);
                        acfReader.Close();
                    }

                    KeyValue currentBuild = acfKeys.Children.FirstOrDefault(k => k.Name.Equals("buildid", StringComparison.OrdinalIgnoreCase));
                    if (currentBuild != null)
                    {
                        if (Equals(currentBuild.Value, steamBuild))
                        {
                            Execute.OnUiThreadSync(() =>
                            {
                                var popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
                                popup.Headline.Content = "Game update using Steam";
                                popup.SetMessage(gameName + " might be corrupted.\n" +
                                                    "Please click the following link to validate:");
                                popup.SetLink("steam://validate/" + appId.ToString() + "/", "Update " + gameName);
                                popup.Closed += (sender, args) => view.CheckForUpdates();
                                popup.Show();
                            }, null, DispatcherPriority.Input);
                        }
                        else
                        {
                            KeyValue gameState = acfKeys.Children.FirstOrDefault(k => k.Name.Equals("StateFlags", StringComparison.OrdinalIgnoreCase));
                            if (gameState == null)
                            {
                                gameState = new KeyValue("StateFlags");
                                acfKeys.Children.Add(gameState);
                            }
                            KeyValue autoUpdate = acfKeys.Children.FirstOrDefault(k => k.Name.Equals("AutoUpdateBehavior", StringComparison.OrdinalIgnoreCase));
                            if (autoUpdate != null)
                            {
                                autoUpdate.Value = "0"; // Auto update.
                            }

                            currentBuild.Value = steamBuild;
                            gameState.Value = "2"; // Needs updating.

                            KeyValue userConfig = acfKeys.Children.FirstOrDefault(k => k.Name.Equals("UserConfig", StringComparison.OrdinalIgnoreCase));
                            if (userConfig != null)
                            {
                                KeyValue betaKey = userConfig.Children.FirstOrDefault(k => k.Name.Equals("BetaKey", StringComparison.OrdinalIgnoreCase));
                                if (betaKey == null)
                                {
                                    betaKey = new KeyValue("BetaKey");
                                    userConfig.Children.Add(betaKey);
                                }

                                if (steamBeta)
                                {
                                    betaKey.Value = "beta";
                                }
                                else
                                {
                                    betaKey.Value = "";
                                }
                            }

                            acfKeys.SaveToFile(fullManifestPath, false);
                            Thread.Sleep(250);

                            Process.Start("explorer.exe", @"steam://run/" + appId + "/");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Patching failed, '" + gameName + "' is not located inside a SteamLibrary folder.",
                            "Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    return;
                }
                else
                {
                    Execute.OnUiThreadSync(() =>
                    {
                        var popup = new InfoPopup("User intervention required", MainWindow.GetWindow(view));
                        popup.Headline.Content = "Game update using Steam";
                        popup.SetMessage(gameName + " is not installed.\n" +
                                            "Please install it from the Library tab.\n" +
                                            "Or by clicking on the following link:");
                        popup.SetLink("steam://install/" + appId.ToString() + "/", "Install " + gameName);
                        popup.Closed += (sender, args) => view.CheckForUpdates();
                        popup.Show();
                    }, null, DispatcherPriority.Input);

                    return;
                }
            }
            else
            {
                MessageBox.Show("Patching failed, '" + gameName + "' is not located inside a SteamLibrary folder.",
                    "Patch error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}