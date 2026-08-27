using KBAtomCreator.Atoms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBAtomCreator.Abilies
{
    internal class AbilityParser
    {
        public List<string> ParseAbilityNames(string abilitiesString)
        {
            // Разделяем названия способностей по запятой
            var abilityNames = abilitiesString.Split(',')
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            return abilityNames;
        }
        public List<AtomAbility> ParseAbilitiesFromAttacks(string attacksValue, string abilitiesText)
        {
            var abilities = new List<AtomAbility>();

            if (string.IsNullOrEmpty(attacksValue))
                return abilities;

            // Разделяем названия способностей по запятой
            var abilityNames = attacksValue.Split(',')
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            foreach (var abilityName in abilityNames)
            {
                var ability = ParseAbility(abilityName, abilitiesText);
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }

            return abilities;
        }
        /// <summary>
        /// Парсит одну способность из текстового файла по имени
        /// </summary>
        public AtomAbility ParseAbility(string abilityName, string abilitiesText)
        {
            if (string.IsNullOrEmpty(abilitiesText) || string.IsNullOrEmpty(abilityName))
                return null;

            int searchIndex = 0;
            while (searchIndex < abilitiesText.Length)
            {
                // Ищем начало блока способности
                int startIndex = abilitiesText.IndexOf(abilityName + " {", searchIndex, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1)
                    break;

                // Находим начало тела способности (после имени и открывающей скобки)
                int bodyStart = startIndex + abilityName.Length + 1; // +1 для пробела перед {
                int braceCount = 0;
                int contentStart = -1;
                int contentEnd = -1;

                for (int i = bodyStart; i < abilitiesText.Length; i++)
                {
                    char c = abilitiesText[i];

                    if (c == '{')
                    {
                        if (braceCount == 0)
                        {
                            contentStart = i + 1; // Начинаем после открывающей скобки
                        }
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            contentEnd = i;
                            break;
                        }
                    }
                }

                if (contentStart != -1 && contentEnd != -1)
                {
                    string abilityContent = abilitiesText.Substring(contentStart, contentEnd - contentStart).Trim();
                    return ParseAbilityContent(abilityName, abilityContent);
                }

                searchIndex = bodyStart + 1;
            }

            return null;
        }

        /// <summary>
        /// Парсит содержимое блока способности
        /// </summary>
        private AtomAbility ParseAbilityContent(string abilityName, string content)
        {
            var ability = new AtomAbility { Name = abilityName };
            var lines = SplitContentLines(content);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // Проверяем, является ли строка началом блока
                if (line.EndsWith("{"))
                {
                    string blockName = line.Substring(0, line.Length - 1).Trim();
                    var blockContent = ExtractBlockContent(lines, ref i);
                    ability.SetProperty(blockName, ParseBlock(blockContent, blockName));
                }
                else if (line.Contains("="))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Пытаемся определить тип значения
                        object parsedValue = ParseValue(value);
                        ability.SetProperty(key, parsedValue);
                    }
                }
            }

            return ability;
        }

        /// <summary>
        /// Парсит вложенный блок
        /// </summary>
        private Dictionary<string, object> ParseBlock(string content, string blockName)
        {
            var block = new Dictionary<string, object>();
            var lines = SplitContentLines(content);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.Contains("="))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        block[key] = ParseValue(value);
                    }
                }
            }

            return block;
        }

        /// <summary>
        /// Парсит значение, пытаясь определить его тип
        /// </summary>
        private object ParseValue(string value)
        {
            // Убираем кавычки если есть
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Substring(1, value.Length - 2);

            // Пытаемся парсить как число
            if (int.TryParse(value, out int intValue))
                return intValue;

            // Пытаемся парсить как double
            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double doubleValue))
                return doubleValue;

            // Возвращаем как строку
            return value;
        }

        /// <summary>
        /// Извлекает содержимое блока из массива строк
        /// </summary>
        private string ExtractBlockContent(string[] lines, ref int currentIndex)
        {
            var content = new StringBuilder();
            int braceCount = 1; // Уже находимся внутри блока

            for (int i = currentIndex; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                foreach (char c in line)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                }

                if (braceCount == 0)
                {
                    currentIndex = i;
                    break;
                }

                if (i != currentIndex) // Не включаем первую строку с {
                {
                    content.AppendLine(line);
                }
            }

            return content.ToString().Trim();
        }

        /// <summary>
        /// Разделяет содержимое на строки, учитывая переносы строк
        /// </summary>
        private string[] SplitContentLines(string content)
        {
            return content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(line => line.Trim())
                         .Where(line => !string.IsNullOrEmpty(line))
                         .ToArray();
        }

        /// <summary>
        /// Сериализует список способностей обратно в строку для атрибута Attacks
        /// </summary>
        public string GetAbilitiesString(List<AtomAbility> abilities)
        {
            if (abilities == null || abilities.Count == 0)
                return string.Empty;
            
            var sb = new StringBuilder();
            //обязательная
            sb.Append("moveattack");
            if (abilities.Count > 0)
                sb.Append(",");
            foreach (var ability in abilities)
            {
                sb.Append(ability.Name);
                if (abilities.IndexOf(ability) != abilities.Count -1)
                    sb.Append(",");
            }

            //string.Join(",", abilities.Select(a => a.Name));
            return sb.ToString();
        }


        public List<string> GetAbilitiesList(List<AtomAbility> abilities)
        {
            if (abilities == null || abilities.Count == 0)
                return new List<string>();

            var abilitiesList = new List<string>();
            foreach (var ability in abilities)
            {
                abilitiesList.Add(ability.Name);
            }

            return abilitiesList;
        }
        /// <summary>
        /// Сериализует способность в строку для записи в файл
        /// </summary>
        public string SerializeAbility(AtomAbility ability)
        {
            if (ability == null)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"{ability.Name} {{");

            // Сначала простые свойства
            foreach (var prop in ability.Properties)
            {
                if (prop.Value is Dictionary<string, object> nestedBlock)
                {
                    sb.AppendLine($"    {prop.Key} {{");
                    foreach (var nestedProp in nestedBlock)
                    {
                        sb.AppendLine($"        {nestedProp.Key}={FormatValue(nestedProp.Value)}");
                    }
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    {prop.Key}={FormatValue(prop.Value)}");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public string SerializeAbilities(List<AtomAbility> abilities)
        {
            var sb = new StringBuilder();
            foreach (var ability in abilities)
                sb.AppendLine(SerializeAbility(ability));
            return sb.ToString();
        }

        /// <summary>
        /// Форматирует значение для сериализации
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string str)
            {
                // Если строка содержит пробелы или специальные символы, заключаем в кавычки
                if (str.Contains(" ") || str.Contains("=") || str.Contains("{") || str.Contains("}"))
                    return $"\"{str}\"";
                return str;
            }

            return value.ToString();
        }

        public static List<string> ExtractScriptFields(string content)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(content)) return result;

            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("script_attack=") ||
                    trimmed.StartsWith("script_calccells=") ||
                    trimmed.StartsWith("script_filter=") ||
                    trimmed.StartsWith("script_highlight="))
                {
                    var parts = trimmed.Split('=');
                    if (parts.Length == 2)
                    {
                        var value = parts[1].Trim();
                        if (!string.IsNullOrEmpty(value))
                            result.Add(value);
                    }
                }
            }
            return result;
        }
    }
}
