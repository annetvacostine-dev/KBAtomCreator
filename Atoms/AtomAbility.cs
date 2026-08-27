using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace KBAtomCreator.Atoms
{
    internal class AtomAbility : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;

        //public AbilityTemplate AbilityTemplate
        //{
        //    get;
        //    set;
        //}
        private AbilityTemplate _abilityTemplate = AbilityTemplate.SingleTarget;
        public AbilityTemplate AbilityTemplate
        {
            get => _abilityTemplate;
            set
            {
                if (_abilityTemplate != value)
                {
                    _abilityTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        private AbilityAction _abilityAction;
        public AbilityAction AbilityAction
        {
            get => _abilityAction;
            set { _abilityAction = value; OnPropertyChanged(); }
        }

        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (Properties.ContainsKey(key) && Properties[key] is T value)
            {
                return value;
            }
            return defaultValue;
        }

        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public enum AbilityTemplate
    {
        [Description("Одиночная цель")]
        SingleTarget,

        [Description("Несколько целей")]
        MultiTarget,

        [Description("На себя")]
        SelfCast,

        [Description("Массовая по врагам")]
        MassEnemyCast,

        [Description("Массовая по союзникам")]
        MassAllyCast
    }

    public enum AbilityAction
    {
        
        [Description("Генерация Lua")]
        GenerateLua,
        [Description("Не копировать")]
        NoCopy,
        [Description("Копировать Исх. Код")]
        CopyCode
    }


}
