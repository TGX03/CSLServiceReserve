using System;
using System.Collections.Generic;
using ColossalFramework.Math;
using JetBrains.Annotations;
using UnityEngine;
//using ColossalFramework.Steamworks;

namespace CSLServiceReserve
{
    internal static class KhVehicleManager
    {

        private static bool createVehicle(VehicleManager vMgr, out ushort vehicle, ref Randomizer r, VehicleInfo info, Vector3 position, TransferManager.TransferReason type, bool transferToSource, bool transferToTarget)
        {
            bool attemptFlag = false;
            uint reserveMax = vMgr.m_vehicles.m_size - 1 - Mod.reserveamount; //we subtract 1 cause game doesn't use entry 0 for a real vehicle.
            int currentVehicleNum = vMgr.m_vehicleCount;                      //vMgr.m_vehicles.ItemCount(); //found they were never different ~+\- a nanosecond.
            Mod.timesCvCalledTotal++;                                         //stat tracking.
            if (currentVehicleNum >= reserveMax && type != TransferManager.TransferReason.Fire && type != TransferManager.TransferReason.Sick
                && type != TransferManager.TransferReason.Garbage && type != TransferManager.TransferReason.Dead
                && type != TransferManager.TransferReason.Crime && type != TransferManager.TransferReason.Bus
                && type != TransferManager.TransferReason.MetroTrain && type != TransferManager.TransferReason.PassengerTrain
                && type != TransferManager.TransferReason.DeadMove && type != TransferManager.TransferReason.CriminalMove
                && type != TransferManager.TransferReason.Taxi && type != TransferManager.TransferReason.GarbageMove
                && type != TransferManager.TransferReason.Tram && type != TransferManager.TransferReason.RoadMaintenance
                && type != TransferManager.TransferReason.Snow && type != TransferManager.TransferReason.SnowMove
                && type != TransferManager.TransferReason.Fire2 && type != TransferManager.TransferReason.ForestFire
                && type != TransferManager.TransferReason.FloodWater && type != TransferManager.TransferReason.SickMove
                && type != TransferManager.TransferReason.Sick2 && type != TransferManager.TransferReason.EvacuateVipA
                && type != TransferManager.TransferReason.EvacuateVipB && type != TransferManager.TransferReason.EvacuateVipC
                && type != TransferManager.TransferReason.EvacuateVipD && type != TransferManager.TransferReason.Monorail
                && type != TransferManager.TransferReason.Ferry){
                Mod.timesFailedByReserve++; //stat tracking
                Mod.timesFailedToCreate++;  //stat tracking
                vehicle = 0;
                return false;
            }

            if (currentVehicleNum >= reserveMax){
                attemptFlag = true;
                Mod.timesReservedAttempted++;                                                 //stat tracking.
                if (currentVehicleNum == vMgr.m_vehicles.m_size - 1) Mod.timesLimitReached++; //stattracking
                if (Mod.debugLOGOn && Mod.debugLOGLevel >= 3)
                    Helper.dbgLog(" Vehicles[" + currentVehicleNum +
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
                        Helper.dbgLog(" Vehicles[" + currentVehicleNum +
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
            return true;
        }
    }
}
