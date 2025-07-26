using HarmonyLib;
using RimWorld;
using RimWorld.SketchGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreHunterDrones.Patches
{
    // ����� ��� ���������� ������������ ��������� ����������� ������
    // ��� ����� ����������, ����� ����� (parts) ��������� � ������ �������

    // 1. �������� ���� ��� LayoutRoomDef.ResolveContents - ����� ��������, ��� ����� � �������
    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
    public static class LayoutRoomDefResolveContentsPatch
    {
        public static void Prefix(LayoutRoomDef __instance, Map map, LayoutRoom room, float? threatPoints, Faction faction)
        {
            if (!Prefs.DevMode) return;

            // �������� ��� parts, ������� ����� �������
            if (__instance.parts != null && __instance.parts.Count > 0)
            {
                Log.Message($"[RoomGen] ������� �������� {__instance.parts.Count} ������:");
                for (int i = 0; i < __instance.parts.Count; i++)
                {
                    var part = __instance.parts[i];
                    if (part != null)
                    {
                        Log.Message($"[RoomGen]   Part {i + 1}: {part.GetType().Name}");
                        LogPartDetails(part, i + 1);
                    }
                }
            }

        }

        // ��������� ����������� ���������� � ����� ������� (���������� ������)
        private static void LogPartDetails(LayoutPartParms part, int partNumber)
        {
            if (part == null) return;

            // �������� ������ ������� ���������� � ���� �����
            Log.Message($"[RoomGen]     - ��� �����: {part.GetType().Name}");

            // ���������� ����������� ����� ����� ���������
            var partType = part.GetType();
            var fields = partType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(part);
                    if (value != null)
                    {
                        // �������� ������ ������� ���� ��� ��������� ������
                        if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
                        {
                            Log.Message($"[RoomGen]     - {field.Name}: {value}");
                        }
                        else if (value is Def def)
                        {
                            Log.Message($"[RoomGen]     - {field.Name}: {def.defName}");
                        }
                    }
                }
                catch
                {
                    // ���������� ������ ��������� �������� �����
                }
            }
        }
    }
}