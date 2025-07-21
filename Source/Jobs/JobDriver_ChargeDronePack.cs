using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using MoreHunterDrones.Comps;
using System.Linq;

namespace MoreHunterDrones.Jobs
{
    public class JobDriver_ChargeDronePack : JobDriver
    {
        private CompChargeDronePack ChargeComp => job.targetA.Thing?.TryGetComp<CompChargeDronePack>();
        private Dictionary<ThingDef, int> Ingredients => ChargeComp?.ChargeIngredients;
        private Toil waitToil;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override string GetReport()
        {
            return "MoreHunterDrones_JobReportString".Translate();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (ChargeComp == null || Ingredients == null)
            {
                Log.Error("[ChargeDronePack] ChargeComp or Ingredients is null in JobDriver_ChargeDronePack");
                yield break;
            }

            // Toil для поиска следующего ингредиента
            var findIngredientToil = new Toil
            {
                initAction = () =>
                {
                    if (AllIngredientsCollected())
                    {
                        JumpToToil(waitToil); // Переходим к ожиданию
                        return;
                    }

                    if (!FindNextIngredientTarget())
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return findIngredientToil;

            // Toil для движения к найденному предмету
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // Toil для подбора предмета
            yield return new Toil
            {
                initAction = () =>
                {
                    Thing targetThing = job.targetB.Thing;
                    if (targetThing != null && targetThing.Spawned)
                    {
                        int needed = GetNeededAmount(targetThing.def);
                        int takeCount = System.Math.Min(targetThing.stackCount, needed);
                        
                        if (takeCount > 0)
                        {
                            Thing takenThing = targetThing.SplitOff(takeCount);
                            if (!pawn.inventory.innerContainer.TryAdd(takenThing))
                            {
                                takenThing.Destroy();
                                Log.Warning($"[ChargeDronePack] Не удалось добавить {targetThing.def.defName} в инвентарь");
                            }
                        }
                    }
                    
                    // Возвращаемся к поиску следующего ингредиента
                    JumpToToil(findIngredientToil);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // Ожидание (имитация процесса зарядки)
            waitToil = Toils_General.Wait(1200).WithProgressBarToilDelay(TargetIndex.A);
            yield return waitToil;

            // Завершение зарядки
            yield return new Toil
            {
                initAction = () =>
                {
                    ChargeComp?.CompleteChargeProcess(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private bool AllIngredientsCollected()
        {
            foreach (var pair in Ingredients)
            {
                int needed = pair.Value;
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == pair.Key).Sum(t => t.stackCount) ?? 0;
                if (inInventory < needed)
                {
                    return false;
                }
            }
            return true;
        }

        private bool FindNextIngredientTarget()
        {
            foreach (var pair in Ingredients)
            {
                int needed = pair.Value;
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == pair.Key).Sum(t => t.stackCount) ?? 0;
                int toCollect = needed - inInventory;

                if (toCollect > 0)
                {
                    Thing thing = GenClosest.ClosestThingReachable(
                        pawn.Position,
                        pawn.Map,
                        ThingRequest.ForDef(pair.Key),
                        PathEndMode.ClosestTouch,
                        TraverseParms.For(pawn),
                        9999,
                        t => !t.IsForbidden(pawn) && t.stackCount > 0
                    );

                    if (thing != null)
                    {
                        job.SetTarget(TargetIndex.B, thing);
                        return true;
                    }
                }
            }
            
            Log.Warning("[ChargeDronePack] Не удалось найти нужные ингредиенты");
            return false;
        }

        private int GetNeededAmount(ThingDef thingDef)
        {
            if (Ingredients.TryGetValue(thingDef, out int needed))
            {
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == thingDef).Sum(t => t.stackCount) ?? 0;
                return needed - inInventory;
            }
            return 0;
        }
    }
}
