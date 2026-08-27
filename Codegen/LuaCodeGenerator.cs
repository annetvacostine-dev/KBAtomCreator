using KBAtomCreator.Abilies;
using KBAtomCreator.Atoms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KBAtomCreator.Atoms.AtomInfo;

namespace KBAtomCreator.Codegen
{
    internal class LuaCodeGenerator
    {
        private static readonly string singleTargetCode = @"    local target = Attack.get_target()
    Attack.act_aseq(0,""cast"")
    local dmgts = Attack.aseq_time(0, ""x"")  -- x time of attacker
    Attack.atom_spawn(target, dmgts ""oldaxe"" )  -- summon atom at x time

    local dmg_min,dmg_max = text_range_dec(Attack.get_custom_param(""damage""))
    local typedmg=Attack.get_custom_param(""typedmg"")

    Attack.atk_set_damage(typedmg,dmg_min,dmg_max)
    common_cell_apply_damage(target , dmgts)
    -- Проверка урона
    --local damage = math.min(Attack.act_totalhp(target), (Attack.act_damage_results(target)))
  ";

        private static readonly string singleTargetCallccells = @"    for c=0,Attack.cell_count()-1 do
        local i = Attack.cell_get(c)
        if Attack.act_takesdmg(i) and Attack.act_applicable(i) then Attack.marktarget(i) end
      end";

        public static void SaveAbility(AtomAbility ability, string luaFile)
        {
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));

            if (string.IsNullOrEmpty(luaFile))
                throw new ArgumentException("Lua file path cannot be null or empty", nameof(luaFile));

            var abilityClass = ability.GetProperty<string>("class");
            // для нескриптованных способностей код не нужен
            if (abilityClass != "scripted")
                return;

            try
            {
                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(luaFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Генерируем Lua код
                string luaCode = GenerateAbilityCode(ability);

                // Записываем в файл
                File.WriteAllText(luaFile, luaCode, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving ability to Lua file: {ex.Message}", ex);
            }
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="abilities">список сопобностей</param>
        /// <param name="luaFile">куда сохранять</param>
        /// <param name="AllLuaFuncs">Словарь со всеми скриптами</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static void SaveAbilities(
            List<AtomAbility> abilities,
            string luaFile,
            Dictionary<string, string> AllLuaFuncs,
            List<AttackBlock> attacksInfo)  // <-- добавлен параметр
        {
            if (abilities == null)
                throw new ArgumentNullException(nameof(abilities));
            if (string.IsNullOrEmpty(luaFile))
                throw new ArgumentException("Lua file path cannot be null or empty", nameof(luaFile));

            // Создаём директорию, если её нет
            var directory = Path.GetDirectoryName(luaFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var luaCode = new StringBuilder();
            var usedFunctions = new HashSet<string>(); // предотвращает дублирование кода функций

            foreach (var ability in abilities)
            {
                // Пропускаем способности с действием NoCopy
                if (ability.AbilityAction == AbilityAction.NoCopy)
                    continue;

                //  берём функции из блоков атак
                if (ability.AbilityAction == AbilityAction.CopyCode)
                {
                    
                    if (string.IsNullOrEmpty(ability.Name))
                        continue; // нет имени – не можем найти блок

                    var attackBlock = attacksInfo?.FirstOrDefault(a => a.Name == ability.Name);
                    if (attackBlock == null)
                        continue; // блок не найден

                    var scriptFields = AbilityParser.ExtractScriptFields(attackBlock.Content);
                    foreach (var funcName in scriptFields)
                    {
                        if (string.IsNullOrEmpty(funcName))
                            continue;

                        if (AllLuaFuncs.TryGetValue(funcName, out string funcCode))
                        {
                            if (usedFunctions.Add(funcName)) // добавляем только если ещё не было
                            {
                                luaCode.AppendLine(funcCode);
                                luaCode.AppendLine(); // пустая строка между функциями
                            }
                        }
                        // Если функция не найдена – можно вывести предупреждение (опционально)
                    }
                }
                // 2) Генерация Lua из шаблона (старая логика)
                else if (ability.AbilityAction == AbilityAction.GenerateLua)
                {
                    //var abilityClass = ability.GetProperty<string>("class");
                    //if (abilityClass == "scripted")
                    //{
                       
                    var code = GenerateAbilityCode(ability);
                    if (!string.IsNullOrEmpty(code))
                    {
                        luaCode.AppendLine(code);
                        luaCode.AppendLine();
                    }
                    //}
                }
                // Другие типы действия игнорируем
            }

            // Записываем в файл в кодировке Windows-1251
            var win1251Encoding = Encoding.GetEncoding("windows-1251");
            File.WriteAllText(luaFile, luaCode.ToString(), win1251Encoding);
        }

        private static string GenerateAbilityCode(AtomAbility ability)

        {

            if (string.IsNullOrEmpty(ability.Name))
            {
                throw new InvalidOperationException("Ability TranslatedName cannot be null or empty");
            }

            var sb = new StringBuilder();

            // Генерируем комментарий с информацией о способности
            //sb.AppendLine($"-- {ability.Name}");
            //if (!string.IsNullOrEmpty(ability.Description))
            //{
            //    sb.AppendLine($"-- Description: {ability.Description}");
            //}
            //sb.AppendLine($"-- Ability Class: {ability.AbilityClass}");
            //sb.AppendLine();
            string calccellsName = $"calccells_{ability.Name}";

            // Генерируем функцию расчёта целей
            sb.AppendLine($"function {calccellsName}()");
            sb.AppendLine($"{singleTargetCallccells}");
            sb.AppendLine("    return true");
            sb.AppendLine("end");

            var templateName = ability.AbilityTemplate;

            string functionName = $"special_{ability.Name}";
            // Генерируем функцию способности         
            sb.AppendLine($"function {functionName}()");
            sb.AppendLine("    -- TODO: Implement ability logic");

            sb.Append(GenerateAbiCodeByTemplate(templateName));


     
            sb.AppendLine("    ");

            // Добавляем базовую логику в зависимости от класса способности

            // чтобы способность работала
            sb.AppendLine("    return true");
            sb.AppendLine("end");

            return sb.ToString();
        }
        private static string GenerateAbiCodeByTemplate(AbilityTemplate abilityTemplate)
        {
            string abiCode = "";
            switch (abilityTemplate)
            {
                case AbilityTemplate.SingleTarget:
                    abiCode = singleTargetCode; break;
                default: return singleTargetCode;


            } 

            return abiCode;
        }

        // Метод для генерации кода с дополнительными параметрами
        public static string GenerateAbilityCode(AtomAbility ability, Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();

            // Генерируем комментарий с информацией о способности
            //sb.AppendLine($"-- {ability.Name}");
            //if (!string.IsNullOrEmpty(ability.Description))
            //{
            //    sb.AppendLine($"-- Description: {ability.Description}");
            //}
            //sb.AppendLine($"-- Ability Class: {ability.AbilityClass}");

            // Добавляем параметры в комментарии
            if (parameters != null && parameters.Count > 0)
            {
                sb.AppendLine("-- Parameters:");
                foreach (var param in parameters)
                {
                    sb.AppendLine($"--   {param.Key}: {param.Value}");
                }
            }
            sb.AppendLine();

            // Генерируем функцию с параметрами
            string functionName = $"special_{ability.Name}";
            string parameterList = GenerateParameterList(parameters);

            sb.AppendLine($"function {functionName}({parameterList})");
            sb.AppendLine("    -- TODO: Implement ability logic using provided parameters");
            sb.AppendLine("    ");

            // Генерируем примеры использования параметров
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    sb.AppendLine($"    -- Using parameter: {param.Key}");
                }
            }

            sb.AppendLine("end");

            return sb.ToString();
        }

        private static string GenerateParameterList(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "unit, target, x, y";
            }

            var paramList = new List<string> { "unit", "target", "x", "y" };
            paramList.AddRange(parameters.Keys);

            return string.Join(", ", paramList);
        }

        // Метод для создания шаблона модуля способностей
        //public static string GenerateAbilityModule(List<AtomAbility> abilities)
        //{
        //    var sb = new StringBuilder();

        //    sb.AppendLine("-- Ability Module");
        //    sb.AppendLine("-- Generated automatically by KBAtomCreator");
        //    sb.AppendLine("-- Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //    sb.AppendLine();

        //    sb.AppendLine("local abilities = {}");
        //    sb.AppendLine();

        //    // Генерируем функции для каждой способности
        //    foreach (var ability in abilities)
        //    {
        //        if (!string.IsNullOrEmpty(ability.TranslatedName))
        //        {
        //            sb.AppendLine(GenerateAbilityCode(ability));
        //            sb.AppendLine();

        //            // Добавляем в таблицу способностей
        //            string functionName = $"special_{ability.TranslatedName}";
        //            sb.AppendLine($"abilities.{ability.TranslatedName} = {functionName}");
        //            sb.AppendLine();
        //        }
        //    }

        //    sb.AppendLine("return abilities");

        //    return sb.ToString();
        //}
    }
}
