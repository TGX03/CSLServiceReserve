using System;
using System.IO;
using System.Xml.Serialization;
namespace CSLServiceReserve
{
    public class Configuration
    {
        [XmlIgnoreAttribute] public const uint CURRENT_VERSION = 1; //running version

        public float autoRefreshSeconds = 3.0f;
        public string configReserved = ""; //reserved value again for possible migration\upgrade data or some unknown use.

        public uint configVersion = 1; //saved version incase we ever need to migrate or wipe config values.
        public const string CUSTOM_LOG_FILE_PATH = "CSLServiceReserve_Log.txt";
        public bool debugLogging = false;
        public const byte DEBUG_LOGGING_LEVEL = 0; //detail: 1 basically very similar to just on+0 ; 2 = Very detailed; 3+ extreme only meant for me during dev...if that. 
        public const string DUMP_STATS_FILE_PATH = "CSLServiceReserve_InfoDump.txt";
        public bool dumpStatsOnMapEnd = false;
        public bool enableGui = true;
        public float guiOpacity = 0.90f;
        public float refreshVehicleCounterSeconds = 0.180f; //~5-6x per second or every ~200 miliseconds (it's not exact but should be close).
        public uint resetStatsEveryXMin = 20;               //applies to guimode only
        public bool resetStatsEveryXMinutesEnabled = false; //applies to guimode only
        public bool useAutoRefresh = true;
        public const bool USE_CUSTOM_DUMP_FILE = false;
        public readonly bool useCustomLogFile = false;
        public ushort vehicleReserveAmount = 16;
        public int vehicleReserveAmountIndex = 1; //used to set dropbox default index in options screen. Make sure it matches. with default text and classes default value if you change!

        public static bool isCurrentVersion(uint iVersion)
        {
            if (iVersion != CURRENT_VERSION) return false;
            return true;
        }

        public static void serialize(string filename, Configuration config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            try{
                using (StreamWriter writer = new StreamWriter(filename)){
                    serializer.Serialize(writer, config);
                }
            }
            catch (IOException ex1){
                Helper.dbgLog("Filesystem or IO Error: \r\n", ex1, true);
            }
            catch (Exception ex1){
                Helper.dbgLog(ex1.Message + "\r\n", ex1, true);
            }
        }

        public static Configuration deserialize(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));

            try{
                using (StreamReader reader = new StreamReader(filename)){
                    Configuration config = (Configuration)serializer.Deserialize(reader);
                    validateConfig(ref config);
                    return config;
                }
            }

            catch (FileNotFoundException ex4){
                Helper.dbgLog("File not found. This is expected if no config file. \r\n", ex4);
            }

            catch (IOException ex1){
                Helper.dbgLog("Filesystem or IO Error: \r\n", ex1, true);
            }
            catch (Exception ex1){
                Helper.dbgLog(ex1.Message + "\r\n", ex1, true);
            }

            return null;
        }

        /// <summary>
        ///     Constrain certain values read in from the config file that will either cause issue or just make no sense.
        /// </summary>
        /// <param name="tmpConfig"> An instance of an initialized Configuration object (byref)</param>
        public static void validateConfig(ref Configuration tmpConfig)
        {
            if (tmpConfig.guiOpacity > 1.0f | tmpConfig.guiOpacity < 0.1f) tmpConfig.guiOpacity = 1.0f;
            if (tmpConfig.autoRefreshSeconds > 60.0f | tmpConfig.autoRefreshSeconds < 1.0f) tmpConfig.autoRefreshSeconds = 3.0f;
            if (tmpConfig.refreshVehicleCounterSeconds > 10.0f | tmpConfig.refreshVehicleCounterSeconds < 0.05f) tmpConfig.refreshVehicleCounterSeconds = 0.180f;
            if (tmpConfig.resetStatsEveryXMin < 1 | tmpConfig.resetStatsEveryXMin > 10000000) tmpConfig.resetStatsEveryXMin = 20;
        }
    }
}
