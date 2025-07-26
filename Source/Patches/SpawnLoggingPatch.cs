using HarmonyLib;
using RimWorld;
using Verse;

namespace MoreHunterDrones.Patches
{
    // Временно отключаем все патчи для проверки инициализации мода
    // Раскомментируйте нужные патчи после тестирования


    //// Простой патч для отслеживания спавна зданий
    //[HarmonyPatch(typeof(Building), "SpawnSetup")]
    //public static class BuildingSpawnSetupPatch
    //{
    //    public static void Postfix(Building __instance, Map map, bool respawningAfterLoad)
    //    {
    //        if (!Prefs.DevMode || respawningAfterLoad) return;
            
    //        Log.Message($"[SpawnLog] Здание '{__instance.def.defName}' " +
    //                   $"установлено на позиции {__instance.Position}");
    //    }
    //}
    
    //// Простой патч для отслеживания спавна персонажей
    //[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    //public static class PawnSpawnSetupPatch
    //{
    //    public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
    //    {
    //        if (!Prefs.DevMode || respawningAfterLoad) return;
            
    //        Log.Message($"[SpawnLog] Персонаж '{__instance.def.defName}' " +
    //                   $"заспавнился на позиции {__instance.Position} " +
    //                   $"(фракция: {__instance.Faction?.Name ?? "нет"})");
    //    }
    //}

}