using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace KBAtomCreator.FileProcess
{
    internal class FileProcessing
    {
        public static void CreateEmptyFileIfNotExists(string filePath)
        {
            // Check if the file already exists
            if (!File.Exists(filePath))
            {
                // If the file does not exist, create it.
                // File.Create returns a FileStream, which should be disposed.
                // The 'using' statement ensures proper disposal.
                using (FileStream fs = File.Create(filePath))
                {
                    // No content is written, so the file remains empty.
                }
            }
        }

        public static void CopyFile(string filePath, string destinationFolder)
        {
            var filename = Path.GetFileName(filePath);
            var destinationFilename = Path.Combine(destinationFolder, filename);
            if (!Path.Exists(destinationFilename))
                File.Copy(filePath, destinationFilename);
            else
                Logger.WriteLog($"Копирование отменено. Указанный файл {destinationFilename} уже существует в {destinationFolder}.");
        }

        public static void CopyFileWithRename(string filePath, string destinationFolder, string newFilename)
        {
            var filename = Path.GetFileName(filePath);
            var destinationFilename = Path.Combine(destinationFolder, filename);
            var destinationFilenameNew = Path.Combine(destinationFolder, newFilename);

            if (Path.Exists(destinationFilenameNew))
            {
                Logger.WriteLog($"Копирование отменено. Указанный файл {destinationFilename} уже существует в {destinationFolder}.");
                return;
            }
            File.Copy(filePath, destinationFilename);
            File.Move(destinationFilename, destinationFilenameNew);
         
                
        }

        public static void CopyFiles(List<string> fileList, string destinationFolder)
        {
            foreach (string path in fileList)
            {
                var filename = Path.GetFileName(path);
                var destinationFilename = Path.Combine(destinationFolder, filename);
                if (!Path.Exists(destinationFilename))
                    File.Copy(path, destinationFilename);
                else
                    Logger.WriteLog($"Копирование отменено. Указанный файл {destinationFilename} уже существует в {destinationFolder}.");
            }
        }


        public static bool FindAndCopy(string sourceFilename, string findFolder, string destinationFolder)
        {
            var sourceFilepath = FindFile(findFolder, sourceFilename);

            if (sourceFilepath == null)
            {
                Logger.WriteLog($"Копирование отменено. Указанный файл {sourceFilename} не найден в {findFolder}.");
                return false;
            }

            if (sourceFilepath != null)
                CopyFile(sourceFilepath, destinationFolder);
            return true;
        }

        public static void FindAndCopyWithRename(string sourceFilename, string findFolder, string destinationFolder, string newFilename) 
        {
            var destinationFilename = Path.Combine(destinationFolder, sourceFilename);
            var destinationFilenameNew = Path.Combine(destinationFolder, newFilename);
            if (Path.Exists(destinationFilenameNew))
            {
                Logger.WriteLog($"Переименование отменено. Указанный файл {destinationFilenameNew} уже существует.");
                return;
            }

            if (Path.Exists(sourceFilename))
            {
                Logger.WriteLog($"Копирование не выполнено. Исходный файл {sourceFilename} не существует.");
                return;
            }

            

            var fileMoved = FindAndCopy(sourceFilename, findFolder, destinationFolder);
            if (fileMoved)
                File.Move(destinationFilename, destinationFilenameNew);


            
        }

        public static List<string> FindFilesInDirectoryLinq(string sourceFolder,string extension)
        {
            var findedPaths = new List<string>();

            try
            {
                if (!Directory.Exists(sourceFolder))
                {
    

                    Logger.WriteLog($"Directory does not exist.");
                    return findedPaths;
                }

                // Используем LINQ для поиска всех .dds файлов (регистронезависимо)
                findedPaths = Directory
                    .GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                    .Where(file => Path.GetExtension(file).Equals(extension,
                                 StringComparison.OrdinalIgnoreCase))
                    .ToList();

            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error searching directory.", ex);
            }

            return findedPaths;
        }

        public static string FindFile(string directory, string fileName)
        {
            // Проверяем существование директории
            if (!Directory.Exists(directory))
                return null;

            try
            {
                // Ищем файл в текущей директории
                string[] files = Directory.GetFiles(directory, fileName);
                if (files.Length > 0)
                    return files[0]; // Возвращаем первый найденный файл

                // Рекурсивный поиск в поддиректориях
                foreach (string subDirectory in Directory.GetDirectories(directory))
                {
                    string foundFile = FindFile(subDirectory, fileName);
                    if (foundFile != null)
                        return foundFile;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Обрабатываем ошибки доступа (можно добавить логирование)
                Logger.WriteLog($"Не смогли найти файл {fileName}.", ex);
            }

            return null; // Файл не найден
        }

    }
}
