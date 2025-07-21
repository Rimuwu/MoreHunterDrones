using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using System.Linq;
using MoreHunterDrones.Jobs;

namespace MoreHunterDrones.Comps
{

    public class CompProperties_ChargeDronePack : CompProperties
    {
        public Dictionary<ThingDef, int> chargeIngredients = new Dictionary<ThingDef, int>();

        public CompProperties_ChargeDronePack()
        {
            compClass = typeof(CompChargeDronePack);
        }
    }

    public class CompChargeDronePack : ThingComp
    {
        public CompProperties_ChargeDronePack Props => (CompProperties_ChargeDronePack)props;
        public Dictionary<ThingDef, int> ChargeIngredients => Props.chargeIngredients;

        public bool CanStartChargeProcess(Pawn pawn)
        {
            if (ChargeIngredients == null || ChargeIngredients.Count == 0)
            {
                Log.Error("[ChargeDronePack] ChargeIngredients is null or empty");
                return false;
            }

            // Проверка доступности всех ингредиентов
            foreach (var pair in ChargeIngredients)
            {
                int needed = pair.Value;
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == pair.Key).Sum(t => t.stackCount) ?? 0;
                int found = inInventory;

                if (found < needed)
                {
                    foreach (var thing in pawn.Map.listerThings.ThingsOfDef(pair.Key))
                    {
                        if (thing.IsForbidden(pawn) || !pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.None))
                            continue;
                        found += thing.stackCount;
                        if (found >= needed) break;
                    }
                }
                
                if (found < needed) 
                {
                    return false;
                }
            }
            
            return true;
        }

        public void StartChargeJob(Pawn pawn)
        {
            // Запускаем работу по сбору ингредиентов и зарядке
            var job = new Job(MyJobDefOf.ChargeDronePack, parent);

            bool jobStarted = pawn.jobs.TryTakeOrderedJob(job);

            if (!jobStarted)
            {
                Log.Warning("[ChargeDronePack] Failed to start charge job");
            }
        }

        public void CompleteChargeProcess(Pawn pawn)
        {
            if (ChargeIngredients == null || ChargeIngredients.Count == 0)
            {
                Log.Warning("[ChargeDronePack] ChargeIngredients is null or empty in CompleteChargeProcess");
                return;
            }

            // Проверяем, что все ингредиенты есть в инвентаре перед удалением
            foreach (var pair in ChargeIngredients)
            {
                int needed = pair.Value;
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == pair.Key).Sum(t => t.stackCount) ?? 0;
                
                if (inInventory < needed)
                {
                    Log.Error($"[ChargeDronePack] Missing ingredient {pair.Key.defName}: needed {needed}, have {inInventory}");
                    Messages.Message("Ошибка: не хватает ингредиентов для зарядки!", MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            // Удаление ингредиентов из инвентаря
            foreach (var pair in ChargeIngredients)
            {
                int toRemove = pair.Value;
                foreach (var thing in pawn.inventory.innerContainer.Where(t => t.def == pair.Key).ToList())
                {
                    int removeCount = System.Math.Min(thing.stackCount, toRemove);
                    if (removeCount > 0)
                    {
                        thing.SplitOff(removeCount).Destroy();
                        toRemove -= removeCount;
                    }
                    if (toRemove <= 0) break;
                }
                
                if (toRemove > 0)
                {
                    Log.Error($"[ChargeDronePack] Could not remove all {pair.Key.defName}: {toRemove} still needed");
                }
            }

            // Добавление заряда (если есть компонент заряжаемого)
            var chargedComp = parent.TryGetComp<CompApparelVerbOwner_Charged>();
            
            if (chargedComp != null)
            {
                // Пробуем привести к нашему наследнику
                var reloadableComp = chargedComp as CompApparelVerbOwner_ChargedReloadable;
                if (reloadableComp != null)
                {
                    // Используем публичный метод
                    reloadableComp.AddCharge(1);
                    Messages.Message("Заряд добавлен!", MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    // Fallback: используем рефлексию для стандартного компонента
                    var chargesField = typeof(CompApparelVerbOwner_Charged).GetField("remainingCharges", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
     
                    if (chargesField != null)
                    {
                        int currentCharges = (int)chargesField.GetValue(chargedComp);
                        chargesField.SetValue(chargedComp, currentCharges + 1);
                        Messages.Message("Заряд добавлен!", MessageTypeDefOf.PositiveEvent, false);
                    }
                    else
                    {
                        Log.Warning("[ChargeDronePack] Could not access remainingCharges field via reflection");
                    }
                }
            }
            else
            {
                Log.Warning("[ChargeDronePack] ChargedComp not found - cannot add charge");
            }
        }
    }
}