using HarmonyLib;
using RimWorld;
using Verse;

namespace MoreHunterDrones.Patches
{
    /// <summary>
    /// Патч для замены отключенных дронов на стандартного HunterDrone в древних структурах
    /// </summary>
    [HarmonyPatch]
    public static class DynamicDronePatches
    {
        /// <summary>
        /// Патч для замены отключенных дрон-ловушек на стандартного HunterDrone
        /// </summary>
        [HarmonyPatch(typeof(ThingMaker), "MakeThing")]
        [HarmonyPostfix]
        public static void MakeThing_Postfix(ThingDef def, ref Thing __result)
        {
            // Проверяем, является ли это отключенной дрон-ловушкой
            if (__result != null && IsDisabledDroneTrap(def))
            {
                // Уничтожаем отключенную дрон-ловушку
                __result.Destroy(DestroyMode.Vanish);
                
                // Пытаемся заменить на стандартного HunterDrone
                var standardHunterDroneTrap = DroneSpawnManager.GetStandardHunterDroneTrap();
                if (standardHunterDroneTrap != null)
                {
                    // Создаем стандартную ловушку HunterDrone
                    if (standardHunterDroneTrap.MadeFromStuff)
                    {
                        var defaultStuff = GenStuff.DefaultStuffFor(standardHunterDroneTrap);
                        __result = ThingMaker.MakeThing(standardHunterDroneTrap, defaultStuff);
                    }
                    else
                    {
                        __result = ThingMaker.MakeThing(standardHunterDroneTrap);
                    }
                    
                    if (DebugSettings.godMode)
                    {
                        Log.Message($"[MoreHunterDrones] Replaced disabled drone trap {def.defName} with standard HunterDrone");
                    }
                }
                else
                {
                    // Fallback - создаем кусок металлолома если стандартный HunterDrone не найден
                    var fallbackDef = ThingDefOf.ChunkSlagSteel ?? ThingDefOf.Steel;
                    if (fallbackDef != null)
                    {
                        __result = ThingMaker.MakeThing(fallbackDef);
                        if (DebugSettings.godMode)
                        {
                            Log.Message($"[MoreHunterDrones] Standard HunterDrone not found, replaced {def.defName} with {fallbackDef.defName}");
                        }
                    }
                    else
                    {
                        // Последний fallback - просто удаляем
                        __result = null;
                        if (DebugSettings.godMode)
                        {
                            Log.Warning($"[MoreHunterDrones] No replacement found, removed disabled drone trap {def.defName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверка, является ли ThingDef отключенной дрон-ловушкой
        /// </summary>
        private static bool IsDisabledDroneTrap(ThingDef thingDef)
        {
            if (thingDef?.defName == null)
                return false;

            // Проверяем только если это дрон-ловушка
            if (!IsDroneTrap(thingDef))
                return false;

            string pawnKindDefName = GetPawnKindDefFromTrapDef(thingDef);
            return pawnKindDefName != null && !DroneSpawnManager.IsDroneEnabledFast(pawnKindDefName);
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
                default:
                    return null;
            }
        }
    }
}