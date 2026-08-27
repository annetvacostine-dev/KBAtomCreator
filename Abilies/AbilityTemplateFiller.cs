using KBAtomCreator.Atoms;
using KBAtomCreator.DescriptionAndLocale;
using System.Collections.Generic;

namespace KBAtomCreator.Abilies
{
    internal class AbilityTemplateFiller
    {
        /// <summary>
        /// Заполняет способность по шаблону с использованием параметров из формы
        /// </summary>
        /// <param name="abilityName">Название способности</param>
        /// <param name="abilityHead">Заголовок способности для hinthead</param>
        /// <param name="abilityHint">Текст подсказки для hint</param>
        /// <param name="abilityClass">Класс способности</param>
        /// <param name="reload">Время перезарядки</param>
        /// <param name="nFeatures">Особенности (по умолчанию "pawn")</param>
        /// <param name="moves">Количество перемещений (опционально)</param>
        /// <param name="endMove">Завершение хода (по умолчанию 0)</param>
        /// <param name="minDamage">Минимальный урон (для moveattack и throw)</param>
        /// <param name="maxDamage">Максимальный урон (для moveattack и throw)</param>
        /// <param name="distance">Дистанция (для throw)</param>
        /// <param name="mindist">Минимальная дистанция (для throw)</param>
        /// <param name="penalty">Штраф (для throw)</param>
        /// <param name="animation">Анимация (для throw)</param>
        /// <param name="throwObject">Объект для броска (для throw)</param>
        /// <param name="framekey">Ключ кадра (для throw)</param>
        /// <param name="spell">Название заклинания (для spell)</param>
        /// <param name="customParams">Дополнительные параметры для custom_params</param>
        /// <returns>Заполненная способность</returns>
        public static AtomAbility FillAbilityByTemplate(
            string abilityName,
            string abilityHead,
            string abilityHint,
            AbilityClass abilityClass,
            int reload = 4,
            string nFeatures = "pawn",
            int? moves = null,
            int endMove = 0,
            int? minDamage = null,
            int? maxDamage = null,
            int? distance = null,
            int? mindist = null,
            double? penalty = null,
            string animation = null,
            string throwObject = null,
            string framekey = null,
            string spell = null,
            Dictionary<string, object> customParams = null)
        {
            var ability = new AtomAbility
            {
                Name = abilityName
            };

            // Базовые свойства для всех способностей
            ability.SetProperty("class", abilityClass.ToString().ToLower());
            ability.SetProperty("picture_small", $"icon_{abilityName}.png");
            ability.SetProperty("picture", $"icon_{abilityName}_");
            ability.SetProperty("hinthead", abilityHead);
            ability.SetProperty("hint", abilityHint);
            ability.SetProperty("nfeatures", nFeatures);
            ability.SetProperty("reload", reload);

            // Опциональное свойство moves
            if (moves.HasValue)
            {
                ability.SetProperty("moves", moves.Value);
            }

            ability.SetProperty("endmove", endMove);

            // Заполняем специфичные свойства для каждого класса
            switch (abilityClass)
            {
                case AbilityClass.moveattack:
                    FillMoveAttackAbility(ability, minDamage, maxDamage);
                    break;
                case AbilityClass.@throw:
                    FillThrowAbility(ability, minDamage, maxDamage, distance, mindist, penalty, animation, throwObject, framekey);
                    break;
                case AbilityClass.spell:
                    FillSpellAbility(ability, spell);
                    break;
                case AbilityClass.scripted:
                    FillScriptedAbility(ability, abilityName, minDamage, maxDamage);
                    break;
            }

            // Создаем блок custom_params
            var customParamsBlock = customParams ?? new Dictionary<string, object>();

            // Добавляем параметры урона в custom_params для moveattack
            if (abilityClass == AbilityClass.moveattack && minDamage.HasValue && maxDamage.HasValue)
            {
                customParamsBlock["dam"] = $"{minDamage.Value},{maxDamage.Value}";
            }

            ability.SetProperty("custom_params", customParamsBlock);

            return ability;
        }

        /// <summary>
        /// Заполняет свойства для способности типа moveattack
        /// </summary>
        private static void FillMoveAttackAbility(AtomAbility ability, int? minDamage, int? maxDamage)
        {
            ability.SetProperty("base_attack", 0);
            ability.SetProperty("ad_factor", 1);
            ability.SetProperty("options", "disablerush,used_if_damaged");
            ability.SetProperty("anim_attack", "attack");
            ability.SetProperty("no_hint", 1);

            // Блок damage
            if (minDamage.HasValue && maxDamage.HasValue)
            {
                var damageBlock = new Dictionary<string, object>
                {
                    ["physical"] = $"{minDamage.Value},{maxDamage.Value}"
                };
                ability.SetProperty("damage", damageBlock);
            }
        }

        /// <summary>
        /// Заполняет свойства для способности типа throw
        /// </summary>
        private static void FillThrowAbility(AtomAbility ability,
            int? minDamage, int? maxDamage,
            int? distance, int? mindist, double? penalty,
            string animation, string throwObject, string framekey)
        {
            ability.SetProperty("group", "1,2");
            ability.SetProperty("showdmg", 1);
            ability.SetProperty("base_attack", 0);
            ability.SetProperty("distance", distance ?? 6);
            ability.SetProperty("mindist", mindist ?? 2);
            ability.SetProperty("penalty", penalty ?? 0.5);
            ability.SetProperty("animation", animation ?? "cast/throw/thtarget");
            ability.SetProperty("throw", throwObject ?? "bowman_arrow");
            ability.SetProperty("framekey", framekey ?? "x");

            // Блок damage
            if (minDamage.HasValue && maxDamage.HasValue)
            {
                var damageBlock = new Dictionary<string, object>
                {
                    ["physical"] = $"{minDamage.Value},{maxDamage.Value}"
                };
                ability.SetProperty("damage", damageBlock);
            }
        }

        /// <summary>
        /// Заполняет свойства для способности типа spell
        /// </summary>
        private static void FillSpellAbility(AtomAbility ability, string spell)
        {
            ability.SetProperty("spell", spell ?? $"special_{ability.Name}");
            ability.SetProperty("reload", 3); // Переопределяем reload для spell
        }

        /// <summary>
        /// Заполняет свойства для способности типа scripted
        /// </summary>
        private static void FillScriptedAbility(AtomAbility ability, string abilityName, int? minDamage, int? maxDamage)
        {
            ability.SetProperty("script_attack", $"special_{abilityName}");
            ability.SetProperty("script_calccells", $"calccells_{abilityName}");
            ability.SetProperty("attack_cursor", "magicstick");
            ability.SetProperty("reload", 5); // Переопределяем reload для scripted

            // Блок damage для scripted
            if (minDamage.HasValue && maxDamage.HasValue)
            {
                var damageBlock = new Dictionary<string, object>
                {
                    ["physical"] = $"{minDamage.Value},{maxDamage.Value}"
                };
                ability.SetProperty("damage", damageBlock);
            }
        }

        /// <summary>
        /// Создает способность с предустановленными параметрами для определенного типа
        /// </summary>
        public static AtomAbility CreatePredefinedAbility(
            string abilityName,
            string abilityHead,
            string abilityHint,
            AbilityClass abilityClass,
            AbilityAction abilityAction = AbilityAction.GenerateLua, // новый параметр
            Dictionary<string, object> additionalParams = null)
        {
            var (reload, nFeatures, minDamage, maxDamage, customParams) = GetPresetByType(abilityClass);

            // Объединяем предустановленные custom_params с дополнительными
            if (additionalParams != null)
            {
                customParams = customParams ?? new Dictionary<string, object>();
                foreach (var param in additionalParams)
                {
                    customParams[param.Key] = param.Value;
                }
            }

            var ability = FillAbilityByTemplate(
                    abilityName,
                    abilityHead,
                    abilityHint,
                    abilityClass,
                    reload,
                    nFeatures,
                    customParams: customParams,
                    minDamage: minDamage,
                    maxDamage: maxDamage);

            // Устанавливаем AbilityAction
            ability.AbilityAction = abilityAction;

            return ability;
        }

        /// <summary>
        /// Получает предустановки для различных типов способностей
        /// </summary>
        private static (int reload, string nFeatures, int? minDamage, int? maxDamage, Dictionary<string, object> customParams)
            GetPresetByType(AbilityClass abilityClass)
        {
            return abilityClass switch
            {
                AbilityClass.moveattack => (2, "pawn",4, 5, new Dictionary<string, object>()),
                AbilityClass.@throw => (2, "pawn", 2, 3, new Dictionary<string, object>()),
                AbilityClass.spell => (3, "pawn", null, null, new Dictionary<string, object>()),
                AbilityClass.scripted => (3, "pawn", 4, 5, new Dictionary<string, object>()),
                _ => (3, "pawn", null, null, new Dictionary<string, object>()),
            };
        }

        /// <summary>
        /// Обновляет существующую способность по шаблону
        /// </summary>
        /// <summary>
        /// Обновляет существующую способность по шаблону
        /// </summary>
        public static void UpdateAbilityByTemplate(
            AtomAbility ability,
            string abilityHead,
            string abilityHint,
            AbilityClass abilityClass,
            string scriptAttack = null,
            string scriptCalcCells = null,
            int? reload = null,
            string nFeatures = null,
            int? moves = null,
            int? minDamage = null,
            int? maxDamage = null,
            Dictionary<string, object> customParams = null)
        {
            if (ability == null) return;

            // Обновляем только переданные параметры
            if (abilityHead != null) ability.SetProperty("hinthead", abilityHead);
            if (abilityHint != null) ability.SetProperty("hint", abilityHint);
            if (scriptAttack != null) ability.SetProperty("script_attack", scriptAttack);
            if (scriptCalcCells != null) ability.SetProperty("script_calccells", scriptCalcCells);
            if (reload.HasValue) ability.SetProperty("reload", reload.Value);
            if (nFeatures != null) ability.SetProperty("nfeatures", nFeatures);
            if (moves.HasValue) ability.SetProperty("moves", moves.Value);
            ability.SetProperty("class", abilityClass.ToString().ToLower());

            // Обновляем damage блок для scripted, moveattack и throw
            if ((abilityClass == AbilityClass.scripted || abilityClass == AbilityClass.moveattack || abilityClass == AbilityClass.@throw) &&
                minDamage.HasValue && maxDamage.HasValue)
            {
                var damageBlock = new Dictionary<string, object>
                {
                    ["physical"] = $"{minDamage.Value},{maxDamage.Value}"
                };
                ability.SetProperty("damage", damageBlock);
            }

            // Обновляем custom_params если переданы
            if (customParams != null)
            {
                var existingParams = ability.GetProperty<Dictionary<string, object>>("custom_params") ?? new Dictionary<string, object>();
                foreach (var param in customParams)
                {
                    existingParams[param.Key] = param.Value;
                }
                ability.SetProperty("custom_params", existingParams);
            }

            // Обновляем dam в custom_params для moveattack и scripted
            if ((abilityClass == AbilityClass.moveattack || abilityClass == AbilityClass.scripted) &&
                minDamage.HasValue && maxDamage.HasValue)
            {
                var existingParams = ability.GetProperty<Dictionary<string, object>>("custom_params") ?? new Dictionary<string, object>();
                existingParams["dam"] = $"{minDamage.Value},{maxDamage.Value}";
                ability.SetProperty("custom_params", existingParams);
            }
        }
    }


}
