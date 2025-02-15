using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteelSeriesAPI;
using SteelSeriesAPI.Sonar.Enums;
using com.rydersir.sonargg.Domains;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

namespace com.rydersir.sonargg
{
    [PluginActionId("com.rydersir.sonargg.volumebar")]
    public class VolumeBarAction : KeypadBase
    {
        const string DEFAULT_TRACKER_COLOR = "#dddddd";
        const string DEFAULT_VOLUME_BORDER_COLOR = "#ffffff";
        const string DEFAULT_VOLUME_LEVEL_COLOR = "#ffffff";

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.SonarDevice = (int)Device.Game;
                instance.VolumeOperation = (int)VolumeOperation.Increase;
                instance.Orientation = (int)DisplayOrientation.Vertical;
                instance.Amount = 5;
                instance.DisplayTracker = true;
                return instance;
            }

            [JsonProperty(PropertyName = "sonarDevice")]
            public int SonarDevice { get; set; }

            [JsonProperty(PropertyName = "orientation")]
            public DisplayOrientation Orientation { get; set; }

            [JsonProperty(PropertyName = "volumeOperation")]
            public VolumeOperation VolumeOperation { get; set; }

            [JsonProperty(PropertyName = "amount")]
            public int Amount { get; set; }

            [JsonProperty(PropertyName = "displayTracker")]
            public bool DisplayTracker { get; set; }

            [JsonProperty(PropertyName = "trackerColor")]
            public string TrackerColor { get; set; }

            [JsonProperty(PropertyName = "volumeBorderColor")]
            public string VolumeBorderColor { get; set; }

            [JsonProperty(PropertyName = "volumeLevelColor")]
            public string VolumeLevelColor { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private SonarBridge sonarManager;

        #endregion
        public VolumeBarAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
            var currentVolume = sonarManager.GetVolume((Device)settings.SonarDevice);
            var amount = settings.Amount;

            switch (settings.VolumeOperation)
            {
                case VolumeOperation.Increase:
                    currentVolume += (amount * 0.01);
                    currentVolume = currentVolume > 1 ? 1 : currentVolume;
                    break;

                case VolumeOperation.Decrease:
                    currentVolume -= (amount * 0.01);
                    currentVolume = currentVolume < 0 ? 0 : currentVolume;
                    break;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, $"Setting volume of {settings.SonarDevice} to {currentVolume}");
            sonarManager.SetVolume(currentVolume, (Device)settings.SonarDevice);
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            try
            {
                var mediaVolume = sonarManager.GetVolume((Device)settings.SonarDevice);

                if (settings.Orientation == DisplayOrientation.Vertical)
                {
                    await DisplayVerticalVolumeBar(mediaVolume);
                }
                else if (settings.Orientation == DisplayOrientation.Horizontal)
                {
                    await DisplayHorizontalVolumeBar(mediaVolume);
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

        private async Task DisplayVerticalVolumeBar(double volume)
        {
            var displayTopHalf = settings.VolumeOperation == VolumeOperation.Increase;
            var colorConverter = new ColorConverter();

            var volumeBorderColor = string.IsNullOrEmpty(settings.VolumeBorderColor) ? DEFAULT_VOLUME_BORDER_COLOR : settings.VolumeBorderColor;
            var volumeLevelColor = string.IsNullOrEmpty(settings.VolumeLevelColor) ? DEFAULT_VOLUME_LEVEL_COLOR : settings.VolumeLevelColor;
            var trackerColor = string.IsNullOrEmpty(settings.TrackerColor) ? DEFAULT_TRACKER_COLOR : settings.TrackerColor;

            var volumeBorderBrush = new SolidBrush((Color)colorConverter.ConvertFromString(volumeBorderColor));
            var volumeLevelBrush = new SolidBrush((Color)colorConverter.ConvertFromString(volumeLevelColor));
            var trackerBrush = new SolidBrush((Color)colorConverter.ConvertFromString(trackerColor));

            using var bitmap = Tools.GenerateGenericKeyImage(out var graphics);
            var height = bitmap.Height;
            var width = bitmap.Width;

            int viewportX = 0;
            int viewportY = displayTopHalf ? 0 : height;

            // SVG properties for the elements
            var rectWidth = width / 2;
            var rectHeight = height * 2;
            var rectX = width / 4;

            //circular radius
            var rectRx = 30;
            var rectRy = 30;

            var volumeY = rectHeight - ((float)volume * rectHeight);
            var volumeHeight = (float)volume * rectHeight;

            var cx = width / 2;
            var cy = (int)volumeY;
            var r = (rectWidth / 2) + 10;

            int circleX = cx - r;
            int circleY = cy - r;
            int circleDiameter = 2 * r;

            // Translate the graphics object to the viewBox origin
            graphics.TranslateTransform(0, -viewportY);

            // Set the background color
            graphics.Clear(Color.Transparent);

            // Draw the background rectangle with rounded corners
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rectX, 0, rectRx * 2, rectRy * 2, 180, 90);
                path.AddArc(rectX + rectWidth - rectRx * 2, 0, rectRx * 2, rectRy * 2, 270, 90);
                path.AddArc(rectX + rectWidth - rectRx * 2, rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 0, 90);
                path.AddArc(rectX, rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 90, 90);
                path.CloseFigure();

                var pen = new Pen(volumeBorderBrush.Color, 2);
                graphics.DrawPath(pen, path);
            }

            // Draw the current volume level with rounded corners
            using (GraphicsPath volumePath = new GraphicsPath())
            {
                volumePath.AddArc(rectX, volumeY, rectRx * 2, rectRy * 2, 180, 90);
                volumePath.AddArc(rectX + rectWidth - rectRx * 2, volumeY, rectRx * 2, rectRy * 2, 270, 90);
                volumePath.AddArc(rectX + rectWidth - rectRx * 2, volumeY + volumeHeight - rectRy * 2, rectRx * 2, rectRy * 2, 0, 90);
                volumePath.AddArc(rectX, volumeY + volumeHeight - rectRy * 2, rectRx * 2, rectRy * 2, 90, 90);
                volumePath.CloseFigure();

                graphics.FillPath(volumeLevelBrush, volumePath);
            }

            if (settings.DisplayTracker)
            {
                // Draw the wider circular tracker using the calculated values
                graphics.FillEllipse(trackerBrush, circleX, circleY, circleDiameter, circleDiameter);
            }

            await Connection.SetImageAsync(bitmap);
        }

        private async Task DisplayHorizontalVolumeBar(double volume)
        {
            var displayRight = settings.VolumeOperation == VolumeOperation.Increase;
            var colorConverter = new ColorConverter();

            var volumeBorderColor = string.IsNullOrEmpty(settings.VolumeBorderColor) ? DEFAULT_VOLUME_BORDER_COLOR : settings.VolumeBorderColor;
            var volumeLevelColor = string.IsNullOrEmpty(settings.VolumeLevelColor) ? DEFAULT_VOLUME_LEVEL_COLOR : settings.VolumeLevelColor;
            var trackerColor = string.IsNullOrEmpty(settings.TrackerColor) ? DEFAULT_TRACKER_COLOR : settings.TrackerColor;

            var volumeBorderBrush = new SolidBrush((Color)colorConverter.ConvertFromString(volumeBorderColor));
            var volumeLevelBrush = new SolidBrush((Color)colorConverter.ConvertFromString(volumeLevelColor));
            var trackerBrush = new SolidBrush((Color)colorConverter.ConvertFromString(trackerColor));

            using var bitmap = Tools.GenerateGenericKeyImage(out var graphics);
            var height = bitmap.Height;
            var width = bitmap.Width;

            int padding = 1;  // Padding on the left and right

            // SVG properties for the elements
            var rectWidth = (width - 2 * padding) * 2;  // Adjusted to include padding
            var rectHeight = 20;
            var rectX = padding;
            var rectY = (height - rectHeight) / 2;
            var rectRx = 10;
            var rectRy = 10;

            var volumeWidth = (float)volume * rectWidth;
            var volumeX = padding;

            var cx = (int)volumeWidth; //displayRight ? width - padding - (int)(rectWidth * volume) / 2 : padding + (int)(rectWidth * volume) / 2;
            var cy = height / 2;
            var r = 15;

            int circleX = cx - r;
            int circleY = cy - r;
            int circleDiameter = 2 * r;

            // Translate the graphics object to the viewBox origin
            graphics.TranslateTransform(displayRight ? -width : -padding, 0);

            // Set the background color
            graphics.Clear(Color.Transparent);

            // Draw the background rectangle with borders and no fill
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rectX, rectY, rectRx * 2, rectRy * 2, 180, 90);
                path.AddArc(rectX + rectWidth - rectRx * 2, rectY, rectRx * 2, rectRy * 2, 270, 90);
                path.AddArc(rectX + rectWidth - rectRx * 2, rectY + rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 0, 90);
                path.AddArc(rectX, rectY + rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 90, 90);
                path.CloseFigure();

                var pen = new Pen(volumeBorderBrush.Color, 2);
                graphics.DrawPath(pen, path);
            }

            // Draw the current volume level with rounded corners
            using (GraphicsPath volumePath = new GraphicsPath())
            {
                volumePath.AddArc(volumeX, rectY, rectRx * 2, rectRy * 2, 180, 90);
                volumePath.AddArc(volumeX + volumeWidth - rectRx * 2, rectY, rectRx * 2, rectRy * 2, 270, 90);
                volumePath.AddArc(volumeX + volumeWidth - rectRx * 2, rectY + rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 0, 90);
                volumePath.AddArc(volumeX, rectY + rectHeight - rectRy * 2, rectRx * 2, rectRy * 2, 90, 90);
                volumePath.CloseFigure();

                graphics.FillPath(volumeLevelBrush, volumePath);
            }

            if (settings.DisplayTracker)
            {
                // Draw the wider circular tracker
                graphics.FillEllipse(trackerBrush, circleX, circleY, circleDiameter, circleDiameter);
            }

            await Connection.SetImageAsync(bitmap);
        }

        #endregion
    }
}