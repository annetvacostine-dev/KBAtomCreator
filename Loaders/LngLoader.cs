using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBAtomCreator.Loaders
{
    internal class LngLoader
    {
        public static void LoadAllLngFiles(string directoryPath, Dictionary<string, string> AllLngEntries)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                return;

            var lngFiles = Directory.GetFiles(directoryPath, "*.lng", SearchOption.AllDirectories);
            foreach (var file in lngFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(file, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        // Ищем первое '=' – ключ и значение
                        int eqIndex = line.IndexOf('=');
                        if (eqIndex <= 0) continue;
                        string key = line.Substring(0, eqIndex).Trim();
                        string value = line.Substring(eqIndex + 1).Trim();
                        // Если ключ уже есть – перезаписываем (можно и пропустить, но перезапись безопаснее)
                        AllLngEntries[key] = value;
                    }
                }
                catch (Exception ex)
                {
                    // Логируем или игнорируем ошибку конкретного файла
                    Debug.WriteLine($"Ошибка загрузки {file}: {ex.Message}");
                }
            }

            var a = 0;
        }
    }
}
