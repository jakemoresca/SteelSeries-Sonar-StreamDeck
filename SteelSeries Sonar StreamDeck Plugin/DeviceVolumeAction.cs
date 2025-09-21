using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteelSeriesAPI.Sonar.Enums;
using System;
using System.Threading.Tasks;
using SteelSeriesAPI.Sonar;

namespace com.rydersir.sonargg
{
    [PluginActionId("com.rydersir.sonargg.devicevolume")]
    public class DeviceVolumeAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.SonarDevice = (int)Channel.GAME;
                return instance;
            }

            [JsonProperty(PropertyName = "sonarDevice")]
            public int SonarDevice { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private SonarBridge sonarManager;

        #endregion
        public DeviceVolumeAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

            var isMuted = sonarManager.VolumeSettings.GetMute((Channel)settings.SonarDevice);
            var toggleAction = !isMuted;
            if (toggleAction)
            {
                await Connection.SetStateAsync(1);
            }
            else
            {
                await Connection.SetStateAsync(0);
            }

            sonarManager.VolumeSettings.SetMute(toggleAction, (Channel)settings.SonarDevice);
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

                var mediaVolume = sonarManager.VolumeSettings.GetVolume((Channel)settings.SonarDevice);
                await DisplayVolumeBar(mediaVolume);

                var isMuted = sonarManager.VolumeSettings.GetMute((Channel)settings.SonarDevice);
                if (isMuted)
                {
                    await Connection.SetStateAsync(1);
                }
                else
                {
                    await Connection.SetStateAsync(0);
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

        private async Task DisplayVolumeBar(double volume)
        {
            var volumeString = Math.Round(volume * 100);
            await Connection.SetTitleAsync(volumeString.ToString());
        }

        #endregion
    }
}