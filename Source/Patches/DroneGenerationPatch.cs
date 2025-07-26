using HarmonyLib;
using RimWorld;
using RimWorld.SketchGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreHunterDrones.Patches
{
    // Патчи для детального отслеживания генерации содержимого комнат
    // Эти патчи показывают, какие части (parts) создаются в каждой комнате

    // 1. Основной патч для LayoutRoomDef.ResolveContents - здесь решается, что будет в комнате
    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
    public static class LayoutRoomDefResolveContentsPatch
    {
        public static void Prefix(LayoutRoomDef __instance, Map map, LayoutRoom room, float? threatPoints, Faction faction)
        {
            if (!Prefs.DevMode) return;

            // Логируем все parts, которые будут созданы
            if (__instance.parts != null && __instance.parts.Count > 0)
            {
                Log.Message($"[RoomGen] Комната содержит {__instance.parts.Count} частей:");
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

        // Детальное логирование информации о части комнаты (безопасная версия)
        private static void LogPartDetails(LayoutPartParms part, int partNumber)
        {
            if (part == null) return;

            // Логируем только базовую информацию о типе части
            Log.Message($"[RoomGen]     - Тип части: {part.GetType().Name}");

            // Безопасное логирование полей через рефлексию
            var partType = part.GetType();
            var fields = partType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(part);
                    if (value != null)
                    {
                        // Логируем только простые типы для избежания ошибок
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
                    // Игнорируем ошибки получения значений полей
                }
            }
        }
    }
}