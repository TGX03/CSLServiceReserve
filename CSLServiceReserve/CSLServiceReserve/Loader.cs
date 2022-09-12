using System;
using ColossalFramework.UI;
using ICities;
//using ColossalFramework.Steamworks;

namespace CSLServiceReserve
{
    public class Loader : LoadingExtensionBase
    {
        public static UIView parentGuiView;
        public static CslServiceReserveGUI guiPanel;
        public static bool isGuiRunning;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try{
                if (Mod.debugLOGOn) Helper.dbgLog("Resetting Stats and Reloading config before mapload.");
                //reset stats between maps.
                // *reload config values again after map load. This should not be problem atm.
                // *So long as we do this before OnLevelLoaded we should be ok;
                Mod.resetStatValues();
                Mod.reloadConfigValues(false, false);
            }
            catch (Exception ex){
                Helper.dbgLog("Error:", ex, true);
            }
        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            try{
                if (Mod.debugLOGOn && Mod.debugLOGLevel > 0) Helper.dbgLog("LoadMode:" + mode);
                if (Mod.isEnabled & Mod.isRedirectActive == false){
                    // only setup redirect when in a real game
                    if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame){
                        if (Mod.debugLOGOn) Helper.dbgLog("Map or Asset modes not detcted");
                        Mod.setupRedirects(); //setup the redirects.

                        if (Mod.isGuiEnabled) setupGui(); //setup gui if we're enabled.
                    }
                }
                else if (Mod.isEnabled == false & Mod.isRedirectActive){
                    //This should never happen.
                    if (Mod.debugLOGOn) Helper.dbgLog("Redirects were active when mod disabled?");
                    Mod.reverseRedirects(); //attempt to revert redirects 

                    if (Mod.isGuiEnabled) removeGui();
                }
            }
            catch (Exception ex){
                Helper.dbgLog("Error:", ex, true);
            }
        }


        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            try{
                if (Mod.isEnabled | Mod.isRedirectActive){
                    if (Mod.debugLOGOn) Helper.dbgLog("OnLevelUnloading redirects enabled, let's revert them.");
                    Mod.reverseRedirects(); //attempt to revert redirects 
                }

                if (Mod.config.dumpStatsOnMapEnd) //Dump the Stats if we're told too.
                {
                    if (Configuration.USE_CUSTOM_DUMP_FILE)
                        Helper.logExtentedWrapper(Helper.DumpOption.DEFAULT | Helper.DumpOption.VEHICLE_DATA | Helper.DumpOption.USE_SEPERATE_FILE |
                            (isGuiRunning ? Helper.DumpOption.MAP_LOADED : Helper.DumpOption.NONE));
                    else Helper.logExtentedWrapper(Helper.DumpOption.DEFAULT | Helper.DumpOption.VEHICLE_DATA | (isGuiRunning ? Helper.DumpOption.MAP_LOADED : Helper.DumpOption.NONE));
                }
            }
            catch (Exception ex1){
                Helper.dbgLog("Error: \r\n", ex1, true);
            }


            if (Mod.isEnabled & (Mod.isGuiEnabled | isGuiRunning)) removeGui();
        }


        public override void OnReleased()
        {
            base.OnReleased();
            if (Mod.debugLOGOn) Helper.dbgLog("Releasing Completed.");
            /*            if (Mod.IsEnabled == true | Mod.IsRedirectActive == true)
            {
                Mod.ReverseRedirects(); //attempt to revert redirects ca
            }
 */
        }

        public static void setupGui()
        {
            //if(Mod.IsEnabled && Mod.IsGuiEnabled)
            if (Mod.debugLOGOn) Helper.dbgLog(" Setting up Gui panel.");
            try{
                parentGuiView = null;
                parentGuiView = UIView.GetAView();
                if (guiPanel == null){
                    guiPanel = (CslServiceReserveGUI)parentGuiView.AddUIComponent(typeof(CslServiceReserveGUI));
                    if (Mod.debugLOGOn) Helper.dbgLog(" GUI Setup.");
                    //guiPanel.Hide();
                }
                isGuiRunning = true;
            }
            catch (Exception ex){
                Helper.dbgLog("Error: \r\n", ex, true);
            }
        }

        public static void removeGui()
        {
            if (Mod.debugLOGOn) Helper.dbgLog(" Removing Gui.");
            try{
                if (guiPanel != null)
                    //is this causing on exit exception problem?
                    //guiPanel.gameObject.SetActive(false);
                    //GameObject.DestroyImmediate(guiPanel.gameObject);
                    //guiPanel = null;
                    if (Mod.debugLOGOn)
                        Helper.dbgLog("Destroyed GUI objects.");
            }
            catch (Exception ex){
                Helper.dbgLog("Error: ", ex, true);
            }

            isGuiRunning = false;
            if (parentGuiView != null) parentGuiView = null; //toast our ref to guiview
        }
    }
}
