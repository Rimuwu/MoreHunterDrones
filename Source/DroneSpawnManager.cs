using System;
using System.Collections.Generic;
using System.Reflection;
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
            
            // Получаем состояния всех известных дронов через рефлексию из DronPawnsKindDefOf
            var droneDefNames = new List<string>();
            
            try
            {
                var fields = typeof(DronPawnsKindDefOf).GetFields(BindingFlags.Public | BindingFlags.Static);
                
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(PawnKindDef))
                    {
                        var pawnKindDef = (PawnKindDef)field.GetValue(null);
                        if (pawnKindDef != null)
                        {
                            droneDefNames.Add(pawnKindDef.defName);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Failed to get drone kinds via reflection: {ex.Message}");
                
                // Fallback - используем хардкодированный список
                droneDefNames.AddRange(new string[]
                {
                    "Drone_HunterToxic",
                    "Drone_HunterAntigrainWarhead", 
                    "Drone_HunterIncendiary",
                    "Drone_HunterEMP",
                    "Drone_HunterSmoke"
                });
            }
            
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
        /// Получение ThingDef для стандартного HunterDroneTrap как замены
        /// </summary>
        public static ThingDef GetStandardHunterDroneTrap()
        {
            // Ищем стандартную ловушку HunterDroneTrap
            var hunterDroneTrap = DefDatabase<ThingDef>.GetNamedSilentFail("HunterDroneTrap");
            
            if (hunterDroneTrap == null && DebugSettings.godMode)
            {
                Log.Warning("[MoreHunterDrones] HunterDroneTrap not found, using fallback");
            }
            
            return hunterDroneTrap;
        }
    }
}