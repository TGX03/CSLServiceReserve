using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace CSLServiceReserve
{
    public class Helper
    {
        [Flags]
        public enum DumpOption : byte
        {
            NONE = 0,
            DEFAULT = 1,
            MAP_LOADED = 2,
            OPTIONS_ONLY = 4,
            DEBUG_INFO = 8,
            USE_SEPERATE_FILE = 16,
            VEHICLE_DATA = 32,
            GUI_ACTIVE = 64,
            EXTENDED_INFO = 128,
            ALL = 255
        }
        private const string DUMP_STATS_HEADER = "\r\n---------- CSLServiceReserve StatsDump ----------\r\n";
        private const string DUMP_VERSION = "ModVersion: {0}   Current DateTime: {1}\r\n";
        private const string DUMP_V0 = "\r\n----- Vehicle Data -----\r\n";
        private const string DUMP_V1 = "Number of Vehicles in use: {0} \r\n";
        private const string DUMP_V2 = "Selected # of reserved vehicles: {1} \r\n";
        private const string DUMP_V3 = "Number of Vehicles Max: {2}   New Reserved Limit: {3} \r\n";
        private const string DUMP_V4 = "# Attempts to use reserved vehicles: {4} \r\n";
        private const string DUMP_V5 = "# Attempts to use reserved vehicles (but failed): {5} \r\n";
        private const string DUMP_V6 = "# Times the reserved limits were exceeded: {6} \r\n";
        private const string DUMPV7 = "# Times game failed to create a non-reserved vehicle:  {7} \r\n";
        private const string DUMPV8 = "# Times game failed to create any vehicle:  {8} \r\n";
        private const string DUMPV9 = "# Times in total game tried to create a vehicle:  {9} \r\n";

        private const string DBG_DUMPSTR1 = "\r\n----- Debug info -----\r\n";
        private const string DBG_DUMPSTR2 = "DebugEnabled: {0}  DebugLogLevel: {1}  isGuiEnabled {2}  AutoRefreshEnabled {3} \r\n";
        private const string DBG_DUMPSTR3 = "ReserveAmount: {4}  IsEnabled: {5}  IsInited: {6}  isGuiRunning {7} \r\n";
        private const string DBG_DUMPSTR4 = "IsRedirectActive: {8}  UseAutoRefreshOption: {9}  AutoRefreshSeconds: {10}  GUIOpacity: {11} \r\n";
        private const string DBG_DUMPSTR5 = "ResetStatsEnabled: {12}  ResetStatsEvery: {13}min  UseCustomLogfile: {14}  DumpLogOnMapEnd: {15} \r\n";
        private const string DBG_DUMPSTR6 = "UseCustomDumpFile: {16}  DumpFileFullpath: {17}  \r\n";
        private const string DBG_DUMPSTR_GUI_EXTRA1 = "IsAutoRefreshActive {3}  CoRoutineVehc: {5}  CoRoutineDataReset: {6}  CoRoutineDisplayData: {7} \r\nNextDataResetTime: {8}\r\n";
        private const string DBG_DUMPSTR_GUI_EXTRA2 = "NewGameAppVersion: {0}  CityName: {1}  Paused: {2} \r\n";
        private const string DBG_DUMP_PATHS = "Path Info: \r\n AppBase: {0} \r\n AppExe: {1} \r\n Mods: {2} \r\n Saves: {3} \r\n gContent: {4} \r\n AppLocal: {5} \r\n";

        private const string DBG_GAME_VERSION = "UnityProd: {0}  UnityPlatform: {1} \r\nProductName: {2}  ProductVersion: {3}  ProductVersionString: {4}\r\n";

        private const string SBG_MAP_LIMITS1 = "#NetSegments: {0} | {1}   #NetNodes: {2} | {3}  #NetLanes: {4} | {5} \r\n";
        private const string SBG_MAP_LIMITS2 = "#Buildings: {0} | {1}  #ZonedBlocks: {2} | {3} \r\n";
        private const string SBG_MAP_LIMITS3 = "#Transportlines: {4}  #UserProps: {5}  #PathUnits: {6} \r\n#Areas: {8}  #Districts: {9} \r\n#BrokenAssets: {7}\r\n";
        private const string SBG_MAP_LIMITS4 = "#Citizens: {0}  #Families: {1}  #ActiveCitzenAgents: {2} \r\n";


        private static object[] TMPVer;
        private static object[] TMPVehc;
        private static object[] TMPPaths;
        private static object[] Tmpdbg;
        private static object[] TMPGuiExtra;
        private static object[] TMPGuiExtra2;

        //should be enough for most log messages and we want this guy in the HFHeap.
        private static readonly StringBuilder LOG_SB = new StringBuilder(512);

        public static void logExtentedWrapper(DumpOption bMode)
        {
            StringBuilder sb = new StringBuilder((bMode | DumpOption.GUI_ACTIVE) == bMode ? 8192 : 4096);
            refreshSourceData(bMode);
            buildTheString(sb, bMode);
            dumpStatsToLog(sb.ToString(), bMode);
        }


        private static void refreshSourceData(DumpOption bMode)
        {
            //Version & Platform data
            TMPVer = new object[]
            { Application.productName, Application.platform.ToString(),
              DataLocation.productName, DataLocation.productVersion.ToString(),
              DataLocation.productVersionString };
            //PathData
            TMPPaths = new object[]
            { DataLocation.applicationBase, DataLocation.executableDirectory,
              DataLocation.modsPath, DataLocation.saveLocation, DataLocation.gameContentPath, DataLocation.localApplicationData };

            //VehicleData
            TMPVehc = new object[]
            { (bMode | DumpOption.MAP_LOADED) == bMode ? Singleton<VehicleManager>.instance.m_vehicleCount.ToString() : "n\\a",
              Mod.reserveamount.ToString(),
              (bMode | DumpOption.MAP_LOADED) == bMode ? (Singleton<VehicleManager>.instance.m_vehicles.m_size - 1).ToString() : "16383",
              (16383 - Mod.reserveamount).ToString(), Mod.timesReservedAttempted.ToString(),
              Mod.timesReserveAttemptFailed.ToString(), Mod.timesLimitReached.ToString(),
              Mod.timesFailedByReserve.ToString(), Mod.timesFailedToCreate.ToString(), Mod.timesCvCalledTotal.ToString() };

            //debugdata
            Tmpdbg = new object[]
            { Mod.debugLOGOn.ToString(), Mod.debugLOGLevel.ToString(), Mod.isGuiEnabled.ToString(),
              Mod.useAutoRefreshOption.ToString(), Mod.reserveamount.ToString(), Mod.isEnabled.ToString(), Mod.isInited.ToString(),
              Loader.isGuiRunning.ToString(), Mod.isRedirectActive.ToString(), Mod.useAutoRefreshOption.ToString(),
              Mod.autoRefreshSeconds.ToString("F2"), Mod.config.guiOpacity.ToString("F04"), Mod.config.resetStatsEveryXMinutesEnabled.ToString(),
              Mod.config.resetStatsEveryXMin.ToString("f2"), Mod.config.useCustomLogFile.ToString(),
              Mod.config.dumpStatsOnMapEnd.ToString(), Configuration.USE_CUSTOM_DUMP_FILE,
              Configuration.DUMP_STATS_FILE_PATH };

            if ((bMode | DumpOption.GUI_ACTIVE) == bMode){ //gui mode exclusive
                TMPGuiExtra2 = new object[]
                { Singleton<SimulationManager>.instance.m_metaData != null ? Singleton<SimulationManager>.instance.m_metaData.m_newGameAppVersion.ToString() : "n/a",
                  Singleton<SimulationManager>.instance.m_metaData != null ? Singleton<SimulationManager>.instance.m_metaData.m_CityName : "n/a",
                  Singleton<SimulationManager>.exists ? Singleton<SimulationManager>.instance.SimulationPaused.ToString() : "n/a" };

                ExternalData mytmp;
                mytmp = CslServiceReserveGUI.getInternalData();
                TMPGuiExtra = mytmp.toStringArray();
                //CSLServiceReserveGUI.GetInternalData.ToStringArray();
            }
        }


        private static void addGetPluginList(StringBuilder sb)
        {
            int tmpcount = 0;
            sb.AppendLine("\r\n----- Enabled Mod List ------");
            try{
                foreach (PluginManager.PluginInfo p in Singleton<PluginManager>.instance.GetPluginsInfo()){
                    if (p.isEnabled){
                        var tmpInstances = p.GetInstances<IUserMod>();
                        if (tmpInstances.Length == 1) sb.AppendLine(string.Concat("Mod Fullname: ", tmpInstances[0].Name, "  Description: ", tmpInstances[0].Description));
                        else sb.AppendLine(string.Concat("(***MultipleInstances***)Mod Fullname: ", tmpInstances[0].Name, "  Description: ", tmpInstances[0].Description));
                        sb.AppendLine(string.Concat("LocalName: ", p.name, "  WorkShopID: ", p.publishedFileID.AsUInt64.ToString(), "  AssemblyPath: ", p.modPath, p.assembliesString));
                        tmpcount++;
                    }
                }
                sb.AppendLine(string.Format("{0} Plugins\\Mods are enabled of {1} installed.", tmpcount.ToString(), Singleton<PluginManager>.instance.modCount.ToString()));
            }
            catch (Exception ex){
                dbgLog("Error getting list of plugins.", ex, true);
            }

            ////
            //PackageManager  PkgMrg = Singleton<PackageManager>..instance ;
            //PkgMrg.m

/*            if(Steam.active == true && Steam.workshop != null && PackageManager.noWorkshop == false)
            {
                PackageManager ppp = Singleton<PackageManager>.instance;
                //object = PackageManager.

                List<FileSystemReporter> hhh = (List<FileSystemReporter>)typeof(PackageManager).GetField("m_FileSystemReporters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ppp);//.GetValue(ppp);
                
                dbgLog("Number of FileSystemReporters: " + hhh.Count().ToString());
                
                dbgLog("PackageManager.allPackages.Count: " + PackageManager.allPackages.Count().ToString());
                //PMgr = Singleton<PackageManager>;
                int tmpPackageCount = 0;
                foreach(Package pkg in PackageManager.allPackages)
                {
                    tmpPackageCount++;
                    Helper.dbgLog( pkg.packageName + " Author: " + pkg.packageAuthor + " MainAssett" + pkg.packageMainAsset +
                        "\r\nPath:" + pkg.packagePath + " " + pkg.packageVersionStr + "");
                    Dictionary<string,Package.Asset> bbb = (Dictionary<string,Package.Asset>)typeof(Package).GetField("m_IndexTable",System.Reflection.BindingFlags.NonPublic |System.Reflection.BindingFlags.Instance).GetValue(pkg);

                    foreach (KeyValuePair<string, Package.Asset> kvp in bbb)
                    {
                        Package.Asset pa = kvp.Value;
                        Helper.dbgLog("PkgAsset_idx: " + kvp.Key + "  PkgAsset_name: " + pa.name + " PkgAsset_Type: " + pa.type +
                            "\n PathonDisk: " + pa.pathOnDisk + "\n Size:"+ pa.size.ToString() + " isWorkshop: " + pa.isWorkshopAsset.ToString() );
                    }
                }
            }
*/
////screw around with this crap some other time it don't belong in this project anyway.
/*            if (Steam.active == true && Steam.workshop != null && PackageManager.noWorkshop == false)
            {
                PublishedFileId[] SteamSubItems = Steam.workshop.GetSubscribedItems();
                int tWksCount = SteamSubItems.Count();
                int folderwithfiles = 0;
                int filecount = 0;
                long Totalbytes = 0;
                if (tWksCount > 0)
                {
                    for (int i = 0; i < SteamSubItems.Length; i++)
                    {
                        string subscribedItemPath = Steam.workshop.GetSubscribedItemPath(SteamSubItems[i]);
                        if (subscribedItemPath != null)
                        {
                            DirectoryInfo folders = new DirectoryInfo(subscribedItemPath);
                            FileInfo[] files = folders.GetFiles();
                            if (files.Length > 0)
                            {
                                foreach (FileInfo f in files)
                                {
                                    filecount++;
                                    if (Path.GetExtension(f.FullName) == PackageManager.packageExtension)
                                    {
                                        Totalbytes += f.Length;
                                        folderwithfiles++;
                                    }

                                }
                            }
                            DirectoryInfo[] folders2 = folders.GetDirectories();
                            if(folders2.Count() > 0) 
                            {
                                foreach (DirectoryInfo di in folders2)
                                {
                                    foreach (FileInfo f in files)
                                    {
                                        filecount++;
                                        if (Path.GetExtension(f.FullName) == PackageManager.packageExtension)
                                        {
                                            Totalbytes += f.Length;
                                            folderwithfiles++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                object[] wksobj = new object[] { tWksCount.ToString(), folderwithfiles.ToString(), filecount.ToString(), (Totalbytes / 1024).ToString() };
                sb.AppendFormat("\r\nYou are subscribed to {0} workshop items, \r\n stored in about {1} folders, containing about {2} total files\r\n with a total size of {3}kb .", wksobj);
            }
*/
            sb.AppendLine("--------End Plugins--------\r\n");
//            if(Singleton<SimulationManager>.exists)
//            {
//                string abc = Singleton<SimulationManager>.instance.SimulationPaused.ToString();
//                    string abc2 = Singleton<SimulationManager>.instance.m_metaData.m_MapName.ToString();
//                    string abc3 = Singleton<SimulationManager>.instance.m_metaData.m_CityName.ToString();
//                    string abc4 = String.Format("AppFullVersion: {0}  AppDataFormatVersion: {1}",BuildConfig.applicationVersionFull,BuildConfig.DATA_FORMAT_VERSION.ToString());
//                   string abc5 = string.Format("UnlockDLCAssets: {0} IgnoreDLCAssets{1}",Singleton<LoadingManager>.instance.m_unlockDlcAssets.ToString(),Singleton<LoadingManager>.instance.m_ignoreDlcAssets.ToString());
//                    string abc5 = string.Format("BrokenAssets: {0} \n IgnoreDLCAssets{1}",Singleton<LoadingManager>.instance.m_brokenAssets.ToString(),Singleton<LoadingManager>.instance.ToString());
//                    string ab = Steam.workshop.GetSubscribedItems
//            }
        }

        private static void buildTheString(StringBuilder sb, DumpOption bMode)
        {
            try{
//                Debug.Log(string.Concat("[CSLServiceReserve.Helper] elements tmpVer:", tmpVer.Length.ToString(),
//                    " tmpPaths:", tmpPaths.Length.ToString()," tmpVehc:", tmpVehc.Length.ToString(),
//                    " tmpdbg:",tmpdbg.Length.ToString()," tmpGuiExtra:", tmpGuiExtra.Length.ToString(), " tmpguiExtra2:",tmpGuiExtra2.Length.ToString() ));

                // Do our header & Mod version info if Default.
                sb.Append(DUMP_STATS_HEADER);

                if ((bMode | DumpOption.DEFAULT) == bMode){
                    sb.Append(string.Format(DUMP_VERSION, Mod.VERSION_BUILD_NUMBER, DateTime.Now.ToString()));
                    sb.AppendLine(string.Format("CSLAppFullVersion: {0}  AppDataFormatVersion: {1}\r\n", BuildConfig.applicationVersionFull, BuildConfig.DATA_FORMAT_VERSION.ToString()));
                }

                //dump Version and Path data if DebugInfo enabled.
                if ((bMode | DumpOption.DEBUG_INFO) == bMode){
                    sb.Append("raw commandline: " + CommandLine.raw + "\r\n");
                    sb.Append(string.Format(DBG_GAME_VERSION, TMPVer));
                    sb.Append(string.Format(DBG_DUMP_PATHS, TMPPaths));
                }

                //dump VechData if enabled.
                if ((bMode | DumpOption.VEHICLE_DATA) == bMode)
                    sb.AppendFormat(string.Concat(DUMP_V0, DUMP_V1, DUMP_V2, DUMP_V3, DUMP_V4, DUMP_V5, DUMP_V6,
                        DUMPV7, DUMPV8, DUMPV9), TMPVehc);

                //debug into
                if ((bMode | DumpOption.DEBUG_INFO) == bMode)
                    sb.Append(string.Format(string.Concat(DBG_DUMPSTR1, DBG_DUMPSTR2, DBG_DUMPSTR3,
                        DBG_DUMPSTR4, DBG_DUMPSTR5, DBG_DUMPSTR6), Tmpdbg));

                //gui | map for sure loaded related things.
                if ((bMode | DumpOption.GUI_ACTIVE) == bMode & (bMode | DumpOption.DEBUG_INFO) == bMode){
                    sb.Append(string.Format(DBG_DUMPSTR_GUI_EXTRA1, TMPGuiExtra));
                    sb.Append(string.Format(DBG_DUMPSTR_GUI_EXTRA2, TMPGuiExtra2));
                    addLimitData(sb);
                }

                //dump pluging\mod info
                if ((bMode | DumpOption.EXTENDED_INFO) == bMode) addGetPluginList(sb);
                sb.AppendLine("--------End Dump--------\r\n");
            }
            catch (Exception ex){
                dbgLog("Error:\r\n", ex, true);
            }

            if (Mod.debugLOGOn & Mod.debugLOGLevel >= 2) dbgLog("Built the log string to dump.");
        }

        /// <summary>
        ///     Adds building and network limit information into the string builder stream... we added other data why not this.
        /// </summary>
        /// <param name="sb">an already created stringbuilder object.</param>
        private static void addLimitData(StringBuilder sb)
        {
            try{
                sb.AppendLine("\r\n----- Map Limit Data -----\r\n");
                NetManager tMgr = Singleton<NetManager>.instance;
                object[] tmpdata =
                { tMgr.m_segmentCount.ToString(), tMgr.m_segments.ItemCount().ToString(),
                  tMgr.m_nodeCount.ToString(), tMgr.m_nodes.ItemCount().ToString(), tMgr.m_laneCount.ToString(),
                  tMgr.m_lanes.ItemCount().ToString() };
                sb.AppendFormat(SBG_MAP_LIMITS1, tmpdata);

                CitizenManager cMgr = Singleton<CitizenManager>.instance;
                tmpdata = new object[]
                { cMgr.m_citizens.ItemCount().ToString(), cMgr.m_units.ItemCount().ToString(),
                  cMgr.m_instances.ItemCount().ToString() };
                sb.AppendFormat(SBG_MAP_LIMITS4, tmpdata);

                tmpdata = new object[]
                { Singleton<BuildingManager>.instance.m_buildingCount.ToString(),
                  Singleton<BuildingManager>.instance.m_buildings.ItemCount().ToString(), Singleton<ZoneManager>.instance.m_blockCount.ToString(),
                  Singleton<ZoneManager>.instance.m_blocks.ItemCount(), Singleton<TransportManager>.instance.m_lines.ItemCount().ToString(),
                  Singleton<PropManager>.instance.m_props.ItemCount(), Singleton<PathManager>.instance.m_pathUnits.ItemCount(),
                  Singleton<LoadingManager>.instance.m_brokenAssets, Singleton<GameAreaManager>.instance.m_areaCount.ToString(),
                  Singleton<DistrictManager>.instance.m_districts.ItemCount().ToString() };
                sb.AppendFormat(SBG_MAP_LIMITS2, tmpdata);
                sb.AppendFormat(SBG_MAP_LIMITS3, tmpdata);
            }
            catch (Exception ex){
                dbgLog("Error:\r\n", ex, true);
            }
        }


        /// <summary>
        ///     Dumps our stats to a custom file or normal log file.
        /// </summary>
        /// <param name="strText">The string data.</param>
        /// <param name="bMode">The options flags that was used to create it (used to know if custom file or not)</param>
        public static void dumpStatsToLog(string strText, DumpOption bMode = 0)
        {
            try{
                string strTempPath = "";
                bool bDumpToLog = true;
                if ((bMode | DumpOption.USE_SEPERATE_FILE) == bMode){
                    if (Mod.debugLOGLevel > 1) dbgLog("\r\n-----Using Seperate file mode flagged" + bMode + "-----\r\n");
                }

                if (bDumpToLog){
                    if (Mod.debugLOGOn) dbgLog("\r\n Dumping to default game log.");
                    Debug.Log(strText);
                }
            }
            catch (Exception ex){
                dbgLog("Error:\r\n", ex, true);
            }
        }


        /// <summary>
        ///     Our LogWrapper...used everywhere.
        /// </summary>
        /// <param name="sText">Text to log</param>
        /// <param name="ex">An Exception - if not null it's basic data will be printed.</param>
        /// <param name="bDumpStack">If an Exception was passed do you want the stack trace?</param>
        /// <param name="bNoIncMethod">If for some reason you don't want the method name prefaced with the log line.</param>
        public static void dbgLog(string sText, Exception ex = null, bool bDumpStack = false, bool bNoIncMethod = false)
        {
            try{
                LOG_SB.Length = 0;
                string sPrefix = string.Concat("[", Mod.MOD_DBG_PREFIX);
                if (bNoIncMethod){ string.Concat(sPrefix, "] "); }
                else{
                    StackFrame oStack = new StackFrame(1); //pop back one frame, ie our caller.
                    sPrefix = string.Concat(sPrefix, ":", oStack.GetMethod().DeclaringType.Name, ".", oStack.GetMethod().Name, "] ");
                }
                LOG_SB.Append(string.Concat(sPrefix, sText));

                if (ex != null) LOG_SB.Append(string.Concat("\r\nException: ", ex.Message));
                if (bDumpStack) LOG_SB.Append(string.Concat("\r\nStackTrace: ", ex.StackTrace));
                if (Mod.config != null && Mod.config.useCustomLogFile){
                    string strPath = Directory.Exists(Path.GetDirectoryName(Configuration.CUSTOM_LOG_FILE_PATH)) ? Configuration.CUSTOM_LOG_FILE_PATH : Path.Combine(DataLocation.executableDirectory, Configuration.CUSTOM_LOG_FILE_PATH);
                    using (StreamWriter streamWriter = new StreamWriter(strPath, true)){
                        streamWriter.WriteLine(LOG_SB.ToString());
                    }
                }
                else{
                    Debug.Log(LOG_SB.ToString());
                }
            }
            catch (Exception exp){
                Debug.Log(string.Concat("[CSLServiceReserve.Helper.dbgLog()] Error in log attempt!  ", exp.Message));
            }
        }
        public class ExternalData
        {
            public string cachedname = "n/a";
            public bool coDataRefreshEnabled;
            public bool coDisplayRefreshEnabled;
            public bool coVechRefreshEnabled;
            public bool isAutoRefreshActive;
            public bool isVisable;
            public string name = "n/a";
            public DateTime statsResetTime;
            public string tag = "n/a";

            public object[] toStringArray()
            {
                object[] tmpStrArr =
                { name, cachedname,
                  tag, isAutoRefreshActive.ToString(), isVisable.ToString(), coVechRefreshEnabled.ToString(),
                  coDataRefreshEnabled.ToString(), coDisplayRefreshEnabled.ToString(),
                  statsResetTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt") };
                return tmpStrArr;
            }
        }
    }
}
