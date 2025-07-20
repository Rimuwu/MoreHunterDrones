using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace MoreHunterDrones.Comps
{

    // Компоненты
    // Дрон, который не наносит урон вокруг, а взрывается эффектом, как граната
    public class CompProperties_ExplosiveHunterDrone : CompProperties
    {

        public float explosionRadius = 1.9f;
        public DamageDef explosionDamageType;
        public Thing instigator;
        public int damAmout = -1;
        public float armorPenetration = -1f;
        public SoundDef explosionSound = null;
        public ThingDef weapon = null;
        public ThingDef projectile = null;
        public Thing intendedTarget = null;
        public ThingDef postExplosionSpawnThingDef = null;
        public float postExplosionSpawnChance = 0f;
        public int postExplosionSpawnThingCount = 1;
        public GasType? postExplosionGasType = null;
        public float? postExplosionGasRadiusOverride = null;
        public int postExplosionGasAmount = 255;
        public bool applyDamageToExplosionCellsNeighbors = false;
        public ThingDef preExplosionSpawnThingDef = null;
        public float preExplosionSpawnChance = 0f;
        public int preExplosionSpawnThingCount = 1;
        public float chanceToStartFire = 0f; 
        public bool damageFalloff = false;
        public float? direction = null;
        public List<Thing> ignoredThings = null;
        public FloatRange? affectedAngle = null;
        public bool doVisualEffects = true;
        public float propagationSpeed = 1f;
        public float excludeRadius = 0f;
        public bool doSoundEffects = true;
        public ThingDef postExplosionSpawnThingDefWater = null;
        public float screenShakeFactor = 1f;
        public SimpleCurve flammabilityChanceCurve = null;
        public List<IntVec3> overrideCells = null;
        public ThingDef postExplosionSpawnSingleThingDef = null;
        public ThingDef preExplosionSpawnSingleThingDef = null;

        public CompProperties_ExplosiveHunterDrone()
        {
            compClass = typeof(CompHunterDrone_explosive);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
        }
    }

    // Рабочие классы
    public class CompHunterDrone_explosive : ThingComp
    {
        private bool wickStarted;

        private int wickTicks;

        [Unsaved(false)]
        private Sustainer wickSoundSustainer;

        [Unsaved(false)]
        private OverlayHandle? overlayBurningWick;

        private CompProperties_ExplosiveHunterDrone Props => (CompProperties_ExplosiveHunterDrone)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref wickStarted, "wickStarted", defaultValue: false);
            Scribe_Values.Look(ref wickTicks, "wickTicks", 0);
        }

        public override void CompTickInterval(int delta)
        {

            // Изменение кода для совместимости с C# 7.3  
            if (!wickStarted && parent.IsHashIntervalTick(30, delta) && parent is Pawn pawn && pawn.Spawned && !pawn.Downed && PawnUtility.EnemiesAreNearby(pawn, 9, passDoors: true, 1.5f))
                StartWick();

        }

        public override void CompTick()
        {
            if (wickStarted)
            {
                if (wickSoundSustainer != null)
                {
                    wickSoundSustainer.Maintain();
                }
                wickTicks--;
                if (wickTicks <= 0)
                {
                    Detonate();
                }
            }
        }

        private void StartWick()
        {
            if (!wickStarted)
            {
                wickStarted = true;
                overlayBurningWick = parent.Map.overlayDrawer.Enable(parent, OverlayTypes.BurningWick);
                wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
                wickTicks = 120;
            }
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            if (dinfo.HasValue)
            {
                Detonate(prevMap);
            }
        }

        public void Detonate(Map map = null)
        {
            IntVec3 position = parent.Position;
            if (map == null)
            {
                map = parent.Map;
            }
            if (!parent.Destroyed)
            {
                parent.Destroy();
            }

            GenExplosion.DoExplosion(
                position, // позиция взрыва
                map, // карта
                Props.explosionRadius, // радиус взрыва
                Props.explosionDamageType, // тип урона
                Props.instigator, // инициатор
                Props.damAmout, // урон
                Props.armorPenetration, // бронепробиваемость
                Props.explosionSound, // Звук взрыва
                Props.weapon, // оружие
                Props.projectile, // снаряд
                Props.intendedTarget, // цель
                Props.postExplosionSpawnThingDef, // предмет после взрыва
                Props.postExplosionSpawnChance, // шанс появления предмета
                Props.postExplosionSpawnThingCount, // количество предметов
                Props.postExplosionGasType, // тип газа после взрыва
                Props.postExplosionGasRadiusOverride, // радиус газа
                Props.postExplosionGasAmount, // количество газа
                Props.applyDamageToExplosionCellsNeighbors, // урон соседним клеткам
                Props.preExplosionSpawnThingDef, // предмет до взрыва
                Props.preExplosionSpawnChance, // шанс появления предмета до взрыва
                Props.preExplosionSpawnThingCount, // количество предметов до взрыва
                Props.chanceToStartFire, // шанс поджога
                Props.damageFalloff, // затухание урона
                Props.direction, // направление
                Props.ignoredThings, // игнорируемые объекты
                Props.affectedAngle, // угол воздействия
                Props.doVisualEffects, // визуальные эффекты
                Props.propagationSpeed, // скорость распространения
                Props.excludeRadius, // радиус исключения
                Props.doSoundEffects, // звуковые эффекты
                Props.postExplosionSpawnThingDefWater, // предмет после взрыва (вода)
                Props.screenShakeFactor, // сила тряски экрана
                Props.flammabilityChanceCurve, // кривая воспламеняемости
                Props.overrideCells, // переопределённые клетки
                Props.postExplosionSpawnSingleThingDef, // одиночный предмет после взрыва
                Props.preExplosionSpawnSingleThingDef // одиночный предмет до взрыва
            );


            if (base.ParentHolder is Corpse corpse)
            {
                corpse.Destroy();
            }
        }
    }
}