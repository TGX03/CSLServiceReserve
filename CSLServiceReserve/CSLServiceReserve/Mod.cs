using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CSLServiceReserve
{
     public class Mod : IUserMod
    {
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        internal const ulong MOD_WORKSHOPID = 529979180uL;
        internal const string MOD_OFFICIAL_NAME = "CSL Service Reserve";  //debug==must match folder name
        internal const string MOD_DESCRIPTION = "Allows you to reserve vehicles for critical services.";
        internal static readonly string MOD_DBG_Prefix = "CSLServiceReserve"; //same..for now.
        internal const string VERSION_BUILD_NUMBER = "1.2.1-f1 build_002";
        public static readonly string MOD_CONFIGPATH = "CSLServiceReserve_Config.xml";
        
        public static bool IsEnabled = false;           //tracks if the mod is enabled.
        public static bool IsInited = false;            //tracks if we're inited
        public static bool IsRedirectActive = false;    //tracks detouring state.
        public static bool IsGuiEnabled = false;        //tracks if the gui option is set.

        public static float AutoRefreshSeconds = 3.0f;  //why are storing these here again and not just using mod.config? //oldcode.
        public static bool UseAutoRefreshOption = false;
        public static ushort RESERVEAMOUNT = 16;    //oldcode holdover vs mod.config.

        public static ulong timesReservedAttempted = 0u;
        public static ulong timesLimitReached = 0u;
        public static ulong timesFailedToCreate = 0u;
        public static ulong timesFailedByReserve = 0u;
        public static ulong timesReserveAttemptFailed = 0u;
        public static ulong timesCV_CalledTotal = 0u;
        private static Dictionary<MethodInfo, RedirectCallsState> redirectDic = new Dictionary<MethodInfo, RedirectCallsState>();
        public static Configuration config;

//        private static readonly object _locker = new object(); //our lock object.


        public string Description
        {
            get
            {
                return Mod.MOD_DESCRIPTION;
            }
        }

        public string Name
        {
            get
            {

                return Mod.MOD_OFFICIAL_NAME;

            }
        }

        public void OnLoad()
        { }
        public void OnUnload()
        { }

        public void OnEnabled()
        {

            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            Mod.IsEnabled = true;
            if (Mod.IsInited == false)
            {
                Helper.dbgLog(" This mod has been set enabled.");
                Mod.init();
            }
        }

        public void OnDisabled()
        {
            if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            Mod.IsEnabled = false;
            un_init();
            Helper.dbgLog(Mod.MOD_OFFICIAL_NAME + " v" + VERSION_BUILD_NUMBER + " This mod has been set disabled or unloaded.");
        }

         
         
         /// <summary>
         /// Public Constructor on load we grab our config info and init();
         /// </summary>
        public Mod()
		{
            Helper.dbgLog("\r\n" + Mod.MOD_OFFICIAL_NAME + " v" + Mod.VERSION_BUILD_NUMBER + " Mod has been loaded.");
            if (!IsInited)
            { 
                ReloadConfigValues(false, false);
                init();
            }
 
        }
        
         /// <summary>
         /// Called to either initially load, or force a reload our config file var; called by mod initialization and again at mapload. 
         /// </summary>
         /// <param name="bForceReread">Set to true to flush the old object and create a new one.</param>
         /// <param name="bNoReloadVars">Set this to true to NOT reload the values from the new read of config file to our class level counterpart vars</param>
         public static void ReloadConfigValues(bool bForceReread, bool bNoReloadVars)
         {
             if (bForceReread)
             {
                 config = null;
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Config wipe requested."); }
             }
             config = Configuration.Deserialize(MOD_CONFIGPATH);
             config.ConfigVersion = Configuration.CurrentVersion;
             if (config == null)
             {
                 config = new Configuration();
                 config.ConfigVersion = Configuration.CurrentVersion;
                 //reset of setting should pull defaults
                 Helper.dbgLog("Existing config was null. Created new one.");
             }

             if (config != null && bNoReloadVars==false) //set\refresh our vars by default.
             {
                 DEBUG_LOG_ON = config.DebugLogging;
                 DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                 RESERVEAMOUNT = config.VehicleReserveAmount;
                 IsGuiEnabled = config.EnableGui;
                 UseAutoRefreshOption = config.UseAutoRefresh;
                 AutoRefreshSeconds = config.AutoRefreshSeconds;
                 config.VehicleReserveAmountIndex = GetOptionIndexFromValue(config.VehicleReserveAmount); 
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Vars refreshed"); }
             }
             if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog(string.Format("Reloaded Config data ({0}:{1} :{2})", bForceReread.ToString(), bNoReloadVars.ToString(), config.ConfigVersion.ToString())); }
         }

        internal static void init()
        {

            if (IsInited == false)
            {
                IsInited = true;
                //PluginsChanged();
                Singleton<PluginManager>.instance.eventPluginsChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                Singleton<PluginManager>.instance.eventPluginsStateChanged += new PluginManager.PluginsChangedHandler(PluginsChanged);
                if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Init completed." + DateTime.Now.ToLongTimeString()); }
            }
        }

         internal static void un_init()
         {
             if (IsInited)
             {
                 Singleton<PluginManager>.instance.eventPluginsChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                 Singleton<PluginManager>.instance.eventPluginsStateChanged -= new PluginManager.PluginsChangedHandler(PluginsChanged);
                 IsInited = false;
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Un-Init triggered."); }
             }
         }


        public static void ResetStatValues()
        {
            //lock(_locker) //I lock here maybe unneccessarily. 
            //{
                timesReservedAttempted = 0u;
                timesLimitReached = 0u;
                timesFailedToCreate = 0u;
                timesFailedByReserve = 0u;
                timesReserveAttemptFailed = 0u;
                timesCV_CalledTotal = 0u;
            //}
        }

        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

         //called from gui screen.
        public static void UpdateUseAutoRefeshValue(bool en)
        {
            UseAutoRefreshOption = en;
            config.UseAutoRefresh = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


         /// <summary>
         /// Convert the returned selected index to a real value. 
         /// </summary>
         /// <param name="en"></param>
        private void ReservedVehiclesChanged(int en)
        {
            switch(en)
            {
                case 0:
                    RESERVEAMOUNT = 8;
                    break;
                case 1:
                    RESERVEAMOUNT = 16;
                    break;
                case 2:
                    RESERVEAMOUNT = 24;
                    break;
                case 3:
                    RESERVEAMOUNT = 32;
                    break;
                case 4:
                    RESERVEAMOUNT = 48;
                    break;
                case 5:
                    RESERVEAMOUNT = 64;
                    break;
                case 6:
                    RESERVEAMOUNT = 96;
                    break;
                case 7:
                    RESERVEAMOUNT = 128;
                    break;
                case 8:
                    RESERVEAMOUNT = Mod.config.VehicleReserveAmount;
                    break;

                default:
                    RESERVEAMOUNT = 16;
                    break;
            }

            config.VehicleReserveAmount = RESERVEAMOUNT;
            config.VehicleReserveAmountIndex = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

         /// <summary>
         /// Use this to feed it the current reserve amount and get back and option index so our option panel
         /// always matches especially when custom config amount; there is probably a better way to do all this by head is fried atm.
         /// </summary>
         /// <param name="ivalue">the already set reserved amount</param>
         /// <returns>Option index; default value if all else</returns>
        private static int GetOptionIndexFromValue(int ivalue)
        {
            switch (ivalue)
            {
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
                    return 8;   //custom set config.
            }
        }

        private void OnUseGuiToggle(bool en)
        {
            IsGuiEnabled = en;
            config.EnableGui = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


        private void OnDumpStatsAtMapEnd(bool en)
        {
            config.DumpStatsOnMapEnd = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        private void ResetStatsEveryXMin(bool en)
        {
            config.ResetStatsEveryXMinutesEnabled = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        private void OpenConfigFile()
        {
            string tmppath = Environment.CurrentDirectory + "\\" + Mod.MOD_CONFIGPATH;
            if (System.IO.File.Exists(tmppath))
            {
                System.Diagnostics.Process tmpproc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", tmppath) { UseShellExecute = false });
                tmpproc.Close();  //this will still cause CSL to not fully release unless user closes notepad.
            }

        }

        private void eventVisibilityChanged(UIComponent component, bool value)
        {
            if (value)
            {
                component.eventVisibilityChanged -= eventVisibilityChanged;
                component.parent.StartCoroutine(DoToolTips(component));
            }
        }

         /// <summary>
         /// Sets up tool tips. Would have been much easier if they would have let us specify the name of the components.
         /// </summary>
         /// <param name="component"></param>
         /// <returns></returns>
        private System.Collections.IEnumerator DoToolTips(UIComponent component)
        {
            yield return new WaitForSeconds(0.500f);
            try
            {
                UICheckBox[] cb = component.GetComponentsInChildren<UICheckBox>(true);
                List<UIDropDown> dd = new List<UIDropDown>();
                component.GetComponentsInChildren<UIDropDown>(true, dd);
                if (dd.Count > 0)
                {
                    dd[0].tooltip = "Sets the number of vehicles you want to reserve.\nStart small and work you way up, rarely will you ever need more than the 8-24 range.\n Option can be changed during game.";
                    dd[0].selectedIndex = GetOptionIndexFromValue(config.VehicleReserveAmount);
                }
                if (cb != null && cb.Length > 0)
                {
                    for (int i = 0; i < (cb.Length); i++)
                    {
                        switch (cb[i].text)
                        {
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
                                cb[i].tooltip = cb[i].tabIndex.ToString() + " " + cb[i].name + " - " + cb[i].cachedName.ToString();
                                if (cb[i].text.Contains("GUI: Reset Stats every so often"))
                                {
                                    cb[i].tooltip = string.Concat("Enabled this to periodically clear the vehicle statistics data.\n Option should* be set before loading game.\n",
                                        "The frequency can be changed in your config file by the ResetStatsEveryXMin entry.\n *While you can change this during a game the effect may not be seen till toggling autorefresh.");
                                }
                                break;
                        }
                    }
                }

                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    List<UIButton> bb = new List<UIButton>();
                    component.GetComponentsInChildren<UIButton>(true, bb);
                    if ( bb.Count > 0)
                    { bb[0].tooltip = "On windows this will open the config file in notepad for you.\n *PLEASE CLOSE NOTEPAD* when you're done editing the conifg.\n If you don't and close the game steam will think CSL is still running till you do."; }

                }

            }
            catch(Exception ex)
            {
                /* I don't really care.*/
                Helper.dbgLog("", ex, true);
            }
            yield break;
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper hp = (UIHelper)helper;
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            panel.eventVisibilityChanged += eventVisibilityChanged;

            string[] sOptions = new string[]{"8 - (JustTheTip)","16 - (Default)","24 - (Medium)","32 - (Large)","48 - (Very Large)","64 - (Massive)","96 - (Really WTF?)","128 - (FixYourMap!)","Custom - (SetInConfigFile)"};
            UIHelperBase group = helper.AddGroup("CSLVehicleReserve");
            group.AddDropdown("Number of Reserved Vehicles:", sOptions, GetOptionIndexFromValue(config.VehicleReserveAmount) , ReservedVehiclesChanged);
            group.AddCheckbox("Enable CTRL+(S + V) GUI", IsGuiEnabled, OnUseGuiToggle);
            group.AddCheckbox(string.Concat("GUI: Reset Stats every so often  (", config.ResetStatsEveryXMin, "min)"), config.ResetStatsEveryXMinutesEnabled , ResetStatsEveryXMin);
            group.AddCheckbox("Dump Stats to log on map exit", config.DumpStatsOnMapEnd, OnDumpStatsAtMapEnd);
            group.AddCheckbox("Enable Verbose Logging", DEBUG_LOG_ON, LoggingChecked);
            group.AddSpace(20);
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                group.AddButton("Open config file (Windows™ only)", OpenConfigFile);
            }
            
          
        }

         /// <summary>
         /// We use this guy to make sure if we just disappeared from plugin list or disabled we make sure we're not active.
         /// Actually I don't think I really need this anymore.
         /// </summary>
         /// <param name="caller"></param>
        public static void CheckForChange(string caller)
        {
            byte flags = 0;
            if (IsEnabled == false & IsRedirectActive == true )
            {
                ReverseRedirects();
                flags = 1;
            }

            if(DEBUG_LOG_ON & DEBUG_LOG_LEVEL > 0){ 
                Helper.dbgLog(" caller==" + caller.ToString() + " flags==" + flags.ToString() + 
                " IsRedirectActive == " + IsRedirectActive.ToString() + " ReserveAmount==" + RESERVEAMOUNT.ToString());
            }

        }

        public static void PluginsChanged()
        {
            try
            {
                PluginManager.PluginInfo pluginInfo = (
                    from p in Singleton<PluginManager>.instance.GetPluginsInfo()
#if (DEBUG)  //used for local debug testing
                    where p.name.ToString() == MOD_OFFICIAL_NAME
#else   //used for steam distribution - public release.
                    where p.publishedFileID.AsUInt64 == MOD_WORKSHOPID
#endif
                    select p).FirstOrDefault<PluginManager.PluginInfo>();
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

                if (pluginInfo == null)
                {
                    Mod.IsEnabled = false;
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TreeUnlimiter::PluginsChanged()] Can't find self. No idea if this mod is enabled.");
                    Helper.dbgLog("Plugin can't find itself in plugin-list. No idea if this mod is enabled");
                    CheckForChange("PluginChanged");
                }
              else
                {
                    //we have this here incase maybe some other plugin wants to disable us upon thier own loading so we
                    //keep checking everytime there is a plugin change that we're still enabled. This whole idea may be overkill
                    Mod.IsEnabled = pluginInfo.isEnabled;

                   
                } 
            }
            catch (Exception exception1)
            {
                Debug.LogException(exception1);
                Helper.dbgLog("PluginsChanged() triggered exception: ", exception1, true);
            }
        }


        public static void SetupRedirects()
        {
            if (IsRedirectActive) { return; }

            try
            {

                RedirectCalls(typeof(VehicleManager), typeof(KHVehicleManager), "CreateVehicle");
                IsRedirectActive = true;

                if (DEBUG_LOG_ON) { Helper.dbgLog("Redirected function calls."); }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, exception.ToString());
                Helper.dbgLog(" *Critical exception triggered while setting up redirects: ", exception,true);
            }
        }


        public static void ReverseRedirects()
        {
            if (IsRedirectActive == false) { return; }
            if (redirectDic.Count == 0)
            {
                if (DEBUG_LOG_ON) { Helper.dbgLog("No state entries exists to Revert"); }
                return;
            }
            try
            {
                foreach (var keypair in redirectDic)
                {
                    RedirectionHelper.RevertRedirect(keypair.Key, keypair.Value);
                }
                redirectDic.Clear();
                IsRedirectActive = false;
                if (DEBUG_LOG_ON) { Helper.dbgLog("Reverted redirected function calls"); }
            }
            catch (Exception exception1)
            { Helper.dbgLog(" ***Critical error while reverting redirected back.", exception1, true); }
        }



        private static void RedirectCalls(Type type1, Type type2, string p)
        {
            var bindflags1 = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var bindflags2 = BindingFlags.Static | BindingFlags.NonPublic;
            var theMethod = type1.GetMethod(p, bindflags1);
            redirectDic.Add(theMethod, RedirectionHelper.RedirectCalls(theMethod, type2.GetMethod(p, bindflags2), false));
            //RedirectionHelper.RedirectCalls(type1.GetMethod(p, bindflags1), type2.GetMethod(p, bindflags2), false);
        }


    }

}
