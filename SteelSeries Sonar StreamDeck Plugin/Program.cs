using BarRaider.SdTools;
using SteelSeriesAPI.Sonar;
using SteelSeriesAPI.Sonar.Enums;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace com.rydersir.sonargg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            /*
            var sonarManager = new SonarBridge();
            var volume = sonarManager.VolumeSettings.GetVolume(Channel.GAME);
            DrawVolume(volume, false, "left.png");
            DrawVolume(volume, true, "right.png");

            SDWrapper.Run(args);
            */

            SDWrapper.Run(args);
        }

        static void DrawVolume(double volume, bool displayRight, string filename)
        {
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

                var pen = new Pen(Color.LightGray, 2);
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
                graphics.FillPath(Brushes.Green, volumePath);
            }

            // Draw the wider circular tracker
            graphics.FillEllipse(Brushes.OrangeRed, circleX, circleY, circleDiameter, circleDiameter);

            // Save the bitmap to a file
            bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }


    }
}
