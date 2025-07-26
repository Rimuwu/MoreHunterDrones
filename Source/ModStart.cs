using HarmonyLib;
using Verse;

namespace MoreHunterDrones
{
    [StaticConstructorOnStartup]
    public static class MoreHunterDrones
    {
        static MoreHunterDrones()
        {
            try
            {
                Harmony harmony = new Harmony("rimworld.mod.as1aw.morehunterdrones");
                harmony.PatchAll();

                // Инициализируем систему управления дронами
                //DroneSpawnManager.Initialize();

                Log.Message("[MoreHunterDrones] Mod initialized with dynamic drone control system");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[MoreHunterDrones] Failed to initialize mod: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}