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
                                string originalDesc = "MoreHunterDrones_VerbChargeDesc".Translate(Props?.chargeNoun);
                                string ingredientsDesc = GetIngredientsDescription(chargeComp);

                                verbCommand.defaultDesc = string.IsNullOrEmpty(originalDesc)
                                    ? ingredientsDesc
                                    : originalDesc + "\n" + ingredientsDesc;

                                if (RemainingCharges > 0)
                                {
                                    verbCommand.Disable();
                                    verbCommand.disabledReason = "MoreHunterDrones_IsCharged".Translate(Props?.chargeNoun);
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
                                verbCommand.Disable();
                                verbCommand.disabledReason = "MoreHunterDrones_NoCharged".Translate(Props?.chargeNoun);
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
                string add_text = "MoreHunterDrones_ChargeIngredientsDesc".Translate(Props?.chargeNoun);
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

        public void AddCharge(int amount = 1)
        {
            remainingCharges = System.Math.Min(remainingCharges + amount, MaxCharges);
        }

    }
}
