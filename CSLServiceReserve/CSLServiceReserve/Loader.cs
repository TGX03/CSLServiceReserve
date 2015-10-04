using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace CSLServiceReserve
{
	public class Loader : LoadingExtensionBase
	{
        public static UIView parentGuiView;
        public static CSLServiceReserveGUI guiPanel;
        public static bool isGuiRunning = false;
        public Loader() { }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Resetting Stats and Reloading config before mapload."); }
                //reset stats between maps.
                // *reload config values again after map load. This should not be problem atm.
                // *So long as we do this before OnLevelLoaded we should be ok;
                Mod.ResetStatValues();
                Mod.ReloadConfigValues(false, false);
            }
            catch (Exception ex)
            { Helper.dbgLog("Error:", ex, true); }
        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            try
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 0) { Helper.dbgLog("LoadMode:" + mode.ToString()); }
                if (Mod.IsEnabled == true & Mod.IsRedirectActive == false)
                {
                    // only setup redirect when in a real game
                    if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
                    {
                        if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Map or Asset modes not detcted"); }
                        Mod.SetupRedirects();  //setup the redirects.

                        if (Mod.IsGuiEnabled) { SetupGui(); } //setup gui if we're enabled.
                    }
                }
                else if (Mod.IsEnabled == false & Mod.IsRedirectActive == true)
                {
                    //This should never happen.
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Redirects were active when mod disabled?"); }
                    Mod.ReverseRedirects(); //attempt to revert redirects 

                    if (Mod.IsGuiEnabled) { RemoveGui(); }
                }
            }
            catch(Exception ex)
            { Helper.dbgLog("Error:", ex, true); }
        }


        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            try
            {

                if (Mod.IsEnabled == true | Mod.IsRedirectActive == true)
                {
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("OnLevelUnloading redirects enabled, let's revert them."); }
                    Mod.ReverseRedirects(); //attempt to revert redirects 
                }

                if (Mod.config.DumpStatsOnMapEnd) //Dump the Stats if we're told too.
                {
                    if (Mod.config.UseCustomDumpFile)
                    {
                        Helper.LogExtentedWrapper(Helper.DumpOption.Default | Helper.DumpOption.VehicleData | Helper.DumpOption.UseSeperateFile |
                           (isGuiRunning ? Helper.DumpOption.MapLoaded : Helper.DumpOption.None));
                    }
                    else
                    {
                        Helper.LogExtentedWrapper(Helper.DumpOption.Default | Helper.DumpOption.VehicleData | (isGuiRunning ? Helper.DumpOption.MapLoaded : Helper.DumpOption.None));
                    }
                }
            }
            catch (Exception ex1)
            {
                Helper.dbgLog("Error: \r\n", ex1, true);
            }


            if (Mod.IsEnabled & (Mod.IsGuiEnabled | isGuiRunning ))
            {
                RemoveGui();
            }
        }


        public override void OnReleased()
        {
            base.OnReleased();
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog ("Releasing Completed."); }
/*            if (Mod.IsEnabled == true | Mod.IsRedirectActive == true)
            {
                Mod.ReverseRedirects(); //attempt to revert redirects ca
            }
 */ 
        }

        public static void SetupGui()
        {
            //if(Mod.IsEnabled && Mod.IsGuiEnabled)
            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" Setting up Gui panel.");
            try
            {
                parentGuiView = null;
                parentGuiView = UIView.GetAView();
                if (guiPanel == null)
                {
                    guiPanel = (CSLServiceReserveGUI)parentGuiView.AddUIComponent(typeof(CSLServiceReserveGUI));
                    if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" GUI Setup.");
                    //guiPanel.Hide();
                }
                isGuiRunning = true;
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error: \r\n", ex,true);
            }

        }

        public static void RemoveGui()
        {

            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" Removing Gui.");
            try
            {
                if (guiPanel != null)
                {
                    //is this causing on exit exception problem?
                    //guiPanel.gameObject.SetActive(false);
                    //GameObject.DestroyImmediate(guiPanel.gameObject);
                    //guiPanel = null;
                    if (Mod.DEBUG_LOG_ON) Helper.dbgLog("Destroyed GUI objects.");
                }
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error: ",ex,true);
            }

            isGuiRunning = false;
            if (parentGuiView != null) { parentGuiView = null; } //toast our ref to guiview
        }

	}
}
