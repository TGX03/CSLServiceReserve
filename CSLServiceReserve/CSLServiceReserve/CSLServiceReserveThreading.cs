using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Timers;
using System.Linq;
using System.IO ;
using System.Collections.Generic;
namespace CSLServiceReserve
{
    class CSLServiceReserveThreading : ThreadingExtensionBase
    {
        public static CSLServiceReserveThreading instance;
        public static System.Timers.Timer AutoRefreshTimer;
        private static readonly object _templock = new object();
        public static bool isCurrentlyRefreshing;
        private static bool ForceAbort;
        public static bool isInitialized= false;
        public static bool isTimerCreated = false;
        private bool MyThreadingDebug = false;
        private static System.Text.StringBuilder mystringbuilder = new System.Text.StringBuilder(1024);
        private static long myframecounter = 0;

        public CSLServiceReserveThreading()
        {
            LogItToFile("Constructor!");
        }

        ~CSLServiceReserveThreading()
        {
            Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  deconstructor called: " + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
            if (isInitialized == true & isTimerCreated == true)
            {
                StopTimer();
                DestroyTimer(ref AutoRefreshTimer);
                isInitialized = false;
            }
        }

        public static void Init() 
        {
            if (!isInitialized)
            {
                if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  Initialize called and false:  handelcount=" + Mod.EventHandlerCounter.ToString() + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
                isCurrentlyRefreshing = false;
                ForceAbort = false;
                SetupTimer(Mod.AutoRefreshSeconds);
                isInitialized = true;
             }
        }

        //timer event that gets fired.
        public static void AutoRefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEventFired: " + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
           // lock (_templock)
           // {
                if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent Inside locked area: " + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
                if (isCurrentlyRefreshing || ForceAbort)
                {
                    if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEventFired: " + (isCurrentlyRefreshing ? "IsCurrentlyRefreshing" : "FORCEABORT"));
                    return;
                }
                isCurrentlyRefreshing = true;

                if (sender == null || e == null) //object destroyed already?
                {
                    if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: **Sender ref was null! or no data.");
                    isCurrentlyRefreshing = false;
                    return;
                }

                try
                {
                    System.Timers.Timer timer = (System.Timers.Timer)sender;
                    if (!Mod.UseAutoRefreshOption) //are we disabled?
                    {
                        if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: AutoUpdateOption NoLonger Enabled");
                        if (timer == null) //doublecheck we have valid object.
                        { 
                            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: **Sender ref could not be converted.");
                            isCurrentlyRefreshing = false;
                            return; 
                        }

                        //if we're already stopped then we are a lingering event.
                        if (timer.Enabled == false)
                        {
                            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: Option not enabeld and neither is the timer. deref event.");
                            ForceAbort = true; //ignore any more who fire untill someone resets us.
                            //de-reffer eventhandler else long standing object
                            //timer.Elapsed -= AutoRefreshTimer_Elapsed;
                            isCurrentlyRefreshing = false;
                            return;
                        }
                        else
                        {
                            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: Option disabled but timer is not. calling stop");
                            ForceAbort = true; //ignore any more who fire.
                            // DestroyTimer(ref timer);
                            timer.Stop(); //stop
                            //timer.Elapsed -= AutoRefreshTimer_Elapsed;  //decouple
                            //Mod.EventHandlerCounter--;
                        }
                    }
                    else
                    {   //we are not disabled
                        if (timer == null) //doublecheck we have valid object if not return now!
                        { 
                            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: **Sender ref could not be converted2.");
                            isCurrentlyRefreshing = false;
                            return;
                        }
                        //We are have a vaild timer refference.

                        //if timer is not enabled...then we are lingering event. 
                        //and we're probably out of sync.
                        if (timer.Enabled == false)
                        {
                            if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: We option enabled but timer is not. treating as old lingering");
                            //what should we do ignor it or disable it?
                            //let's just ignore it and assume it was from switching\options too quickly.
                            isCurrentlyRefreshing = false;
                            return;
                        }
                        // ok we option is enabled and we're a valid object.
                        if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  TimerEvent: We have valid request; eventtriggered @ :" + e.SignalTime.ToLongTimeString() + ":" + e.SignalTime.Millisecond.ToString() + ":" + e.SignalTime.Ticks.ToString());
                        Loader.guiPanel.RefreshMyData();
                        isCurrentlyRefreshing = false;

                    }
                }
                catch (Exception ex)
                {
                    Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  Exception: " + ex.Message.ToString() + " \n " + (ex.InnerException == null ? "" : ex.InnerException.ToString()));
                }
                finally
                {
                    isCurrentlyRefreshing = false;
                    if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  Exiting Event: ");
                }
                if (Mod.DEBUG_LOG_ON) Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING]  Exiting lockingEvent: ");
            //}
        }


        public static void SetupTimer(int iTheTime)
        {
            Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:SetupTimer] SetupTimer entered");
            try
            {
                if (AutoRefreshTimer != null | isTimerCreated == true)
                {
                    Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:SetupTimer] Timer Pre-Existed (not null) will try to destory." + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
                    DestroyTimer(ref AutoRefreshTimer);
                }
                lock (_templock)
                {
                    AutoRefreshTimer = new System.Timers.Timer();
                    AutoRefreshTimer.Elapsed += new ElapsedEventHandler(AutoRefreshTimer_Elapsed);
                    Mod.EventHandlerCounter++;
                    isTimerCreated = true;
                }
                if (Mod.AutoRefreshSeconds < 1 || Mod.AutoRefreshSeconds > 120) { iTheTime = 5; }
                AutoRefreshTimer.Interval = (iTheTime * 1000);
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:SetupTimer] Timer was Created Fresh." + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
            }
            catch (Exception ex) { Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:setuptimer] " + ex.Message.ToString()); }
        }

        public static void StartTimer()
        {
            if (AutoRefreshTimer != null)
            {
                AutoRefreshTimer.Start();
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:StartTimer] Timer was Started." + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
            }
            else
            {
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:StartTimer] Timer was NULL!!.");

            }
        }

        
        public static void StopTimer()
        {
            if (AutoRefreshTimer != null & isTimerCreated == true)
            {
                AutoRefreshTimer.Stop();
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:StopTimer] Timer was Stopped." + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond.ToString());
            }
            else 
            {
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:StopTimer] Timer was NULL!!.");
            }
        }

        public static bool DestroyTimer()
        { return DestroyTimer(ref AutoRefreshTimer); }

        public static bool DestroyTimer(ref System.Timers.Timer theTimer)
        {
            
            try
            {
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:DestroyTimer] Timer Destroy Attemped.");
                if (theTimer != null )
                {
                    lock (_templock)
                    {
                        theTimer.Stop();
                        theTimer.Elapsed -= AutoRefreshTimer_Elapsed;
                        Mod.EventHandlerCounter--;
                        theTimer.Close();  //clean up system resources
                        theTimer.Dispose(); //let it be garbage collected
                        isTimerCreated = false;
                    }
                    theTimer = null; //kill our reference to it, if above fails we've left it lingering out there but hopefully 'stopped'.
                    Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:DestroyTimer] TimerDestroyed. and nulled");
                    return true;
                }
                else
                {
                    Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:DestroyTimer] Timer already Destroyed. and nulled");
                    isTimerCreated = false;
                    return false;
                }

            }
            catch (Exception ex)
            {
                isTimerCreated = false;
                Debug.Log("[CSLServiecReserve:CSLServiceReserveTHREADING:DestroyTimer] " + ex.Message.ToString());
                return false; 
            }
 
        }

        
        public override void OnCreated(IThreading threading) 
        {
            mystringbuilder.AppendLine(string.Concat("[CSLServiceReserve:CSLServiceReserveTHREADING:] OnCreated Fired.  dbg:", Mod.DEBUG_LOG_LEVEL.ToString()));
            mystringbuilder.AppendLine(ColossalFramework.IO.DataLocation.executableDirectory.ToString());
            mystringbuilder.AppendLine(ColossalFramework.IO.DataLocation.currentDirectory.ToString());
            mystringbuilder.AppendLine(ColossalFramework.IO.DataLocation.gameContentPath.ToString());
            mystringbuilder.AppendLine(ColossalFramework.IO.DataLocation.modsPath.ToString());
            mystringbuilder.AppendLine(ColossalFramework.IO.DataLocation.applicationBase.ToString());
            LogItToFile(mystringbuilder.ToString());
            mystringbuilder.Length = 0;
            MyThreadingDebug = Configuration.Deserialize(Mod.MOD_CONFIGPATH).DebugLogging;
            Debug.Log("[CSLServiceReserve:CSLServiceReserveTHREADING: OnCreated] Debugmode:" + Mod.DEBUG_LOG_ON.ToString());
            CSLServiceReserveThreading.instance = this;
            base.OnCreated(threading);

        }

        public override void OnReleased()
        {
            base.OnReleased();
            if (Mod.DEBUG_LOG_ON) { Debug.Log("[CSLServiceReserve:CSLServiceReserveTHREADING: OnReleased.]"); }
            if (isInitialized)
            {
                try
                {
                    if (isTimerCreated == true || AutoRefreshTimer != null)
                    {
                        if (Mod.DEBUG_LOG_ON) 
                        {
                            Debug.Log("[CSLServiceReserve:CSLServiceReserveTHREADING: OnReleased.] Destroying.  eventcount:" + Mod.EventHandlerCounter.ToString()); 
                            mystringbuilder.AppendLine("[CSLServiceReserve:CSLServiceReserveTHREADING: OnReleased.] Destroying.  eventcount:" + Mod.EventHandlerCounter.ToString());
                            LogItToFile(mystringbuilder.ToString());
                        }
                        DestroyTimer(ref AutoRefreshTimer);
                        isInitialized = false;
                        if (Mod.DEBUG_LOG_ON) { Debug.Log("[CSLServiceReserve:CSLServiceReserveTHREADING: OnReleased.] Destroying done eventcount:" + Mod.EventHandlerCounter.ToString()); }
                        LogItToFile("[CSLServiceReserve:CSLServiceReserveTHREADING: OnReleased.] Destroying done eventcount:" + Mod.EventHandlerCounter.ToString());
                    }
                }
                catch (Exception ex)
                {
                    LogItToFile("[CSLServiceReserve:CSLServiceReserveTHREADING] OnReleased Error:" + ex.Message.ToString());
                }
            }
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) 
        {
            myframecounter++;
            if (myframecounter > (60 * 30))
            {
                LogItToFile("Frame: " + myframecounter.ToString());
                myframecounter = 0;
            }

        }

        public void LogItToFile(string strText)
        {
            using (StreamWriter streamWriter = new StreamWriter("C:\\temp\\Cities_threadtest.txt", true))
            {
                streamWriter.WriteLine(strText);
            }
        }
    }
}