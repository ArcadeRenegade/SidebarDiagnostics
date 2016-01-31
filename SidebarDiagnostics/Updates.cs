using System;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SidebarDiagnostics.Updates
{
    public static class UpdateManager
    {
        public async static Task Check(bool showInfoDialogs)
        {
            CheckResult _result = await CheckVersionAsync();

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

        private async static Task<CheckResult> CheckVersionAsync()
        {
            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(Constants.GITHUB.GITHUBAPI);
            _request.Method = WebRequestMethods.Http.Get;
            _request.UserAgent = Constants.GITHUB.USERAGENT;

            try
            {
                using (WebResponse _response = await _request.GetResponseAsync())
                {
                    using (Stream _responseStream = _response.GetResponseStream())
                    {
                        DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(GitHubRelease));
                        GitHubRelease _release = (GitHubRelease)_json.ReadObject(_responseStream);

                        string _newVersion = _release.tag_name;
                        string _thisVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

                        if (!_thisVersion.StartsWith(_newVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            return new CheckResult()
                            {
                                Success = true,
                                NewVersion = true,
                                VersionNo = _newVersion,
                                URL = _release.html_url
                            };
                        }
                    }
                }
            }
            catch (WebException)
            {
                return CheckResult.Failed;
            }

            return CheckResult.None;
        }

        private static void ShowNoUpdateDialog()
        {
            MessageBox.Show("You have the latest version.", Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private static void ShowUpdateDialog(string newVersionNo, string downloadURL)
        {
            MessageBoxResult _result = MessageBox.Show(string.Format("A new version v{0} is available. Download it?", newVersionNo), Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);

            if (_result == MessageBoxResult.Yes)
            {
                Process.Start(downloadURL);
            }
        }

        private static void ShowErrorDialog()
        {
            MessageBox.Show("Could not check for updates. Check your internet connection.", Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private class CheckResult
        {
            public static CheckResult None = new CheckResult() { Success = true, NewVersion = false };
            public static CheckResult Failed = new CheckResult() { Success = false };

            public bool Success { get; set; }
            public bool NewVersion { get; set; }
            public string VersionNo { get; set; }
            public string URL { get; set; }
        }

        [DataContract]
        private class GitHubRelease
        {
            [DataMember]
            public string tag_name { get; set; }

            [DataMember]
            public string html_url { get; set; }
        }
    }


}
