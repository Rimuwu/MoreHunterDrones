using RimWorld;
using Verse;

namespace MoreHunterDrones.Buildings
{
    public class Building_TrapReleaseHunter_Toxic : Building_TrapReleaseEntity
    {
        protected override int CountToSpawn => 1;
        protected override PawnKindDef PawnToSpawn => DronPawnsKindDefOf.Drone_HunterToxic;
    };

    public class Building_TrapReleaseHunter_AntigrainWarhead : Building_TrapReleaseEntity
    {
        protected override int CountToSpawn => 1;
        protected override PawnKindDef PawnToSpawn => DronPawnsKindDefOf.Drone_HunterAntigrainWarhead;
    };

    public class Building_TrapReleaseHunter_Incendiary : Building_TrapReleaseEntity
    {
        protected override int CountToSpawn => 1;
        protected override PawnKindDef PawnToSpawn => DronPawnsKindDefOf.Drone_HunterIncendiary;
    };

    public class Building_TrapReleaseHunter_EMP : Building_TrapReleaseEntity
    {
        protected override int CountToSpawn => 1;
        protected override PawnKindDef PawnToSpawn => DronPawnsKindDefOf.Drone_HunterEMP;
    };

    public class Building_TrapReleaseHunter_Smoke : Building_TrapReleaseEntity
    {
        protected override int CountToSpawn => 1;
        protected override PawnKindDef PawnToSpawn => DronPawnsKindDefOf.Drone_HunterSmoke;
    }

}

