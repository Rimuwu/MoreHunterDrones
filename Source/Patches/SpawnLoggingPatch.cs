using HarmonyLib;
using RimWorld;
using Verse;

namespace MoreHunterDrones.Patches
{
    // �������� ��������� ��� ����� ��� �������� ������������� ����
    // ���������������� ������ ����� ����� ������������


    //// ������� ���� ��� ������������ ������ ������
    //[HarmonyPatch(typeof(Building), "SpawnSetup")]
    //public static class BuildingSpawnSetupPatch
    //{
    //    public static void Postfix(Building __instance, Map map, bool respawningAfterLoad)
    //    {
    //        if (!Prefs.DevMode || respawningAfterLoad) return;
            
    //        Log.Message($"[SpawnLog] ������ '{__instance.def.defName}' " +
    //                   $"����������� �� ������� {__instance.Position}");
    //    }
    //}
    
    //// ������� ���� ��� ������������ ������ ����������
    //[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    //public static class PawnSpawnSetupPatch
    //{
    //    public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
    //    {
    //        if (!Prefs.DevMode || respawningAfterLoad) return;
            
    //        Log.Message($"[SpawnLog] �������� '{__instance.def.defName}' " +
    //                   $"����������� �� ������� {__instance.Position} " +
    //                   $"(�������: {__instance.Faction?.Name ?? "���"})");
    //    }
    //}

}