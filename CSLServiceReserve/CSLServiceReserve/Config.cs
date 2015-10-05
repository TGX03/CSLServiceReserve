using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace CSLServiceReserve
{
    public class Configuration
    {
        [XmlIgnoreAttribute]
        public static readonly uint CurrentVersion = 1;  //running version

        public uint ConfigVersion = 1;    //saved version incase we ever need to migrate or wipe config values.
        public string ConfigReserved = ""; //reserved value again for possible migration\upgrade data or some unknown use.
        public bool DebugLogging = false;   
        public byte DebugLoggingLevel = 0;  //detail: 1 basically very similar to just on+0 ; 2 = Very detailed; 3+ extreme only meant for me during dev...if that. 
        public ushort VehicleReserveAmount = 16;
        public int VehicleReserveAmountIndex = 1; //used to set dropbox default index in options screen. Make sure it matches. with default text and classes default value if you change!
        public bool EnableGui = true;
        public bool UseAutoRefresh = true;
        public float AutoRefreshSeconds = 3.0f;
        public float GuiOpacity = 0.90f;
        public bool DumpStatsOnMapEnd = false;
        public bool ResetStatsEveryXMinutesEnabled=false; //applies to guimode only
        public uint ResetStatsEveryXMin = 20;  //applies to guimode only
        public float RefreshVehicleCounterSeconds = 0.180f; //~5-6x per second or every ~200 miliseconds (it's not exact but should be close).
        public bool UseCustomDumpFile = false;
        public string DumpStatsFilePath = "CSLServiceReserve_InfoDump.txt";
        public bool UseCustomLogFile = false;
        public string CustomLogFilePath = "CSLServiceReserve_Log.txt";
        public Configuration() { }

        public static bool isCurrentVersion(uint iVersion)
        {
            if(iVersion != CurrentVersion)
            {
                return false;
            }
            return true;
        }

        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n", ex1, true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n", ex1, true);
            }
        }

        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    ValidateConfig(ref config);
                    return config;
                }
            }
            
            catch(System.IO.FileNotFoundException ex4)
            {
                Helper.dbgLog("File not found. This is expected if no config file. \r\n",ex4,false);
            }

            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n",ex1,true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n",ex1,true);
            }

            return null;
        }

        /// <summary>
        /// Constrain certain values read in from the config file that will either cause issue or just make no sense. 
        /// </summary>
        /// <param name="tmpConfig"> An instance of an initialized Configuration object (byref)</param>

        public static void ValidateConfig(ref Configuration tmpConfig)
        {
            if (tmpConfig.GuiOpacity > 1.0f | tmpConfig.GuiOpacity < 0.1f) tmpConfig.GuiOpacity = 1.0f;
            if (tmpConfig.VehicleReserveAmount > 512 | tmpConfig.VehicleReserveAmount < 2) tmpConfig.VehicleReserveAmount = 16;
            if (tmpConfig.AutoRefreshSeconds > 60.0f | tmpConfig.AutoRefreshSeconds < 1.0f) tmpConfig.AutoRefreshSeconds=3.0f;
            if (tmpConfig.RefreshVehicleCounterSeconds > 10.0f | tmpConfig.RefreshVehicleCounterSeconds < 0.05f) tmpConfig.RefreshVehicleCounterSeconds = 0.180f;
            if (tmpConfig.ResetStatsEveryXMin < 1 | tmpConfig.ResetStatsEveryXMin > 10000000) tmpConfig.ResetStatsEveryXMin = 20;
        }
    }
}
