using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using ColossalFramework.Math;
using JetBrains.Annotations;
using UnityEngine;
//using ColossalFramework.Steamworks;

namespace CSLServiceReserve
{
    internal static class KhVehicleManager
    {
        private static readonly HashSet<TransferManager.TransferReason> VALID_REASONS = new HashSet<TransferManager.TransferReason>();
        private static volatile int NormalVehicleCount = 0;
        private static readonly object Lock = new object();
        private static readonly MethodInfo actualRelease = VehicleManager.instance.GetType().GetMethod("ReleaseVehicleImplementation", BindingFlags.NonPublic | BindingFlags.Instance);

        static KhVehicleManager()
        {
            TransferManager.TransferReason[] reasons =
            { TransferManager.TransferReason.Fire, TransferManager.TransferReason.Fire2, TransferManager.TransferReason.ForestFire, TransferManager.TransferReason.Sick, TransferManager.TransferReason.Sick2, TransferManager.TransferReason.SickMove,
              TransferManager.TransferReason.Garbage, TransferManager.TransferReason.GarbageMove, TransferManager.TransferReason.GarbageTransfer, TransferManager.TransferReason.Dead, TransferManager.TransferReason.DeadMove, TransferManager.TransferReason.Crime,
              TransferManager.TransferReason.CriminalMove, TransferManager.TransferReason.Bus, TransferManager.TransferReason.MetroTrain, TransferManager.TransferReason.PassengerTrain, TransferManager.TransferReason.PassengerHelicopter, TransferManager.TransferReason.PassengerPlane,
              TransferManager.TransferReason.PassengerShip, TransferManager.TransferReason.Tram, TransferManager.TransferReason.RoadMaintenance, TransferManager.TransferReason.Snow, TransferManager.TransferReason.SnowMove, TransferManager.TransferReason.FloodWater,
              TransferManager.TransferReason.EvacuateA, TransferManager.TransferReason.EvacuateB, TransferManager.TransferReason.EvacuateC, TransferManager.TransferReason.EvacuateD, TransferManager.TransferReason.EvacuateVipA, TransferManager.TransferReason.EvacuateVipB,
              TransferManager.TransferReason.EvacuateVipC, TransferManager.TransferReason.EvacuateVipD, TransferManager.TransferReason.Monorail, TransferManager.TransferReason.Ferry, TransferManager.TransferReason.Blimp, TransferManager.TransferReason.Mail, TransferManager.TransferReason.Trolleybus,
              TransferManager.TransferReason.CableCar, TransferManager.TransferReason.IncomingMail, TransferManager.TransferReason.IntercityBus, TransferManager.TransferReason.OutgoingMail, TransferManager.TransferReason.SortedMail, TransferManager.TransferReason.UnsortedMail };
            VALID_REASONS.UnionWith(reasons.ToList());
            new Thread(countVehicles).Start();
        }

        private static bool createVehicle(VehicleManager vMgr, out ushort vehicle, ref Randomizer r, VehicleInfo info, Vector3 position, TransferManager.TransferReason type, bool transferToSource, bool transferToTarget)
        {
            bool attemptFlag = false;
            uint reserveMax = vMgr.m_vehicles.m_size - 1 - Mod.reserveamount; //we subtract 1 cause game doesn't use entry 0 for a real vehicle.
            Mod.timesCvCalledTotal++;                                         //stat tracking.
            bool normalVehicle = false;
            if (!VALID_REASONS.Contains(type)){
                normalVehicle = true;
                if (NormalVehicleCount >= reserveMax){
                    Mod.timesFailedByReserve++; //stat tracking
                    Mod.timesFailedToCreate++;  //stat tracking
                    vehicle = 0;
                    return false;
                }
            }

            if (NormalVehicleCount >= reserveMax){
                attemptFlag = true;
                Mod.timesReservedAttempted++;                                                   //stat tracking.
                if (vMgr.m_vehicleCount == vMgr.m_vehicles.m_size - 1) Mod.timesLimitReached++; //stattracking
                if (Mod.debugLOGOn && Mod.debugLOGLevel >= 3)
                    Helper.dbgLog(" Vehicles[" + NormalVehicleCount +
                        "] max reached, attempting to use reserve for a " + type + " - " + DateTime.Now +
                        " : " + DateTime.Now.Millisecond + " counter=" + Mod.timesReservedAttempted + " reservemax=" +
                        reserveMax);
            }


            //Original Untouched Below except for attemptflag and Mod.timeFailedToCreate Counters and debug logging.
            ushort num;
            if (!vMgr.m_vehicles.CreateItem(out num, ref r)){
                vehicle = 0;
                if (attemptFlag){
                    Mod.timesReserveAttemptFailed++; //stat tracking.
                    if (Mod.debugLOGOn && Mod.debugLOGLevel >= 2)
                        Helper.dbgLog(" Vehicles[" + NormalVehicleCount +
                            "] max reached, attempted to use reserve for a " + type + " but Failed! " + DateTime.Now + " : " +
                            DateTime.Now.Millisecond + " counter=" + Mod.timesReservedAttempted);
                }

                Mod.timesFailedToCreate++; //stat tracking
                return false;
            }


            vehicle = num;
            Vehicle.Frame frame = new Vehicle.Frame(position, Quaternion.identity);
            vMgr.m_vehicles.m_buffer[vehicle].m_flags = Vehicle.Flags.Created;
            if (transferToSource) vMgr.m_vehicles.m_buffer[vehicle].m_flags = vMgr.m_vehicles.m_buffer[vehicle].m_flags | Vehicle.Flags.TransferToSource;
            if (transferToTarget) vMgr.m_vehicles.m_buffer[vehicle].m_flags = vMgr.m_vehicles.m_buffer[vehicle].m_flags | Vehicle.Flags.TransferToTarget;
            vMgr.m_vehicles.m_buffer[vehicle].Info = info;
            vMgr.m_vehicles.m_buffer[vehicle].m_frame0 = frame;
            vMgr.m_vehicles.m_buffer[vehicle].m_frame1 = frame;
            vMgr.m_vehicles.m_buffer[vehicle].m_frame2 = frame;
            vMgr.m_vehicles.m_buffer[vehicle].m_frame3 = frame;
            vMgr.m_vehicles.m_buffer[vehicle].m_targetPos0 = Vector4.zero;
            vMgr.m_vehicles.m_buffer[vehicle].m_targetPos1 = Vector4.zero;
            vMgr.m_vehicles.m_buffer[vehicle].m_targetPos2 = Vector4.zero;
            vMgr.m_vehicles.m_buffer[vehicle].m_targetPos3 = Vector4.zero;
            vMgr.m_vehicles.m_buffer[vehicle].m_sourceBuilding = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_targetBuilding = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_transferType = (byte)type;
            vMgr.m_vehicles.m_buffer[vehicle].m_transferSize = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_waitCounter = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_blockCounter = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_nextGridVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_nextOwnVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_nextGuestVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_nextLineVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_transportLine = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_leadingVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_trailingVehicle = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_cargoParent = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_firstCargo = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_nextCargo = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_citizenUnits = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_path = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_lastFrame = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_pathPositionIndex = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_lastPathOffset = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_gateIndex = 0;
            vMgr.m_vehicles.m_buffer[vehicle].m_waterSource = 0;
            info.m_vehicleAI.CreateVehicle(vehicle, ref vMgr.m_vehicles.m_buffer[vehicle]);
            info.m_vehicleAI.FrameDataUpdated(vehicle, ref vMgr.m_vehicles.m_buffer[vehicle], ref vMgr.m_vehicles.m_buffer[vehicle].m_frame0);
            vMgr.m_vehicleCount = (int)(vMgr.m_vehicles.ItemCount() - 1);
            if (normalVehicle) Interlocked.Increment(ref NormalVehicleCount);
            return true;
        }

        private static void countVehicles()
        {
            while (true){
                int result = 0;
                Array16<Vehicle> vehicles = VehicleManager.instance.m_vehicles;
                foreach (Vehicle current in vehicles.m_buffer){
                    if (!VALID_REASONS.Contains((TransferManager.TransferReason)current.m_transferType)){
                        result++;
                    }
                }
                NormalVehicleCount = result;
                Thread.Sleep(500);
            }
        }
    }
}
