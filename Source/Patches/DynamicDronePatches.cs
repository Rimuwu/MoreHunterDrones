using HarmonyLib;
using RimWorld;
using Verse;

namespace MoreHunterDrones.Patches
{
    /// <summary>
    /// ���� ��� ������ ����������� ������ �� ������������ HunterDrone � ������� ����������
    /// </summary>
    [HarmonyPatch]
    public static class DynamicDronePatches
    {
        /// <summary>
        /// ���� ��� ������ ����������� ����-������� �� ������������ HunterDrone
        /// </summary>
        [HarmonyPatch(typeof(ThingMaker), "MakeThing")]
        [HarmonyPostfix]
        public static void MakeThing_Postfix(ThingDef def, ref Thing __result)
        {
            // ���������, �������� �� ��� ����������� ����-��������
            if (__result != null && IsDisabledDroneTrap(def))
            {
                // ���������� ����������� ����-�������
                __result.Destroy(DestroyMode.Vanish);
                
                // �������� �������� �� ������������ HunterDrone
                var standardHunterDroneTrap = DroneSpawnManager.GetStandardHunterDroneTrap();
                if (standardHunterDroneTrap != null)
                {
                    // ������� ����������� ������� HunterDrone
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
                    // Fallback - ������� ����� ����������� ���� ����������� HunterDrone �� ������
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
                        // ��������� fallback - ������ �������
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
        /// ��������, �������� �� ThingDef ����������� ����-��������
        /// </summary>
        private static bool IsDisabledDroneTrap(ThingDef thingDef)
        {
            if (thingDef?.defName == null)
                return false;

            // ��������� ������ ���� ��� ����-�������
            if (!IsDroneTrap(thingDef))
                return false;

            string pawnKindDefName = GetPawnKindDefFromTrapDef(thingDef);
            return pawnKindDefName != null && !DroneSpawnManager.IsDroneEnabledFast(pawnKindDefName);
        }

        /// <summary>
        /// ��������, �������� �� ThingDef ����-�������� (�����)
        /// </summary>
        private static bool IsDroneTrap(ThingDef thingDef)
        {
            if (thingDef?.defName == null)
                return false;

            return thingDef.defName.Contains("Drone") && thingDef.defName.EndsWith("_Trap");
        }

        /// <summary>
        /// ��������� PawnKindDef ����� �� ThingDef ����-�������
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