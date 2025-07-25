
using Verse;

namespace MoreHunterDrones
{
    [StaticConstructorOnStartup]
    public static class MoreHunterDrones
    {
        static MoreHunterDrones()
        {

            //Harmony harmony = new Harmony("rimworld.mod.as1aw.morehunterdrones");
            //harmony.PatchAll();

            Log.Message("[MoreHunterDrones] Mod initialized");
        }
    }

}