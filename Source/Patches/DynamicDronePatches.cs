using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace MoreHunterDrones.Patches
{
    /// <summary>
    /// Патч для контроля спавна дронов в древних структурах
    /// </summary>
    [HarmonyPatch]
    public static class DynamicDronePatches
    {
        // Счетчик для генерации уникальных ID комнат
        private static int roomIdCounter = 0;
        
        // Текущий ID обрабатываемой комнаты
        private static string currentRoomId = "";

        /// <summary>
        /// Патч для отслеживания начала генерации комнаты
        /// </summary>
        [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
        [HarmonyPrefix]
        public static void ResolveContents_Prefix()
        {
            // Создаем новый ID для комнаты и сбрасываем счетчик
            currentRoomId = $"room_{roomIdCounter++}";
            DroneSpawnManager.ResetRoomDroneCount(currentRoomId);
        }

        /// <summary>
        /// Патч для умной замены отключенных дрон-ловушек
        /// </summary>
        [HarmonyPatch(typeof(ThingMaker), "MakeThing")]
        [HarmonyPostfix]
        public static void MakeThing_Postfix(ThingDef def, ref Thing __result)
        {
            // Проверяем, является ли это дрон-ловушкой
            if (__result != null && IsDroneTrap(def))
            {
                string pawnKindDefName = GetPawnKindDefFromTrapDef(def);
                bool isDisabled = pawnKindDefName != null && !DroneSpawnManager.IsDroneEnabledFast(pawnKindDefName);
                bool roomHasSpace = DroneSpawnManager.CanAddMoreDronesToRoom(currentRoomId);

                if (isDisabled)
                {
                    // Дрон отключен
                    __result.Destroy(DestroyMode.Vanish);

                    if (roomHasSpace)
                    {
                        // Есть место - заменяем на базовый дрон
                        var replacementTrap = DroneSpawnManager.GetReplacementDroneTrap();
                        if (replacementTrap != null)
                        {
                            if (replacementTrap.MadeFromStuff)
                            {
                                // Для ловушек, требующих материал
                                var defaultStuff = GenStuff.DefaultStuffFor(replacementTrap);
                                __result = ThingMaker.MakeThing(replacementTrap, defaultStuff);
                            }
                            else
                            {
                                __result = ThingMaker.MakeThing(replacementTrap);
                            }
                            
                            DroneSpawnManager.IncrementRoomDroneCount(currentRoomId);
                            
                            if (DebugSettings.godMode)
                            {
                                Log.Message($"[MoreHunterDrones] Replaced disabled drone {def.defName} with {replacementTrap.defName}");
                            }
                        }
                        else
                        {
                            __result = null;
                        }
                    }
                    else
                    {
                        // Нет места - просто удаляем
                        __result = null;
                        
                        if (DebugSettings.godMode)
                        {
                            Log.Message($"[MoreHunterDrones] Removed disabled drone {def.defName} - room at drone limit");
                        }
                    }
                }
                else if (pawnKindDefName != null)
                {
                    // Дрон включен - считаем его
                    if (roomHasSpace)
                    {
                        DroneSpawnManager.IncrementRoomDroneCount(currentRoomId);
                    }
                    else
                    {
                        // Превышен лимит - заменяем на обычную ловушку
                        __result.Destroy(DestroyMode.Vanish);
                        
                        var spikeTrap = ThingDefOf.TrapSpike;
                        if (spikeTrap != null)
                        {
                            var defaultStuff = GenStuff.DefaultStuffFor(spikeTrap);
                            __result = ThingMaker.MakeThing(spikeTrap, defaultStuff);
                        }
                        else
                        {
                            __result = null;
                        }
                        
                        if (DebugSettings.godMode)
                        {
                            Log.Message($"[MoreHunterDrones] Replaced drone {def.defName} with spike trap - room at drone limit");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверка, является ли ThingDef дрон-ловушкой (любой)
        /// </summary>
        private static bool IsDroneTrap(ThingDef thingDef)
        {
            if (thingDef?.defName == null)
                return false;

            return thingDef.defName.Contains("Drone") && thingDef.defName.EndsWith("_Trap");
        }

        /// <summary>
        /// Получение PawnKindDef имени из ThingDef дрон-ловушки
        /// </summary>
        private static string GetPawnKindDefFromTrapDef(ThingDef thingDef)
        {
            if (thingDef?.defName == null)
                return null;

            string defName = thingDef.defName;

            switch (defName)
            {
                case "Drone_HunterToxic_Trap":
                    return "Drone_HunterToxic";
                case "Drone_HunterAntigrainWarhead_Trap":
                    return "Drone_HunterAntigrainWarhead";
                case "Drone_HunterIncendiary_Trap":
                    return "Drone_HunterIncendiary";
                case "Drone_HunterEMP_Trap":
                    return "Drone_HunterEMP";
                case "Drone_HunterSmoke_Trap":
                    return "Drone_HunterSmoke";
                // Можно добавить базовые дроны если они есть
                case "HunterDrone_Trap":
                    return "HunterDrone";
                case "WaspDrone_Trap":
                    return "WaspDrone";
                default:
                    return null;
            }
        }
    }
}