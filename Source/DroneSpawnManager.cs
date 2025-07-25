using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MoreHunterDrones
{
    /// <summary>
    /// Система управления состоянием дронов для блокировки спавна в структурах
    /// </summary>
    public static class DroneSpawnManager
    {
        // Кэш состояний дронов для быстрого доступа
        private static Dictionary<string, bool> cachedDroneStates = new Dictionary<string, bool>();
        
        // Счетчик дронов по комнатам для контроля лимитов
        private static Dictionary<string, int> roomDroneCount = new Dictionary<string, int>();

        /// <summary>
        /// Инициализация системы управления дронами
        /// </summary>
        public static void Initialize()
        {
            RefreshAllDroneStates();
            Log.Message("[MoreHunterDrones] DroneSpawnManager initialized");
        }

        /// <summary>
        /// Обновление состояния конкретного дрона
        /// </summary>
        public static void RefreshDroneState(string pawnKindDefName, bool enabled)
        {
            if (cachedDroneStates.TryGetValue(pawnKindDefName, out bool currentValue) && currentValue == enabled)
                return; // Нет изменений

            cachedDroneStates[pawnKindDefName] = enabled;
            
            if (DebugSettings.godMode)
            {
                Log.Message($"[MoreHunterDrones] Drone {pawnKindDefName} state changed to {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Обновление всех состояний дронов из настроек
        /// </summary>
        public static void RefreshAllDroneStates()
        {
            var mod = LoadedModManager.GetMod<HunterDroneMod>();
            if (mod == null)
                return;

            cachedDroneStates.Clear();
            
            // Получаем состояния всех известных дронов
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
        /// Быстрая проверка включенности дрона по PawnKindDef имени
        /// </summary>
        public static bool IsDroneEnabledFast(string pawnKindDefName)
        {
            if (string.IsNullOrEmpty(pawnKindDefName))
                return true;

            if (cachedDroneStates.TryGetValue(pawnKindDefName, out bool enabled))
                return enabled;
                
            // Fallback на прямую проверку настроек
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
                return true; // По умолчанию разрешаем, если не можем проверить
            }
        }

        /// <summary>
        /// Сброс счетчика дронов для новой комнате
        /// </summary>
        public static void ResetRoomDroneCount(string roomId)
        {
            roomDroneCount[roomId] = 0;
        }

        /// <summary>
        /// Увеличение счетчика дронов в комнате
        /// </summary>
        public static void IncrementRoomDroneCount(string roomId)
        {
            if (!roomDroneCount.ContainsKey(roomId))
                roomDroneCount[roomId] = 0;
            roomDroneCount[roomId]++;
        }

        /// <summary>
        /// Проверка, можно ли добавить еще дронов в комнату
        /// </summary>
        public static bool CanAddMoreDronesToRoom(string roomId)
        {
            int currentCount = roomDroneCount.TryGetValue(roomId, out int count) ? count : 0;
            int maxDrones = HunterDroneMod.GetMaxDronesPerRoom();
            return currentCount < maxDrones;
        }

        /// <summary>
        /// Получение ThingDef для базового дрона-охотника как замены
        /// </summary>
        public static ThingDef GetReplacementDroneTrap()
        {
            // Пытаемся найти базовый HunterDrone или WaspDrone как замену
            var hunterDroneTrap = DefDatabase<ThingDef>.GetNamedSilentFail("HunterDrone_Trap");
            if (hunterDroneTrap != null)
                return hunterDroneTrap;

            var waspDroneTrap = DefDatabase<ThingDef>.GetNamedSilentFail("WaspDrone_Trap");
            if (waspDroneTrap != null)
                return waspDroneTrap;

            // Если нет базовых дронов, используем обычную ловушку
            return ThingDefOf.TrapSpike;
        }
    }
}