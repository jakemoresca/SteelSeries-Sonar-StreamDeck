using BarRaider.SdTools;
using SteelSeriesAPI;
using SteelSeriesAPI.Sonar.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.rydersir.sonargg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            //var sonarManager = new SonarBridge();
            //var volume = sonarManager.GetVolume(Device.Game);
            //DrawVolume(volume, false, "bottom.png");
            //DrawVolume(volume, true, "top.png");

            SDWrapper.Run(args);
        }

    }
}
