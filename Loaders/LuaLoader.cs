using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;


namespace KBAtomCreator.Loaders
{
    internal class LuaLoader
    {
        

        private static Dictionary<string, string> ExtractLuaFunctions(string luaCode)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(luaCode)) return result;

            int length = luaCode.Length;
            int pos = 0;

            while (pos < length)
            {
                // Пропускаем пробелы и переводы строк
                while (pos < length && char.IsWhiteSpace(luaCode[pos])) pos++;

                // Ищем ключевое слово "function" как целое слово
                if (pos + 8 <= length && luaCode.Substring(pos, 8) == "function" &&
                    (pos == 0 || !char.IsLetterOrDigit(luaCode[pos - 1])) &&
                    (pos + 8 >= length || !char.IsLetterOrDigit(luaCode[pos + 8])))
                {
                    int funcStart = pos;
                    int i = pos + 8;

                    // Пропускаем пробелы после function
                    while (i < length && char.IsWhiteSpace(luaCode[i])) i++;

                    // Извлекаем имя функции (до '(' или пробела)
                    int nameStart = i;
                    while (i < length && !char.IsWhiteSpace(luaCode[i]) && luaCode[i] != '(' && luaCode[i] != ':') i++;
                    string funcName = (i > nameStart) ? luaCode.Substring(nameStart, i - nameStart) : null;
                    if (string.IsNullOrEmpty(funcName))
                    {
                        pos++;
                        continue;
                    }

                    // Теперь ищем соответствующий "end" или "until" с учётом вложенности
                    int level = 1;
                    int searchPos = i;
                    bool inString = false;
                    char stringDelimiter = '\0';
                    bool inLongString = false;
                    bool inComment = false;
                    bool inLongComment = false;

                    while (searchPos < length && level > 0)
                    {
                        char c = luaCode[searchPos];

                        // --- Обработка строк ---
                        if (!inComment && !inLongComment && !inLongString)
                        {
                            if (!inString && (c == '"' || c == '\''))
                            {
                                inString = true;
                                stringDelimiter = c;
                                searchPos++;
                                continue;
                            }
                            else if (inString)
                            {
                                if (c == '\\' && searchPos + 1 < length)
                                {
                                    searchPos += 2;
                                    continue;
                                }
                                if (c == stringDelimiter)
                                    inString = false;
                                searchPos++;
                                continue;
                            }
                        }

                        // --- Обработка длинных строк [[...]] ---
                        if (!inComment && !inLongComment && !inString && searchPos + 1 < length && luaCode[searchPos] == '[' && luaCode[searchPos + 1] == '[')
                        {
                            inLongString = true;
                            searchPos += 2;
                            continue;
                        }
                        if (inLongString)
                        {
                            if (searchPos + 1 < length && luaCode[searchPos] == ']' && luaCode[searchPos + 1] == ']')
                            {
                                inLongString = false;
                                searchPos += 2;
                                continue;
                            }
                            searchPos++;
                            continue;
                        }

                        // --- Обработка комментариев ---
                        if (!inLongComment && !inString && !inLongString && searchPos + 1 < length && luaCode[searchPos] == '-' && luaCode[searchPos + 1] == '-')
                        {
                            if (searchPos + 3 < length && luaCode[searchPos + 2] == '[' && luaCode[searchPos + 3] == '[')
                            {
                                inLongComment = true;
                                searchPos += 4;
                                continue;
                            }
                            else
                            {
                                inComment = true;
                                searchPos += 2;
                                continue;
                            }
                        }
                        if (inComment)
                        {
                            if (c == '\n')
                                inComment = false;
                            searchPos++;
                            continue;
                        }
                        if (inLongComment)
                        {
                            if (searchPos + 1 < length && luaCode[searchPos] == ']' && luaCode[searchPos + 1] == ']')
                            {
                                inLongComment = false;
                                searchPos += 2;
                                continue;
                            }
                            searchPos++;
                            continue;
                        }

                        // --- Проверка ключевых слов, увеличивающих уровень ---
                        bool isKeyword = false;
                        if (searchPos + 8 <= length && luaCode.Substring(searchPos, 8) == "function" &&
                            (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                            (searchPos + 8 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 8])))
                        {
                            level++;
                            searchPos += 8;
                            isKeyword = true;
                        }
                        else if (searchPos + 2 <= length && luaCode.Substring(searchPos, 2) == "if" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 2 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 2])))
                        {
                            level++;
                            searchPos += 2;
                            isKeyword = true;
                        }
                        else if (searchPos + 3 <= length && luaCode.Substring(searchPos, 3) == "for" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 3 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 3])))
                        {
                            level++;
                            searchPos += 3;
                            isKeyword = true;
                        }
                        else if (searchPos + 5 <= length && luaCode.Substring(searchPos, 5) == "while" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 5 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 5])))
                        {
                            level++;
                            searchPos += 5;
                            isKeyword = true;
                        }
                        else if (searchPos + 2 <= length && luaCode.Substring(searchPos, 2) == "do" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 2 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 2])))
                        {
                            level++;
                            searchPos += 2;
                            isKeyword = true;
                        }
                        else if (searchPos + 6 <= length && luaCode.Substring(searchPos, 6) == "repeat" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 6 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 6])))
                        {
                            level++;
                            searchPos += 6;
                            isKeyword = true;
                        }
                        // --- Проверка закрывающих ключевых слов ---
                        else if (searchPos + 3 <= length && luaCode.Substring(searchPos, 3) == "end" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 3 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 3])))
                        {
                            level--;
                            if (level == 0)
                            {
                                int funcEnd = searchPos + 3;
                                string funcCode = luaCode.Substring(funcStart, funcEnd - funcStart);
                                result[funcName] = funcCode;
                                pos = funcEnd; // переходим к концу end
                                break;
                            }
                            searchPos += 3;
                            isKeyword = true;
                        }
                        else if (searchPos + 5 <= length && luaCode.Substring(searchPos, 5) == "until" &&
                                 (searchPos == 0 || !char.IsLetterOrDigit(luaCode[searchPos - 1])) &&
                                 (searchPos + 5 >= length || !char.IsLetterOrDigit(luaCode[searchPos + 5])))
                        {
                            level--;
                            if (level == 0)
                            {
                                int funcEnd = searchPos + 5;
                                string funcCode = luaCode.Substring(funcStart, funcEnd - funcStart);
                                result[funcName] = funcCode;
                                pos = funcEnd;
                                break;
                            }
                            searchPos += 5;
                            isKeyword = true;
                        }

                        if (!isKeyword)
                        {
                            searchPos++;
                        }
                    }

                    // Если функция была найдена (level == 0), мы уже вышли из while с установленным pos, поэтому continue
                    if (level == 0)
                        continue;
                    else
                        pos++; // если не найдена, сдвигаемся на 1
                }
                else
                {
                    pos++;
                }
            }

            return result;
        }

        public static void LoadAllLuaFunctions(string directoryPath,Dictionary<string,string> AllLuaFuncs)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return;

            var luaFiles = Directory.GetFiles(directoryPath, "*.lua", SearchOption.AllDirectories);
            //Encoding.UTF8
            var encoding = Encoding.GetEncoding("windows-1251");

            foreach (var file in luaFiles)
            {
                try
                {
                    var content = File.ReadAllText(file, encoding);
                    // Парсим все функции
                    var functions = ExtractLuaFunctions(content);
                    foreach (var kvp in functions)
                    {
                        // Если функция с таким именем уже существует, можно перезаписать или пропустить – выбираем перезапись (последний файл)
                        AllLuaFuncs[kvp.Key] = kvp.Value;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка загрузки Lua-файла {file}: {ex.Message}");
                }
            }

            //var res = 0;
        }
    }
}
