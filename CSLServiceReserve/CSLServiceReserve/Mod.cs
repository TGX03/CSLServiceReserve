using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;
namespace CSLServiceReserve
{
    public class Mod : IUserMod
    {
        internal const ulong MOD_WORKSHOPID = 529979180uL;
        private const string MOD_OFFICIAL_NAME = "CSL Service Reserve"; //debug==must match folder name
        private const string MOD_DESCRIPTION = "Allows you to reserve vehicles for critical services.";
        internal const string VERSION_BUILD_NUMBER = "1.7.0-f5 build_002";
        public static bool debugLOGOn;
        public static byte debugLOGLevel;
        internal static readonly string MOD_DBG_PREFIX = "CSLServiceReserve"; //same..for now.
        private const string MOD_CONFIGPATH = "CSLServiceReserve_Config.xml";

        public static bool isEnabled;        //tracks if the mod is enabled.
        public static bool isInited;         //tracks if we're inited
        public static bool isRedirectActive; //tracks detouring state.
        public static bool isGuiEnabled;     //tracks if the gui option is set.

        public static float autoRefreshSeconds = 3.0f; //why are storing these here again and not just using mod.config? //oldcode.
        public static bool useAutoRefreshOption;
        public static ushort reserveamount = 16; //oldcode holdover vs mod.config.

        public static ulong timesReservedAttempted;
        public static ulong timesLimitReached;
        public static ulong timesFailedToCreate;
        public static ulong timesFailedByReserve;
        public static ulong timesReserveAttemptFailed;
        public static ulong timesCvCalledTotal;
        private static readonly Dictionary<MethodInfo, RedirectCallsState> REDIRECT_DIC = new Dictionary<MethodInfo, RedirectCallsState>();
        public static Configuration config;



        /// <summary>
        ///     Public Constructor on load we grab our config info and init();
        /// </summary>
        public Mod()
        {
            try{
                Helper.dbgLog("\r\n" + MOD_OFFICIAL_NAME + " v" + VERSION_BUILD_NUMBER + " Mod has been loaded.");
                if (!isInited){
                    reloadConfigValues(false, false);
                    init();
                }
            }
            catch (Exception ex){
                Helper.dbgLog("[" + MOD_DBG_PREFIX + "}", ex, true);
            }
        }

//        private static readonly object _locker = new object(); //our lock object.


        public string Description
        {
            get
            {
                return MOD_DESCRIPTION;
            }
        }

        public string Name
        {
            get
            {
                return MOD_OFFICIAL_NAME;
            }
        }

        public void OnLoad() { }
        public void OnUnload() { }

        public void OnEnabled()
        {
            if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog("fired.");
            isEnabled = true;
            if (isInited) return;
            Helper.dbgLog(" This mod has been set enabled.");
            init();
        }

        public void OnDisabled()
        {
            if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog("fired.");
            isEnabled = false;
            un_init();
            Helper.dbgLog(MOD_OFFICIAL_NAME + " v" + VERSION_BUILD_NUMBER + " This mod has been set disabled or unloaded.");
        }

        /// <summary>
        ///     Called to either initially load, or force a reload our config file var; called by mod initialization and again at
        ///     mapload.
        /// </summary>
        /// <param name="bForceReread">Set to true to flush the old object and create a new one.</param>
        /// <param name="bNoReloadVars">
        ///     Set this to true to NOT reload the values from the new read of config file to our class
        ///     level counterpart vars
        /// </param>
        public static void reloadConfigValues(bool bForceReread, bool bNoReloadVars)
        {
            try{
                if (bForceReread){
                    config = null;
                    if (debugLOGOn & debugLOGLevel >= 1) Helper.dbgLog("Config wipe requested.");
                }
                config = Configuration.deserialize(MOD_CONFIGPATH);
                if (config == null){
                    config = new Configuration();
                    config.configVersion = Configuration.CURRENT_VERSION;
                    //reset of setting should pull defaults
                    Helper.dbgLog("Existing config was null. Created new one.");
                    Configuration.serialize(MOD_CONFIGPATH, config); //let's write it.
                }
                if (config != null && bNoReloadVars == false) //set\refresh our vars by default.
                {
                    config.configVersion = Configuration.CURRENT_VERSION;
                    debugLOGOn = config.debugLogging;
                    debugLOGLevel = Configuration.DEBUG_LOGGING_LEVEL;
                    reserveamount = config.vehicleReserveAmount;
                    isGuiEnabled = config.enableGui;
                    useAutoRefreshOption = config.useAutoRefresh;
                    autoRefreshSeconds = config.autoRefreshSeconds;
                    config.vehicleReserveAmountIndex = getOptionIndexFromValue(config.vehicleReserveAmount);
                    if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog("Vars refreshed");
                }
                if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog(string.Format("Reloaded Config data ({0}:{1} :{2})", bForceReread.ToString(), bNoReloadVars.ToString(), config.configVersion.ToString()));
            }
            catch (Exception ex){
                Helper.dbgLog("Exception while loading config values.", ex, true);
            }
        }

        internal static void init()
        {
            if (isInited == false){
                isInited = true;
                // if (Mod.config == null)
                //{ ReloadConfigValues(false, false);}

                //KH 12.2.2016 do we really this crap anymore?
                //PluginsChanged();
                //Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                //Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog("Init completed." + DateTime.Now.ToLongTimeString());
            }
        }

        internal static void un_init()
        {
            if (isInited){
                //KH 12.2.2016 do we really this crap anymore?
                //Singleton<PluginManager>.instance.eventPluginsChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                //Singleton<PluginManager>.instance.eventPluginsStateChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                isInited = false;
                if (debugLOGOn & debugLOGLevel >= 2) Helper.dbgLog("Un-Init triggered.");
            }
        }


        public static void resetStatValues()
        {
            //lock(_locker) //I lock here maybe unneccessarily. 
            //{
            timesReservedAttempted = 0u;
            timesLimitReached = 0u;
            timesFailedToCreate = 0u;
            timesFailedByReserve = 0u;
            timesReserveAttemptFailed = 0u;
            timesCvCalledTotal = 0u;
            //}
        }

        private void loggingChecked(bool en)
        {
            debugLOGOn = en;
            config.debugLogging = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }

        //called from gui screen.
        public static void updateUseAutoRefeshValue(bool en)
        {
            useAutoRefreshOption = en;
            config.useAutoRefresh = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }


        /// <summary>
        ///     Convert the returned selected index to a real value.
        /// </summary>
        /// <param name="en"></param>
        private void reservedVehiclesChanged(int en)
        {
            switch (en){
                case 0:
                    reserveamount = 8;
                    break;
                case 1:
                    reserveamount = 16;
                    break;
                case 2:
                    reserveamount = 24;
                    break;
                case 3:
                    reserveamount = 32;
                    break;
                case 4:
                    reserveamount = 48;
                    break;
                case 5:
                    reserveamount = 64;
                    break;
                case 6:
                    reserveamount = 96;
                    break;
                case 7:
                    reserveamount = 128;
                    break;
                case 8:
                    reserveamount = config.vehicleReserveAmount;
                    break;

                default:
                    reserveamount = 16;
                    break;
            }

            config.vehicleReserveAmount = reserveamount;
            config.vehicleReserveAmountIndex = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }

        /// <summary>
        ///     Use this to feed it the current reserve amount and get back and option index so our option panel
        ///     always matches especially when custom config amount; there is probably a better way to do all this by head is fried
        ///     atm.
        /// </summary>
        /// <param name="ivalue">the already set reserved amount</param>
        /// <returns>Option index; default value if all else</returns>
        private static int getOptionIndexFromValue(int ivalue)
        {
            switch (ivalue){
                case 8:
                    return 0;
                case 16:
                    return 1;
                case 24:
                    return 2;
                case 32:
                    return 3;
                case 48:
                    return 4;
                case 64:
                    return 5;
                case 96:
                    return 6;
                case 128:
                    return 7;
                default:
                    return 8; //custom set config.
            }
        }

        private void OnUseGuiToggle(bool en)
        {
            isGuiEnabled = en;
            config.enableGui = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }


        private void OnDumpStatsAtMapEnd(bool en)
        {
            config.dumpStatsOnMapEnd = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }

        private void resetStatsEveryXMin(bool en)
        {
            config.resetStatsEveryXMinutesEnabled = en;
            Configuration.serialize(MOD_CONFIGPATH, config);
        }

        private void openConfigFile()
        {
            string tmppath = Environment.CurrentDirectory + "\\" + MOD_CONFIGPATH;
            if (File.Exists(tmppath)){
                Process tmpproc = Process.Start(new ProcessStartInfo("notepad.exe", tmppath)
                { UseShellExecute = false });
                tmpproc.Close(); //this will still cause CSL to not fully release unless user closes notepad.
            }
        }

        private void eventVisibilityChanged(UIComponent component, bool value)
        {
            if (value){
                component.eventVisibilityChanged -= eventVisibilityChanged;
                component.parent.StartCoroutine(doToolTips(component));
            }
        }

        /// <summary>
        ///     Sets up tool tips. Would have been much easier if they would have let us specify the name of the components.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        private IEnumerator doToolTips(UIComponent component)
        {
            yield return new WaitForSeconds(0.500f);
            try{
                var cb = component.GetComponentsInChildren<UICheckBox>(true);
                var dd = new List<UIDropDown>();
                component.GetComponentsInChildren(true, dd);
                if (dd.Count > 0){
                    dd[0].tooltip = "Sets the number of vehicles you want to reserve.\nStart small and work you way up, rarely will you ever need more than the 8-24 range.\n Option can be changed during game.";
                    dd[0].selectedIndex = getOptionIndexFromValue(config.vehicleReserveAmount);
                }
                if (cb != null && cb.Length > 0)
                    for (int i = 0; i < cb.Length; i++){
                        switch (cb[i].text){
                            case "Enable Verbose Logging":
                                cb[i].tooltip = "Enables detailed logging for debugging purposes\n See config file for even more options, unless there are problems you probably don't want to enable this.\n Option must be set before loading game.";
                                break;
                            case "Enable CTRL+(S + V) GUI":
                                cb[i].tooltip = "Enable the availability of the in game gui\n (very handy but technically not required)\n Option must be set before loading game.";
                                break;
                            case "Dump Stats to log on map exit":
                                cb[i].tooltip = "Enable this to have the vehicle stats writen to your log file upon each map unload.\n You may configure this to use a seperate file if you like by a setting in your config file.\n Option must be set before loading game.";
                                break;
                            default:
                                cb[i].tooltip = cb[i].tabIndex + " " + cb[i].name + " - " + cb[i].cachedName;
                                if (cb[i].text.Contains("GUI: Reset Stats every so often"))
                                    cb[i].tooltip = string.Concat("Enabled this to periodically clear the vehicle statistics data.\n Option should* be set before loading game.\n",
                                        "The frequency can be changed in your config file by the ResetStatsEveryXMin entry.\n *While you can change this during a game the effect may not be seen till toggling autorefresh.");
                                break;
                        }
                    }

                if (Application.platform == RuntimePlatform.WindowsPlayer){
                    var bb = new List<UIButton>();
                    component.GetComponentsInChildren(true, bb);
                    if (bb.Count > 0) bb[0].tooltip = "On windows this will open the config file in notepad for you.\n *PLEASE CLOSE NOTEPAD* when you're done editing the conifg.\n If you don't and close the game steam will think CSL is still running till you do.";
                }
            }
            catch (Exception ex){
                /* I don't really care.*/
                Helper.dbgLog("", ex, true);
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper hp = (UIHelper)helper;
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            panel.eventVisibilityChanged += eventVisibilityChanged;

            string[] sOptions =
            { "8 - (JustTheTip)", "16 - (Default)", "24 - (Medium)", "32 - (Large)", "48 - (Very Large)", "64 - (Massive)", "96 - (Really WTF?)", "128 - (FixYourMap!)", "Custom - (SetInConfigFile)" };
            UIHelperBase group = helper.AddGroup("CSLVehicleReserve");
            group.AddDropdown("Number of Reserved Vehicles:", sOptions, getOptionIndexFromValue(config.vehicleReserveAmount), reservedVehiclesChanged);
            group.AddCheckbox("Enable CTRL+(S + V) GUI", isGuiEnabled, OnUseGuiToggle);
            group.AddCheckbox(string.Concat("GUI: Reset Stats every so often  (", config.resetStatsEveryXMin, "min)"), config.resetStatsEveryXMinutesEnabled, resetStatsEveryXMin);
            group.AddCheckbox("Dump Stats to log on map exit", config.dumpStatsOnMapEnd, OnDumpStatsAtMapEnd);
            group.AddCheckbox("Enable Verbose Logging", debugLOGOn, loggingChecked);
            group.AddSpace(20);
            if (Application.platform == RuntimePlatform.WindowsPlayer) group.AddButton("Open config file (Windows™ only)", openConfigFile);
        }

        /// <summary>
        ///     We use this guy to make sure if we just disappeared from plugin list or disabled we make sure we're not active.
        ///     Actually I don't think I really need this anymore.
        /// </summary>
        /// <param name="caller"></param>
        public static void checkForChange(string caller)
        {
            byte flags = 0;
            if (isEnabled == false & isRedirectActive){
                reverseRedirects();
                flags = 1;
            }

            if (debugLOGOn & debugLOGLevel > 0)
                Helper.dbgLog(" caller==" + caller + " flags==" + flags +
                    " IsRedirectActive == " + isRedirectActive + " ReserveAmount==" + reserveamount);
        }


        //effecive 1.6.0f4 build002 this is now dead code and never called.
        public static void pluginsChanged()
        {
            try{
                PluginManager.PluginInfo pluginInfo = (
                    from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG) //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
#else //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == MOD_WORKSHOPID
#endif
                    select p).FirstOrDefault();
/*
#if (DEBUG)
                System.Text.StringBuilder sbuilder = new System.Text.StringBuilder("[CSLToggleDLC::Pluginchanged name dump.]",1024);
                foreach (PluginManager.PluginInfo PI in Singleton<PluginManager>.instance.GetPluginsInfo())
                {
                    sbuilder.AppendLine(PI.modPath.ToString() + "  |  " + PI.name.ToString());
                }
                Helper.dbgLog(sbuilder.ToString());
#endif
*/

                if (pluginInfo == null){
                    isEnabled = false;
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TreeUnlimiter::PluginsChanged()] Can't find self. No idea if this mod is enabled.");
                    Helper.dbgLog("Plugin can't find itself in plugin-list. No idea if this mod is enabled");
                    checkForChange("PluginChanged");
                }
                else{
                    //we have this here incase maybe some other plugin wants to disable us upon thier own loading so we
                    //keep checking everytime there is a plugin change that we're still enabled. This whole idea may be overkill
                    isEnabled = pluginInfo.isEnabled;
                    if (debugLOGOn & debugLOGLevel > 2) Helper.dbgLog("PluginChangeNotify, mod is still enabled.");
                }
            }
            catch (Exception exception1){
                Helper.dbgLog("PluginsChanged() triggered exception: ", exception1, true);
            }
        }


        public static void setupRedirects()
        {
            if (isRedirectActive) return;

            try{
                redirectCalls(typeof(VehicleManager), typeof(KhVehicleManager), "CreateVehicle", "createVehicle");
                redirectCalls(typeof(VehicleManager), typeof(KhVehicleManager), "ReleaseVehicle", "releaseVehicle");
                isRedirectActive = true;

                if (debugLOGOn) Helper.dbgLog("Redirected function calls.");
            }
            catch (Exception exception1){
                Exception exception = exception1;
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, exception.ToString());
                Helper.dbgLog(" *Critical exception triggered while setting up redirects: ", exception, true);
            }
        }


        public static void reverseRedirects()
        {
            if (isRedirectActive == false) return;
            if (REDIRECT_DIC.Count == 0){
                if (debugLOGOn) Helper.dbgLog("No state entries exists to Revert");
                return;
            }
            try{
                foreach (var keypair in REDIRECT_DIC){
                    RedirectionHelper.revertRedirect(keypair.Key, keypair.Value);
                }
                REDIRECT_DIC.Clear();
                isRedirectActive = false;
                if (debugLOGOn) Helper.dbgLog("Reverted redirected function calls");
            }
            catch (Exception exception1){
                Helper.dbgLog(" ***Critical error while reverting redirected back.", exception1, true);
            }
        }



        private static void redirectCalls(Type type1, Type type2, string originalMethod, string newMethod)
        {
            BindingFlags bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo theMethod = type1.GetMethod(originalMethod, bindflags1);
            REDIRECT_DIC.Add(theMethod, RedirectionHelper.redirectCalls(theMethod, type2.GetMethod(newMethod, bindflags2), false));
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }
    }
}
