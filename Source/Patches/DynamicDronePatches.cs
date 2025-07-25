using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace MoreHunterDrones.Patches
{
    /// <summary>
    /// ���� ��� �������� ������ ������ � ������� ����������
    /// </summary>
    [HarmonyPatch]
    public static class DynamicDronePatches
    {
        // ������� ��� ��������� ���������� ID ������
        private static int roomIdCounter = 0;
        
        // ������� ID �������������� �������
        private static string currentRoomId = "";

        /// <summary>
        /// ���� ��� ������������ ������ ��������� �������
        /// </summary>
        [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
        [HarmonyPrefix]
        public static void ResolveContents_Prefix()
        {
            // ������� ����� ID ��� ������� � ���������� �������
            currentRoomId = $"room_{roomIdCounter++}";
            DroneSpawnManager.ResetRoomDroneCount(currentRoomId);
        }

        /// <summary>
        /// ���� ��� ����� ������ ����������� ����-�������
        /// </summary>
        [HarmonyPatch(typeof(ThingMaker), "MakeThing")]
        [HarmonyPostfix]
        public static void MakeThing_Postfix(ThingDef def, ref Thing __result)
        {
            // ���������, �������� �� ��� ����-��������
            if (__result != null && IsDroneTrap(def))
            {
                string pawnKindDefName = GetPawnKindDefFromTrapDef(def);
                bool isDisabled = pawnKindDefName != null && !DroneSpawnManager.IsDroneEnabledFast(pawnKindDefName);
                bool roomHasSpace = DroneSpawnManager.CanAddMoreDronesToRoom(currentRoomId);

                if (isDisabled)
                {
                    // ���� ��������
                    __result.Destroy(DestroyMode.Vanish);

                    if (roomHasSpace)
                    {
                        // ���� ����� - �������� �� ������� ����
                        var replacementTrap = DroneSpawnManager.GetReplacementDroneTrap();
                        if (replacementTrap != null)
                        {
                            if (replacementTrap.MadeFromStuff)
                            {
                                // ��� �������, ��������� ��������
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
                        // ��� ����� - ������ �������
                        __result = null;
                        
                        if (DebugSettings.godMode)
                        {
                            Log.Message($"[MoreHunterDrones] Removed disabled drone {def.defName} - room at drone limit");
                        }
                    }
                }
                else if (pawnKindDefName != null)
                {
                    // ���� ������� - ������� ���
                    if (roomHasSpace)
                    {
                        DroneSpawnManager.IncrementRoomDroneCount(currentRoomId);
                    }
                    else
                    {
                        // �������� ����� - �������� �� ������� �������
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
                // ����� �������� ������� ����� ���� ��� ����
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