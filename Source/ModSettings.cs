using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using UnityEngine;
using Verse;

namespace MoreHunterDrones
{

    // Класс для хранения информации о каждом дроне
    public class DroneInfo
    {
        public string defName;
        public string label;
        public string texPath;
        public bool enabled;

        public DroneInfo(string defName, string label, string texPath, bool enabled = true)
        {
            this.defName = defName;
            this.label = label;
            this.texPath = texPath;
            this.enabled = enabled;
        }
    }

    // Класс настроек мода, который будет хранить состояние включения/выключения дронов
    public class HunterDroneSettings : ModSettings
    {
        // Словарь для хранения состояния включения/выключения дронов (инициализируем его, для сохранения в памяти)
        public Dictionary<string, bool> droneEnabledStates = new Dictionary<string, bool>();

        // Получение всех дронов из DronPawnsKindDefOf
        private List<PawnKindDef> GetAllDroneKinds()
        {
            var droneKinds = new List<PawnKindDef>();
            
            // Используем рефлексию для получения всех полей из DronPawnsKindDefOf
            var fields = typeof(DronPawnsKindDefOf).GetFields(BindingFlags.Public | BindingFlags.Static);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(PawnKindDef))
                {
                    var pawnKindDef = (PawnKindDef)field.GetValue(null);
                    if (pawnKindDef != null)
                    {
                        droneKinds.Add(pawnKindDef);
                    }
                }
            }
            
            return droneKinds;
        }
        
        // Инициализация дронов на основе DronPawnsKindDefOf
        private void InitializeDefaultDrones()
        {
            var allDroneKinds = GetAllDroneKinds();
            
            foreach (var droneKind in allDroneKinds)
            {
                if (!droneEnabledStates.ContainsKey(droneKind.defName))
                {
                    droneEnabledStates[droneKind.defName] = true; // По умолчанию все включены
                }
            }
        }

        public bool IsDroneEnabled(string defName)
        {
            InitializeDefaultDrones();
            return droneEnabledStates.TryGetValue(defName, out bool enabled) ? enabled : true;
        }

        public void SetDroneEnabled(string defName, bool enabled)
        {
            InitializeDefaultDrones();
            
            // Проверяем, изменилось ли состояние
            bool currentState = droneEnabledStates.TryGetValue(defName, out bool current) ? current : true;
            if (currentState == enabled)
                return; // Нет изменений

            droneEnabledStates[defName] = enabled;

            // Немедленно обновляем кэш в DroneSpawnManager
            //DroneSpawnManager.RefreshDroneState(defName, enabled);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref droneEnabledStates, "droneEnabledStates", LookMode.Value, LookMode.Value);
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit) // Проверяем после загрузки
            {
                InitializeDefaultDrones();
                // Обновляем DroneSpawnManager после загрузки
                //DroneSpawnManager.RefreshAllDroneStates();
            }
        }
    }

    // Страница настроек мода
    public class HunterDroneMod : Mod
    {
        HunterDroneSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly float ICON_SIZE = 32f;
        private static readonly float ROW_HEIGHT = 40f;
        private static readonly float COLUMN_SPACING = 10f;
        private static readonly int COLUMNS_COUNT = 2;

        public HunterDroneMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<HunterDroneSettings>();
        }

        // Получение всех дронов из DronPawnsKindDefOf
        private List<DroneInfo> GetAllDrones()
        {
            var droneInfos = new List<DroneInfo>();
            
            // Используем рефлексию для получения всех полей из DronPawnsKindDefOf
            var fields = typeof(DronPawnsKindDefOf).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(PawnKindDef))
                {
                    var pawnKindDef = (PawnKindDef)field.GetValue(null);
                    if (pawnKindDef != null)
                    {
                        // Извлекаем путь к текстуре из PawnKindDef
                        string texPath = GetDroneTexturePath(pawnKindDef);
                        
                        var droneInfo = new DroneInfo(
                            pawnKindDef.defName,
                            pawnKindDef.LabelCap,
                            texPath
                        );
                        
                        droneInfos.Add(droneInfo);
                    }
                }
            }
            
            // Сортируем по названию для стабильного порядка
            return droneInfos.OrderBy(d => d.label).ToList();
        }

        // Метод для получения пути к текстуре дрона
        private string GetDroneTexturePath(PawnKindDef pawnKindDef)
        {
            // Пытаемся получить путь к текстуре из lifeStages
            if (pawnKindDef.lifeStages != null && pawnKindDef.lifeStages.Count > 0)
            {
                var firstStage = pawnKindDef.lifeStages[0];
                if (firstStage.bodyGraphicData != null && !firstStage.bodyGraphicData.texPath.NullOrEmpty())
                {
                    return firstStage.bodyGraphicData.texPath;
                }
            }
            
            // Если не удалось получить из lifeStages, строим путь на основе defName
            string defName = pawnKindDef.defName;
            if (defName.StartsWith("Drone_Hunter"))
            {
                return $"Pawns/{defName}/{defName}";
            }

            // Запасной вариант
            return "UI/Icons/Unknown";
        }

        // Переопределяем метод для отрисовки окна настроек
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect); // начинаем новый список

            listingStandard.GapLine(ROW_HEIGHT);

            // Заголовок
            Text.Font = GameFont.Medium;
            listingStandard.Label("MoreHunterDrones.StructureSpawnTitle".Translate()); // Перевод и указать то что это спавн в локациях
            Text.Font = GameFont.Small;
            // Описание
            listingStandard.Label("MoreHunterDrones.StructureSpawnDesc".Translate());

            listingStandard.Gap(12f);

            // Получаем актуальный список дронов
            var allDrones = GetAllDrones();
            
            if (allDrones.Count == 0)
            {
                listingStandard.Label("Дроны не найдены. Убедитесь, что мод загружен правильно.");
                listingStandard.End();
                return;
            }

            // Рассчитываем количество строк (округляем вверх)
            int rowsCount = Mathf.CeilToInt((float)allDrones.Count / COLUMNS_COUNT);

            // Создаем область для прокрутки
            //┌─────────────────────────────────┐ ← scrollRect(видимая область, макс 350px)
            //│ ┌─────────────────────────────┐ │ 
            //│ │     Drone 1[✓]              │ │ ← viewRect(виртуальная область)
            //│ │     Drone 2[✓]              │ │   может быть больше scrollRect
            //│ │     Drone 3[ ]              │ │
            //│ │     Drone 4[✓]              │ │
            //│ │     ...                     │ │
            //│ │     Drone N[]               │ │ ← если дронов много, появится
            //│ └─────────────────────────────┘ │   полоса прокрутки
            //└─────────────────────────────────┘
            //                                 ▲ 16px для скроллбара
            float totalHeight = rowsCount * ROW_HEIGHT + 20f;
            Rect scrollRect = listingStandard.GetRect(Mathf.Min(350f, totalHeight));
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, totalHeight);

            // Начинаем прокрутку
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            // Рассчитываем ширину каждого столбца
            float columnWidth = (viewRect.width - COLUMN_SPACING * (COLUMNS_COUNT - 1)) / COLUMNS_COUNT;

            // Отрисовываем дронов в 2 столбца
            for (int row = 0; row < rowsCount; row++)
            {
                for (int col = 0; col < COLUMNS_COUNT; col++)
                {
                    int droneIndex = row * COLUMNS_COUNT + col;
                    if (droneIndex >= allDrones.Count)
                        break;

                    var drone = allDrones[droneIndex];
                    
                    // Рассчитываем позицию прямоугольника для текущего дрона
                    float x = col * (columnWidth + COLUMN_SPACING);
                    float y = row * ROW_HEIGHT;
                    
                    Rect droneRect = new Rect(x, y, columnWidth, ROW_HEIGHT);
                    DrawDroneRow(droneRect, drone);
                }
            }

            Widgets.EndScrollView();

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        // Метод для отрисовки строки с информацией о дроне
        private void DrawDroneRow(Rect rect, DroneInfo drone)
        {
            bool isEnabled = settings.IsDroneEnabled(drone.defName);
            
            // Проверяем клик по всей строке
            if (Widgets.ButtonInvisible(rect))
            {
                settings.SetDroneEnabled(drone.defName, !isEnabled);
            }

            // Подсветка при наведении мыши на всю строку
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            // Иконка дрона
            Rect iconRect = new Rect(rect.x + 5f, rect.y + (rect.height - ICON_SIZE) / 2f, ICON_SIZE, ICON_SIZE);

            // Попытка загрузить текстуру
            Texture2D droneIcon = null;
            try
            {
                droneIcon = ContentFinder<Texture2D>.Get(drone.texPath + "_east", false);
                if (droneIcon == null)
                {
                    droneIcon = ContentFinder<Texture2D>.Get(drone.texPath, false);
                }
            }
            catch
            {
                // Используем дефолтную иконку если не можем загрузить
            }

            if (droneIcon != null)
            {
                GUI.DrawTexture(iconRect, droneIcon);
            }
            else
            {
                // Рисуем заглушку если иконка не найдена
                Widgets.DrawBoxSolid(iconRect, Color.gray);
                GUI.color = Color.white;
            }

            // Название дрона (с учетом доступной ширины столбца)
            float availableWidth = rect.width - iconRect.width - 60f; // 60f для чекбокса и отступов
            Rect labelRect = new Rect(iconRect.xMax + 10f, rect.y, availableWidth, rect.height);

            // Обрезаем текст если он слишком длинный
            string displayText = drone.label;
            if (Text.CalcSize(displayText).x > availableWidth)
            {
                // Сокращаем текст до подходящего размера
                while (displayText.Length > 0 && Text.CalcSize(displayText + "...").x > availableWidth)
                {
                    displayText = displayText.Substring(0, displayText.Length - 1);
                }
                displayText += "...";
            }

            Widgets.Label(labelRect, displayText);

            // Иконка состояния: галочка или крестик
            Rect statusRect = new Rect(rect.xMax - 30f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);

            if (isEnabled)
            {
                // Включен - рисуем зеленую галочку
                GUI.DrawTexture(statusRect, Widgets.CheckboxOnTex);
            }
            else
            {
                // Выключен - рисуем красный крестик
                Color originalColor = GUI.color;
                GUI.color = Color.red;
                GUI.DrawTexture(statusRect, TexButton.CloseXSmall);
                GUI.color = originalColor;
            }

            // Показываем полное название в tooltip если оно было сокращено
            if (displayText != drone.label && Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, drone.label);
            }
        }

        public override string SettingsCategory()
        {
            return "More Hunter Drones";
        }

        // Статический метод для проверки включен ли дрон (для использования в других частях мода)
        public static bool IsDroneEnabled(string defName)
        {
            var mod = LoadedModManager.GetMod<HunterDroneMod>();
            return mod?.settings?.IsDroneEnabled(defName) ?? true;
        }
    }
}