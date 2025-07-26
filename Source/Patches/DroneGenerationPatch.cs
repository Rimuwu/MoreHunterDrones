using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreHunterDrones.Patches
{
    [HarmonyPatch(typeof(RoomContentsWorker), "TrySpawnParts")]
    public class RoomContentsWorker_Patch
    {
        [HarmonyPrefix]
        public static bool TrySpawnParts(RoomContentsWorker __instance, Map map, LayoutRoom room, Faction faction, float? threatPoints, bool post)
        {
            // Получаем определение комнаты
            var roomDef = __instance.RoomDef;
            if (roomDef == null) return true;

            // Базовые очки угрозы
            float threatPointsValue = threatPoints ?? 300f;

            if (threatPoints.HasValue && roomDef.threatPointsScaleCurve != null)
            {
                threatPointsValue = roomDef.threatPointsScaleCurve.Evaluate(threatPoints.Value);
            }

            // Получаем части комнаты через приватный метод PartParms
            var partParmsMethod = typeof(RoomContentsWorker).GetMethod("PartParms",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (partParmsMethod != null)
            {
                var partParms = (IEnumerable<LayoutPartParms>)partParmsMethod.Invoke(__instance, new object[] { room });

                foreach (LayoutPartParms partParm in partParms)
                {
                    if (partParm?.def?.Worker == null) continue;

                    // Проверяем, должна ли эта часть заполняться на текущем этапе
                    if (partParm.def.Worker.FillOnPost == post)
                    {
                        // Проверяем, является ли это дроном-охотником
                        bool isHunterDronePart = partParm.def.defName.StartsWith("HunterDrone");

                        // Если это дрон-охотник, проверяем настройки
                        if (isHunterDronePart)
                        {
                            // Пытаемся определить тип дрона по defName части
                            string droneDefName = GetDroneDefNameFromPart(partParm.def.defName);
                            
                            // Если удалось определить тип дрона, проверяем настройки
                            if (!string.IsNullOrEmpty(droneDefName) && !HunterDroneMod.IsDroneEnabled(droneDefName))
                            {
                                // Дрон отключен в настройках - пропускаем его
                                continue;
                            }
                        }

                        int spawnCount = 1;

                        // Определяем количество для спавна
                        if (partParm.countRange != IntRange.Invalid)
                        {
                            spawnCount = partParm.countRange.RandomInRange;
                        }
                        else
                        {
                            // Проверяем шанс спавна
                            if (!Rand.Chance(partParm.chance))
                            {
                                continue;
                            }
                        }

                        // Определяем эффективные очки угрозы
                        float effectiveThreatPoints = threatPointsValue;
                        if (partParm.threatPointsRange != IntRange.Invalid)
                        {
                            effectiveThreatPoints = partParm.threatPointsRange.RandomInRange;
                        }

                        // Спавним части
                        for (int i = 0; i < spawnCount; i++)
                        {
                            try
                            {
                                partParm.def.Worker.FillRoom(map, room, faction, effectiveThreatPoints);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error($"[MoreHunterDrones] Ошибка при заполнении комнаты частью {partParm.def?.defName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Возвращаем false, чтобы пропустить оригинальный метод
            return false;
        }

        /// Пытается определить defName дрона по названию части комнаты
        /// <param name="partDefName">Название части комнаты</param>
        /// <returns>DefName дрона или null, если не удалось определить</returns>
        private static string GetDroneDefNameFromPart(string partDefName)
        {
            if (string.IsNullOrEmpty(partDefName))
                return null;

            // Маппинг частей комнат на типы дронов
            // Это нужно адаптировать под ваши реальные названия частей
            var partToDroneMapping = new Dictionary<string, string>
            {
                { "HunterDroneToxic", "Drone_HunterToxic" },
                { "HunterDroneAntigrainWarhead", "Drone_HunterAntigrainWarhead" },
                { "HunterDroneIncendiary", "Drone_HunterIncendiary" },
                { "HunterDroneEMP", "Drone_HunterEMP" },
                { "HunterDroneSmoke", "Drone_HunterSmoke" }
            };

            // Прямое совпадение
            if (partToDroneMapping.TryGetValue(partDefName, out string droneDefName))
            {
                return droneDefName;
            }

            // Поиск по частичному совпадению
            foreach (var mapping in partToDroneMapping)
            {
                if (partDefName.Contains(mapping.Key) || mapping.Key.Contains(partDefName))
                {
                    return mapping.Value;
                }
            }

            // Если не удалось найти точное совпадение, попробуем извлечь тип из названия
            if (partDefName.StartsWith("HunterDrone"))
            {
                string droneType = partDefName.Substring("HunterDrone".Length);
                return $"Drone_Hunter{droneType}";
            }

            return null;
        }
    }
}