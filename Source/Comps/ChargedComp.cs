using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

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
            // Показываем только кнопки вербов запуска дронов (не зарядки)
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                if (item is Command_VerbTarget verbCommand)
                {
                    // Пропускаем кнопку зарядки - она теперь управляется CompChargeDronePack
                    // Verb_ChargeDronePack больше не используется
                    // Оставляем только Verb_LaunchDrone
                    // Если это кнопка запуска, проверяем заряды
                    if (verbCommand.verb.GetType().Name == "Verb_LaunchDrone")
                    {
                        if (RemainingCharges <= 0)
                        {

                            verbCommand.Disable();
                            verbCommand.disabledReason = "MoreHunterDrones_NoCharged".Translate(Props?.chargeNoun);

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

        public void AddCharge(int amount = 1)
        {
            remainingCharges = System.Math.Min(remainingCharges + amount, MaxCharges);
        }

    }
}
