using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using static KBAtomCreator.Atoms.AtomInfo;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace KBAtomCreator.Atoms
{
    internal class AtomInfoSerializer
    {
        public static AtomInfo Deserialize(string content, string atom_name = "")
        {
            string[] parsed_blocks = { "main", "arena_params", "resistances", "model" };
            var atomInfo = new AtomInfo();
            atomInfo.AtomName = atom_name;
            
            var lines = content.Split('\n')
               .Select(line => line.Trim())
               .Where(line => !string.IsNullOrEmpty(line))
               .ToArray();
           

            var currentParentBlock = "";
            var nestedBlock = "";

            var nestLevel = 0;
            string tabulation = "  ";

            StringBuilder mainAdditionalInfoBuilder = new StringBuilder();
            StringBuilder arenaAdditionalInfoBuilder = new StringBuilder();
            StringBuilder otherInfoBlocksBuilder = new StringBuilder();

            if (atom_name == "alchemist.atom")
                nestLevel = 0;



            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.EndsWith("{"))
                {
                    var blockName = line.Split(' ')[0];
                    if (nestLevel == 0)
                        currentParentBlock = blockName;
                    else
                        nestedBlock = blockName;

                    nestLevel += 1;
                    if (parsed_blocks.Contains(blockName))
                        continue;

                }

                if (line == "}")
                {
                    bool skipParse = false;

                    if (parsed_blocks.Contains(nestedBlock))
                        skipParse = true;
                    nestLevel = nestLevel > 0 ? nestLevel - 1 : 0;
                    //если это вложенный блок, закрываем только его
                    if (nestLevel == 0)
                    {
                        if (parsed_blocks.Contains(currentParentBlock))
                            skipParse = true;
                        currentParentBlock = "";
                        

                    }            
                    nestedBlock = "";
                    if (skipParse) continue;


                }

                if (currentParentBlock == "main")
                {
                    atomInfo.Main = atomInfo.Main ?? new AtomInfo.MainBlock();

                    if (nestedBlock == "")
                        ParseMainLine(line, atomInfo.Main, mainAdditionalInfoBuilder);
                    //составные модели
                    else if (nestedBlock == "model")
                    {
                        atomInfo.Main = atomInfo.Main ?? new AtomInfo.MainBlock();
                        ParseMainModel(line, atomInfo.Main, atom_name);
                    }
                    else
                    {
                        // добавляем отступы
                        if (!line.EndsWith("{") && !line.EndsWith("}")) mainAdditionalInfoBuilder.Append(tabulation);
                        for (int tab = 1; tab <= nestLevel; tab++) mainAdditionalInfoBuilder.Append(tabulation);
                        mainAdditionalInfoBuilder.AppendLine(line);
                    }

                }
                else if (currentParentBlock == "arena_params")
                {
                    atomInfo.ArenaParams = atomInfo.ArenaParams ?? new AtomInfo.ArenaParamsBlock();
                    if (IsKnownArenaParam(line) && nestLevel == 1)
                    {
                        ParseArenaParamsLine(line, atomInfo.ArenaParams);
                    }
                    else if (nestedBlock == "resistances")
                    {
                        atomInfo.ArenaParams.Resistances = atomInfo.ArenaParams.Resistances ?? new AtomInfo.ResistancesBlock();
                        ParseResistance(line, atomInfo.ArenaParams.Resistances, atom_name);
                    }
                    else
                    {
                        // добавляем отступы
                       for (int tab = 1; tab <= nestLevel+1; tab++) arenaAdditionalInfoBuilder.Append(tabulation);

                        arenaAdditionalInfoBuilder.AppendLine(line);
                    }
                            
                }
                else
                {
                    // добавляем отступы
                    //if (line.Length > 1)  for (int tab = 0; tab <= 1; tab++) arenaAdditionalInfoBuilder.Append(tabulation);
                    if (!line.EndsWith("{") && !line.EndsWith("}")) otherInfoBlocksBuilder.Append(tabulation);
                    for (int tab = 0; tab < nestLevel-1; tab++) otherInfoBlocksBuilder.Append(tabulation);
                    otherInfoBlocksBuilder.AppendLine(line);
                   
                }
          
                

                

            }
            //Остаток добалвляем в AdditionalInfo
            if (atomInfo.ArenaParams != null)
            {
                atomInfo.ArenaParams.AdditionalInfo = arenaAdditionalInfoBuilder.ToString();

                //затем анализируем его вытаскиввая все способности
                ParseAttacksInfo(atomInfo.ArenaParams);

            }

            if (atomInfo.Main != null)
                atomInfo.Main.AdditionalInfo = mainAdditionalInfoBuilder.ToString();
            atomInfo.OtherAtomData = otherInfoBlocksBuilder.ToString();
            

            

            return atomInfo;
        }
        public static void ParseAttacksInfo(AtomInfo.ArenaParamsBlock arenaParams)
        {
            if (arenaParams == null || string.IsNullOrEmpty(arenaParams.AdditionalInfo))
                return;

            var attackNames = arenaParams.Attacks;
            if (attackNames == null || attackNames.Count == 0)
                return;

            var lines = arenaParams.AdditionalInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(line => line.TrimEnd()) // сохраняем начальные пробелы, но можем и не сохранять? Лучше сохранить исходные строки.
                                                  .ToList();

            // Чтобы сохранить оригинальные отступы, будем работать с исходными строками (без Trim)
            // Делим по '\n' (учитывая \r\n)
            var rawLines = arenaParams.AdditionalInfo.Split(new[] { '\n' }, StringSplitOptions.None)
                                                   .Select(line => line.TrimEnd('\r'))
                                                   .ToList();

            // Ищем блоки по именам
            var result = new List<AttackBlock>();
            foreach (var attackName in attackNames)
            {
                // Ищем строку, которая содержит attackName и заканчивается на "{" или содержит "{" после имени
                // Ищем точное совпадение: строка начинается с пробелов, затем attackName, затем пробелы, затем "{"
                // Используем регулярное выражение или просто ищем подстроку.
                // Поскольку отступы могут быть разными, ищем строку, где attackName встречается как отдельное слово перед "{".
                int startIndex = -1;
                for (int i = 0; i < rawLines.Count; i++)
                {
                    var trimmed = rawLines[i].Trim();
                    if (trimmed.StartsWith(attackName + " {") || trimmed.StartsWith(attackName + "\t{"))
                    {
                        startIndex = i;
                        break;
                    }
                }
                if (startIndex == -1)
                    continue;

                // Теперь собираем блок с вложенными скобками
                int braceLevel = 0;
                bool blockStarted = false;
                var contentBuilder = new StringBuilder();
                for (int i = startIndex; i < rawLines.Count; i++)
                {
                    string line = rawLines[i];
                    if (!blockStarted)
                    {
                        // Начинаем блок, добавляем строку
                        contentBuilder.AppendLine(line);
                        blockStarted = true;
                        // считаем количество открывающих скобок в этой строке (пока только открывающие)
                        braceLevel += line.Count(c => c == '{');
                        braceLevel -= line.Count(c => c == '}');
                    }
                    else
                    {
                        contentBuilder.AppendLine(line);
                        braceLevel += line.Count(c => c == '{');
                        braceLevel -= line.Count(c => c == '}');
                        if (braceLevel == 0)
                        {
                            // Закончили блок
                            break;
                        }
                    }
                }

                if (contentBuilder.Length > 0)
                {
                    result.Add(new AttackBlock { Name = attackName, Content = contentBuilder.ToString() });
                }
            }

            arenaParams.AttacksInfo = result;
        }

        private static bool IsKnownArenaParam(string line)
        {
            var knownProperties = new HashSet<string>
        {
            "features_label", "features_hints", "race", "cost", "level", "leadership",
            "attack", "defense", "defenseup", "initiative", "speed", "hitpoint",
            "movetype", "krit", "hitback", "hitbackprotect", "attacks", "posthitmaster","posthitslave",
            "each_turn_script", "autofight", "features"
        };

            var parts = line.Split('=');
            if (parts.Length < 2) return false;

            var key = parts[0].Trim();
            return knownProperties.Contains(key);
        }

        private static void ParseMainModel(string line, AtomInfo.MainBlock main, string atomName)
        {
            var parts = line.Split('=');
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (value == string.Empty)
            {
                Logger.WriteLog($"{atomName}: Не удалось вытащить модель из {key}={value} из {line}");
                return;
            }

            main.Models = main.Models ?? new List<string>();
            try
            {
                main.Models.Add(value);
            }
            catch (Exception e)
            {
                Logger.WriteLog($"{atomName}: Не удалось вытащить модель {key}={value} из {line}", e);
            }
        }

        private static void ParseMainLine(string line, AtomInfo.MainBlock main, StringBuilder mainAdditionalInfoBuilder)
        {
            var parts = line.Split('=');
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            var undefinedValue = false;

            switch (key)
            {
                case "class": main.Class = value; break;
                case "model":
                {
                        main.Models = main.Models ?? new List<string>();
                        main.Models.Add(value);
                        break;
                }
                case "cullcat": main.Cullcat = int.Parse(value); break;
                default: undefinedValue = true;  break;
            }
            if (undefinedValue)
            {
                mainAdditionalInfoBuilder.AppendLine($"  {line}");
            }
        }

        private static void SetArenaParam(string key, string value, AtomInfo.ArenaParamsBlock arenaParams)
        {
            switch (key)
            {
                case "features_label": arenaParams.FeaturesLabel = value; break;
                case "features_hints": arenaParams.FeaturesHints = value; break;
                case "race": arenaParams.Race = value; break;
                case "cost": arenaParams.Cost = int.Parse(value); break;
                case "level": arenaParams.Level = int.Parse(value); break;
                case "leadership": arenaParams.Leadership = int.Parse(value); break;
                case "attack": arenaParams.Attack = int.Parse(value); break;
                case "defense": arenaParams.Defense = int.Parse(value); break;
                case "defenseup": arenaParams.Defenseup = int.Parse(value); break;
                case "initiative": arenaParams.Initiative = int.Parse(value); break;
                case "speed": arenaParams.Speed = int.Parse(value); break;
                case "hitpoint": arenaParams.Hitpoint = int.Parse(value); break;
                case "movetype": arenaParams.Movetype = int.Parse(value); break;
                case "krit": arenaParams.Krit = int.Parse(value); break;
                case "hitback": arenaParams.Hitback = int.Parse(value); break;
                case "hitbackprotect": arenaParams.Hitbackprotect = int.Parse(value); break;
                case "attacks":
                    // Разбиваем строку по запятым и создаём список
                    arenaParams.Attacks = string.IsNullOrEmpty(value)
                        ? new List<string>()
                        : value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .ToList();
                    break;

                case "posthitmaster": arenaParams.Posthitmaster = value; break;
                case "posthitslave": arenaParams.Posthitslave = value; break;
                case "autofight": arenaParams.Autofight = int.Parse(value); break;
                case "features": arenaParams.Features = value; break;
            }
        }

        private static void ParseArenaParamsLine(string line, AtomInfo.ArenaParamsBlock arenaParams)
        {
            var parts = line.Split('=');
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            try
            {
                SetArenaParam(key, value,arenaParams);
            }
            catch (Exception e)
            {
                Logger.WriteLog($"Не удалось записать параметр арены {key} из {line}", e);
            }


        }

        private static void ParseResistance(string line, AtomInfo.ResistancesBlock resistances, string atomName)
        {
            var parts = line.Split('=');
            if (parts.Length != 2) return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            try
            {
                SaveResistances(key, value, resistances);
            }
            catch (Exception e)
            {
                Logger.WriteLog($"{atomName}: Не удалось вытащить сопротивляемость {key} из {line}", e);
            }
        }

        private static void  SaveResistances(string key, string value, AtomInfo.ResistancesBlock resistances)
        {
            switch (key)
            {
                case "physical": resistances.Physical = int.Parse(value); break;
                case "poison": resistances.Poison = int.Parse(value); break;
                case "magic": resistances.Magic = int.Parse(value); break;
                case "fire": resistances.Fire = int.Parse(value); break;
                case "astral": resistances.Astral = int.Parse(value); break;
                    //case "glacial": resistances.Glacial = int.Parse(value); break;

            }
        }


        private static AtomInfo.ResistancesBlock ParseResistancesBlock(string[] lines, ref int index)
        {
            var resistances = new AtomInfo.ResistancesBlock();

            while (index < lines.Length)
            {
                var line = lines[index].Trim();
                if (line == "}") break;

                var parts = line.Split('=');
                if (parts.Length != 2)
                {
                    index++;
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                try
                {
                    SaveResistances(key, value, resistances);
                }
                catch (Exception e)
                {
                    Logger.WriteLog($"Не удалось вытащить сопротивляемость {key} из {line}", e);
                }



                index++;
            }

            return resistances;
        }

        public static string Serialize(AtomInfo atomInfo)
        {
            var sb = new StringBuilder();

            sb.AppendLine("main {");
            if (atomInfo.Main.Class != null)
                sb.AppendLine($"  class={atomInfo.Main.Class}");          
            if (atomInfo.Main.Models != null)
            {
                //для одной модели сохраняем через равно
                if (atomInfo.Main.Models.Count == 1)
                    sb.AppendLine($"  model={atomInfo.Main.Models[0]}");
                //для нескольких создаём отдельный блок
                else
                {
                        sb.AppendLine("  model {");
                        for (int i = 0; i < atomInfo.Main.Models.Count; i++)
                        {
                            sb.AppendLine($"    {i}={atomInfo.Main.Models[i]}");
                        }
                        sb.AppendLine("  }");
                }
            }

            sb.AppendLine($"  cullcat={atomInfo.Main.Cullcat}");
            sb.AppendLine(atomInfo.Main.AdditionalInfo);
            sb.AppendLine("}");

            sb.AppendLine("arena_params {");
            AppendIfNotEmpty(sb, "features_label", atomInfo.ArenaParams.FeaturesLabel);
            AppendIfNotEmpty(sb, "features_hints", atomInfo.ArenaParams.FeaturesHints);
            AppendIfNotEmpty(sb, "race", atomInfo.ArenaParams.Race);
            AppendIfNotEmpty(sb, "cost", atomInfo.ArenaParams.Cost);
            AppendIfNotEmpty(sb, "level", atomInfo.ArenaParams.Level);
            AppendIfNotEmpty(sb, "leadership", atomInfo.ArenaParams.Leadership);
            AppendIfNotEmpty(sb, "attack", atomInfo.ArenaParams.Attack);
            AppendIfNotEmpty(sb, "defense", atomInfo.ArenaParams.Defense);
            AppendIfNotEmpty(sb, "defenseup", atomInfo.ArenaParams.Defenseup);
            AppendIfNotEmpty(sb, "initiative", atomInfo.ArenaParams.Initiative);
            AppendIfNotEmpty(sb, "speed", atomInfo.ArenaParams.Speed);
            AppendIfNotEmpty(sb, "hitpoint", atomInfo.ArenaParams.Hitpoint);
            AppendIfNotEmpty(sb, "movetype", atomInfo.ArenaParams.Movetype);
            AppendIfNotEmpty(sb, "krit", atomInfo.ArenaParams.Krit);
            AppendIfNotEmpty(sb, "hitback", atomInfo.ArenaParams.Hitback);
            AppendIfNotEmpty(sb, "hitbackprotect", atomInfo.ArenaParams.Hitbackprotect);

            if (atomInfo.ArenaParams.Attacks != null && atomInfo.ArenaParams.Attacks.Any())
            {
                sb.AppendLine($"  attacks={string.Join(",", atomInfo.ArenaParams.Attacks)}");
            }

            //AppendIfNotEmpty(sb, "attacks", atomInfo.ArenaParams.Attacks);


            AppendIfNotEmpty(sb, "posthitmaster", atomInfo.ArenaParams.Posthitmaster);
            AppendIfNotEmpty(sb, "posthitslave", atomInfo.ArenaParams.Posthitslave);
            AppendIfNotEmpty(sb, "autofight", atomInfo.ArenaParams.Autofight);
            AppendIfNotEmpty(sb, "features", atomInfo.ArenaParams.Features);

            if (atomInfo.ArenaParams.Resistances != null)
            {
                sb.AppendLine("  resistances {");
                AppendIfNotEmpty(sb, "    physical", atomInfo.ArenaParams.Resistances.Physical);
                AppendIfNotEmpty(sb, "    magic", atomInfo.ArenaParams.Resistances.Magic);
                AppendIfNotEmpty(sb, "    fire", atomInfo.ArenaParams.Resistances.Fire);
                AppendIfNotEmpty(sb, "    poison", atomInfo.ArenaParams.Resistances.Poison);
                AppendIfNotEmpty(sb, "    astral", atomInfo.ArenaParams.Resistances.Astral);
                sb.AppendLine("  }");
            }

            // Добавляем дополнительные вложенные блоки
            if (!string.IsNullOrEmpty(atomInfo.ArenaParams.AdditionalInfo))
            {
                sb.Append(atomInfo.ArenaParams.AdditionalInfo);
            }

            sb.AppendLine("}");
            sb.AppendLine(atomInfo.OtherAtomData);

            //var res =sb.ToString();
            return sb.ToString();
        }

        private static void AppendIfNotEmpty(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                sb.AppendLine($"  {key}={value}");
        }

        private static void AppendIfNotEmpty(StringBuilder sb, string key, int value)
        {
            sb.AppendLine($"  {key}={value}");
        }
    }
}
