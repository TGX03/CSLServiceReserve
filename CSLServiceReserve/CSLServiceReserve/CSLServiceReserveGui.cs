using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
//using System.Diagnostics;
//using CSLServiceReserve.Configuration;
namespace CSLServiceReserve
{
    public class CSLServiceReserveGUI : UIPanel
    {
        public static readonly string cacheName = "CSLServiceReserveGUI";
        public static CSLServiceReserveGUI instance;
        private const string DTMilli = "MM/dd/yyyy hh:mm:ss.fff tt";
        private static readonly float WIDTH = 600f;
        private static readonly float HEIGHT = 400f;
        private static readonly float HEADER = 40f;
        private static readonly float SPACING = 10f;
        private static readonly float SPACING22 = 22f;
        private static bool isRefreshing = false;  //Used basically as a safety lock.
        private bool CoResetDataEnabled = false;   //These tell us if certain coroutine is running. 
        private bool CoVehcRefreshEnabled = false;
        private bool CoDisplayRefreshEnabled = false;
        private DateTime statsResetTime = DateTime.Now.AddMinutes((double)Mod.config.ResetStatsEveryXMin);

        //real lock used during clearing of data probably don't need this, but what if CreateVehicle() and our Reset are called from different threads?
        static readonly object _mylock = new object();

        UIDragHandle m_DragHandler; //lets us move the panel around.
        UIButton m_closeButton; //our close button
        UILabel m_title;
        UIButton m_refresh;  //our manual refresh button
        UILabel m_AutoRefreshChkboxText; //label that get assigned to the AutoRefreshCheckbox.
        UICheckBox m_AutoRefreshCheckbox; //Our AutoRefresh checkbox

        UILabel m_CurrentNumOfVehText;
        UILabel m_CurrentNumOfVehValues;
        UILabel m_NewMaxNumberofVecText;
        UILabel m_NewMaxNumberofVecValue;
        UILabel m_ReserveAttemptsText;
        UILabel m_ReserveAttemptsValue;
        UILabel m_ReserveAttemptsFailedText;
        UILabel m_ReserveAttemptsFailedValue;
        UILabel m_FailedToCreateText;
        UILabel m_FailedToCreateValue;
        UILabel m_FailedDueToReserveText;
        UILabel m_FailedDueToReserveValue;
        UILabel m_TimesReserveExceededText;
        UILabel m_TimesReserveExceededValue;
        UILabel m_TimesCalledTotalText;
        UILabel m_TimesCalledTotalValue;
        UILabel m_AdditionalText1Text;
        UIButton m_LogdataButton;
        UIButton m_ClearDataButton;


        /// <summary>
        /// Function gets called by unity on every single frame.
        /// We just check for our key combo, maybe there is a better way to register this with the game?
        /// </summary>
        public override void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) & Input.GetKeyDown(KeyCode.V))
            {
                this.ProcessVisibility();
            }
            base.Update();
        }


        /// <summary>
        /// Gets called upon the base UI component's creation. Basically it's the constructor...but not really.
        /// </summary>
        public override void Start()
        {
            base.Start();
            CSLServiceReserveGUI.instance = this;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog(string.Concat("Attempting to create our display panel.  ",DateTime.Now.ToString(DTMilli).ToString()));
            this.size = new Vector2(WIDTH, HEIGHT);
            this.backgroundSprite = "MenuPanel";
            this.canFocus = true;
            this.isInteractive = true;
            this.BringToFront();
            this.relativePosition = new Vector3((Loader.parentGuiView.fixedWidth / 2) - 200, (Loader.parentGuiView.fixedHeight / 2) - 200);
            this.opacity = ((Mod.config.GuiOpacity > 0.50f & Mod.config.GuiOpacity < 1.0) ? Mod.config.GuiOpacity : 1.0f);
            this.cachedName = cacheName;

            //DragHandler
            m_DragHandler = this.AddUIComponent<UIDragHandle>();
            m_DragHandler.target = this;
            //Title UILabel
            m_title = this.AddUIComponent<UILabel>();
            m_title.text = "Service Vehicle Reserve Data      "; //spaces on purpose
            m_title.relativePosition = new Vector3(WIDTH / 2 - (m_title.width / 2) - 20f, (HEADER / 2) - (m_title.height / 2));
            m_title.textAlignment = UIHorizontalAlignment.Center;
            //Close Button UIButton
            m_closeButton = this.AddUIComponent<UIButton>();
            m_closeButton.normalBgSprite = "buttonclose";
            m_closeButton.hoveredBgSprite = "buttonclosehover";
            m_closeButton.pressedBgSprite = "buttonclosepressed";
            m_closeButton.relativePosition = new Vector3(WIDTH - 35, 5, 10);
            m_closeButton.eventClick += (component, eventParam) =>
            {
                this.Hide();
            };
            
            CreateTextLabels();
            CreateDataLabels();
            this.Hide();
            DoOnStartup();
            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(string.Concat("Display panel created. ",DateTime.Now.ToString(DTMilli).ToString()));
        }

        /// <summary>
        /// Our initialize stuff; call after panel\form setup.
        /// </summary>
        private void DoOnStartup()
        {
            RefreshDisplayData(); //fill'em up manually initially.
            if (Mod.config.ResetStatsEveryXMinutesEnabled)
            {
                this.StartCoroutine(CheckForStatsReset());
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("CheckForStatsReset coroutine started."); }
            }
            if (m_AutoRefreshCheckbox.isChecked)
            {
                this.StartCoroutine(RefreshDisplayDataWrapper());
                this.StartCoroutine(RefreshVehcCount());
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("RefreshDisplayDataWrapper & RefreshVehcCount coroutines started."); }
            }
        }


        /// <summary>
        /// Create and setup up default text and stuff for our Text UILabels;
        /// </summary>
        private void CreateTextLabels() 
        {
            m_CurrentNumOfVehText = this.AddUIComponent<UILabel>();
            m_CurrentNumOfVehText.text = string.Concat("Total number of vehicles in use: (", (Singleton<VehicleManager>.instance.m_vehicles.m_size - 1).ToString(), " max) ");
            m_CurrentNumOfVehText.tooltip = "The present number of vehicles the game was last actively using.\n #ResInUse: is the number of reserved vehicles in use during last update.";
            m_CurrentNumOfVehText.relativePosition = new Vector3(SPACING, 50f);
            //m_NumOfVehText.minimumSize = new Vector2(100, 20);
            //m_NumOfVehText.maximumSize = new Vector2(this.width - SPACING * 2, 30);
            m_CurrentNumOfVehText.autoSize = true;

            m_NewMaxNumberofVecText = this.AddUIComponent<UILabel>();
            m_NewMaxNumberofVecText.relativePosition = new Vector3(SPACING, 50f + (SPACING * 2));
            m_NewMaxNumberofVecText.text = "New limit for non-service vehicles: ";
            m_NewMaxNumberofVecText.tooltip = "The max limit value the mod now uses for non-critical vehicle requests \n #Reserved is the number you selected to reserve for critical services.\n Wonding why max is 16383 and not 16384? It is because the game does not use entry #0";
            m_NewMaxNumberofVecText.autoSize = true;

            m_ReserveAttemptsText = this.AddUIComponent<UILabel>();
            m_ReserveAttemptsText.relativePosition = new Vector3(SPACING, (m_NewMaxNumberofVecText.relativePosition.y + SPACING22));
            m_ReserveAttemptsText.text = "# Attempts to use reserved vehicles: ";
            m_ReserveAttemptsText.tooltip = "The number of times a request was processed for a critical service that was allowed to use a reserved vehicle.";
            m_ReserveAttemptsText.autoSize = true;

            m_ReserveAttemptsFailedText = this.AddUIComponent<UILabel>();
            m_ReserveAttemptsFailedText.relativePosition = new Vector3(SPACING, (m_ReserveAttemptsText.relativePosition.y + SPACING22));
            m_ReserveAttemptsFailedText.text = "# Attempts to use reserved vehicles (but failed): ";
            m_ReserveAttemptsFailedText.tooltip = "The number of times a request was processed for a critical service that was allowed to use a reserved vehicle,\n but still failed to be created.  A high number here(say 20+ over just a few minutes)\n might indicated the need for a higher reservered vehicle amount.";
            m_ReserveAttemptsFailedText.autoSize = true;

            m_TimesReserveExceededText = this.AddUIComponent<UILabel>();
            m_TimesReserveExceededText.relativePosition = new Vector3(SPACING, (m_ReserveAttemptsFailedText.relativePosition.y + SPACING22));
            m_TimesReserveExceededText.text = "# Times the reserved limits were exceeded: ";
            m_TimesReserveExceededText.tooltip = "The number of requests where it was noticed that we had already used up all our reserved vehicles. \n This, if more than a few is an indicator that you may need to increase the reserves to the next level.\n This value should generally match the #Attempts to use reserve failures.";
            m_TimesReserveExceededText.autoSize = true;

            m_FailedDueToReserveText = this.AddUIComponent<UILabel>();
            m_FailedDueToReserveText.relativePosition = new Vector3(SPACING, (m_TimesReserveExceededText.relativePosition.y + SPACING22));
            m_FailedDueToReserveText.text = "# Times game failed to create a non-reserved vehicle: ";
            m_FailedDueToReserveText.tooltip = "This is the raw number of requests where a vehicle was asked for but was denied because of hitting the reserve limiter.\n This is just provided as a statistic.";
            m_FailedDueToReserveText.autoSize = true;

            m_FailedToCreateText = this.AddUIComponent<UILabel>();
            m_FailedToCreateText.relativePosition = new Vector3(SPACING, (m_FailedDueToReserveText.relativePosition.y + SPACING22));
            m_FailedToCreateText.text = "# Total times game failed to create any vehicle: ";
            m_FailedToCreateText.tooltip = "This is the raw number of requests where no matter the reason a vehicle was asked for \n but failed to be created because none were available, or some other odd error.";
            m_FailedToCreateText.autoSize = true;

            m_TimesCalledTotalText = this.AddUIComponent<UILabel>();
            m_TimesCalledTotalText.relativePosition = new Vector3(SPACING, (m_FailedToCreateText.relativePosition.y + SPACING22));
            m_TimesCalledTotalText.text = "# Total number of times CreateVehicle() was called: ";
            m_TimesCalledTotalText.tooltip = "This is the raw total of all calls to CreateVehicle no matter if they succeeded or not.";
            m_TimesCalledTotalText.autoSize = true;


            m_AutoRefreshCheckbox = this.AddUIComponent<UICheckBox>();
            m_AutoRefreshCheckbox.relativePosition = new Vector3((SPACING), (m_TimesCalledTotalText.relativePosition.y + 35f));

            m_AutoRefreshChkboxText = this.AddUIComponent<UILabel>();
            m_AutoRefreshChkboxText.relativePosition = new Vector3(m_AutoRefreshCheckbox.relativePosition.x + m_AutoRefreshCheckbox.width + (SPACING * 3), (m_AutoRefreshCheckbox.relativePosition.y) + 5f);
            //m_AutoRefreshChkboxText.text = "Use Auto Refresh";
            m_AutoRefreshChkboxText.tooltip = "Enables these stats to update every few seconds \n Default is 5 seconds.";
            //m_AutoRefreshChkboxText.autoSize = true;

            m_AutoRefreshCheckbox.height = 16;
            m_AutoRefreshCheckbox.width = 16;
            m_AutoRefreshCheckbox.label = m_AutoRefreshChkboxText;
            m_AutoRefreshCheckbox.text = string.Concat("Use AutoRefresh  (", Mod.AutoRefreshSeconds.ToString("f1"), " sec)");

            UISprite uncheckSprite = m_AutoRefreshCheckbox.AddUIComponent<UISprite>();
            uncheckSprite.height = 20;
            uncheckSprite.width = 20;
            uncheckSprite.relativePosition = new Vector3(0, 0);
            uncheckSprite.spriteName = "check-unchecked";
            uncheckSprite.isVisible = true;

            UISprite checkSprite = m_AutoRefreshCheckbox.AddUIComponent<UISprite>();
            checkSprite.height = 20;
            checkSprite.width = 20;
            checkSprite.relativePosition = new Vector3(0, 0);
            checkSprite.spriteName = "check-checked";

            m_AutoRefreshCheckbox.checkedBoxObject = checkSprite;
            m_AutoRefreshCheckbox.isChecked = Mod.UseAutoRefreshOption;
            m_AutoRefreshCheckbox.isEnabled = true;
            m_AutoRefreshCheckbox.isVisible = true;
            m_AutoRefreshCheckbox.canFocus = true;
            m_AutoRefreshCheckbox.isInteractive = true;
            //can't use this? m_AutoRefreshCheckbox.autoSize = true;  
            m_AutoRefreshCheckbox.eventCheckChanged += (component, eventParam) => { AutoRefreshCheckbox_OnCheckChanged(component, eventParam); };
            //AutoRefreshCheckbox_OnCheckChanged;

            m_AdditionalText1Text = this.AddUIComponent<UILabel>();
            m_AdditionalText1Text.relativePosition = new Vector3(m_AutoRefreshCheckbox.relativePosition.x + m_AutoRefreshCheckbox.width + SPACING, (m_AutoRefreshCheckbox.relativePosition.y) + 40f);
            m_AdditionalText1Text.width = 300f;
            m_AdditionalText1Text.height = 50f;
            //m_AdditionalText1Text.autoSize = true;
            //m_AdditionalText1Text.wordWrap = true;
            m_AdditionalText1Text.text = "* Use CTRL + S + V (S&V at same time) to show again. \n  More options available in CSLServiceReserve_Config.xml";

            m_refresh = this.AddUIComponent<UIButton>();
            m_refresh.size = new Vector2(120, 24);
            m_refresh.text = "Manual Refresh";
            m_refresh.tooltip = "Use to manually refresh the data. \n (use when auto enabled is off)";
            m_refresh.textScale = 0.875f;
            m_refresh.normalBgSprite = "ButtonMenu";
            m_refresh.hoveredBgSprite = "ButtonMenuHovered";
            m_refresh.pressedBgSprite = "ButtonMenuPressed";
            m_refresh.disabledBgSprite = "ButtonMenuDisabled";
            //m_refresh.relativePosition = m_closeButton.relativePosition + new Vector3(-60 - SPACING, 6f);
            m_refresh.relativePosition = m_AutoRefreshChkboxText.relativePosition + new Vector3((m_AutoRefreshChkboxText.width + SPACING * 2), -5f);
            m_refresh.eventClick += (component, eventParam) => { RefreshDisplayData(); };

            m_LogdataButton = this.AddUIComponent<UIButton>();
            m_LogdataButton.size = new Vector2(80, 24);
            m_LogdataButton.text = "Log Data";
            m_LogdataButton.tooltip = "Use to Log the current data to log file. \n (Saved to CSL standard output_log.txt log file)";
            m_LogdataButton.textScale = 0.875f;
            m_LogdataButton.normalBgSprite = "ButtonMenu";
            m_LogdataButton.hoveredBgSprite = "ButtonMenuHovered";
            m_LogdataButton.pressedBgSprite = "ButtonMenuPressed";
            m_LogdataButton.disabledBgSprite = "ButtonMenuDisabled";
            m_LogdataButton.relativePosition = m_refresh.relativePosition + new Vector3((m_refresh.width + SPACING * 3), 0f);
            m_LogdataButton.eventClick += (component, eventParam) => { ProcessOnLogButton(); };

            m_ClearDataButton = this.AddUIComponent<UIButton>();
            m_ClearDataButton.size = new Vector2(50, 24);
            m_ClearDataButton.text = "Clear";
            m_ClearDataButton.tooltip = "Use to manually clear and reset the above data values. \n Usefull to watch for changes over specific periods of time \n This is in addition to the ResetLogEveryFewMin option.";
            m_ClearDataButton.textScale = 0.875f;
            m_ClearDataButton.normalBgSprite = "ButtonMenu";
            m_ClearDataButton.hoveredBgSprite = "ButtonMenuHovered";
            m_ClearDataButton.pressedBgSprite = "ButtonMenuPressed";
            m_ClearDataButton.disabledBgSprite = "ButtonMenuDisabled";
            m_ClearDataButton.relativePosition = m_LogdataButton.relativePosition + new Vector3((m_LogdataButton.width + SPACING * 3), 0f);
            m_ClearDataButton.eventClick += (component, eventParam) => { ProcessOnClearButton(); };
        }


        /// <summary>
        /// Event handler for clicking on AutoRefreshbutton.
        /// </summary>
        /// <param name="UIComp">The triggering UIComponent</param>
        /// <param name="bValue">The Value True|False (Checked|Unchecked)</param>

        private void AutoRefreshCheckbox_OnCheckChanged(UIComponent UIComp, bool bValue)
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("AutoRefreshButton was toggled to: " + bValue.ToString()); }
            Mod.UpdateUseAutoRefeshValue(bValue);
            if (bValue == true)
            {
                byte bflag = 0;
                if (!CoVehcRefreshEnabled) { this.StartCoroutine(RefreshVehcCount()); bflag += 1; }
                if (!CoDisplayRefreshEnabled) { this.StartCoroutine(RefreshDisplayDataWrapper()); bflag += 2; }
                if (!CoResetDataEnabled) { this.StartCoroutine(CheckForStatsReset()); bflag += 4; }
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Starting all coroutines that were not already started " + 
                    bValue.ToString() + " bflag=" + bflag.ToString()); }
            }
            else
            {
                //upon disabling auto refresh we *also* disable the the stat data refresher
                //I think this is logical, as people might want to start at data, plus can always manually refresh.
                this.StopAllCoroutines();
                ResetAllCoroutineState(false); //cleanup
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Stopping all coroutines: " + bValue.ToString()); }
            }
            return;
        }

        /// <summary>
        /// Sadly needed to reset state of Coroutines after forced stop.
        /// </summary>
        /// <param name="bStatus">True|False</param>
        private void ResetAllCoroutineState(bool bStatus)
        {
            CoVehcRefreshEnabled = bStatus;
            CoResetDataEnabled = bStatus;
            CoDisplayRefreshEnabled = bStatus;
        }

        /// <summary>
        /// Function to check if we need to reset the stats, ment to check only every so often..like once a minute
        /// or modify it 
        /// </summary>
        private IEnumerator CheckForStatsReset()
        {
            if (CoResetDataEnabled == true)
            {
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog(" StatResetChecker* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 copy at a time.
            while (Mod.config.ResetStatsEveryXMinutesEnabled)
            {
                CoResetDataEnabled = true;
                DateTime tmNow = DateTime.Now;
                if (DateTime.Compare(tmNow, statsResetTime) >= 0)
                {
                    if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 2) Helper.dbgLog(string.Concat("Stats reseting. current time is ",
                        DateTime.Now.ToString(DTMilli), " adding ", Mod.config.ResetStatsEveryXMin.ToString(), "min."));
                    statsResetTime = tmNow.AddMinutes(((double)(Mod.config.ResetStatsEveryXMin)));
                    Mod.ResetStatValues();
                    if (Mod.DEBUG_LOG_ON) Helper.dbgLog(string.Concat("Stats reset. Next stats time reset to ", statsResetTime.ToString(DTMilli)));
                }

                // We hard code this to only check once a minute, keeps things simple,
                // do we really need too much accuracy on the reset?  If you change account for that in AutoRefresh toggle
                // cause you'll need to adjust the next time according to first run after some delay so they get in sync again
                // this just avoids the problem ...originaly was doing it the nice and complex way...but really... why???
                yield return new WaitForSeconds(60.0f);
            }
            CoResetDataEnabled = false;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("StatsResetChecker coroutine exited due to ResetStatsEveryXMinutesEnabled = false.");
            yield break;
        }



        /// <summary>
        /// The CoRoutine that gets spun up to update the displayed Vehicle count, it runs till visibility or autorrefresh
        /// selection changes and generally will run several times per second, or whatever the user sets in the config.
        /// We run this fast because otherwise people may not notice the value actually changing or going over new max limit.
        /// </summary>
        /// <returns></returns>
        private IEnumerator RefreshVehcCount()
        {
            //we don't need to lookup\init these two up every x milliseconds.
            float tmpfloattime = Mod.config.RefreshVehicleCounterSeconds; 
            int tmpResInUse = 0;
            int vehcCount = 0;
            VehicleManager vMgr = Singleton<VehicleManager>.instance;
            if (CoVehcRefreshEnabled == true)
            {
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleCount* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 active?

            while (this.isVisible & this.m_AutoRefreshCheckbox.isChecked)
            {
                CoVehcRefreshEnabled  = true;
                vehcCount = (int)vMgr.m_vehicleCount;
                tmpResInUse = (int)vehcCount - ((int)(vMgr.m_vehicles.m_size - 1) - (int)Mod.RESERVEAMOUNT);
                m_CurrentNumOfVehValues.text = string.Concat(vehcCount.ToString(), "    ResInUse: ", tmpResInUse < 0 ? "0" : tmpResInUse.ToString());
                yield return new WaitForSeconds(tmpfloattime);
            }
            CoVehcRefreshEnabled = false;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleCount coroutine exited due to already refreshing or visiblity change.");
            yield break;
        }



        /// <summary>
        /// Primary coroutine function to update the more static (seconds) information display.
        /// as there really is no need to update this more then once per second.
        /// </summary>
        private IEnumerator RefreshDisplayDataWrapper() 
        {
            if (CoDisplayRefreshEnabled == true)
            {
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleData* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 active. 
            while (isRefreshing == false && this.isVisible == true && m_AutoRefreshCheckbox.isChecked)
            {
                CoDisplayRefreshEnabled  = true;
                RefreshDisplayData();
                yield return new WaitForSeconds(Mod.config.AutoRefreshSeconds);
            }
            CoDisplayRefreshEnabled = false;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleData coroutine exited due to AutoRefresh disabled, visiblity change, or already refreshing.");
            yield break;
        }


        /// <summary>
        /// Function refreshes the display data. mostly called from coroutine timer.
        /// </summary>
        private void RefreshDisplayData()
        {
            isRefreshing = true; //safety lock so we never get more then one of these, probably don't need after co-routine refactor.
            try
            {
               // m_title.text = string.Concat("Service Vehicle Reserve Data  (c:" , mcount.ToString(),")");
                VehicleManager vMgr = Singleton<VehicleManager>.instance;
                int tmpNewMax = ((int)(vMgr.m_vehicles.m_size - 1) - (int)Mod.RESERVEAMOUNT);
                int tmpResInUse = ( (int)vMgr.m_vehicleCount - tmpNewMax ) ;
                m_CurrentNumOfVehValues.text = string.Concat(vMgr.m_vehicleCount.ToString(),"    ResInUse: ", tmpResInUse < 0 ? "0" : tmpResInUse.ToString());
                m_NewMaxNumberofVecValue.text = string.Concat(tmpNewMax.ToString(),"    #Reserved: ", Mod.RESERVEAMOUNT.ToString());
                m_ReserveAttemptsValue.text = Mod.timesReservedAttempted.ToString();
                m_ReserveAttemptsFailedValue.text = Mod.timesReserveAttemptFailed.ToString();
                m_TimesReserveExceededValue.text = Mod.timesLimitReached.ToString();
                m_FailedDueToReserveValue.text = Mod.timesFailedByReserve.ToString();
                m_FailedToCreateValue.text = Mod.timesFailedToCreate.ToString();
                double tmpval;
                if (Mod.timesCV_CalledTotal > 0)
                { tmpval = ((double)Mod.timesFailedToCreate / (double)Mod.timesCV_CalledTotal) * 100.0d; }
                else { tmpval = 0.0d; }
                m_TimesCalledTotalValue.text = string.Format("{0}  [{1} fail %]", Mod.timesCV_CalledTotal.ToString(), tmpval.ToString("F02"));

                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 3) Helper.dbgLog("Refreshing vehicle data completed. " + DateTime.Now.ToString(DTMilli));
            }
            catch (Exception ex)
            {
                isRefreshing = false;
                Helper.dbgLog("ERROR during RefreshDisplayData. ",ex,true);
            }
            isRefreshing = false;

        }


        /// <summary>
        /// Creates all our UILabels that store data that changes\gets refreshed.
        /// </summary>
        private void CreateDataLabels() 
        {
            VehicleManager vMgr = Singleton<VehicleManager>.instance;
            m_CurrentNumOfVehValues = this.AddUIComponent<UILabel>();
            m_CurrentNumOfVehValues.text = string.Concat(vMgr.m_vehicleCount.ToString(),"    #ResInUse: 0");
            m_CurrentNumOfVehValues.relativePosition = new Vector3(m_CurrentNumOfVehText.relativePosition.x + m_CurrentNumOfVehText.width  + SPACING, m_CurrentNumOfVehText.relativePosition.y);
            m_CurrentNumOfVehValues.autoSize = true;
            m_CurrentNumOfVehValues.tooltip = "The max # is off by 1 because entry 0 for a live vehicle in the game so 16384 is really 16383 usable.\n #ResInUse: is the number of reserved vehicles in use, if things are well number should rarely hit your reserved amount";

            m_NewMaxNumberofVecValue= this.AddUIComponent<UILabel>();
            m_NewMaxNumberofVecValue.relativePosition = new Vector3(m_NewMaxNumberofVecText.relativePosition.x + m_NewMaxNumberofVecText.width  + SPACING, m_NewMaxNumberofVecText.relativePosition.y);
            m_NewMaxNumberofVecValue.autoSize = true;
            m_NewMaxNumberofVecValue.text = string.Concat(((vMgr.m_vehicles.m_size - 1)- Mod.RESERVEAMOUNT).ToString(),
                "    #Reserved: ", Mod.RESERVEAMOUNT.ToString());

            m_ReserveAttemptsValue = this.AddUIComponent<UILabel>();
            m_ReserveAttemptsValue.relativePosition = new Vector3(m_ReserveAttemptsText.relativePosition.x + m_ReserveAttemptsText.width + SPACING, m_ReserveAttemptsText.relativePosition.y);
            m_ReserveAttemptsValue.autoSize = true;
            m_ReserveAttemptsValue.text = Mod.timesReservedAttempted .ToString();

            m_ReserveAttemptsFailedValue = this.AddUIComponent<UILabel>();
            m_ReserveAttemptsFailedValue.relativePosition = new Vector3(m_ReserveAttemptsFailedText.relativePosition.x + m_ReserveAttemptsFailedText.width + SPACING, m_ReserveAttemptsFailedText.relativePosition.y);
            m_ReserveAttemptsFailedValue.autoSize = true;
            m_ReserveAttemptsFailedValue.text = Mod.timesReserveAttemptFailed.ToString();

            m_TimesReserveExceededValue = this.AddUIComponent<UILabel>();
            m_TimesReserveExceededValue.relativePosition = new Vector3(m_TimesReserveExceededText.relativePosition.x + m_TimesReserveExceededText.width + SPACING, m_TimesReserveExceededText.relativePosition.y);
            m_TimesReserveExceededValue.autoSize = true;
            m_TimesReserveExceededValue.text = Mod.timesLimitReached.ToString();

            m_FailedDueToReserveValue = this.AddUIComponent<UILabel>();
            m_FailedDueToReserveValue.relativePosition = new Vector3(m_FailedDueToReserveText.relativePosition.x + m_FailedDueToReserveText.width + SPACING, m_FailedDueToReserveText.relativePosition.y);
            m_FailedDueToReserveValue.autoSize = true;
            m_FailedDueToReserveValue.text = Mod.timesFailedByReserve.ToString();

            m_FailedToCreateValue = this.AddUIComponent<UILabel>();
            m_FailedToCreateValue.relativePosition = new Vector3(m_FailedToCreateText.relativePosition.x + m_FailedToCreateText.width + SPACING, m_FailedToCreateText.relativePosition.y);
            m_FailedToCreateValue.autoSize = true;
            m_FailedToCreateValue.text = Mod.timesFailedToCreate.ToString();

            m_TimesCalledTotalValue  = this.AddUIComponent<UILabel>();
            m_TimesCalledTotalValue.relativePosition = new Vector3(m_TimesCalledTotalText.relativePosition.x + m_TimesCalledTotalText.width + SPACING, m_TimesCalledTotalText.relativePosition.y);
            m_TimesCalledTotalValue.autoSize = true;
            m_TimesCalledTotalValue.text = Mod.timesCV_CalledTotal.ToString();

        }

        /// <summary>
        /// Handle action for Hide\Show events.
        /// </summary>
        private void ProcessVisibility()
        {
            if (!this.isVisible)
            {
                this.Show();
                if (!CoVehcRefreshEnabled) { this.StartCoroutine(RefreshVehcCount()); }
                if (!CoDisplayRefreshEnabled) { this.StartCoroutine(RefreshDisplayDataWrapper()); }
                //we do not touch the Resetting of StatsData; that's left to autorefresh on\off only atm.
            }
            else
            {
                this.Hide();
                //we don't have to stop the two above coroutines, 
                //should do that themselves via their own visibility checks.
            }
        
        }

        /// <summary>
        /// Handles the Clear button press event
        /// </summary>
        private void ProcessOnClearButton()
        {
            Mod.ResetStatValues();
            RefreshDisplayData();
        }


        /// <summary>
        /// Handles action for pressing Log Data Button
        /// </summary>
        private void ProcessOnLogButton()
        {
            if (Mod.config.UseCustomDumpFile)
            { Helper.LogExtentedWrapper((Helper.DumpOption.All) ); }
            else
            {
                Helper.LogExtentedWrapper(Helper.DumpOption.All ^ Helper.DumpOption.UseSeperateFile);
            }
        }

        /// <summary>
        /// Returns current private values from gui to caller.
        /// </summary>
        /// <returns></returns>
        public static  Helper.ExternalData GetInternalData()
        {
                Helper.ExternalData tmpobj = new Helper.ExternalData();
                tmpobj.CoVechRefreshEnabled = instance.CoVehcRefreshEnabled;
                tmpobj.CoDataRefreshEnabled = instance.CoResetDataEnabled;
                tmpobj.CoDisplayRefreshEnabled = instance.CoDisplayRefreshEnabled;
                tmpobj.statsResetTime = instance.statsResetTime;
                tmpobj.cachedname = instance.cachedName.ToString();
                tmpobj.tag = instance.tag.ToString();
                tmpobj.name = instance.name.ToString();
                bool.TryParse(instance.m_IsVisible.ToString(), out tmpobj.isVisable);
                bool.TryParse(instance.m_AutoRefreshChkboxText.isEnabled.ToString(), out tmpobj.isAutoRefreshActive);
                if(Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 3) Helper.dbgLog("GetInternalData created and returning.");
                return tmpobj;
        }
        

    }
}
