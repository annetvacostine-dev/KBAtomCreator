using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;

namespace KBAtomCreator.Atoms
{
    internal class AtomLoader
    {
        
        public List<string> AtomList { get; private set; }
        public List<AtomInfo> AtomsInfo { get; private set; }
        public Dictionary<string,string>  AtomModels { get; private set; }


        private readonly string _filterClass;


        // Допустимые значения фильтра (можно вынести в константы или перечисление)
        private static readonly HashSet<string> ValidFilters = new HashSet<string>
        {
            "hero", "chesspiece", "castle", "throwable", "static", "pawn"
        };

        //public List<string> GetAtomFilesSafely(string directoryPath)
        //{
        //    var atomFiles = new List<string>();

        //    try
        //    {
        //        if (!Directory.Exists(directoryPath))
        //            return atomFiles;

        //        // Получаем файлы из текущего каталога
        //        try
        //        {
        //            var files = Directory.GetFiles(directoryPath, "*.atom");
        //            atomFiles.AddRange(files);
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            Console.WriteLine($"Нет доступа к каталогу: {directoryPath}");
        //        }

        //        // Рекурсивно обрабатываем подкаталоги
        //        string[] subDirectories;
        //        try
        //        {
        //            subDirectories = Directory.GetDirectories(directoryPath);
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            return atomFiles;
        //        }

        //        foreach (string subDirectory in subDirectories)
        //        {
        //            try
        //            {
        //                atomFiles.AddRange(GetAtomFilesSafely(subDirectory));
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Ошибка при обработке каталога {subDirectory}: {ex.Message}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Общая ошибка: {ex.Message}");
        //    }

        //    return atomFiles;
        //}
        public AtomLoader(string directoryPath, string filterClass = null) {

            _filterClass = filterClass;

            AtomModels = new Dictionary<string,string>();
            AtomList = LoadFilesByPattern(directoryPath, "*.atom");
            var modelsList = LoadFilesByPattern(directoryPath, "*.bma");
            var modelsStaticList = LoadFilesByPattern(directoryPath, "*.bms");


            modelsList.AddRange(modelsStaticList);
            foreach (var modelFile in modelsList)
            {
                var modelName = Path.GetFileName(modelFile);
                if (!AtomModels.ContainsKey(modelName))
                    AtomModels.Add(modelName, modelFile);

            }


            AtomsInfo = new List<AtomInfo>();
            //информация об атомах
            foreach (var atomfile in AtomList)
            {
                var atominfo = LoadAtomInfo(atomfile);


                // Условие фильтрации
                bool filterPasses = string.IsNullOrEmpty(_filterClass) ||
                                    atominfo.Main.Class == _filterClass;
                
                if (filterPasses)
                {                    
                    if (atominfo.Main.Models != null)
                    {
                        atominfo.ModelPaths = new List<string>();
                        foreach (var model in atominfo.Main.Models)
                        {
                            atominfo.ModelPaths.Add(AtomModels.GetValueOrDefault(model, ""));
                        }
                        
                    }
                    AtomsInfo.Add(atominfo);
                }
                

            }
            //AtomInfoSerializer.Serialize
            //var example = AtomsInfo[136];

        }
        private AtomInfo LoadAtomInfo(string filename)
        {
            var atomText = File.ReadAllText(filename, Encoding.UTF8);
            var atomName = Path.GetFileName(filename);
            var atomInfo = AtomInfoSerializer.Deserialize(atomText, atomName);
            return atomInfo;
        }
        public static List<string> LoadFilesByPattern(string directoryPath, string pattern) {

            if (string.IsNullOrEmpty(directoryPath))
            {
                Console.WriteLine("Путь не указан.");
                return new List<string>();
            }

            if (!Directory.Exists(directoryPath))
            {
                return new List<string>();
            }
            //var result = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories)
            //               .ToList();
            //return result;


            try
            {
                var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
                Console.WriteLine($"Найдено файлов: {files.Length} по паттерну '{pattern}' в {directoryPath}");
                return files.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при поиске: {ex.Message}");
                return new List<string>();
            }



        }

        

    }
}
