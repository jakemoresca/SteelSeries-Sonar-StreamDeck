using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteelSeriesAPI.Sonar.Enums;
using System;
using System.Threading.Tasks;
using SteelSeriesAPI.Sonar;
using System.Collections.Generic;
using SteelSeriesAPI.Sonar.Models;
using System.Linq;
using com.rydersir.sonargg.Domains;
using System.Drawing;
using System.Diagnostics;

namespace com.rydersir.sonargg
{
    [PluginActionId("com.rydersir.sonargg.navigateapp")]
    public class NavigatePageAppAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.SonarDevice = (int)Channel.GAME;
                instance.NavigateOperation = (int)NavigateOperation.Next;
                return instance;
            }

            [JsonProperty(PropertyName = "sonarDevice")]
            public int SonarDevice { get; set; }

            [JsonProperty(PropertyName = "navigateOperation")]
            public NavigateOperation NavigateOperation { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private SonarBridge sonarManager;
        private AppPageSettings appPageSettings;

        #endregion
        public NavigatePageAppAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += NextAppAction_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();

            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                settings = payload.Settings.ToObject<PluginSettings>();
            }

            sonarManager = new SonarBridge();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings -= NextAppAction_OnReceivedGlobalSettings;
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            if (settings.NavigateOperation == NavigateOperation.Next)
            {
                await NextPage();
                await Connection.SetStateAsync(0);
            }
            else if (settings.NavigateOperation == NavigateOperation.Previous)
            {
                await PreviousPage();
                await Connection.SetStateAsync(1);
            }
            else
            {
                await DisplayCurrentAppName();
                await Connection.SetStateAsync(2);
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            try
            {
                if (!sonarManager.IsRunning)
                {
                    await Connection.ShowAlert();
                }

                var currentDevicePage = GetCurrentDevicePage();

                if (settings.NavigateOperation == NavigateOperation.Next)
                {
                    await Connection.SetStateAsync(0);
                    await DisplayPageNumber(currentDevicePage);
                }
                else if (settings.NavigateOperation == NavigateOperation.Previous)
                {
                    await Connection.SetStateAsync(1);
                    await DisplayPageNumber(currentDevicePage);
                }
                else
                {
                    await DisplayCurrentAppName();
                    await Connection.SetStateAsync(2);
                }
            }
            catch (Exception)
            {
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private int GetCurrentDevicePage()
        {
            return appPageSettings.DevicePages[settings.SonarDevice];
        }

        private void SetCurrentDevicePage(int page)
        {
            appPageSettings.DevicePages[settings.SonarDevice] = page;
        }

        private Task NextPage()
        {
            var activeProcesses = sonarManager.RoutedProcesses.GetActiveRoutedProcesses((Channel)settings.SonarDevice).ToList();
            var maxPages = activeProcesses.Count;
            var currentPage = GetCurrentDevicePage();
            var nextPage = currentPage + 1;

            if (nextPage > maxPages)
            {
                nextPage = 1;
            }

            SetCurrentDevicePage(nextPage);

            return Task.CompletedTask;
        }

        private Task PreviousPage()
        {
            var activeProcesses = sonarManager.RoutedProcesses.GetActiveRoutedProcesses((Channel)settings.SonarDevice).ToList();
            var maxPages = activeProcesses.Count;
            var currentPage = GetCurrentDevicePage();
            var previousPage = currentPage - 1;

            if (previousPage < 0)
            {
                previousPage = maxPages;
            }

            SetCurrentDevicePage(previousPage);

            return Task.CompletedTask;
        }

        private async Task DisplayPageNumber(int page)
        {
            await Connection.SetTitleAsync(page.ToString());
        }

        private async Task DisplayCurrentAppName()
        {
            var activeProcesses = sonarManager.RoutedProcesses.GetActiveRoutedProcesses((Channel)settings.SonarDevice).ToList();
            var currentPage = GetCurrentDevicePage();

            if (activeProcesses.Count >= currentPage)
            {
                var currentApp = activeProcesses[currentPage - 1];
                var process = Process.GetProcessById(currentApp.ProcessId);

                var filePath = process.MainModule?.FileName;
                var icon = Icon.ExtractAssociatedIcon(filePath);

                var prettifiedTitle = currentApp.DisplayName.Replace(" ", "\n");
                await Connection.SetTitleAsync(prettifiedTitle);

                await Connection.SetImageAsync(icon.ToBitmap());
            }
        }

        private void NextAppAction_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null && payload.Settings.Count > 0)
            {
                appPageSettings = payload.Settings.ToObject<AppPageSettings>();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"No global settings found, creating new object");
                appPageSettings = new AppPageSettings()
                {
                    DevicePages = new Dictionary<int, int>
                    {
                     { (int)Channel.MASTER, 1 },
                     { (int)Channel.GAME, 1 },
                     { (int)Channel.CHAT, 1 },
                     { (int)Channel.MEDIA, 1 },
                     { (int)Channel.AUX, 1 }
                    }
                };
                SetGlobalSettings();
            }
        }

        private void SetGlobalSettings()
        {
            Connection.SetGlobalSettingsAsync(JObject.FromObject(appPageSettings));
        }

        #endregion

        public class AppPageSettings
        {
            [JsonProperty(PropertyName = "devicePages")]
            public Dictionary<int, int> DevicePages { get; set; }
        }
    }
}