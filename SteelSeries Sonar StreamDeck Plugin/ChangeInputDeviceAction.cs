using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteelSeriesAPI.Sonar.Enums;
using com.rydersir.sonargg.Domains;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SteelSeriesAPI.Sonar;

namespace com.rydersir.sonargg
{
    [PluginActionId("com.rydersir.sonargg.changeinputdevice")]
    public class ChangeInputDeviceAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();

                instance.SonarDevice = (int)Channel.GAME;
                instance.RedirectionDevices1 = new List<RedirectionDevice>();
                instance.RedirectionDevices2 = new List<RedirectionDevice>();

                return instance;
            }

            [JsonProperty(PropertyName = "sonarDevice")]
            public int SonarDevice { get; set; }

            [JsonProperty(PropertyName = "redirectionDevices1")]
            public List<RedirectionDevice> RedirectionDevices1 { get; set; }

            [JsonProperty(PropertyName = "redirectionDevices2")]
            public List<RedirectionDevice> RedirectionDevices2 { get; set; }

            [JsonProperty(PropertyName = "redirectionDevice1")]
            public string RedirectionDevice1 { get; set; }

            [JsonProperty(PropertyName = "redirectionDevice2")]
            public string RedirectionDevice2 { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private SonarBridge sonarManager;

        #endregion
        public ChangeInputDeviceAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
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
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            if(!sonarManager.IsRunning || string.IsNullOrEmpty(settings.RedirectionDevice1) || string.IsNullOrEmpty(settings.RedirectionDevice2))
            { 
                return; 
            }

            var currentPlaybackDevice = sonarManager.PlaybackDevices.GetPlaybackDevice((Channel)settings.SonarDevice);
            if (currentPlaybackDevice.Id == settings.RedirectionDevice1)
            {
                sonarManager.PlaybackDevices.SetPlaybackDevice(settings.RedirectionDevice2, (Channel)settings.SonarDevice);
                await Connection.SetStateAsync(1);
            }
            else
            {
                sonarManager.PlaybackDevices.SetPlaybackDevice(settings.RedirectionDevice1, (Channel)settings.SonarDevice);
                await Connection.SetStateAsync(0);
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            try
            {
                if(!sonarManager.IsRunning)
                {
                    await Connection.ShowAlert();
                }

                if (settings.RedirectionDevices1 == null || settings.RedirectionDevices2 == null || settings.RedirectionDevices1?.Count == 0 || settings.RedirectionDevices2?.Count == 0)
                {
                    var devices = sonarManager.PlaybackDevices.GetInputPlaybackDevices();

                    if (devices.Any())
                    {
                        settings.RedirectionDevices1 = devices.Select(x => new RedirectionDevice(x)).ToList();
                        settings.RedirectionDevices2 = devices.Select(x => new RedirectionDevice(x)).ToList();
                        await SaveSettings();
                    }
                    return;
                }

                var currentPlaybackDevice = sonarManager.PlaybackDevices.GetPlaybackDevice((Channel)settings.SonarDevice);
                if (currentPlaybackDevice.Id == settings.RedirectionDevice1)
                {
                    await Connection.SetStateAsync(0);
                }
                else if(currentPlaybackDevice.Id == settings.RedirectionDevice2)
                {
                    await Connection.SetStateAsync(1);
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

        #endregion
    }
}