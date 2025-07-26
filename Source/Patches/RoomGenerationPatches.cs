//using HarmonyLib;
//using RimWorld;
//using RimWorld.SketchGen;
//using System.Collections.Generic;
//using System.Linq;
//using Verse;

//namespace MoreHunterDrones.Patches
//{
//    // ����� ��� ���������� ������������ ��������� ����������� ������
//    // ��� ����� ����������, ����� ����� (parts) ��������� � ������ �������

//    // 1. �������� ���� ��� LayoutRoomDef.ResolveContents - ����� ��������, ��� ����� � �������
//    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
//    public static class LayoutRoomDefResolveContentsPatch
//    {
//        public static void Prefix(LayoutRoomDef __instance, Map map, LayoutRoom room, float? threatPoints, Faction faction)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] ==== ������ ��������� ������� ====");
//            Log.Message($"[RoomGen] ��� �������: '{__instance.defName}'");
//            Log.Message($"[RoomGen] �������: {room?.Area ?? 0} ������");
//            Log.Message($"[RoomGen] �������: '{faction?.Name ?? "�����������"}'");
//            Log.Message($"[RoomGen] ������� ������: {threatPoints?.ToString("F1") ?? "�� ������"}");

//            // �������� ��� parts, ������� ����� �������
//            if (__instance.parts != null && __instance.parts.Count > 0)
//            {
//                Log.Message($"[RoomGen] ������� �������� {__instance.parts.Count} ������:");
//                for (int i = 0; i < __instance.parts.Count; i++)
//                {
//                    var part = __instance.parts[i];
//                    if (part != null)
//                    {
//                        Log.Message($"[RoomGen]   Part {i + 1}: {part.GetType().Name}");
//                        LogPartDetails(part, i + 1);
//                    }
//                }
//            }
//            else
//            {
//                Log.Message($"[RoomGen] ������� �� �������� ������ (parts)");
//            }

//            // �������� scatter ������� (������������ ��������/������)
//            if (__instance.scatter != null && __instance.scatter.Count > 0)
//            {
//                Log.Message($"[RoomGen] Scatter ��������: {__instance.scatter.Count}");
//                for (int i = 0; i < __instance.scatter.Count; i++)
//                {
//                    var scatter = __instance.scatter[i];
//                    Log.Message($"[RoomGen]   Scatter {i + 1}: ��� {scatter?.GetType()?.Name ?? "�����������"}");
//                }
//            }

//            // �������� fill interior (���������� ���������)
//            if (__instance.fillInterior != null && __instance.fillInterior.Count > 0)
//            {
//                Log.Message($"[RoomGen] Fill Interior ��������: {__instance.fillInterior.Count}");
//                for (int i = 0; i < __instance.fillInterior.Count; i++)
//                {
//                    var fill = __instance.fillInterior[i];
//                    Log.Message($"[RoomGen]   Interior {i + 1}: ��� {fill?.GetType()?.Name ?? "�����������"}");
//                }
//            }

//            // �������� wall attachments (��������� ���������)
//            if (__instance.wallAttachments != null && __instance.wallAttachments.Count > 0)
//            {
//                Log.Message($"[RoomGen] Wall Attachments: {__instance.wallAttachments.Count}");
//                for (int i = 0; i < __instance.wallAttachments.Count; i++)
//                {
//                    var wall = __instance.wallAttachments[i];
//                    Log.Message($"[RoomGen]   Wall {i + 1}: ��� {wall?.GetType()?.Name ?? "�����������"}");
//                }
//            }

//            // �������� ThingSetMaker, ���� ����
//            if (__instance.thingSetMakerDef != null)
//            {
//                Log.Message($"[RoomGen] ThingSetMaker: {__instance.thingSetMakerDef.defName}");
//            }
//        }

//        public static void Postfix(LayoutRoomDef __instance)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] ==== ��������� ��������� ������� '{__instance.defName}' ====");
//        }

//        // ��������� ����������� ���������� � ����� ������� (���������� ������)
//        private static void LogPartDetails(LayoutPartParms part, int partNumber)
//        {
//            if (part == null) return;

//            // �������� ������ ������� ���������� � ���� �����
//            Log.Message($"[RoomGen]     - ��� �����: {part.GetType().Name}");

//            // ���������� ����������� ����� ����� ���������
//            var partType = part.GetType();
//            var fields = partType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

//            foreach (var field in fields)
//            {
//                try
//                {
//                    var value = field.GetValue(part);
//                    if (value != null)
//                    {
//                        // �������� ������ ������� ���� ��� ��������� ������
//                        if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
//                        {
//                            Log.Message($"[RoomGen]     - {field.Name}: {value}");
//                        }
//                        else if (value is Def def)
//                        {
//                            Log.Message($"[RoomGen]     - {field.Name}: {def.defName}");
//                        }
//                    }
//                }
//                catch
//                {
//                    // ���������� ������ ��������� �������� �����
//                }
//            }
//        }
//    }

//    // 2. ���� ��� LayoutRoomDef.ResolveSketch - �������� "������" ������� (���� ����������)
//    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveSketch")]
//    public static class LayoutRoomDefResolveSketchPatch
//    {
//        public static void Prefix(LayoutRoomDef __instance, LayoutRoomParams parms)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] �������� ������ ��� '{__instance.defName}'");
//            Log.Message($"[RoomGen] ��������������� � �������: {parms.room?.rects?.Count ?? 0}");

//            // �������� ���� �����
//            if (__instance.floorTypes != null && __instance.floorTypes.Count > 0)
//            {
//                var floorNames = __instance.floorTypes.Select(f => f.defName).ToArray();
//                Log.Message($"[RoomGen] ���� �����: {string.Join(", ", floorNames)}");
//            }

//            // �������� edge terrain (���� �������)
//            if (__instance.edgeTerrain != null)
//            {
//                Log.Message($"[RoomGen] ��� �����: {__instance.edgeTerrain.defName}");
//            }

//            // �������� ������� �������
//            if (parms.room?.rects != null)
//            {
//                for (int i = 0; i < parms.room.rects.Count; i++)
//                {
//                    var rect = parms.room.rects[i];
//                    Log.Message($"[RoomGen] ������� {i + 1}: {rect} (������: {rect.Width}x{rect.Height})");
//                }
//            }
//        }
//    }

//    // 3. ���� ��� ������������ ������ RoomContentsWorker - �����, ������� ��������� ������� ����������
//    [HarmonyPatch(typeof(RoomContentsWorker), "FillRoom")]
//    public static class RoomContentsWorkerPatch
//    {
//        public static void Prefix(Map map, LayoutRoom room, Faction faction, float? threatPoints)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] RoomContentsWorker ��������� �������");
//            Log.Message($"[RoomGen] �������: {room?.Area ?? 0}, ������: {threatPoints?.ToString("F1") ?? "���"}");
//        }

//        public static void Postfix(Map map, LayoutRoom room)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] RoomContentsWorker �������� ���������� �������");
//        }
//    }
//}