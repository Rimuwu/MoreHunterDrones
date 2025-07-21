using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //protected int remainingCharges;

        public new CompProperties_ApparelVerbOwnerCharged Props => props as CompProperties_ApparelIsCharged;

        //public int RemainingCharges => remainingCharges;

        //public int MaxCharges => Props.maxCharges;

        //public string LabelRemaining => $"{RemainingCharges} / {MaxCharges}";

        public override string GizmoExtraLabel => LabelRemaining;

        public override void PostPostMake()
        {
            base.PostPostMake();
            remainingCharges = MaxCharges;
        }

        public override string CompInspectStringExtra()
        {
            return "ChargesRemaining".Translate(Props.ChargeNounArgument) + ": " + LabelRemaining;
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
            if (enumerable != null)
            {
                foreach (StatDrawEntry item in enumerable)
                {
                    yield return item;
                }
            }
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_ReloadChargesRemaining_Name".Translate(Props.ChargeNounArgument), LabelRemaining, "Stat_Thing_ReloadChargesRemaining_Desc".Translate(Props.ChargeNounArgument), 2749);
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
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
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

        public override string CompTipStringExtra()
        {
            TaggedString taggedString = "Stat_Thing_ReloadChargesRemaining_Name".Translate(Props.ChargeNounArgument).CapitalizeFirst();
            return $"\n\n{taggedString}: {RemainingCharges} / {MaxCharges}";
        }

        public override void UsedOnce()
        {
            base.UsedOnce();
            if (remainingCharges > 0)
            {
                remainingCharges--;
            }
            if (Props.destroyOnEmpty && remainingCharges == 0 && !parent.Destroyed)
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
