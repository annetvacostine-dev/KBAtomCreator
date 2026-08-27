using KBAtomCreator.Atoms;
using KBAtomCreator.DescriptionAndLocale;
using KBAtomCreator.FileProcess;
using KBAtomCreator.Loaders;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;


namespace KBAtomCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Splashscreen splashScreen;
        private AtomLoader? atomLoader;
        private readonly string settingsPath = "settings.txt";
        private AppSettings appSettings = new AppSettings();
        //public string SaveUnitPath { get; set; }
        private string currentResultsFolder = "";

        //особенности
        private static Dictionary<string, string> AllLngEntries = new Dictionary<string, string>();
        private static Dictionary<string, string> AllLuaFuncs = new Dictionary<string, string>();

        //private Dictionary<string, FeatureDescription> oldFeatures;

        private AtomInfo _currentAtomInfo;
        public AtomInfo CurrentAtomInfo
        {
            get => _currentAtomInfo;
            set
            {
                _currentAtomInfo = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<FeatureDescription> featuresNewUnit = new ObservableCollection<FeatureDescription>();
        private ObservableCollection<AbilityDescription> abilitiesNewUnit = new ObservableCollection<AbilityDescription>();

        // Список для автодополнения способностей
        public List<string> AbilityAutoCompleteItems { get; } = new List<string>
        {
            "[br]", "[d]", "[s]", "[lg]", "[u]", "[hc]", "[rc]", "[/s]", "[damage]", "[fdamage]",
            "[pdamage]", "[mdamage]", "[adamage]", "[sdamage]", "[gdamage]", "[cure]", "[cure2]",
            "[ap]", "[duration]", "[shock]", "[power]", "[penalty]", "[power%]", "[penalty%]",
            "[summon]", "[summon2]", "[health_count]", "[lsummon]", "[lsummon2]", "[leadp]",
            "[level]", "[burn]", "[poison]", "[mana]", "[bonus]", "[bonus%]", "[dmg_k]", "[dmg_k%]",
            "[sburn]", "[spoison]", "[sshock]", "[stun]", "[sstun]"
        };

        // Список для автодополнения особенностей
        public List<string> FeatureAutoCompleteItems { get; } = new List<string>
        {
            "[s]", "[/s]", "[moral]", "[mdesc]", "[br]", "[sys]", "[sel]", "[dis]", "[/c]", "[res]",
            "[hero_lead]", "[lead]", "[unit_count]", "[add]", "[unit_max_count]", "[allhp]", "[unitsizes]"
        };

        private List<string> _filterOptions = new List<string> { "hero", "chesspiece", "castle", "throwable", "static", "pawn" };
        private string _currentFilter = "chesspiece"; // по умолчанию



        public MainWindow(Splashscreen splash)
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing; // подписываемся
            splashScreen = splash;
            LoadSettings();
            // Запускаем загрузку ресурсов в фоновом потоке
            var worker = new BackgroundWorker();
            worker.DoWork += LoadResources;
            worker.RunWorkerCompleted += ResourcesLoaded;
            worker.RunWorkerAsync();

            
            

            featuresItemsControl.ItemsSource = featuresNewUnit;
            abilitiesItemsControl.ItemsSource = abilitiesNewUnit;
            //abilityClassComboBox.ItemsSource = Enum.GetValues(typeof(AbilityClass));

            //this.DataContext = CurrentAtomInfo;
            DataContext = this;
        }

         


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings(); 
        }


        private void LoadResources(object sender, DoWorkEventArgs e)
        {
            if (string.IsNullOrEmpty(appSettings.AtomsFolder))
                return;

            string atomsPath = appSettings.AtomsFolder;
            // Выполняем загрузку данных в фоновом потоке (без доступа к UI)
            var filter = _currentFilter; // предполагается, что поле уже содержит "chesspiece"
            var atomLoader = new AtomLoader(atomsPath, filter);

            // грузим файлы локализации
            LngLoader.LoadAllLngFiles(appSettings.AtomsFolder, AllLngEntries);
            LuaLoader.LoadAllLuaFunctions(appSettings.AtomsFolder, AllLuaFuncs); // <-- добавлено

            // Теперь обновляем UI через диспетчер
            Dispatcher.Invoke(() =>
            {
                FilterComboBox.ItemsSource = _filterOptions;
                FilterComboBox.SelectedItem = "chesspiece";

                // Обновляем комбобокс со списком атомов
                AtomSearchComboBox.ItemsSource = atomLoader.AtomsInfo;
                if (atomLoader.AtomsInfo.Any())
                    AtomSearchComboBox.SelectedIndex = 0;

                //// Сохраняем загрузчик для дальнейшей работы (если нужно)
                //_currentLoader = atomLoader;
            });
        }

        private void ResourcesLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            // Закрываем окно загрузки в UI потоке
            splashScreen.Dispatcher.Invoke(() => splashScreen.Close());

            // Показываем главное окно
            this.Dispatcher.Invoke(() => this.Show());
        }

        void SaveSettings()
        { 
            appSettings.AtomsFolder = DataFolderTextbox.Text;
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(appSettings));
        }
        void LoadSettings() {
            if (!Path.Exists(settingsPath))
            {
                var defautsetting = JsonSerializer.Serialize<AppSettings>(
                    new AppSettings()
                    );
                File.WriteAllText(settingsPath,defautsetting);
            }

            var settingsText = File.ReadAllText(settingsPath);
            var settingsObject = JsonSerializer.Deserialize<AppSettings>(settingsText);
            appSettings = settingsObject;

            var atomsPath = settingsObject?.AtomsFolder;
            
            DataFolderTextbox.Text = (atomsPath != null) ? atomsPath : "";
 

        }

        //void FillCombobox(AtomLoader atomLoader) {

        //    Dispatcher.Invoke(() => AtomSearchComboBox.ItemsSource = atomLoader.AtomsInfo);
        //    AtomSearchComboBox.ItemsSource = atomLoader.AtomsInfo;
        //}
        void LoadAtoms(string loadedPath, string filter)
        {
            if (loadedPath != string.Empty)
            {
                atomLoader = new AtomLoader(loadedPath, filter);
                // грузим файлы локализации
                LngLoader.LoadAllLngFiles(appSettings.AtomsFolder, AllLngEntries);
                Dispatcher.Invoke(() => AtomSearchComboBox.ItemsSource = atomLoader.AtomsInfo);
            }

        }

        
        private void DataFolderTextbox_Drop(object sender, DragEventArgs e)
        {
            var loadedPath = "";
            // Check if the dropped data is of the expected format (e.g., a file)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Get the array of dropped file paths
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Assuming you want to display the first file path in the TextBox
                if (files.Length > 0)
                {
                    loadedPath = files[0];
                    if (Directory.Exists(loadedPath))
                    {
                        ((TextBox)sender).Text = loadedPath;
                        SaveSettings();
                    }
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                // Handle dropped text data
                loadedPath = (string)e.Data.GetData(DataFormats.Text);
                if (Directory.Exists(loadedPath))
                {
                    ((TextBox)sender).Text = loadedPath;
                    SaveSettings();
                }
            }
            LoadAtoms(loadedPath, _currentFilter);
            
            e.Handled = true;
        }

        private void OpenDirButton_Click(object sender, RoutedEventArgs e)
        {
            
            using(var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var loadedPath = folderDialog.FileName;
                    if (Directory.Exists(loadedPath))
                        DataFolderTextbox.Text = loadedPath;
                    LoadAtoms(loadedPath, _currentFilter);  
                }
            }
        }

        private void DataFolderTextbox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void DataFolderTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            var old_path = appSettings.AtomsFolder;
            SaveSettings();

            var new_path = appSettings.AtomsFolder;
            LoadAtoms(new_path, _currentFilter);
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterComboBox.SelectedItem != null)
            {
                _currentFilter = FilterComboBox.SelectedItem.ToString();
                var new_path = appSettings.AtomsFolder;
                LoadAtoms(new_path, _currentFilter);
            }
        }

        //private void LoadAtomInfoToForm(AtomInfo atomInfo)
        //{         
        //        // Arena Params
        //        if (atomInfo.ArenaParams != null)
        //        {
        //            atomNameTextBox.Text = atomInfo.AtomName ?? "";
        //            txtFeaturesLabel.Text = atomInfo.ArenaParams.FeaturesLabel ?? "";
        //            txtFeaturesHints.Text = atomInfo.ArenaParams.FeaturesHints ?? "";
        //            txtRace.Text = atomInfo.ArenaParams.Race ?? "";
        //            txtCost.Text = atomInfo.ArenaParams.Cost.ToString();
        //            txtLevel.Text = atomInfo.ArenaParams.Level.ToString();
        //            txtLeadership.Text = atomInfo.ArenaParams.Leadership.ToString();
        //            txtAttack.Text = atomInfo.ArenaParams.Attack.ToString();
        //            txtDefense.Text = atomInfo.ArenaParams.Defense.ToString();
        //            txtDefenseup.Text = atomInfo.ArenaParams.Defenseup.ToString();
        //            txtInitiative.Text = atomInfo.ArenaParams.Initiative.ToString();
        //            txtSpeed.Text = atomInfo.ArenaParams.Speed.ToString();
        //            txtHitpoint.Text = atomInfo.ArenaParams.Hitpoint.ToString();
        //            txtMovetype.Text = atomInfo.ArenaParams.Movetype.ToString();
        //            txtKrit.Text = atomInfo.ArenaParams.Krit.ToString();
        //            txtHitback.Text = atomInfo.ArenaParams.Hitback.ToString();
        //            txtHitbackprotect.Text = atomInfo.ArenaParams.Hitbackprotect.ToString();
        //            txtAttacks.Text = atomInfo.ArenaParams.Attacks ?? "";
        //            txtPosthitmaster.Text = atomInfo.ArenaParams.Posthitmaster ?? "";
        //            txtPosthitslave.Text = atomInfo.ArenaParams.Posthitslave ?? "";
        //            txtAutofight.Text = atomInfo.ArenaParams.Autofight.ToString();
        //            txtFeatures.Text = atomInfo.ArenaParams.Features ?? "";

        //            // Resistances
        //            if (atomInfo.ArenaParams.Resistances != null)
        //            {
        //                txtPhysical.Text = atomInfo.ArenaParams.Resistances.Physical.ToString();
        //                txtPoison.Text = atomInfo.ArenaParams.Resistances.Poison.ToString();
        //                txtMagic.Text = atomInfo.ArenaParams.Resistances.Magic.ToString();
        //                txtFire.Text = atomInfo.ArenaParams.Resistances.Fire.ToString();
        //                txtAstral.Text = atomInfo.ArenaParams.Resistances.Astral.ToString();
        //        }
        //        }

        //}

        private string ReplaceLabels(string input, Dictionary<string, string> dict)
        {
            if (string.IsNullOrEmpty(input) || dict == null || dict.Count == 0)
                return input;

            const int maxIterations = 10; // защита от бесконечного цикла
            int iteration = 0;
            string result = input;
            bool replaced;

            do
            {
                replaced = false;
                // Ищем все вхождения <label=ключ>
                var matches = System.Text.RegularExpressions.Regex.Matches(result, @"<label=([^>]+)>");
                if (matches.Count == 0)
                    break;

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string fullTag = match.Value; // например, "<label=special_lasso_name>"
                    string key = match.Groups[1].Value.Trim(); // "special_lasso_name"

                    if (dict.TryGetValue(key, out string replacement))
                    {
                        result = result.Replace(fullTag, replacement);
                        replaced = true;
                    }
                    // Если ключ не найден, оставляем тег как есть (можно удалить, но лучше оставить)
                }

                iteration++;
            } while (replaced && iteration < maxIterations);

            return result;
        }

        private string StripHintPrefix(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Ищем последний символ '^' – всё, что после него, является чистым текстом
            int lastCaret = input.LastIndexOf('^');
            if (lastCaret >= 0 && lastCaret < input.Length - 1)
                return input.Substring(lastCaret + 1);

            return input; // если '^' не найден, возвращаем как есть
        }

        private void LoadFeatures(AtomInfo atomInfo)
        {
            if (atomInfo?.ArenaParams == null)
                return;

            string featuresHints = atomInfo.ArenaParams.FeaturesHints;
            if (string.IsNullOrWhiteSpace(featuresHints))
            {
                if (featuresNewUnit != null)
                    featuresNewUnit.Clear();
                return;
            }

            // Инициализация коллекции
            if (featuresNewUnit == null)
            {
                featuresNewUnit = new ObservableCollection<FeatureDescription>();
                featuresItemsControl.ItemsSource = featuresNewUnit;
            }
            else
            {
                featuresNewUnit.Clear();
            }

            var parts = featuresHints.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                string trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart))
                    continue;

                var slashParts = trimmedPart.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (slashParts.Length < 2)
                    continue;

                string headerKey = slashParts[0].Trim();   // например, "poison_resistance_header"
                string hintKey = slashParts[1].Trim();     // например, "poison_resistance_hint"

                // Получаем название из словаря, удаляем префикс
                string name = headerKey;
                if (AllLngEntries.TryGetValue(headerKey, out string rawName))
                {
                    name = StripHintPrefix(rawName);
                }

                // Получаем описание из словаря, удаляем префикс
                string description = hintKey;
                if (AllLngEntries.TryGetValue(hintKey, out string rawDescription))
                {
                    //description = StripHintPrefix(rawDescription);
                    description = ReplaceLabels(StripHintPrefix(rawDescription), AllLngEntries); // <-- замена
                }

                // Ресурсное имя – ключ без "_header"
                string resourceName = headerKey;
                if (headerKey.EndsWith("_header", StringComparison.OrdinalIgnoreCase))
                {
                    resourceName = headerKey.Substring(0, headerKey.Length - "_header".Length);
                }

                var feature = new FeatureDescription
                {
                    Name = name,
                    Description = description,
                    ResourceName = resourceName
                };
                featuresNewUnit.Add(feature);
            }
        }

        private void LoadAbilities(AtomInfo atomInfo)
        {
            if (atomInfo?.ArenaParams == null)
                return;

            // Инициализация коллекции
            if (abilitiesNewUnit == null)
            {
                abilitiesNewUnit = new ObservableCollection<AbilityDescription>();
                abilitiesItemsControl.ItemsSource = abilitiesNewUnit;
            }
            else
            {
                abilitiesNewUnit.Clear();
            }

            var attacksInfo = atomInfo.ArenaParams.AttacksInfo;
            if (attacksInfo == null || attacksInfo.Count == 0)
                return;

            foreach (var attackBlock in attacksInfo)
            {
                // Пропускаем атаку "moveattack"
                if (string.Equals(attackBlock.Name, "moveattack", StringComparison.OrdinalIgnoreCase))
                    continue;


                if (string.IsNullOrEmpty(attackBlock.Content))
                    continue;

                // Парсим блок для извлечения hint и head
                string hintKey = null;
                string headKey = null;

                var lines = attackBlock.Content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("hinthead="))
                        headKey = trimmed.Substring("hinthead=".Length).Trim();
                    else if (trimmed.StartsWith("hint="))
                        hintKey = trimmed.Substring("hint=".Length).Trim();
                }

                // Формируем ключ для названия
                string nameKey;
                if (!string.IsNullOrEmpty(headKey) && headKey.EndsWith("_head"))
                {
                    // Заменяем окончание _head на _name
                    nameKey = headKey.Substring(0, headKey.Length - 5) + "_name";
                }
                else
                {
                    // fallback: используем имя блока
                    nameKey = $"{attackBlock.Name}_name";
                }

                // Получаем локализованное название
                string displayName = attackBlock.Name; // fallback
                if (AllLngEntries.TryGetValue(nameKey, out string localizedName))
                    displayName = StripHintPrefix(localizedName);

                // Получаем локализованное описание из hint
                string description = string.Empty;
                if (!string.IsNullOrEmpty(hintKey))
                {
                    if (AllLngEntries.TryGetValue(hintKey, out string localizedHint))
                        //description = StripHintPrefix(localizedHint);
                        description = ReplaceLabels(StripHintPrefix(localizedHint), AllLngEntries);
                    else
                        description = hintKey;
                }

                // Получаем локализованную тональность из head (если есть)
                string abilityTone = string.Empty;
                if (!string.IsNullOrEmpty(headKey))
                {
                    if (AllLngEntries.TryGetValue(headKey, out string localizedHead))
                        abilityTone = StripHintPrefix(localizedHead);
                    else
                        abilityTone = headKey;
                }

                // ResourceName = имя блока атаки
                string resourceName = attackBlock.Name;

                var ability = new AbilityDescription
                {
                    Name = displayName,
                    Description = description,
                    ResourceName = resourceName,
                    AbilityTone = abilityTone,
                    AbilityAction = AbilityAction.NoCopy
                    //AbilityTemplate = AbilityTemplate.LoadedResource  // <-- новое присвоение
                };

                abilitiesNewUnit.Add(ability);
            }
        }

        private void AtomSearchComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var selectedAtomInfo = AtomSearchComboBox.SelectedItem as AtomInfo;
            //if (selectedAtomInfo != null)                
            //    LoadAtomInfoToForm(selectedAtomInfo);
            var selectedAtomInfo = AtomSearchComboBox.SelectedItem as AtomInfo;
            if (selectedAtomInfo == null)
                return;
        
            CurrentAtomInfo = selectedAtomInfo;
            //var atomData = AtomInfoSerializer.Serialize(selectedAtomInfo);
            //var parser = new AbilityParser();
            //var abilities = parser.ParseAbilitiesFromAttacks(selectedAtomInfo.ArenaParams.Attacks, selectedAtomInfo.ArenaParams.AdditionalInfo);

            //// Загружаем старые особенности в словарь (пример)
            LoadFeatures(CurrentAtomInfo);

            // Загрузка способностей (новая)
            LoadAbilities(CurrentAtomInfo);
        }


        private void AddFeatureClick(object sender, RoutedEventArgs e)
        {
            featuresNewUnit.Add(new FeatureDescription());
        }

        private void RemoveFeatureClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FeatureDescription feature)
            {
                featuresNewUnit.Remove(feature);
            }
        }

        private void AddAbilityClick(object sender, RoutedEventArgs e)
        {
            abilitiesNewUnit.Add(new AbilityDescription { AbilityClass = AbilityClass.none });
        }

        private void ClearFeaturesClick(object sender, RoutedEventArgs e)
        {
            if (featuresNewUnit != null)
            {
                featuresNewUnit.Clear();
            }
            // Можно также показать сообщение, но не обязательно
        }

        private void ClearAbilitiesClick(object sender, RoutedEventArgs e)
        {
            if (abilitiesNewUnit != null)
            {
                abilitiesNewUnit.Clear();
            }

        }

        private void RemoveAbilityClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AbilityDescription ability)
            {
                abilitiesNewUnit.Remove(ability);
            }
        }

        private void SaveUnitClick(object sender, RoutedEventArgs e)
        {
            var unitDescription = new UnitDescription
            {
                UnitNameOne = txtUnitNameOne.Text,
                UnitNameMany = txtUnitNameMany.Text,
                Features = featuresNewUnit.ToList(),
                Abilities = abilitiesNewUnit.ToList()
            };

           
            var atomInfo = AtomSearchComboBox.SelectedItem as AtomInfo;

            if (atomInfo == null)
            {
                MessageBox.Show("Не выбран атом для сохранения. Пожалуйста, выберите атом из списка.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var atomNewName = NewUnitTextbox.Text;
            if (atomNewName!= string.Empty) 
            using (var folderDialog = new CommonOpenFileDialog())
            {
                folderDialog.IsFolderPicker = true;
                if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var loadedPath = folderDialog.FileName;
                    var sourceDataFolder = appSettings.AtomsFolder;
                    SaveLogicController.SaveUnit(unitDescription, atomInfo, atomNewName, loadedPath, sourceDataFolder, AllLuaFuncs);

                    // Устанавливаем текущую папку результатов и обновляем список
                    currentResultsFolder = Path.Combine(loadedPath, atomNewName);
                    RefreshResultsList();
                    MessageBox.Show("Атом сохранен!");
                    // Переключаемся на вкладку результатов
                    tabResults.IsSelected = true;
                    }
            }
        }

        private void txtUnitNameOne_TextChanged(object sender, TextChangedEventArgs e)
        {
            var unitName = ((TextBox)sender).Text;
            txtUnitNameMany.Text = $"{unitName}";
            NewUnitTextbox.Text = RussianTransliteration.Transliterate(unitName);
        }

        private void SelectResultsFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = "Выберите папку с результатами";

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentResultsFolder = folderDialog.SelectedPath;
                RefreshResultsList();
            }
        }

        private void RefreshResults_Click(object sender, RoutedEventArgs e)
        {
            RefreshResultsList();
        }

        private void RefreshResultsList()
        {
            if (string.IsNullOrEmpty(currentResultsFolder) || !Directory.Exists(currentResultsFolder))
            {
                resultsListView.ItemsSource = null;
                txtCurrentFolder.Text = "Папка не выбрана";
                return;
            }

            try
            {
                txtCurrentFolder.Text = $"Текущая папка: {currentResultsFolder}";

                var fileItems = new List<FileItem>();
                var directoryInfo = new DirectoryInfo(currentResultsFolder);

                // Получаем все файлы рекурсивно
                var allFiles = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    fileItems.Add(new FileItem
                    {
                        Name = file.Name,
                        Size = FormatFileSize(file.Length),
                        Modified = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        FullPath = file.FullName,
                        FileInfo = file
                    });
                }

                resultsListView.ItemsSource = fileItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading folder: {ex.Message}");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void ResultsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (resultsListView.SelectedItem is FileItem fileItem)
            {
                try
                {
                    // Открываем файл с помощью ассоциированной программы
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fileItem.FileInfo.FullName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}");
                }
            }
        }
        private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentResultsFolder) || !Directory.Exists(currentResultsFolder))
            {
                MessageBox.Show("Выберите корректную папку!");
                return;
            }

            try
            {
                // Открываем папку в проводнике
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentResultsFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия папки: {ex.Message}");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening link: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Меню
        // Открыть ресурсы – вызывает существующий метод OpenDirButton_Click
        private void OpenResourcesMenuClick(object sender, RoutedEventArgs e)
        {
            OpenDirButton_Click(sender, e);
        }

        // Сохранить атом – вызывает существующий метод SaveUnitClick
        private void SaveUnitMenuClick(object sender, RoutedEventArgs e)
        {
            SaveUnitClick(sender, e);
        }

        // О программе – показывает диалоговое окно
        private void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Создатель атомов 1.3.3.\nСоздано Annet Valentine",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


    }
}
