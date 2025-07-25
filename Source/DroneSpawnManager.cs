using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MoreHunterDrones
{
    /// <summary>
    /// ������� ���������� ���������� ������ ��� ���������� ������ � ����������
    /// </summary>
    public static class DroneSpawnManager
    {
        // ��� ��������� ������ ��� �������� �������
        private static Dictionary<string, bool> cachedDroneStates = new Dictionary<string, bool>();
        
        // ������� ������ �� �������� ��� �������� �������
        private static Dictionary<string, int> roomDroneCount = new Dictionary<string, int>();

        /// <summary>
        /// ������������� ������� ���������� �������
        /// </summary>
        public static void Initialize()
        {
            RefreshAllDroneStates();
            Log.Message("[MoreHunterDrones] DroneSpawnManager initialized");
        }

        /// <summary>
        /// ���������� ��������� ����������� �����
        /// </summary>
        public static void RefreshDroneState(string pawnKindDefName, bool enabled)
        {
            if (cachedDroneStates.TryGetValue(pawnKindDefName, out bool currentValue) && currentValue == enabled)
                return; // ��� ���������

            cachedDroneStates[pawnKindDefName] = enabled;
            
            if (DebugSettings.godMode)
            {
                Log.Message($"[MoreHunterDrones] Drone {pawnKindDefName} state changed to {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// ���������� ���� ��������� ������ �� ��������
        /// </summary>
        public static void RefreshAllDroneStates()
        {
            var mod = LoadedModManager.GetMod<HunterDroneMod>();
            if (mod == null)
                return;

            cachedDroneStates.Clear();
            
            // �������� ��������� ���� ��������� ������
            var droneDefNames = new string[]
            {
                "Drone_HunterToxic",
                "Drone_HunterAntigrainWarhead", 
                "Drone_HunterIncendiary",
                "Drone_HunterEMP",
                "Drone_HunterSmoke"
            };
            
            foreach (string pawnKindDefName in droneDefNames)
            {
                bool enabled = HunterDroneMod.IsDroneEnabled(pawnKindDefName);
                cachedDroneStates[pawnKindDefName] = enabled;
            }
        }

        /// <summary>
        /// ������� �������� ������������ ����� �� PawnKindDef �����
        /// </summary>
        public static bool IsDroneEnabledFast(string pawnKindDefName)
        {
            if (string.IsNullOrEmpty(pawnKindDefName))
                return true;

            if (cachedDroneStates.TryGetValue(pawnKindDefName, out bool enabled))
                return enabled;
                
            // Fallback �� ������ �������� ��������
            try
            {
                return HunterDroneMod.IsDroneEnabled(pawnKindDefName);
            }
            catch (System.Exception ex)
            {
                if (DebugSettings.godMode)
                {
                    Log.Warning($"[MoreHunterDrones] Failed to check drone state for {pawnKindDefName}: {ex.Message}");
                }
                return true; // �� ��������� ���������, ���� �� ����� ���������
            }
        }

        /// <summary>
        /// ����� �������� ������ ��� ����� �������
        /// </summary>
        public static void ResetRoomDroneCount(string roomId)
        {
            roomDroneCount[roomId] = 0;
        }

        /// <summary>
        /// ���������� �������� ������ � �������
        /// </summary>
        public static void IncrementRoomDroneCount(string roomId)
        {
            if (!roomDroneCount.ContainsKey(roomId))
                roomDroneCount[roomId] = 0;
            roomDroneCount[roomId]++;
        }

        /// <summary>
        /// ��������, ����� �� �������� ��� ������ � �������
        /// </summary>
        public static bool CanAddMoreDronesToRoom(string roomId)
        {
            int currentCount = roomDroneCount.TryGetValue(roomId, out int count) ? count : 0;
            int maxDrones = HunterDroneMod.GetMaxDronesPerRoom();
            return currentCount < maxDrones;
        }

        /// <summary>
        /// ��������� ThingDef ��� �������� �����-�������� ��� ������
        /// </summary>
        public static ThingDef GetReplacementDroneTrap()
        {
            // �������� ����� ������� HunterDrone ��� WaspDrone ��� ������
            var hunterDroneTrap = DefDatabase<ThingDef>.GetNamedSilentFail("HunterDrone_Trap");
            if (hunterDroneTrap != null)
                return hunterDroneTrap;

            var waspDroneTrap = DefDatabase<ThingDef>.GetNamedSilentFail("WaspDrone_Trap");
            if (waspDroneTrap != null)
                return waspDroneTrap;

            // ���� ��� ������� ������, ���������� ������� �������
            return ThingDefOf.TrapSpike;
        }
    }
}