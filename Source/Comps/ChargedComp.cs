using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;
using MoreHunterDrones.Verbs;

namespace MoreHunterDrones.Comps
{

    public class CompProperties_ApparelIsCharged : CompProperties_ApparelVerbOwnerCharged
    {

        public CompProperties_ApparelIsCharged()
        {
            compClass = typeof(CompApparelVerbOwner_ChargedReloadable);
        }
    }

    // Наследник CompApparelVerbOwner_Charged с возможностью добавления зарядов
    public class CompApparelVerbOwner_ChargedReloadable : CompApparelVerbOwner_Charged
    {

        public new CompProperties_ApparelVerbOwnerCharged Props => props as CompProperties_ApparelIsCharged;

        public override string GizmoExtraLabel => LabelRemaining;

        public override void PostPostMake()
        {
            base.PostPostMake();
            remainingCharges = MaxCharges;
        }

        public override string CompInspectStringExtra()
        {
            try
            {
                var chargeNoun = Props?.ChargeNounArgument;
                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                return "ChargesRemaining".Translate(chargeNounStr) + ": " + LabelRemaining;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Error in CompInspectStringExtra: {ex.Message}");
                return "Charges: " + LabelRemaining;
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            // Безопасно получаем базовыеstatistics
            var resultStats = new List<StatDrawEntry>();
            
            try
            {
                IEnumerable<StatDrawEntry> baseStats = base.SpecialDisplayStats();
                if (baseStats != null)
                {
                    foreach (StatDrawEntry item in baseStats)
                    {
                        if (item != null)
                        {
                            resultStats.Add(item);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Error getting base SpecialDisplayStats: {ex.Message}");
            }

            // Добавляем нашу статистику с защитой от ошибок
            try
            {
                var chargeNoun = Props?.ChargeNounArgument;
                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                string statName = "Stat_Thing_ReloadChargesRemaining_Name".Translate(chargeNounStr).ToString();
                string statDesc = "Stat_Thing_ReloadChargesRemaining_Desc".Translate(chargeNounStr).ToString();
                
                resultStats.Add(new StatDrawEntry(
                    StatCategoryDefOf.Apparel, 
                    statName, 
                    LabelRemaining, 
                    statDesc, 
                    2749
                ));
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Error creating charge stat entry: {ex.Message}");
                // Возвращаем базовую статистику без перевода в случае ошибки
                resultStats.Add(new StatDrawEntry(
                    StatCategoryDefOf.Apparel, 
                    "Charges", 
                    LabelRemaining, 
                    "Remaining charges", 
                    2749
                ));
            }

            // Возвращаем все статистики
            foreach (var stat in resultStats)
            {
                yield return stat;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingCharges, "remainingCharges", -999);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && remainingCharges == -999)
            {
                remainingCharges = MaxCharges;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            // Показываем ВСЕ кнопки вербов - пусть каждый верб сам решает через Available()
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {

                if (item is Command_VerbTarget verbCommand)
                {
                    // Если это кнопка верба зарядки, добавляем информацию об ингредиентах
                    if (verbCommand.verb is Verb_ChargeDronePack chargeVerb)
                    {
                        // Получаем компонент зарядки для информации об ингредиентах
                        var chargeComp = parent.TryGetComp<CompChargeDronePack>();
                        if (chargeComp?.ChargeIngredients != null && chargeComp.ChargeIngredients.Count > 0)
                        {
                            // Добавляем описание ингредиентов к существующему tooltip
                            try
                            {
                                var chargeNoun = Props?.ChargeNounArgument;
                                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                                string originalDesc = "MoreHunterDrones_VerbChargeDesc".Translate(chargeNounStr);
                                string ingredientsDesc = GetIngredientsDescription(chargeComp);

                                verbCommand.defaultDesc = string.IsNullOrEmpty(originalDesc)
                                    ? ingredientsDesc
                                    : originalDesc + "\n" + ingredientsDesc;

                                if (RemainingCharges > 0)
                                {
                                    verbCommand.Disable();
                                    verbCommand.disabledReason = "MoreHunterDrones_IsCharged".Translate(chargeNounStr);
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Log.Warning($"[MoreHunterDrones] Error setting charge verb desc: {ex.Message}");
                            }
                        }
                    }
                    else if (verbCommand.verb is Verb_LaunchDrone launcVerb) // Исправлено: добавлен else
                    {
                        if (RemainingCharges <= 0) // Исправлено: <= вместо <
                        {
                            try
                            {
                                var chargeNoun = Props?.ChargeNounArgument;
                                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                                verbCommand.Disable();
                                verbCommand.disabledReason = "MoreHunterDrones_NoCharged".Translate(chargeNounStr);
                            }
                            catch (System.Exception ex)
                            {
                                Log.Warning($"[MoreHunterDrones] Error setting launch verb reason: {ex.Message}");
                                verbCommand.Disable();
                                verbCommand.disabledReason = "No charges remaining";
                            }
                        }
                    }
                }

                yield return item;
            }

            // Дебаг кнопка для разработки
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Reload to full";
                command_Action.action = delegate
                {
                    remainingCharges = MaxCharges;
                };
                yield return command_Action;
            }
        }

        private string GetIngredientsDescription(CompChargeDronePack chargeComp)
        {
            if (chargeComp?.ChargeIngredients == null)
            {
                return "No ingredients defined";
            }

            var sb = new StringBuilder();
            try
            {
                var chargeNoun = Props?.ChargeNounArgument;
                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                string add_text = "MoreHunterDrones_ChargeIngredientsDesc".Translate(chargeNounStr);
                sb.AppendLine(add_text);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Error translating ingredients desc: {ex.Message}");
                sb.AppendLine("Required ingredients:");
            }
            
            foreach (var pair in chargeComp.ChargeIngredients)
            {
                if (pair.Key != null)
                {
                    sb.AppendLine($"• {pair.Key.LabelCap}: {pair.Value}");
                }
            }
            
            return sb.ToString().TrimEnd();
        }

        public override string CompTipStringExtra()
        {
            try
            {
                var chargeNoun = Props?.ChargeNounArgument;
                string chargeNounStr = chargeNoun?.ToString() ?? "charges";
                TaggedString taggedString = "Stat_Thing_ReloadChargesRemaining_Name".Translate(chargeNounStr).CapitalizeFirst();
                return $"\n\n{taggedString}: {RemainingCharges} / {MaxCharges}";
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[MoreHunterDrones] Error in CompTipStringExtra: {ex.Message}");
                return $"\n\nCharges: {RemainingCharges} / {MaxCharges}";
            }
        }

        public override void UsedOnce()
        {
            base.UsedOnce();
            if (remainingCharges > 0)
            {
                remainingCharges--;
            }
            if (Props != null && Props.destroyOnEmpty && remainingCharges == 0 && !parent.Destroyed)
            {
                parent.Destroy();
            }
        }

        public void AddCharge(int amount = 1)
        {
            remainingCharges = System.Math.Min(remainingCharges + amount, MaxCharges);
        }

    }
}
