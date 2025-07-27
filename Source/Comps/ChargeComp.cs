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

        // Проверка, что можно начать процесс зарядки (есть ингридиенты на карте и их достаточно)
        public bool CanStartChargeProcess(Pawn pawn)
        {
            // Проверка, что есть ингридиенты для зарядки
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

        // Метод для завершения процесса зарядки
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
                    return;
                }
            }

            // Добавление заряда (если есть компонент заряжаемого)
            var chargedComp = parent.TryGetComp<CompApparelVerbOwner_Charged>();

            if (chargedComp != null)
            {
                // Пробуем привести к нашему наследнику
                var reloadableComp = chargedComp as CompApparelVerbOwner_ChargedReloadable;
                string successMessage = "MoreHunterDrones_SuccessMessage".Translate();

                if (reloadableComp != null)
                {
                    // Используем публичный метод
                    reloadableComp.AddCharge(1);

                    Messages.Message(successMessage, MessageTypeDefOf.PositiveEvent, false);
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

                        Messages.Message(successMessage, MessageTypeDefOf.PositiveEvent, false);
                    }
                    else
                    {
                        Log.Error("[ChargeDronePack] Could not access remainingCharges field via reflection");
                    }
                }
            }
            else
            {
                Log.Error("[ChargeDronePack] ChargedComp not found - cannot add charge");
            }
        }

        // Отображение информации в интерфейсе
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            // Кнопка появляется только если предмет надет на пешку
            if (parent.ParentHolder is Pawn_ApparelTracker apparelTracker)
            {
                var pawn = apparelTracker.pawn;
                if (pawn?.Faction == Faction.OfPlayer)
                {
                    var chargedComp = parent.TryGetComp<CompApparelVerbOwner_Charged>();
                    var command = new Command_Action
                    {
                        defaultLabel = "MoreHunterDrones.ChargeLabel".Translate(),
                        defaultDesc = GetChargeDescription(),
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/ChargePack", true),
                        hotKey = KeyBindingDefOf.Misc4,
                        action = () => TryStartChargeProcess(pawn)
                    };

                    // Проверка доступности
                    if (chargedComp?.RemainingCharges > 0)
                        command.Disable("MoreHunterDrones_PackAlreadyCharged".Translate());
                    else if (!CanStartChargeProcess(pawn))
                        command.Disable("MoreHunterDrones_CannotCollectItems".Translate());
                    yield return command;
                }
            }
        }

        private void TryStartChargeProcess(Pawn pawn)
        {
            var chargedComp = parent.TryGetComp<CompApparelVerbOwner_Charged>();
            if (chargedComp?.RemainingCharges > 0)
            {
                Messages.Message("MoreHunterDrones_PackAlreadyCharged".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!CanStartChargeProcess(pawn))
            {
                Messages.Message("MoreHunterDrones_CannotCollectItems".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            StartChargeJob(pawn);
        }

        private string GetChargeDescription()
        {
            if (ChargeIngredients == null || ChargeIngredients.Count == 0)
                return "No ingredients required";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MoreHunterDrones_ChargePackDesc".Translate());
            sb.AppendLine();
            sb.AppendLine("MoreHunterDrones_ChargeIngredientsDesc".Translate());
            foreach (var pair in ChargeIngredients)
            {
                if (pair.Key != null)
                    sb.AppendLine($"• {pair.Key.LabelCap}: {pair.Value}");
            }
            return sb.ToString().TrimEnd();
        }
    }
}