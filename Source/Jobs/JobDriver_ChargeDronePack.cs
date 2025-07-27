using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using MoreHunterDrones.Comps;
using System.Linq;

namespace MoreHunterDrones.Jobs
{ 

    /// JobDriver для выполнения задачи зарядки дрон-пака.
    /// В RimWorld JobDriver управляет поведением персонажа при выполнении определенной работы.
    /// Этот класс координирует процесс сбора ингредиентов и зарядки дрон-пака
    public class JobDriver_ChargeDronePack : JobDriver
    {
        /// Свойства для быстрого доступа к компонентам
        /// Получает компонент зарядки дрон-пака из целевого объекта (targetA)
        private CompChargeDronePack ChargeComp => job.targetA.Thing?.TryGetComp<CompChargeDronePack>();

        /// Словарь ингредиентов, необходимых для зарядки (ThingDef -> количество)
        private Dictionary<ThingDef, int> Ingredients => ChargeComp?.ChargeIngredients;

        /// Ссылка на Toil ожидания, используется для перехода к процессу зарядки
        private Toil waitToil;

        /// Метод резервирования ресурсов перед началом работы.
        /// В данном случае возвращает true, так как специальное резервирование не требуется
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        /// Возвращает текст, отображаемый в интерфейсе при выполнении этой 
        public override string GetReport()
        {
            return "MoreHunterDrones_JobReportString".Translate();
        }

        /// Основной метод, определяющий последовательность действий (Toil'ов) для выполнения работы.
        /// В RimWorld Toil - это атомарное действие в рамках работы (движение, ожидание, взаимодействие и т.д
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Проверка валидности данных перед началом работы
            if (ChargeComp == null || Ingredients == null)
            {
                Log.Error("[ChargeDronePack] ChargeComp or Ingredients is null in JobDriver_ChargeDronePack");
                yield break; // Прерываем выполнение, если данные недоступны
            }

            // ========== TOIL 1: ПОИСК СЛЕДУЮЩЕГО ИНГРЕДИЕНТА ==========
            // Этот Toil проверяет, все ли ингредиенты собраны, и ищет следующий недостающий
            var findIngredientToil = new Toil
            {
                initAction = () =>
                {
                    Log.Message($"[ChargeDronePack] Этап: Поиск следующего ингредиента (pawn={pawn}, job={job})");
                    // Если все ингредиенты уже собраны, переходим к процессу зарядки
                    if (AllIngredientsCollected())
                    {
                        JumpToToil(waitToil); // Переходим к ожиданию (зарядке)
                        return;
                    }

                    // Ищем следующий недостающий ингредиент
                    if (!FindNextIngredientTarget())
                    {
                        // Если не удалось найти нужные ингредиенты, завершаем работу с ошибкой
                        EndJobWith(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant // Выполняется мгновенно
            };
            yield return findIngredientToil;

            // ========== TOIL 2: ДВИЖЕНИЕ К НАЙДЕННОМУ ПРЕДМЕТУ ==========
            // Используем стандартный Toil для движения к объекту, указанному в targetB
            var gotoToil = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B); // Прерываем работу, если объект исчез или запрещен
            gotoToil.initAction += () =>
            {
                Log.Message($"[ChargeDronePack] Этап: Движение к предмету (pawn={pawn}, targetB={job.targetB.Thing})");
            };
            yield return gotoToil;

            // ========== TOIL 3: ПОДБОР ПРЕДМЕТА ==========
            // Этот Toil забирает нужное количество предмета и помещает его в инвентарь персонажа
            var pickUpToil = new Toil
            {
                initAction = () =>
                {
                    Log.Message($"[ChargeDronePack] Этап: Подбор предмета (pawn={pawn}, targetB={job.targetB.Thing})");
                    Thing targetThing = job.targetB.Thing; // Получаем целевой предмет
                    if (targetThing != null && targetThing.Spawned)
                    {
                        // Вычисляем, сколько нам нужно взять
                        int needed = GetNeededAmount(targetThing.def);
                        int takeCount = System.Math.Min(targetThing.stackCount, needed);

                        if (takeCount > 0)
                        {
                            // Отделяем нужное количество от стака
                            Thing takenThing = targetThing.SplitOff(takeCount);
                            
                            // Пытаемся добавить в инвентарь персонажа
                            if (!pawn.inventory.innerContainer.TryAdd(takenThing))
                            {
                                // Если не удалось добавить в инвентарь, уничтожаем предмет
                                Log.Warning($"[ChargeDronePack] Не удалось добавить {targetThing.def.defName} в инвентарь");
                            }
                        }
                    }
                    
                    // После подбора возвращаемся к поиску следующего ингредиента
                    // Это создает цикл: поиск -> движение -> подбор -> поиск...
                    JumpToToil(findIngredientToil);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return pickUpToil;

            // ========== TOIL 4: ОЖИДАНИЕ (ПРОЦЕСС ЗАРЯДКИ) ==========
            // Имитирует процесс зарядки с прогресс-баром
            waitToil = Toils_General.Wait(1200).WithProgressBarToilDelay(TargetIndex.A);
            // 1200 тиков ≈ 20 секунд в игре (60 тиков = 1 секунда)
            // WithProgressBarToilDelay показывает прогресс-бар над targetA (дрон-паком)
            waitToil.initAction += () =>
            {
                Log.Message($"[ChargeDronePack] Этап: Ожидание/зарядка (pawn={pawn}, targetA={job.targetA.Thing})");
            };
            yield return waitToil;

            // ========== TOIL 5: ЗАВЕРШЕНИЕ ЗАРЯДКИ ==========
            // Финальный Toil, который завершает процесс зарядки
            var finishToil = new Toil
            {
                initAction = () =>
                {
                    Log.Message($"[ChargeDronePack] Этап: Завершение зарядки (pawn={pawn}, targetA={job.targetA.Thing})");
                    // Вызываем метод завершения зарядки в компоненте
                    ChargeComp?.CompleteChargeProcess(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finishToil;
        }

        /// Проверяет, все ли необходимые ингредиенты собраны в инвентаре 
        /// <returns>true, если все ингредиенты собраны в нужном количестве</returns>
        private bool AllIngredientsCollected()
        {
            // Проходим по всем требуемым ингредиентам
            foreach (var pair in Ingredients)
            {
                int needed = pair.Value; // Сколько нужно
                
                // Подсчитываем, сколько этого ингредиента есть в инвентаре
                int inInventory = pawn.inventory?.innerContainer
                    .Where(t => t.def == pair.Key) // Фильтруем по типу предмета
                    .Sum(t => t.stackCount) ?? 0;  // Суммируем количество
                
                // Если хотя бы одного ингредиента недостаточно, возвращаем false
                if (inInventory < needed)
                {
                    return false;
                }
            }
            return true; // Все ингредиенты собраны
        }

        /// Ищет ближайший доступный предмет среди недостающих 
        /// <returns>true, если найден подходящий предмет для сбора</returns>
        private bool FindNextIngredientTarget()
        {
            // Проходим по всем требуемым ингредиентам
            foreach (var pair in Ingredients)
            {
                int needed = pair.Value; // Сколько нужно всего
                
                // Считаем, сколько уже есть в инвентаре
                int inInventory = pawn.inventory?.innerContainer
                    .Where(t => t.def == pair.Key)
                    .Sum(t => t.stackCount) ?? 0;
                
                int toCollect = needed - inInventory; // Сколько еще нужно собрать

                // Если этого ингредиента достаточно, переходим к следующему
                if (toCollect > 0)
                {
                    // Ищем ближайший доступный предмет этого типа на карте
                    Thing thing = GenClosest.ClosestThingReachable(
                        pawn.Position,                          // Начальная позиция поиска
                        pawn.Map,                              // Карта для поиска
                        ThingRequest.ForDef(pair.Key),         // Тип искомого предмета
                        PathEndMode.ClosestTouch,              // Как близко нужно подойти
                        TraverseParms.For(pawn),               // Параметры передвижения персонажа
                        9999,                                  // Максимальная дистанция поиска
                        t => !t.IsForbidden(pawn) || !pawn.CanReach(t, PathEndMode.ClosestTouch, Danger.None) // Условие
                    );

                    // Если нашли подходящий предмет
                    if (thing != null)
                    {
                        // Устанавливаем его как цель B для следующих Toil'ов
                        job.SetTarget(TargetIndex.B, thing);
                        return true;
                    }
                }
            }
            
            // Если не удалось найти ни одного нужного предмета
            Log.Warning("[ChargeDronePack] Не удалось найти нужные ингредиенты");
            return false;
        }

        /// Вычисляет, сколько предметов определенного типа еще нужно 
        /// <param name="thingDef">Тип предмета</param>
        /// <returns>Количество предметов, которое нужно добавить в инвентарь</returns>
        private int GetNeededAmount(ThingDef thingDef)
        {
            // Проверяем, есть ли этот предмет в списке требуемых ингредиентов
            if (Ingredients.TryGetValue(thingDef, out int needed))
            {
                // Считаем, сколько уже есть в инвентаре
                int inInventory = pawn.inventory?.innerContainer
                    .Where(t => t.def == thingDef)
                    .Sum(t => t.stackCount) ?? 0;
                
                // Возвращаем разность: сколько нужно - сколько есть
                return needed - inInventory;
            }
            return 0; // Если предмет не нужен для зарядки
        }
    }
}
