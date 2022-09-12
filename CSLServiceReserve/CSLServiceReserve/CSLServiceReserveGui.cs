using System;
using System.Collections;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
//using System.Diagnostics;
//using CSLServiceReserve.Configuration;
namespace CSLServiceReserve
{
    public class CslServiceReserveGUI : UIPanel
    {
        private const string DT_MILLI = "MM/dd/yyyy hh:mm:ss.fff tt";
        private const string CACHE_NAME = "CSLServiceReserveGUI";
        private static CslServiceReserveGUI Instance;
        private const float WIDTH = 600f;
        private const float HEIGHT = 400f;
        private const float HEADER = 40f;
        private const float SPACING = 10f;
        private const float SPACING22 = 22f;
        private static bool IsRefreshing; //Used basically as a safety lock.

        //real lock used during clearing of data probably don't need this, but what if CreateVehicle() and our Reset are called from different threads?
        private bool coDisplayRefreshEnabled;
        private bool coResetDataEnabled; //These tell us if certain coroutine is running. 
        private bool coVehcRefreshEnabled;
        private UILabel mAdditionalText1Text;
        private UICheckBox mAutoRefreshCheckbox; //Our AutoRefresh checkbox
        private UILabel mAutoRefreshChkboxText;  //label that get assigned to the AutoRefreshCheckbox.
        private UIButton mClearDataButton;
        private UIButton mCloseButton; //our close button

        private UILabel mCurrentNumOfVehText;
        private UILabel mCurrentNumOfVehValues;

        private UIDragHandle mDragHandler; //lets us move the panel around.
        private UILabel mFailedDueToReserveText;
        private UILabel mFailedDueToReserveValue;
        private UILabel mFailedToCreateText;
        private UILabel mFailedToCreateValue;
        private UIButton mLogdataButton;
        private UILabel mNewMaxNumberofVecText;
        private UILabel mNewMaxNumberofVecValue;
        private UIButton mRefresh; //our manual refresh button
        private UILabel mReserveAttemptsFailedText;
        private UILabel mReserveAttemptsFailedValue;
        private UILabel mReserveAttemptsText;
        private UILabel mReserveAttemptsValue;
        private UILabel mTimesCalledTotalText;
        private UILabel mTimesCalledTotalValue;
        private UILabel mTimesReserveExceededText;
        private UILabel mTimesReserveExceededValue;
        private UILabel mTitle;
        private DateTime statsResetTime = DateTime.Now.AddMinutes(Mod.config.resetStatsEveryXMin);


        /// <summary>
        ///     Gets called upon the base UI component's creation. Basically it's the constructor...but not really.
        /// </summary>
        public override void Start()
        {
            base.Start();
            Instance = this;
            if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog(string.Concat("Attempting to create our display panel.  ", DateTime.Now.ToString(DT_MILLI)));
            size = new Vector2(WIDTH, HEIGHT);
            backgroundSprite = "MenuPanel";
            canFocus = true;
            isInteractive = true;
            BringToFront();
            relativePosition = new Vector3(Loader.parentGuiView.fixedWidth / 2 - 200, Loader.parentGuiView.fixedHeight / 2 - 200);
            opacity = Mod.config.guiOpacity > 0.50f & Mod.config.guiOpacity < 1.0 ? Mod.config.guiOpacity : 1.0f;
            cachedName = CACHE_NAME;

            //DragHandler
            mDragHandler = AddUIComponent<UIDragHandle>();
            mDragHandler.target = this;
            //Title UILabel
            mTitle = AddUIComponent<UILabel>();
            mTitle.text = "Service Vehicle Reserve Data      "; //spaces on purpose
            mTitle.relativePosition = new Vector3(WIDTH / 2 - mTitle.width / 2 - 20f, HEADER / 2 - mTitle.height / 2);
            mTitle.textAlignment = UIHorizontalAlignment.Center;
            //Close Button UIButton
            mCloseButton = AddUIComponent<UIButton>();
            mCloseButton.normalBgSprite = "buttonclose";
            mCloseButton.hoveredBgSprite = "buttonclosehover";
            mCloseButton.pressedBgSprite = "buttonclosepressed";
            mCloseButton.relativePosition = new Vector3(WIDTH - 35, 5, 10);
            mCloseButton.eventClick += (component, eventParam) => {
                Hide();
            };

            createTextLabels();
            createDataLabels();
            Hide();
            DoOnStartup();
            if (Mod.debugLOGOn) Helper.dbgLog(string.Concat("Display panel created. ", DateTime.Now.ToString(DT_MILLI)));
        }


        /// <summary>
        ///     Function gets called by unity on every single frame.
        ///     We just check for our key combo, maybe there is a better way to register this with the game?
        /// </summary>
        public override void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) & Input.GetKeyDown(KeyCode.V)) processVisibility();
            base.Update();
        }

        /// <summary>
        ///     Our initialize stuff; call after panel\form setup.
        /// </summary>
        private void DoOnStartup()
        {
            refreshDisplayData(); //fill'em up manually initially.
            if (Mod.config.resetStatsEveryXMinutesEnabled){
                StartCoroutine(checkForStatsReset());
                if (Mod.debugLOGOn) Helper.dbgLog("CheckForStatsReset coroutine started.");
            }
            if (mAutoRefreshCheckbox.isChecked){
                StartCoroutine(refreshDisplayDataWrapper());
                StartCoroutine(refreshVehcCount());
                if (Mod.debugLOGOn) Helper.dbgLog("RefreshDisplayDataWrapper & RefreshVehcCount coroutines started.");
            }
        }


        /// <summary>
        ///     Create and setup up default text and stuff for our Text UILabels;
        /// </summary>
        private void createTextLabels()
        {
            mCurrentNumOfVehText = AddUIComponent<UILabel>();
            mCurrentNumOfVehText.text = string.Concat("Total number of vehicles in use: (", (Singleton<VehicleManager>.instance.m_vehicles.m_size - 1).ToString(), " max) ");
            mCurrentNumOfVehText.tooltip = "The present number of vehicles the game was last actively using.\n #ResInUse: is the number of reserved vehicles in use during last update.";
            mCurrentNumOfVehText.relativePosition = new Vector3(SPACING, 50f);
            //m_NumOfVehText.minimumSize = new Vector2(100, 20);
            //m_NumOfVehText.maximumSize = new Vector2(this.width - SPACING * 2, 30);
            mCurrentNumOfVehText.autoSize = true;

            mNewMaxNumberofVecText = AddUIComponent<UILabel>();
            mNewMaxNumberofVecText.relativePosition = new Vector3(SPACING, 50f + SPACING * 2);
            mNewMaxNumberofVecText.text = "New limit for non-service vehicles: ";
            mNewMaxNumberofVecText.tooltip = "The max limit value the mod now uses for non-critical vehicle requests \n #Reserved is the number you selected to reserve for critical services.\n Wonding why max is 16383 and not 16384? It is because the game does not use entry #0";
            mNewMaxNumberofVecText.autoSize = true;

            mReserveAttemptsText = AddUIComponent<UILabel>();
            mReserveAttemptsText.relativePosition = new Vector3(SPACING, mNewMaxNumberofVecText.relativePosition.y + SPACING22);
            mReserveAttemptsText.text = "# Attempts to use reserved vehicles: ";
            mReserveAttemptsText.tooltip = "The number of times a request was processed for a critical service that was allowed to use a reserved vehicle.";
            mReserveAttemptsText.autoSize = true;

            mReserveAttemptsFailedText = AddUIComponent<UILabel>();
            mReserveAttemptsFailedText.relativePosition = new Vector3(SPACING, mReserveAttemptsText.relativePosition.y + SPACING22);
            mReserveAttemptsFailedText.text = "# Attempts to use reserved vehicles (but failed): ";
            mReserveAttemptsFailedText.tooltip = "The number of times a request was processed for a critical service that was allowed to use a reserved vehicle,\n but still failed to be created.  A high number here(say 20+ over just a few minutes)\n might indicated the need for a higher reservered vehicle amount.";
            mReserveAttemptsFailedText.autoSize = true;

            mTimesReserveExceededText = AddUIComponent<UILabel>();
            mTimesReserveExceededText.relativePosition = new Vector3(SPACING, mReserveAttemptsFailedText.relativePosition.y + SPACING22);
            mTimesReserveExceededText.text = "# Times the reserved limits were exceeded: ";
            mTimesReserveExceededText.tooltip = "The number of requests where it was noticed that we had already used up all our reserved vehicles. \n This, if more than a few is an indicator that you may need to increase the reserves to the next level.\n This value should generally match the #Attempts to use reserve failures.";
            mTimesReserveExceededText.autoSize = true;

            mFailedDueToReserveText = AddUIComponent<UILabel>();
            mFailedDueToReserveText.relativePosition = new Vector3(SPACING, mTimesReserveExceededText.relativePosition.y + SPACING22);
            mFailedDueToReserveText.text = "# Times game failed to create a non-reserved vehicle: ";
            mFailedDueToReserveText.tooltip = "This is the raw number of requests where a vehicle was asked for but was denied because of hitting the reserve limiter.\n This is just provided as a statistic.";
            mFailedDueToReserveText.autoSize = true;

            mFailedToCreateText = AddUIComponent<UILabel>();
            mFailedToCreateText.relativePosition = new Vector3(SPACING, mFailedDueToReserveText.relativePosition.y + SPACING22);
            mFailedToCreateText.text = "# Total times game failed to create any vehicle: ";
            mFailedToCreateText.tooltip = "This is the raw number of requests where no matter the reason a vehicle was asked for \n but failed to be created because none were available, or some other odd error.";
            mFailedToCreateText.autoSize = true;

            mTimesCalledTotalText = AddUIComponent<UILabel>();
            mTimesCalledTotalText.relativePosition = new Vector3(SPACING, mFailedToCreateText.relativePosition.y + SPACING22);
            mTimesCalledTotalText.text = "# Total number of times CreateVehicle() was called: ";
            mTimesCalledTotalText.tooltip = "This is the raw total of all calls to CreateVehicle no matter if they succeeded or not.";
            mTimesCalledTotalText.autoSize = true;


            mAutoRefreshCheckbox = AddUIComponent<UICheckBox>();
            mAutoRefreshCheckbox.relativePosition = new Vector3(SPACING, mTimesCalledTotalText.relativePosition.y + 35f);

            mAutoRefreshChkboxText = AddUIComponent<UILabel>();
            mAutoRefreshChkboxText.relativePosition = new Vector3(mAutoRefreshCheckbox.relativePosition.x + mAutoRefreshCheckbox.width + SPACING * 3, mAutoRefreshCheckbox.relativePosition.y + 5f);
            //m_AutoRefreshChkboxText.text = "Use Auto Refresh";
            mAutoRefreshChkboxText.tooltip = "Enables these stats to update every few seconds \n Default is 5 seconds.";
            //m_AutoRefreshChkboxText.autoSize = true;

            mAutoRefreshCheckbox.height = 16;
            mAutoRefreshCheckbox.width = 16;
            mAutoRefreshCheckbox.label = mAutoRefreshChkboxText;
            mAutoRefreshCheckbox.text = string.Concat("Use AutoRefresh  (", Mod.autoRefreshSeconds.ToString("f1"), " sec)");

            UISprite uncheckSprite = mAutoRefreshCheckbox.AddUIComponent<UISprite>();
            uncheckSprite.height = 20;
            uncheckSprite.width = 20;
            uncheckSprite.relativePosition = new Vector3(0, 0);
            uncheckSprite.spriteName = "check-unchecked";
            uncheckSprite.isVisible = true;

            UISprite checkSprite = mAutoRefreshCheckbox.AddUIComponent<UISprite>();
            checkSprite.height = 20;
            checkSprite.width = 20;
            checkSprite.relativePosition = new Vector3(0, 0);
            checkSprite.spriteName = "check-checked";

            mAutoRefreshCheckbox.checkedBoxObject = checkSprite;
            mAutoRefreshCheckbox.isChecked = Mod.useAutoRefreshOption;
            mAutoRefreshCheckbox.isEnabled = true;
            mAutoRefreshCheckbox.isVisible = true;
            mAutoRefreshCheckbox.canFocus = true;
            mAutoRefreshCheckbox.isInteractive = true;
            //can't use this? m_AutoRefreshCheckbox.autoSize = true;  
            mAutoRefreshCheckbox.eventCheckChanged += (component, eventParam) => { AutoRefreshCheckbox_OnCheckChanged(component, eventParam); };
            //AutoRefreshCheckbox_OnCheckChanged;

            mAdditionalText1Text = AddUIComponent<UILabel>();
            mAdditionalText1Text.relativePosition = new Vector3(mAutoRefreshCheckbox.relativePosition.x + mAutoRefreshCheckbox.width + SPACING, mAutoRefreshCheckbox.relativePosition.y + 40f);
            mAdditionalText1Text.width = 300f;
            mAdditionalText1Text.height = 50f;
            //m_AdditionalText1Text.autoSize = true;
            //m_AdditionalText1Text.wordWrap = true;
            mAdditionalText1Text.text = "* Use CTRL + S + V (S&V at same time) to show again. \n  More options available in CSLServiceReserve_Config.xml";

            mRefresh = AddUIComponent<UIButton>();
            mRefresh.size = new Vector2(120, 24);
            mRefresh.text = "Manual Refresh";
            mRefresh.tooltip = "Use to manually refresh the data. \n (use when auto enabled is off)";
            mRefresh.textScale = 0.875f;
            mRefresh.normalBgSprite = "ButtonMenu";
            mRefresh.hoveredBgSprite = "ButtonMenuHovered";
            mRefresh.pressedBgSprite = "ButtonMenuPressed";
            mRefresh.disabledBgSprite = "ButtonMenuDisabled";
            //m_refresh.relativePosition = m_closeButton.relativePosition + new Vector3(-60 - SPACING, 6f);
            mRefresh.relativePosition = mAutoRefreshChkboxText.relativePosition + new Vector3(mAutoRefreshChkboxText.width + SPACING * 2, -5f);
            mRefresh.eventClick += (component, eventParam) => { refreshDisplayData(); };

            mLogdataButton = AddUIComponent<UIButton>();
            mLogdataButton.size = new Vector2(80, 24);
            mLogdataButton.text = "Log Data";
            mLogdataButton.tooltip = "Use to Log the current data to log file. \n (Saved to CSL standard output_log.txt log file)";
            mLogdataButton.textScale = 0.875f;
            mLogdataButton.normalBgSprite = "ButtonMenu";
            mLogdataButton.hoveredBgSprite = "ButtonMenuHovered";
            mLogdataButton.pressedBgSprite = "ButtonMenuPressed";
            mLogdataButton.disabledBgSprite = "ButtonMenuDisabled";
            mLogdataButton.relativePosition = mRefresh.relativePosition + new Vector3(mRefresh.width + SPACING * 3, 0f);
            mLogdataButton.eventClick += (component, eventParam) => { ProcessOnLogButton(); };

            mClearDataButton = AddUIComponent<UIButton>();
            mClearDataButton.size = new Vector2(50, 24);
            mClearDataButton.text = "Clear";
            mClearDataButton.tooltip = "Use to manually clear and reset the above data values. \n Usefull to watch for changes over specific periods of time \n This is in addition to the ResetLogEveryFewMin option.";
            mClearDataButton.textScale = 0.875f;
            mClearDataButton.normalBgSprite = "ButtonMenu";
            mClearDataButton.hoveredBgSprite = "ButtonMenuHovered";
            mClearDataButton.pressedBgSprite = "ButtonMenuPressed";
            mClearDataButton.disabledBgSprite = "ButtonMenuDisabled";
            mClearDataButton.relativePosition = mLogdataButton.relativePosition + new Vector3(mLogdataButton.width + SPACING * 3, 0f);
            mClearDataButton.eventClick += (component, eventParam) => { ProcessOnClearButton(); };
        }


        /// <summary>
        ///     Event handler for clicking on AutoRefreshbutton.
        /// </summary>
        /// <param name="uiComp">The triggering UIComponent</param>
        /// <param name="bValue">The Value True|False (Checked|Unchecked)</param>
        private void AutoRefreshCheckbox_OnCheckChanged(UIComponent uiComp, bool bValue)
        {
            if (Mod.debugLOGOn) Helper.dbgLog("AutoRefreshButton was toggled to: " + bValue);
            Mod.updateUseAutoRefeshValue(bValue);
            if (bValue){
                byte bflag = 0;
                if (!coVehcRefreshEnabled){
                    StartCoroutine(refreshVehcCount());
                    bflag += 1;
                }
                if (!coDisplayRefreshEnabled){
                    StartCoroutine(refreshDisplayDataWrapper());
                    bflag += 2;
                }
                if (!coResetDataEnabled){
                    StartCoroutine(checkForStatsReset());
                    bflag += 4;
                }
                if (Mod.debugLOGOn)
                    Helper.dbgLog("Starting all coroutines that were not already started " +
                        bValue + " bflag=" + bflag);
            }
            else{
                //upon disabling auto refresh we *also* disable the the stat data refresher
                //I think this is logical, as people might want to start at data, plus can always manually refresh.
                StopAllCoroutines();
                resetAllCoroutineState(false); //cleanup
                if (Mod.debugLOGOn) Helper.dbgLog("Stopping all coroutines: " + bValue);
            }
        }

        /// <summary>
        ///     Sadly needed to reset state of Coroutines after forced stop.
        /// </summary>
        /// <param name="bStatus">True|False</param>
        private void resetAllCoroutineState(bool bStatus)
        {
            coVehcRefreshEnabled = bStatus;
            coResetDataEnabled = bStatus;
            coDisplayRefreshEnabled = bStatus;
        }

        /// <summary>
        ///     Function to check if we need to reset the stats, ment to check only every so often..like once a minute
        ///     or modify it
        /// </summary>
        private IEnumerator checkForStatsReset()
        {
            if (coResetDataEnabled){
                if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog(" StatResetChecker* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 copy at a time.
            while (Mod.config.resetStatsEveryXMinutesEnabled){
                coResetDataEnabled = true;
                DateTime tmNow = DateTime.Now;
                if (DateTime.Compare(tmNow, statsResetTime) >= 0){
                    if (Mod.debugLOGOn & Mod.debugLOGLevel >= 2)
                        Helper.dbgLog(string.Concat("Stats reseting. current time is ",
                            DateTime.Now.ToString(DT_MILLI), " adding ", Mod.config.resetStatsEveryXMin.ToString(), "min."));
                    statsResetTime = tmNow.AddMinutes(Mod.config.resetStatsEveryXMin);
                    Mod.resetStatValues();
                    if (Mod.debugLOGOn) Helper.dbgLog(string.Concat("Stats reset. Next stats time reset to ", statsResetTime.ToString(DT_MILLI)));
                }

                // We hard code this to only check once a minute, keeps things simple,
                // do we really need too much accuracy on the reset?  If you change account for that in AutoRefresh toggle
                // cause you'll need to adjust the next time according to first run after some delay so they get in sync again
                // this just avoids the problem ...originaly was doing it the nice and complex way...but really... why???
                yield return new WaitForSeconds(60.0f);
            }
            coResetDataEnabled = false;
            if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog("StatsResetChecker coroutine exited due to ResetStatsEveryXMinutesEnabled = false.");
        }



        /// <summary>
        ///     The CoRoutine that gets spun up to update the displayed Vehicle count, it runs till visibility or autorrefresh
        ///     selection changes and generally will run several times per second, or whatever the user sets in the config.
        ///     We run this fast because otherwise people may not notice the value actually changing or going over new max limit.
        /// </summary>
        /// <returns></returns>
        private IEnumerator refreshVehcCount()
        {
            //we don't need to lookup\init these two up every x milliseconds.
            float tmpfloattime = Mod.config.refreshVehicleCounterSeconds;
            int tmpResInUse = 0;
            int vehcCount = 0;
            VehicleManager vMgr = Singleton<VehicleManager>.instance;
            if (coVehcRefreshEnabled){
                if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog("Refresh vehicleCount* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 active?

            while (isVisible & mAutoRefreshCheckbox.isChecked){
                coVehcRefreshEnabled = true;
                vehcCount = vMgr.m_vehicleCount;
                tmpResInUse = vehcCount - ((int)(vMgr.m_vehicles.m_size - 1) - Mod.reserveamount);
                mCurrentNumOfVehValues.text = string.Concat(vehcCount.ToString(), "    ResInUse: ", tmpResInUse < 0 ? "0" : tmpResInUse.ToString());
                yield return new WaitForSeconds(tmpfloattime);
            }
            coVehcRefreshEnabled = false;
            if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog("Refresh vehicleCount coroutine exited due to already refreshing or visiblity change.");
        }



        /// <summary>
        ///     Primary coroutine function to update the more static (seconds) information display.
        ///     as there really is no need to update this more then once per second.
        /// </summary>
        private IEnumerator refreshDisplayDataWrapper()
        {
            if (coDisplayRefreshEnabled){
                if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog("Refresh vehicleData* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 active. 
            while (IsRefreshing == false && isVisible && mAutoRefreshCheckbox.isChecked){
                coDisplayRefreshEnabled = true;
                refreshDisplayData();
                yield return new WaitForSeconds(Mod.config.autoRefreshSeconds);
            }
            coDisplayRefreshEnabled = false;
            if (Mod.debugLOGOn & Mod.debugLOGLevel > 0) Helper.dbgLog("Refresh vehicleData coroutine exited due to AutoRefresh disabled, visiblity change, or already refreshing.");
        }


        /// <summary>
        ///     Function refreshes the display data. mostly called from coroutine timer.
        /// </summary>
        private void refreshDisplayData()
        {
            IsRefreshing = true; //safety lock so we never get more then one of these, probably don't need after co-routine refactor.
            try{
                // m_title.text = string.Concat("Service Vehicle Reserve Data  (c:" , mcount.ToString(),")");
                VehicleManager vMgr = Singleton<VehicleManager>.instance;
                int tmpNewMax = (int)(vMgr.m_vehicles.m_size - 1) - Mod.reserveamount;
                int tmpResInUse = vMgr.m_vehicleCount - tmpNewMax;
                mCurrentNumOfVehValues.text = string.Concat(vMgr.m_vehicleCount.ToString(), "    ResInUse: ", tmpResInUse < 0 ? "0" : tmpResInUse.ToString());
                mNewMaxNumberofVecValue.text = string.Concat(tmpNewMax.ToString(), "    #Reserved: ", Mod.reserveamount.ToString());
                mReserveAttemptsValue.text = Mod.timesReservedAttempted.ToString();
                mReserveAttemptsFailedValue.text = Mod.timesReserveAttemptFailed.ToString();
                mTimesReserveExceededValue.text = Mod.timesLimitReached.ToString();
                mFailedDueToReserveValue.text = Mod.timesFailedByReserve.ToString();
                mFailedToCreateValue.text = Mod.timesFailedToCreate.ToString();
                double tmpval;
                if (Mod.timesCvCalledTotal > 0) tmpval = Mod.timesFailedToCreate / (double)Mod.timesCvCalledTotal * 100.0d;
                else tmpval = 0.0d;
                mTimesCalledTotalValue.text = string.Format("{0}  [{1} fail %]", Mod.timesCvCalledTotal.ToString(), tmpval.ToString("F02"));

                if (Mod.debugLOGOn & Mod.debugLOGLevel >= 3) Helper.dbgLog("Refreshing vehicle data completed. " + DateTime.Now.ToString(DT_MILLI));
            }
            catch (Exception ex){
                IsRefreshing = false;
                Helper.dbgLog("ERROR during RefreshDisplayData. ", ex, true);
            }
            IsRefreshing = false;
        }


        /// <summary>
        ///     Creates all our UILabels that store data that changes\gets refreshed.
        /// </summary>
        private void createDataLabels()
        {
            VehicleManager vMgr = Singleton<VehicleManager>.instance;
            mCurrentNumOfVehValues = AddUIComponent<UILabel>();
            mCurrentNumOfVehValues.text = string.Concat(vMgr.m_vehicleCount.ToString(), "    #ResInUse: 0");
            mCurrentNumOfVehValues.relativePosition = new Vector3(mCurrentNumOfVehText.relativePosition.x + mCurrentNumOfVehText.width + SPACING, mCurrentNumOfVehText.relativePosition.y);
            mCurrentNumOfVehValues.autoSize = true;
            mCurrentNumOfVehValues.tooltip = "The max # is off by 1 because entry 0 for a live vehicle in the game so 16384 is really 16383 usable.\n #ResInUse: is the number of reserved vehicles in use, if things are well number should rarely hit your reserved amount";

            mNewMaxNumberofVecValue = AddUIComponent<UILabel>();
            mNewMaxNumberofVecValue.relativePosition = new Vector3(mNewMaxNumberofVecText.relativePosition.x + mNewMaxNumberofVecText.width + SPACING, mNewMaxNumberofVecText.relativePosition.y);
            mNewMaxNumberofVecValue.autoSize = true;
            mNewMaxNumberofVecValue.text = string.Concat((vMgr.m_vehicles.m_size - 1 - Mod.reserveamount).ToString(),
                "    #Reserved: ", Mod.reserveamount.ToString());

            mReserveAttemptsValue = AddUIComponent<UILabel>();
            mReserveAttemptsValue.relativePosition = new Vector3(mReserveAttemptsText.relativePosition.x + mReserveAttemptsText.width + SPACING, mReserveAttemptsText.relativePosition.y);
            mReserveAttemptsValue.autoSize = true;
            mReserveAttemptsValue.text = Mod.timesReservedAttempted.ToString();

            mReserveAttemptsFailedValue = AddUIComponent<UILabel>();
            mReserveAttemptsFailedValue.relativePosition = new Vector3(mReserveAttemptsFailedText.relativePosition.x + mReserveAttemptsFailedText.width + SPACING, mReserveAttemptsFailedText.relativePosition.y);
            mReserveAttemptsFailedValue.autoSize = true;
            mReserveAttemptsFailedValue.text = Mod.timesReserveAttemptFailed.ToString();

            mTimesReserveExceededValue = AddUIComponent<UILabel>();
            mTimesReserveExceededValue.relativePosition = new Vector3(mTimesReserveExceededText.relativePosition.x + mTimesReserveExceededText.width + SPACING, mTimesReserveExceededText.relativePosition.y);
            mTimesReserveExceededValue.autoSize = true;
            mTimesReserveExceededValue.text = Mod.timesLimitReached.ToString();

            mFailedDueToReserveValue = AddUIComponent<UILabel>();
            mFailedDueToReserveValue.relativePosition = new Vector3(mFailedDueToReserveText.relativePosition.x + mFailedDueToReserveText.width + SPACING, mFailedDueToReserveText.relativePosition.y);
            mFailedDueToReserveValue.autoSize = true;
            mFailedDueToReserveValue.text = Mod.timesFailedByReserve.ToString();

            mFailedToCreateValue = AddUIComponent<UILabel>();
            mFailedToCreateValue.relativePosition = new Vector3(mFailedToCreateText.relativePosition.x + mFailedToCreateText.width + SPACING, mFailedToCreateText.relativePosition.y);
            mFailedToCreateValue.autoSize = true;
            mFailedToCreateValue.text = Mod.timesFailedToCreate.ToString();

            mTimesCalledTotalValue = AddUIComponent<UILabel>();
            mTimesCalledTotalValue.relativePosition = new Vector3(mTimesCalledTotalText.relativePosition.x + mTimesCalledTotalText.width + SPACING, mTimesCalledTotalText.relativePosition.y);
            mTimesCalledTotalValue.autoSize = true;
            mTimesCalledTotalValue.text = Mod.timesCvCalledTotal.ToString();
        }

        /// <summary>
        ///     Handle action for Hide\Show events.
        /// </summary>
        private void processVisibility()
        {
            if (!isVisible){
                Show();
                if (!coVehcRefreshEnabled) StartCoroutine(refreshVehcCount());
                if (!coDisplayRefreshEnabled) StartCoroutine(refreshDisplayDataWrapper());
                //we do not touch the Resetting of StatsData; that's left to autorefresh on\off only atm.
            }
            else{
                Hide();
                //we don't have to stop the two above coroutines, 
                //should do that themselves via their own visibility checks.
            }
        }

        /// <summary>
        ///     Handles the Clear button press event
        /// </summary>
        private void ProcessOnClearButton()
        {
            Mod.resetStatValues();
            refreshDisplayData();
        }


        /// <summary>
        ///     Handles action for pressing Log Data Button
        /// </summary>
        private void ProcessOnLogButton()
        {
            if (Configuration.USE_CUSTOM_DUMP_FILE) Helper.logExtentedWrapper(Helper.DumpOption.ALL);
            else Helper.logExtentedWrapper(Helper.DumpOption.ALL ^ Helper.DumpOption.USE_SEPERATE_FILE);
        }

        /// <summary>
        ///     Returns current private values from gui to caller.
        /// </summary>
        /// <returns></returns>
        public static Helper.ExternalData getInternalData()
        {
            Helper.ExternalData tmpobj = new Helper.ExternalData();
            tmpobj.coVechRefreshEnabled = Instance.coVehcRefreshEnabled;
            tmpobj.coDataRefreshEnabled = Instance.coResetDataEnabled;
            tmpobj.coDisplayRefreshEnabled = Instance.coDisplayRefreshEnabled;
            tmpobj.statsResetTime = Instance.statsResetTime;
            tmpobj.cachedname = Instance.cachedName;
            tmpobj.tag = Instance.tag;
            tmpobj.name = Instance.name;
            bool.TryParse(Instance.m_IsVisible.ToString(), out tmpobj.isVisable);
            bool.TryParse(Instance.mAutoRefreshChkboxText.isEnabled.ToString(), out tmpobj.isAutoRefreshActive);
            if (Mod.debugLOGOn & Mod.debugLOGLevel >= 3) Helper.dbgLog("GetInternalData created and returning.");
            return tmpobj;
        }
    }
}
