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
            // Попытка сгенерировать дронов в комнате
            var roomDef = __instance.RoomDef;
            if (roomDef == null) return true;

            // Базовое значение угрозы
            float threatPointsValue = threatPoints ?? 300f;

            if (threatPoints.HasValue && roomDef.threatPointsScaleCurve != null)
            {
                threatPointsValue = roomDef.threatPointsScaleCurve.Evaluate(threatPoints.Value);
            }

            // Получаем приватный метод PartParms
            var partParmsMethod = typeof(RoomContentsWorker).GetMethod("PartParms",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (partParmsMethod != null)
            {
                var partParms = (IEnumerable<LayoutPartParms>)partParmsMethod.Invoke(__instance, new object[] { room });

                foreach (LayoutPartParms partParm in partParms)
                {
                    if (partParm?.def?.Worker == null) continue;

                    // Проверяем, подходит ли эта часть для текущей стадии генерации
                    if (partParm.def.Worker.FillOnPost == post)
                    {
                        // Проверяем, является ли это частью дрона-охотника или осы
                        bool isHunterDronePart = partParm.def.defName.StartsWith("HunterDrone") || partParm.def.defName.StartsWith("WaspDrone");

                        // Если это дрон-охотник или оса, проверяем разрешение
                        if (isHunterDronePart)
                        {
                            // Получаем defName дрона по defName части
                            string droneDefName = GetDroneDefNameFromPart(partParm.def.defName);
                            
                            // Если дрон запрещён, заменяем на базового
                            if (!string.IsNullOrEmpty(droneDefName) && !HunterDroneMod.IsDroneEnabled(droneDefName))
                            {
                                if (partParm.def.defName.StartsWith("HunterDrone"))
                                {
                                    partParm.def = DefDatabase<RoomPartDef>.GetNamed("HunterDrone", true);
                                }
                                else if (partParm.def.defName.StartsWith("WaspDrone"))
                                {
                                    partParm.def = DefDatabase<RoomPartDef>.GetNamed("WaspDrone", true);
                                }
                                else
                                {
                                    Log.Warning($"[MoreHunterDrones] Не удалось определить часть {partParm.def?.defName} для замены, пропускаем.");
                                    continue; // Неизвестная часть, пропускаем
                                }
                            }
                        }

                        int spawnCount = 1;

                        // Определяем количество спавна
                        if (partParm.countRange != IntRange.Invalid)
                        {
                            spawnCount = partParm.countRange.RandomInRange;
                        }
                        else
                        {
                            // Проверяем шанс появления
                            if (!Rand.Chance(partParm.chance))
                            {
                                continue;
                            }
                        }

                        // Определяем угрозу для этой части
                        float effectiveThreatPoints = threatPointsValue;
                        if (partParm.threatPointsRange != IntRange.Invalid)
                        {
                            effectiveThreatPoints = partParm.threatPointsRange.RandomInRange;
                        }

                        // Спавним часть
                        for (int i = 0; i < spawnCount; i++)
                        {
                            try
                            {
                                partParm.def.Worker.FillRoom(map, room, faction, effectiveThreatPoints);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error($"[MoreHunterDrones] Ошибка при генерации части {partParm.def?.defName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Возвращаем false, чтобы отменить стандартное выполнение
            return false;
        }

        /// Получить defName дрона по названию части комнаты
        /// <param name="partDefName">Название части комнаты</param>
        /// <returns>DefName дрона или null, если не удалось определить</returns>
        private static string GetDroneDefNameFromPart(string partDefName)
        {
            if (string.IsNullOrEmpty(partDefName))
                return null;

            // Словарь соответствий частей и дронов
            // Можно расширять для новых типов дронов
            var partToDroneMapping = new Dictionary<string, string>();
            
            // Добавляем токсичного дрона только если активен Biotech DLC
            if (ModsConfig.BiotechActive)
            {
                partToDroneMapping.Add("HunterDroneToxic", "Drone_HunterToxic");
            }
            
            partToDroneMapping.Add("HunterDroneAntigrainWarhead", "Drone_HunterAntigrainWarhead");
            partToDroneMapping.Add("HunterDroneIncendiary", "Drone_HunterIncendiary");
            partToDroneMapping.Add("HunterDroneEMP", "Drone_HunterEMP");
            partToDroneMapping.Add("HunterDroneSmoke", "Drone_HunterSmoke");

            // Прямое соответствие
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

            // Если это базовый HunterDrone, формируем имя дрона по шаблону
            if (partDefName.StartsWith("HunterDrone"))
            {
                string droneType = partDefName.Substring("HunterDrone".Length);
                return $"Drone_Hunter{droneType}";
            }

            return null;
        }
    }
}