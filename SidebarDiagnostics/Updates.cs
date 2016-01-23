using System;
using System.Net;
using System.IO;
using System.Reflection;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace SidebarDiagnostics.Updates
{
    public static class UpdateManager
    {
        private const string GitHubAPI = "https://api.github.com/repos/ArcadeRenegade/SidebarDiagnostics/releases/latest";
        private const string UserAgent = "ArcadeRenegade-SidebarDiagnostics";

        public static void Check(bool showInfoDialogs)
        {
            VersionCheckResult _result = CheckNewVersion();

            if (_result.Success)
            {
                if (_result.NewVersion)
                {
                    ShowUpdateDialog(_result.VersionNo, _result.URL);
                }
                else if (showInfoDialogs)
                {
                    ShowNoUpdateDialog();
                }
            }
            else if (showInfoDialogs)
            {
                ShowErrorDialog();
            }
        }

        private static VersionCheckResult CheckNewVersion()
        {
            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(GitHubAPI);
            _request.UserAgent = UserAgent;

            try
            {
                using (WebResponse _response = _request.GetResponse())
                {
                    using (Stream _stream = _response.GetResponseStream())
                    {
                        using (StreamReader _reader = new StreamReader(_stream))
                        {
                            JObject _json = JObject.Parse(_reader.ReadToEnd());

                            string _newVersion = _json.Value<string>("tag_name");

                            string _thisVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(2);

                            if (!_thisVersion.Equals(_newVersion, StringComparison.OrdinalIgnoreCase))
                            {
                                return new VersionCheckResult()
                                {
                                    Success = true,
                                    NewVersion = true,
                                    VersionNo = _newVersion,
                                    URL = _json.Value<string>("html_url")
                                };
                            }
                        }
                    }
                }
            }
            catch (WebException webEx)
            {
                return VersionCheckResult.Failed;
            }

            return VersionCheckResult.None;
        }

        private static void ShowNoUpdateDialog()
        {
            MessageBox.Show("You have the latest version.", Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private static void ShowUpdateDialog(string newVersionNo, string downloadURL)
        {
            MessageBoxResult _result = MessageBox.Show(string.Format("A new version v{0} is available. Download it?", newVersionNo), Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

            if (_result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(downloadURL);
            }
        }

        private static void ShowErrorDialog()
        {
            MessageBox.Show("A web exception was thrown while trying to check for updates. Check internet connection.", Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }

    public class VersionCheckResult
    {
        public static VersionCheckResult None = new VersionCheckResult() { Success = true, NewVersion = false };
        public static VersionCheckResult Failed = new VersionCheckResult() { Success = false };

        public bool Success { get; set; }
        public bool NewVersion { get; set; }
        public string VersionNo { get; set; }
        public string URL { get; set; }
    }
}
