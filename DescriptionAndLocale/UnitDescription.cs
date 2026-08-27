using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MS.WindowsAPICodePack.Internal;
using System.Windows.Documents;
using System.Linq;
using System.Net.Http.Headers;
using System.ComponentModel;
using System.Windows.Markup;
using System;
using System.Runtime.CompilerServices;
using static System.Windows.Forms.AxHost;
using KBAtomCreator.Atoms;

namespace KBAtomCreator.DescriptionAndLocale
{

    //,

    //    [Description("Загруженный ресурс")]
    //LoadedResource
    // перечисление классов способностей
    public enum AbilityClass
    {
        [Description("Перемещение и атака")]
        moveattack,

        [Description("Нет")]
        none,

        [Description("Бросок")]
        @throw,

        [Description("Скриптованная")]
        scripted,

        [Description("Заклинание")]
        spell
    }
    public enum AbilityState
    {
        Good = 1,
        Bad = 0,

    }
    public class UnitDescription
    {
        //имя в единственном
        public string UnitNameOne { get; set; } = string.Empty;
        //имя во множественном
        public string UnitNameMany { get; set; } = string.Empty;
        public List<FeatureDescription> Features { get; set; } = new List<FeatureDescription>();
        public List<AbilityDescription> Abilities { get; set; } = new List<AbilityDescription>();

        public string GetFeaturesLabel(string atomName) 
        {
            return $"cpi_{atomName}_feat";
        }
        public string GetFeaturesHints()
        {
            var featuresSB = new StringBuilder();
            foreach (var feature in Features)
            {
                //список всех фич
                featuresSB.Append($"{feature.ResourceName}_header/{feature.ResourceName}_hint");
                if (Features.IndexOf(feature) != Features.Count - 1)
                    featuresSB.Append(",");
            }
            return featuresSB.ToString();
        }

        public string GetFeaturesHeader(string atomName)
        {
            var featuresSB = new StringBuilder();
            var featuresHints = GetFeaturesHints();
            featuresSB.Append($"cpi_{atomName}_feat={featuresHints}");
            return featuresSB.ToString();
        }

        /// <summary>
        ///  Перечень Особенностей через ,. Особенность1,...,Особенность2.
        /// </summary>
        /// <returns></returns>
        public string LocalizedFeatures()
        {

            var sb = new StringBuilder();
            foreach (var feature in Features)
            {
                sb.Append($"{feature.Name}");
                if (Features.IndexOf(feature) != Features.Count - 1)
                    sb.Append(",");
                else
                    sb.Append(".");

            }
            return sb.ToString();
        }

        public string LocalizedHeader(string atomName)
        {      
            var featuresHeaders = LocalizedFeatures();
            return $"cpi_{atomName}_feat={featuresHeaders}";
        }


        public void SaveToFile(string parentFolder, string atomName)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"cpn_{atomName}={UnitNameOne}");
            sb.AppendLine($"cpsn_{atomName}={UnitNameMany}");
            sb.AppendLine("// Особенности");

            sb.AppendLine(LocalizedHeader(atomName));
            sb.AppendLine("");

            foreach (var feature in Features)
            {
                sb.AppendLine($"{feature.ResourceName}_header=^def_hint_t0^{feature.Name}");
                sb.AppendLine($"{feature.ResourceName}_hint=^def_hint_t1^{feature.Description}");
                sb.AppendLine("");

            }
            sb.AppendLine("// Способности");
            foreach (var ability in Abilities) {
                var abi_state = ability.State == AbilityState.Good ? "[Good]" : "[Bad]";
                sb.AppendLine($"{ability.ResourceName}_name={ability.Name}");
                sb.AppendLine($"{ability.ResourceName}_head=^special_tC^{abi_state}");
                sb.AppendLine($"{ability.ResourceName}_hint=^special_t^{ability.Description}");
                sb.AppendLine("");
            }

            string unitDescString= sb.ToString();

            //Encoding.1
            //// Получаем кодировку Windows-1251 (1251)
            //Encoding win1251Encoding = Encoding.GetEncoding("windows-1251");           

            var filename = Path.Combine(parentFolder, $"rus_{atomName}.lng");
            File.WriteAllText(filename, unitDescString, Encoding.Unicode);
        }
    }

    // Feature.cs
    public class FeatureDescription : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private string _resourceName; // имя в ресурсах игры
        //private bool _isTranslatedNameManual = false;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    ResourceName = RussianTransliteration.Transliterate(value);
                    //// Автоматически обновляем TranslatedName, если он не был изменен вручную
                    //if (!_isTranslatedNameManual)
                    //{
                        
                    //}
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (_resourceName != value)
                {
                    _resourceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() { 
            return $"{Name}:{Description}";
        }
    }

    // Ability.cs
    public class AbilityDescription : INotifyPropertyChanged
    {

        private string _name;
        private string _description;
        private string _resourceName;
        private string _abilityTone;
        private AbilityState _state;
        private AbilityClass _abilityClass;
        private AbilityTemplate _abilityTemplate;
        private AbilityAction _abilityAction;
        public AbilityAction AbilityAction
        {
            get => _abilityAction;
            set
            {
                if (_abilityAction != value)
                {
                    _abilityAction = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsGenerateLua));
                }
            }
        }

        public bool IsGenerateLua => AbilityAction == AbilityAction.GenerateLua;

        public string AbilityTone
        {
            get => _abilityTone;
            set { _abilityTone = value; OnPropertyChanged(); }
        }

        public AbilityState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    ResourceName = RussianTransliteration.Transliterate(value);

                    //// Автоматически обновляем TranslatedName, если он не был изменен вручную
                    //if (!_isTranslatedNameManual)
                    //{
                    //    TranslatedName = RussianTransliteration.Transliterate(value);
                    //}
                

                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (_resourceName != value)
                {
                    _resourceName = value;
                    OnPropertyChanged();

                    //// Помечаем, что пользователь изменил значение вручную
                    //if (!string.IsNullOrEmpty(value))
                    //{
                    //    _isTranslatedNameManual = true;
                    //}
                }
            }
        }

        public AbilityClass AbilityClass
        {
            get => _abilityClass;
            set
            {
                if (_abilityClass != value)
                {
                    _abilityClass = value;
                    OnPropertyChanged();
                    //OnPropertyChanged(nameof(IsScripted)); // Уведомляем об изменении видимости

                    //// Если класс не scripted, сбрасываем шаблон на значение по умолчанию
                    //if (value != AbilityClass.scripted)
                    //{
                    //    AbilityTemplate = AbilityTemplate.SingleTarget;
                    //}
                }
            }
        }

        public AbilityTemplate AbilityTemplate
        {
            get => _abilityTemplate;
            set
            {
                _abilityTemplate = value;
                OnPropertyChanged();
            }
        }

        // Свойство для привязки видимости ComboBox с шаблонами
        //public bool IsScripted => AbilityClass == AbilityClass.scripted;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string NameString
        {
            get { return $"{ResourceName}_name"; }
        }

        public string HeadString
        {
            get { return $"{ResourceName}_head"; }
        }

        public string HintString
        {
            get { return $"{ResourceName}_hint"; }
        }

      
        public override string ToString()
        {
            return $"{Name}:{Description}";
        }
    }
    public class EnumBindingSourceExtension : MarkupExtension
    {
        public Type EnumType { get; private set; }

        public EnumBindingSourceExtension(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
                throw new ArgumentException("Type must be an enum");

            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(EnumType);
        }
    }
}
