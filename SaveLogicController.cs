using KBAtomCreator.Abilies;
using KBAtomCreator.Atoms;
using KBAtomCreator.Codegen;
using KBAtomCreator.DescriptionAndLocale;
using KBAtomCreator.Extractors;
using KBAtomCreator.FileProcess;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Printing;


namespace KBAtomCreator
{
    internal class SaveLogicController
    {
        static readonly string iconsTemplatesPath = "Templates";
        static readonly string[] icon_categories = { "disabled", "normal", "onmouse", "onpress" };
        public static string SaveModelTextures(AtomInfo newAtomInfo, string newAtomName,string saveFolder,string sourceDataFolder)
        {
            string mainModelName = "";
            if (newAtomInfo.Main.Models != null)
                for (int modelIdx = 0; modelIdx < newAtomInfo.Main.Models.Count; modelIdx++)
                {
                    var modelFile = newAtomInfo.ModelPaths[modelIdx];
                    //FileProcessing.CopyFile(modelPath, saveFolder);

                    var sourceFilename = Path.GetFileName(modelFile);
                    var sourceExtension = Path.GetExtension(modelFile);
                    var newModelFile = $"{newAtomName}_{modelIdx}{sourceExtension}";

                    newAtomInfo.Main.Models[modelIdx] = newModelFile;
                    //FileProcessing.CopyFileWithRename(modelFile, saveFolder, newModelFile);

                    //// файлы текстур копируем в новую папку            
                    //List<string> textureNames = ModelExtractor.ExtractTexturesFromModel(modelFile);
                    //List<string> textureFiles = ModelExtractor.FindTexturesByName(sourceDataFolder, textureNames);
                    //FileProcessing.CopyFiles(textureFiles, saveFolder);

                    if (modelIdx== 0)
                        mainModelName = Path.GetFileNameWithoutExtension(modelFile);

                    ModelExtractor.ExtractAndSaveTextures(newAtomName, modelFile, newModelFile, sourceDataFolder, saveFolder);
                }

            return mainModelName;
        }
        public static void CreateAbilityIcons(string pictureSmallName, string pictureName, string saveFolder)
        {
            var smallIconTemplatePath = Path.Combine(iconsTemplatesPath, "ability_template_small.png");

            FileProcessing.CopyFileWithRename(smallIconTemplatePath, saveFolder, pictureSmallName);

            foreach (var category in icon_categories)
            {
                var IconTemplatePath = Path.Combine(iconsTemplatesPath, $"ability_template_{category}.png");
                var newPictureName = $"{pictureName}{category}.png";
                FileProcessing.CopyFileWithRename(IconTemplatePath, saveFolder, newPictureName);
            }
                
        }

        public static void SaveAtom(AtomInfo newAtomInfo, string newAtomName,UnitDescription unitDescription ,string saveFolder, Dictionary<string, string> AllLuaFuncs)
        {
            var luaFile = System.IO.Path.Combine(saveFolder, $"{newAtomName}.lua");
            //обновляем новыми хинтами
            if (newAtomInfo.ArenaParams != null)
            {
                newAtomInfo.ArenaParams.FeaturesLabel = unitDescription.GetFeaturesLabel(newAtomName);
                if (unitDescription.Features.Count > 0)
                    newAtomInfo.ArenaParams.FeaturesHints = unitDescription.GetFeaturesHints();
            }
            newAtomInfo.AtomName = $"{newAtomName}.atom";

            var atomSavePath = Path.Combine(saveFolder, $"{newAtomName}.atom");
            var abilities = new List<AtomAbility>();
            AbilityParser parser = new AbilityParser();

            foreach (var abilityDesc in unitDescription.Abilities)
            {
                // Создаём способность с передачей AbilityAction из описания
                var newAbility = AbilityTemplateFiller.CreatePredefinedAbility(
                    abilityDesc.ResourceName,
                    abilityDesc.HeadString,
                    abilityDesc.HintString,
                    abilityDesc.AbilityClass,
                    abilityAction: abilityDesc.AbilityAction // предполагаем, что свойство существует
                );

                // Создаём иконки (только если способность не NoCopy? Можно оставить как есть)
                string pictureSmallName = newAbility.GetProperty<string>("picture_small");
                string pictureName = newAbility.GetProperty<string>("picture");
                CreateAbilityIcons(pictureSmallName, pictureName, saveFolder);

                abilities.Add(newAbility);
            }

            if (newAtomInfo.ArenaParams != null)
            {
                // сохраняем код способностей     
                LuaCodeGenerator.SaveAbilities(abilities, luaFile, AllLuaFuncs, newAtomInfo.ArenaParams.AttacksInfo);

                newAtomInfo.ArenaParams.Attacks = parser.GetAbilitiesList(abilities);
                    //parser.GetAbilitiesString(abilities);
                //Кидаем способности в начало
                var abilitiesString = parser.SerializeAbilities(abilities);
                newAtomInfo.ArenaParams.AdditionalInfo = $"{abilitiesString}{newAtomInfo.ArenaParams.AdditionalInfo}";
            }

            string atomData = AtomInfoSerializer.Serialize(newAtomInfo);
            File.WriteAllText(atomSavePath, atomData);

            
            
        }
        public static void SaveUnit(UnitDescription unitDescription, AtomInfo atomInfo, string newAtomName, string savePath, string sourceDataFolder, Dictionary<string, string> AllLuaFuncs)
        {
           
            var atomName = System.IO.Path.GetFileNameWithoutExtension(atomInfo.AtomName);
            var saveFolder = System.IO.Path.Combine(savePath, newAtomName);           
            

            //файл локализации
            Directory.CreateDirectory(saveFolder);
            unitDescription.SaveToFile(saveFolder, newAtomName);

            //новый объект
            var newAtomInfo = AtomInfo.CloneAtomInfo(atomInfo);            

            //lua файл
            var luaFile = System.IO.Path.Combine(saveFolder, $"{newAtomName}.lua");
            FileProcessing.CreateEmptyFileIfNotExists(luaFile);



            //копируем все модели и текстуры
            string mainModelName = SaveModelTextures(newAtomInfo, newAtomName, saveFolder, sourceDataFolder);



             // картинка юнита
             var imageFile = $"{atomName}.png";
            FileProcessing.FindAndCopyWithRename(imageFile, sourceDataFolder, saveFolder, $"{newAtomName}.png");

        
            // коллизия
            var collisionFile = $"collision_{mainModelName}.cms";
          
            FileProcessing.FindAndCopyWithRename(collisionFile, sourceDataFolder, saveFolder, $"collision_{newAtomName}.cms");


            // армия
            var armyFile = $"army_{atomName}.atom";
            FileProcessing.FindAndCopyWithRename(armyFile, sourceDataFolder, saveFolder, $"army_{newAtomName}.atom");

            // атом
            SaveAtom(newAtomInfo, newAtomName, unitDescription, saveFolder, AllLuaFuncs);


        }
    }
}
