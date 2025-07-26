//using HarmonyLib;
//using RimWorld;
//using RimWorld.SketchGen;
//using System.Collections.Generic;
//using System.Linq;
//using Verse;

//namespace MoreHunterDrones.Patches
//{
//    // Патчи для детального отслеживания генерации содержимого комнат
//    // Эти патчи показывают, какие части (parts) создаются в каждой комнате

//    // 1. Основной патч для LayoutRoomDef.ResolveContents - здесь решается, что будет в комнате
//    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveContents")]
//    public static class LayoutRoomDefResolveContentsPatch
//    {
//        public static void Prefix(LayoutRoomDef __instance, Map map, LayoutRoom room, float? threatPoints, Faction faction)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] ==== НАЧАЛО ОБРАБОТКИ КОМНАТЫ ====");
//            Log.Message($"[RoomGen] Тип комнаты: '{__instance.defName}'");
//            Log.Message($"[RoomGen] Площадь: {room?.Area ?? 0} клеток");
//            Log.Message($"[RoomGen] Фракция: '{faction?.Name ?? "неизвестная"}'");
//            Log.Message($"[RoomGen] Уровень угрозы: {threatPoints?.ToString("F1") ?? "не указан"}");

//            // Логируем все parts, которые будут созданы
//            if (__instance.parts != null && __instance.parts.Count > 0)
//            {
//                Log.Message($"[RoomGen] Комната содержит {__instance.parts.Count} частей:");
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
//                Log.Message($"[RoomGen] Комната не содержит частей (parts)");
//            }

//            // Логируем scatter объекты (разбросанные предметы/здания)
//            if (__instance.scatter != null && __instance.scatter.Count > 0)
//            {
//                Log.Message($"[RoomGen] Scatter объектов: {__instance.scatter.Count}");
//                for (int i = 0; i < __instance.scatter.Count; i++)
//                {
//                    var scatter = __instance.scatter[i];
//                    Log.Message($"[RoomGen]   Scatter {i + 1}: тип {scatter?.GetType()?.Name ?? "неизвестный"}");
//                }
//            }

//            // Логируем fill interior (заполнение интерьера)
//            if (__instance.fillInterior != null && __instance.fillInterior.Count > 0)
//            {
//                Log.Message($"[RoomGen] Fill Interior объектов: {__instance.fillInterior.Count}");
//                for (int i = 0; i < __instance.fillInterior.Count; i++)
//                {
//                    var fill = __instance.fillInterior[i];
//                    Log.Message($"[RoomGen]   Interior {i + 1}: тип {fill?.GetType()?.Name ?? "неизвестный"}");
//                }
//            }

//            // Логируем wall attachments (настенные крепления)
//            if (__instance.wallAttachments != null && __instance.wallAttachments.Count > 0)
//            {
//                Log.Message($"[RoomGen] Wall Attachments: {__instance.wallAttachments.Count}");
//                for (int i = 0; i < __instance.wallAttachments.Count; i++)
//                {
//                    var wall = __instance.wallAttachments[i];
//                    Log.Message($"[RoomGen]   Wall {i + 1}: тип {wall?.GetType()?.Name ?? "неизвестный"}");
//                }
//            }

//            // Логируем ThingSetMaker, если есть
//            if (__instance.thingSetMakerDef != null)
//            {
//                Log.Message($"[RoomGen] ThingSetMaker: {__instance.thingSetMakerDef.defName}");
//            }
//        }

//        public static void Postfix(LayoutRoomDef __instance)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] ==== ЗАВЕРШЕНА ОБРАБОТКА КОМНАТЫ '{__instance.defName}' ====");
//        }

//        // Детальное логирование информации о части комнаты (безопасная версия)
//        private static void LogPartDetails(LayoutPartParms part, int partNumber)
//        {
//            if (part == null) return;

//            // Логируем только базовую информацию о типе части
//            Log.Message($"[RoomGen]     - Тип части: {part.GetType().Name}");

//            // Безопасное логирование полей через рефлексию
//            var partType = part.GetType();
//            var fields = partType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

//            foreach (var field in fields)
//            {
//                try
//                {
//                    var value = field.GetValue(part);
//                    if (value != null)
//                    {
//                        // Логируем только простые типы для избежания ошибок
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
//                    // Игнорируем ошибки получения значений полей
//                }
//            }
//        }
//    }

//    // 2. Патч для LayoutRoomDef.ResolveSketch - создание "скетча" комнаты (план размещения)
//    [HarmonyPatch(typeof(LayoutRoomDef), "ResolveSketch")]
//    public static class LayoutRoomDefResolveSketchPatch
//    {
//        public static void Prefix(LayoutRoomDef __instance, LayoutRoomParams parms)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] Создание скетча для '{__instance.defName}'");
//            Log.Message($"[RoomGen] Прямоугольников в комнате: {parms.room?.rects?.Count ?? 0}");

//            // Логируем типы полов
//            if (__instance.floorTypes != null && __instance.floorTypes.Count > 0)
//            {
//                var floorNames = __instance.floorTypes.Select(f => f.defName).ToArray();
//                Log.Message($"[RoomGen] Типы полов: {string.Join(", ", floorNames)}");
//            }

//            // Логируем edge terrain (края комнаты)
//            if (__instance.edgeTerrain != null)
//            {
//                Log.Message($"[RoomGen] Тип краев: {__instance.edgeTerrain.defName}");
//            }

//            // Логируем области комнаты
//            if (parms.room?.rects != null)
//            {
//                for (int i = 0; i < parms.room.rects.Count; i++)
//                {
//                    var rect = parms.room.rects[i];
//                    Log.Message($"[RoomGen] Область {i + 1}: {rect} (размер: {rect.Width}x{rect.Height})");
//                }
//            }
//        }
//    }

//    // 3. Патч для отслеживания работы RoomContentsWorker - класс, который заполняет комнаты содержимым
//    [HarmonyPatch(typeof(RoomContentsWorker), "FillRoom")]
//    public static class RoomContentsWorkerPatch
//    {
//        public static void Prefix(Map map, LayoutRoom room, Faction faction, float? threatPoints)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] RoomContentsWorker заполняет комнату");
//            Log.Message($"[RoomGen] Площадь: {room?.Area ?? 0}, Угроза: {threatPoints?.ToString("F1") ?? "нет"}");
//        }

//        public static void Postfix(Map map, LayoutRoom room)
//        {
//            if (!Prefs.DevMode) return;

//            Log.Message($"[RoomGen] RoomContentsWorker завершил заполнение комнаты");
//        }
//    }
//}