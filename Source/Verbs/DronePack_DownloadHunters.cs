// Assembly-CSharp, Version=1.6.9323.2541, Culture=neutral, PublicKeyToken=null
// Verse.Verb_LaunchProjectileStaticOneUse
using MoreHunterDrones.Comps;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using MoreHunterDrones.Jobs;

namespace MoreHunterDrones.Verbs
{
    public class Verb_ChargeDronePack : Verb_LaunchProjectileStatic
    {
        public CompApparelVerbOwner_Charged CompApparelVerbOwner => (CompApparelVerbOwner_Charged)base.EquipmentSource.TryGetComp<CompApparelVerbOwner_Charged>();

        public override bool Available()
        {
            int charges = CompApparelVerbOwner?.RemainingCharges ?? 0;

            if (charges > 0) 
            {
                Messages.Message("Ханетр пак заряжнен!", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Проверяем, может ли пешка поднять все ингредиенты
            var pawn = CasterPawn;
            if (pawn == null) return false;

            var chargeComp = base.EquipmentSource.TryGetComp<CompChargeDronePack>();
            if (chargeComp == null || chargeComp.ChargeIngredients == null) return false;

            float totalMass = 0f;
            foreach (var pair in chargeComp.ChargeIngredients)
            {
                int needed = pair.Value;
                int inInventory = pawn.inventory?.innerContainer.Where(t => t.def == pair.Key).Sum(t => t.stackCount) ?? 0;
                int toCollect = needed - inInventory;
                
                if (toCollect > 0)
                {
                    totalMass += pair.Key.BaseMass * toCollect;
                }
            }

            float currentMass = pawn.inventory?.innerContainer.Sum(t => t.GetStatValue(StatDefOf.Mass) * t.stackCount) ?? 0f;
            float carryCapacity = pawn.GetStatValue(StatDefOf.CarryingCapacity);

            if (currentMass + totalMass > carryCapacity)
            {
                Messages.Message("Поселенец не может поднять все необходимые ингредиенты - слишком тяжело!", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        protected override bool TryCastShot()
        {
            var pawn = CasterPawn;
            if (pawn == null) 
            {
                Log.Error("[ChargeDronePack] CasterPawn is null");
                return false;
            }

            // Получить компонент зарядки
            var chargeComp = base.EquipmentSource.TryGetComp<CompChargeDronePack>();
            if (chargeComp == null)
            {
                Log.Error("[ChargeDronePack] CompChargeDronePack not found");
                Messages.Message("Компонент зарядки не найден!", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Проверить возможность зарядки
            bool canStart = chargeComp.CanStartChargeProcess(pawn);
            
            if (!canStart)
            {
                Messages.Message("Поселенец не может подобрать все необходимые предметы для создания нового охотника.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Запустить процесс зарядки через компонент
            chargeComp.StartChargeJob(pawn);
            
            return true;
        }
    }
}