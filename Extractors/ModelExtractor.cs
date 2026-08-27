using KBAtomCreator.FileProcess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KBAtomCreator.Extractors
{
    internal class ModelExtractor
    {
        public static List<string> ExtractTexturesFromModel(string modelPath)
        {
            var textureNames = new List<string>();

            try
            {
                // Читаем файл в текстовом режиме с кодировкой ANSI
                string fileContent = File.ReadAllText(modelPath, Encoding.Default);

                // Регулярное выражение для поиска имен DDS файлов
                //Regex regex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_\-]*\.dds",
                //                      RegexOptions.IgnoreCase);
                Regex regex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_\-]*diff[a-zA-Z0-9_\-]*\.dds",
                                      RegexOptions.IgnoreCase);


                MatchCollection matches = regex.Matches(fileContent);

                foreach (Match match in matches)
                {
                    string textureName = match.Value;
                    // Добавляем только уникальные имена
                    if (!textureNames.Contains(textureName))
                    {
                        textureNames.Add(textureName);
                        //Logger.WriteLog($"Текстура уже найдена: {textureName}");
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.WriteLog($"Ошибка извлечения текстуры из файла {modelPath}.", ex);
            }

            return textureNames;

       
        }

        public static List<string> FindTexturesByName(string sourceFolder, List<string> textureNames)
        {
            var foundTextures = new List<string>();

            try
            {
                if (!Directory.Exists(sourceFolder))
                {
                    Logger.WriteLog($"[Поиск текстур] Directory does not exist: {sourceFolder}");
                    return foundTextures;
                }

                // Создаем словарь для быстрого поиска по именам файлов
                var fileDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Заполняем словарь: ключ - имя файла, значение - полный путь
                foreach (string file in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);
                    if (!fileDict.ContainsKey(fileName))
                    {
                        fileDict[fileName] = file;
                    }
                }

                // Ищем каждое имя текстуры в словаре
                foreach (string textureName in textureNames)
                {
                    string fileName = Path.GetFileName(textureName);

                    if (fileDict.TryGetValue(fileName, out string foundPath))
                    {
                        foundTextures.Add(foundPath);
                    }
                    else
                    {
                        Logger.WriteLog($"Not found: {textureName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error searching textures",ex);
            }

            return foundTextures;
        }
        // Вспомогательная функция для замены байтов в массиве
        private static bool ReplaceBytesInArray(byte[] array, byte[] oldBytes, byte[] newBytes)
        {
            bool replaced = false;
            for (int i = 0; i <= array.Length - oldBytes.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < oldBytes.Length; j++)
                {
                    if (array[i + j] != oldBytes[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    // Заменяем старые байты на новые
                    for (int j = 0; j < oldBytes.Length; j++)
                    {
                        array[i + j] = newBytes[j];
                    }
                    replaced = true;
                    // Пропускаем длину oldBytes, чтобы не заменять частично перекрывающиеся вхождения
                    i += oldBytes.Length - 1;
                }
            }

            return replaced;
        }
        // Функция для копирования и модификации файла модели
        private static void CopyAndModifyModelFile(string sourceModelPath, string destFolder, string newModelName, Dictionary<string, string> textureReplacements)
        {
            string destPath = Path.Combine(destFolder, newModelName);

            if (sourceModelPath == "")
            {
                Exception e = new Exception("sourceModelPath empty");
                Logger.WriteLog("Исходная модель не найдена", e);
                return;
            }
                

            

            // Читаем бинарное содержимое файла
            byte[] fileBytes = File.ReadAllBytes(sourceModelPath);
            Encoding encoding = Encoding.ASCII;


            // Конвертируем в строку для поиска (может потребоваться использовать определенную кодировку)
            string fileContent = Encoding.UTF8.GetString(fileBytes);

            bool modified = false;           

            foreach (var replacement in textureReplacements)
            {
                string oldTextureName = replacement.Key;
                string newTextureName = replacement.Value;

                // Получаем байтовые представления
                byte[] oldBytes = encoding.GetBytes(oldTextureName);
                byte[] newBytes = encoding.GetBytes(newTextureName);

                // Проверяем, что длины байтовых массивов совпадают
                if (oldBytes.Length != newBytes.Length)
                {
                    // Пропускаем замену, если длины не совпадают
                    // В идеале, мы сгенерировали имена так, чтобы длины в символах совпадали, но в байтах должны быть одинаковы для ASCII
                    continue;
                }

                // Ищем и заменяем все вхождения oldBytes на newBytes в fileBytes
                modified |= ReplaceBytesInArray(fileBytes, oldBytes, newBytes);
            }

            if (modified)
            {
                File.WriteAllBytes(destPath, fileBytes);
            }
            else
            {
                File.Copy(sourceModelPath, destPath, true);
            }
        }

        public static void ExtractAndSaveTextures(string newAtomName, string modelFile, string newModelFile, string sourceDataFolder,string saveFolder)
        {
            // файлы текстур копируем в новую папку            
            List<string> textureNames = ExtractTexturesFromModel(modelFile);
            List<string> textureFiles = FindTexturesByName(sourceDataFolder, textureNames);

            // Создаем словарь для замены имен текстур
            Dictionary<string, string> textureReplacements = new Dictionary<string, string>();
            foreach (string textureName in textureNames)
            {
                string textureNameWithoutExt = Path.GetFileNameWithoutExtension(textureName);
                string extension = Path.GetExtension(textureName); // сохраняем оригинальное расширение

                // Создаем новое имя текстуры с учетом требуемой длины
                string newTextureName = GenerateTextureName(newAtomName, textureNameWithoutExt);

                // Сохраняем расширение .dds 
                string newTextureNameWithExt = newTextureName + ".dds";

                textureReplacements[textureName] = newTextureNameWithExt;
            }

            // Копируем и переименовываем текстуры
            foreach (var textureFile in textureFiles)
            {
                string fileName = Path.GetFileName(textureFile);
                if (textureReplacements.ContainsKey(fileName))
                {
                    string newTextureName = textureReplacements[fileName];
                    FileProcessing.CopyFileWithRename(textureFile, saveFolder, newTextureName);
                }
                else
                {
                    FileProcessing.CopyFile(textureFile, saveFolder);
                }
            }

            // Копируем и модифицируем файл модели с замененными текстурами
            CopyAndModifyModelFile(modelFile, saveFolder, newModelFile, textureReplacements);
        }

        // Функция для генерации нового имени текстуры с той же длиной
        private static string GenerateTextureName(string newAtomName, string originalTextureName)
        {
            string originalNameWithoutExt = Path.GetFileNameWithoutExtension(originalTextureName);
            int targetLength = originalNameWithoutExt.Length;

            // Если новое имя короче требуемой длины - дополняем его
            if (newAtomName.Length < targetLength)
            {
                // Дополняем новое имя до нужной длины, добавляя часть из оригинального имени
                int charsNeeded = targetLength - newAtomName.Length;
                string addition = originalNameWithoutExt.Substring(newAtomName.Length, Math.Min(charsNeeded, originalNameWithoutExt.Length - newAtomName.Length));
                return newAtomName + addition;
            }
            // Если новое имя длиннее - обрезаем его
            else if (newAtomName.Length > targetLength)
            {
                return newAtomName.Substring(0, targetLength);
            }
            // Если длина совпадает - используем как есть
            else
            {
                return newAtomName;
            }
        }
    }
}
