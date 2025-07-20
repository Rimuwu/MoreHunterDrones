// Assembly-CSharp, Version=1.6.9323.2541, Culture=neutral, PublicKeyToken=null
// Verse.Verb_LaunchProjectileStaticOneUse
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using static UnityEngine.GraphicsBuffer;


namespace MoreHunterDrones.Verbs
{

    public class Verb_LaunchProjectileStaticOneUse_alt : Verb_LaunchProjectileStatic
    {

        CompApparelVerbOwner_Charged CompApparelVerbOwner => (CompApparelVerbOwner_Charged)base.EquipmentSource.TryGetComp<CompApparelVerbOwner_Charged>();

        public override bool Available()
        {
            int charges = CompApparelVerbOwner?.RemainingCharges ?? 0; // предполагаем, что Charges — это свойство с количеством зарядов

            bool canUse = charges > 0;

            if (!canUse)
            {
                Messages.Message("MoreHunterDrones_NoCharged".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            }

            return canUse;
        }


        protected override bool TryCastShot()
        {
            // Проверка, можно ли произвести выстрел 1 раз, после нажатия

            if (base.TryCastShot())
            {
                if (burstShotsLeft <= 1)
                {
                    SelfConsume();
                }
                return true;
            }
            if (burstShotsLeft < base.BurstShotCount)
            {
                SelfConsume();
            }
            return false;
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {

            // Проверка, можно ли выбрать цель для атаки (каждый тик) (до вызова OnGUI)

            if (!base.ValidateTarget(target, showMessages: true))
            {
                return false;
            }
            if (target.Cell.GetFirstBuilding(Find.CurrentMap) == null)
            {
                return target.Cell.Standable(Find.CurrentMap);
            }
            return false;
        }

        public override void OnGUI(LocalTargetInfo target)
        {

            // Вызывается каждый тик при нажатии кнопки и выборе цели

            Map currentMap = Find.CurrentMap;
            base.OnGUI(target);

            bool canAttack = ValidateTarget(target, showMessages: false) && Available();

            Texture2D UIIcon = (target.Cell.InBounds(currentMap) ? (canAttack ? TexCommand.Attack : TexCommand.CannotShoot) : TexCommand.CannotShoot);
            GenUI.DrawMouseAttachment(UIIcon);
        }

        public override void Notify_EquipmentLost()
        {
            // Вроде как, если отменено, но не смог воспроиести

            base.Notify_EquipmentLost();
            if (state == VerbState.Bursting && burstShotsLeft < base.BurstShotCount)
            {
                SelfConsume();
            }
        }

        public void SelfConsume()
        {
            // Конец выполнения действия
            bool destroyOnEmpty = CompApparelVerbOwner?.Props?.destroyOnEmpty ?? true; // Получаем destroyOnEmpty

            if (destroyOnEmpty && base.EquipmentSource != null && !base.EquipmentSource.Destroyed)
            {
                base.EquipmentSource.Destroy();
            }

        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {

            // Вызывается при выборе цели для атаки (нажата клавиша лкм)

            Job job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStaticReserve, target);
            job.verbToUse = this;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}