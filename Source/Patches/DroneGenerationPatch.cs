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
            // �������� ����������� �������
            var roomDef = __instance.RoomDef;
            if (roomDef == null) return true;

            // ������� ���� ������
            float threatPointsValue = threatPoints ?? 300f;

            if (threatPoints.HasValue && roomDef.threatPointsScaleCurve != null)
            {
                threatPointsValue = roomDef.threatPointsScaleCurve.Evaluate(threatPoints.Value);
            }

            // �������� ����� ������� ����� ��������� ����� PartParms
            var partParmsMethod = typeof(RoomContentsWorker).GetMethod("PartParms",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (partParmsMethod != null)
            {
                var partParms = (IEnumerable<LayoutPartParms>)partParmsMethod.Invoke(__instance, new object[] { room });

                foreach (LayoutPartParms partParm in partParms)
                {
                    if (partParm?.def?.Worker == null) continue;

                    // ���������, ������ �� ��� ����� ����������� �� ������� �����
                    if (partParm.def.Worker.FillOnPost == post)
                    {
                        // ���������, �������� �� ��� ������-���������
                        bool isHunterDronePart = partParm.def.defName.StartsWith("HunterDrone");

                        // ���� ��� ����-�������, ��������� ���������
                        if (isHunterDronePart)
                        {
                            // �������� ���������� ��� ����� �� defName �����
                            string droneDefName = GetDroneDefNameFromPart(partParm.def.defName);
                            
                            // ���� ������� ���������� ��� �����, ��������� ���������
                            if (!string.IsNullOrEmpty(droneDefName) && !HunterDroneMod.IsDroneEnabled(droneDefName))
                            {
                                // ���� �������� � ���������� - ���������� ���
                                continue;
                            }
                        }

                        int spawnCount = 1;

                        // ���������� ���������� ��� ������
                        if (partParm.countRange != IntRange.Invalid)
                        {
                            spawnCount = partParm.countRange.RandomInRange;
                        }
                        else
                        {
                            // ��������� ���� ������
                            if (!Rand.Chance(partParm.chance))
                            {
                                continue;
                            }
                        }

                        // ���������� ����������� ���� ������
                        float effectiveThreatPoints = threatPointsValue;
                        if (partParm.threatPointsRange != IntRange.Invalid)
                        {
                            effectiveThreatPoints = partParm.threatPointsRange.RandomInRange;
                        }

                        // ������� �����
                        for (int i = 0; i < spawnCount; i++)
                        {
                            try
                            {
                                partParm.def.Worker.FillRoom(map, room, faction, effectiveThreatPoints);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Error($"[MoreHunterDrones] ������ ��� ���������� ������� ������ {partParm.def?.defName}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // ���������� false, ����� ���������� ������������ �����
            return false;
        }

        /// �������� ���������� defName ����� �� �������� ����� �������
        /// <param name="partDefName">�������� ����� �������</param>
        /// <returns>DefName ����� ��� null, ���� �� ������� ����������</returns>
        private static string GetDroneDefNameFromPart(string partDefName)
        {
            if (string.IsNullOrEmpty(partDefName))
                return null;

            // ������� ������ ������ �� ���� ������
            // ��� ����� ������������ ��� ���� �������� �������� ������
            var partToDroneMapping = new Dictionary<string, string>();
            
            // ��������� ������� ����� ������ ��� ������� Biotech DLC
            if (ModsConfig.BiotechActive)
            {
                partToDroneMapping.Add("HunterDroneToxic", "Drone_HunterToxic");
            }
            
            partToDroneMapping.Add("HunterDroneAntigrainWarhead", "Drone_HunterAntigrainWarhead");
            partToDroneMapping.Add("HunterDroneIncendiary", "Drone_HunterIncendiary");
            partToDroneMapping.Add("HunterDroneEMP", "Drone_HunterEMP");
            partToDroneMapping.Add("HunterDroneSmoke", "Drone_HunterSmoke");

            // ������ ����������
            if (partToDroneMapping.TryGetValue(partDefName, out string droneDefName))
            {
                return droneDefName;
            }

            // ����� �� ���������� ����������
            foreach (var mapping in partToDroneMapping)
            {
                if (partDefName.Contains(mapping.Key) || mapping.Key.Contains(partDefName))
                {
                    return mapping.Value;
                }
            }

            // ���� �� ������� ����� ������ ����������, ��������� ������� ��� �� ��������
            if (partDefName.StartsWith("HunterDrone"))
            {
                string droneType = partDefName.Substring("HunterDrone".Length);
                return $"Drone_Hunter{droneType}";
            }

            return null;
        }
    }
}