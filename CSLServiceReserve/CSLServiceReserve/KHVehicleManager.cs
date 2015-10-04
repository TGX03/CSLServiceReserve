using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Steamworks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace CSLServiceReserve
{
    internal static class KHVehicleManager
    {
        private static bool CreateVehicle(VehicleManager vMgr, out ushort vehicle, ref Randomizer r, VehicleInfo info, Vector3 position, TransferManager.TransferReason type, bool transferToSource, bool transferToTarget)
        {
            bool AttemptFlag = false;
            uint ReserveMax = (vMgr.m_vehicles.m_size - 1) - Mod.RESERVEAMOUNT;  //we subtract 1 cause game doesn't use entry 0 for a real vehicle.
            int CurrentVehicleNum = vMgr.m_vehicleCount; //vMgr.m_vehicles.ItemCount(); //found they were never different ~+\- a nanosecond.
            int m_VecCount = vMgr.m_vehicleCount;  //unly
            Mod.timesCV_CalledTotal++; //stat tracking.
            if (CurrentVehicleNum >= ReserveMax && type != TransferManager.TransferReason.Fire && type != TransferManager.TransferReason.Sick
                && type != TransferManager.TransferReason.Garbage && type != TransferManager.TransferReason.Dead 
                && type != TransferManager.TransferReason.Crime && type != TransferManager.TransferReason.Bus 
                && type != TransferManager.TransferReason.MetroTrain && type != TransferManager.TransferReason.PassengerTrain
                && type != TransferManager.TransferReason.DeadMove && type != TransferManager.TransferReason.CriminalMove
                && type != TransferManager.TransferReason.Taxi && type != TransferManager.TransferReason.GarbageMove)
            {
                Mod.timesFailedByReserve++; //stat tracking
                Mod.timesFailedToCreate++;  //stat tracking
                vehicle = 0;
                return false;
            }

            if (CurrentVehicleNum >= ReserveMax)
            {
                AttemptFlag = true;
                Mod.timesReservedAttempted++;  //stat tracking.
                if (CurrentVehicleNum == (vMgr.m_vehicles.m_size -1)) { Mod.timesLimitReached++; } //stattracking
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >= 3) { Helper.dbgLog(" Vehicles[" + CurrentVehicleNum.ToString() + 
                    "] max reached, attempting to use reserve for a " + type.ToString() + " - " + System.DateTime.Now.ToString() +
                    " : " + DateTime.Now.Millisecond.ToString() + " counter=" + Mod.timesReservedAttempted.ToString() + " reservemax=" +
                    ReserveMax.ToString()); }
            }
    

            //Original Untouched Below except for attemptflag and Mod.timeFailedToCreate Counters and debug logging.
            ushort num;
            if (!vMgr.m_vehicles.CreateItem(out num, ref r))
            {
                vehicle = 0;
                if (AttemptFlag) 
                {
                    Mod.timesReserveAttemptFailed++ ; //stat tracking.
                    if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL >= 2) {  Helper.dbgLog(" Vehicles[" + CurrentVehicleNum.ToString() + 
                        "] max reached, attempted to use reserve for a " + type.ToString() + " but Failed! " + System.DateTime.Now.ToString() + " : " +
                        DateTime.Now.Millisecond.ToString() + " counter=" + Mod.timesReservedAttempted.ToString()); }
                }

                Mod.timesFailedToCreate++;  //stat tracking
                return false;
            }


            vehicle = num;
            Vehicle.Frame frame = new Vehicle.Frame(position, Quaternion.identity);
            vMgr.m_vehicles.m_buffer[vehicle].m_flags = Vehicle.Flags.Created;
            if (transferToSource)
            {
                vMgr.m_vehicles.m_buffer[vehicle].m_flags = vMgr.m_vehicles.m_buffer[vehicle].m_flags | Vehicle.Flags.TransferToSource;
            }
            if (transferToTarget)
            {
                vMgr.m_vehicles.m_buffer[vehicle].m_flags = vMgr.m_vehicles.m_buffer[vehicle].m_flags | Vehicle.Flags.TransferToTarget;
            }
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
            info.m_vehicleAI.CreateVehicle(vehicle, ref vMgr.m_vehicles.m_buffer[vehicle]);
            info.m_vehicleAI.FrameDataUpdated(vehicle, ref vMgr.m_vehicles.m_buffer[vehicle], ref vMgr.m_vehicles.m_buffer[vehicle].m_frame0);
            vMgr.m_vehicleCount = (int)(vMgr.m_vehicles.ItemCount() - 1);
            return true;
        }

    }
}
