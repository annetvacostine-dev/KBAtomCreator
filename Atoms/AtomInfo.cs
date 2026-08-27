using System.Collections.Generic;
using System.Linq;

namespace KBAtomCreator.Atoms
{
    
    //: INotifyPropertyChanged
    public class AtomInfo
    {
        
        //class=chesspiece
        //model = barbarian2.bma
        public string? AtomName { get;  set; }
        public MainBlock? Main { get; set; }
        public ArenaParamsBlock? ArenaParams { get; set; }

        // Новое свойство для хранения всех остальных данных
        public string OtherAtomData { get; set; } = string.Empty;

        public List<string> ModelPaths;
        //public string ModelPath { get; set; } = string.Empty;

        //public event PropertyChangedEventHandler PropertyChanged;

        //protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        public class MainBlock
        {
            public string? Class { get; set; }

            public List<string> Models;          

            public int? Cullcat { get; set; }

            public string AdditionalInfo { get; set; } = string.Empty;


            public string MainModel => Models != null && Models.Count > 0 ? Models[0] : string.Empty;
            // Cвойство вместо поля – всегда возвращает Models[0] или ""
            //public string MainModel
            //{
            //    get => Models != null && Models.Count > 0 ? Models[0] : string.Empty;
            //    set
            //    {
            //        // Если устанавливаем значение, синхронизируем со списком
            //        if (Models == null)
            //            Models = new List<string>();
            //        if (Models.Count > 0)
            //            Models[0] = value;
            //        else
            //            Models.Add(value);
            //    }
            //}
        }

        public class ArenaParamsBlock
        {
            public string FeaturesLabel { get; set; } = string.Empty;
            public string FeaturesHints { get; set; } = string.Empty;
            public string Race { get; set; } = string.Empty;
            public int Cost { get; set; }
            public int Level { get; set; }
            public int Leadership { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int Defenseup { get; set; }
            public int Initiative { get; set; }
            public int Speed { get; set; }
            public int Hitpoint { get; set; }
            public int Movetype { get; set; }
            public int Krit { get; set; }
            public int Hitback { get; set; }
            public int Hitbackprotect { get; set; }

            //public string? Attacks { get; set; }
            public List<string> Attacks { get; set; } = new List<string>();

            // Новое свойство для отображения списка атак в виде строки
            public string AttacksText => Attacks != null ? string.Join(",", Attacks) : string.Empty;
            public List<AttackBlock> AttacksInfo { get; set; } = new List<AttackBlock>();
            public string? Posthitmaster { get; set; }

            public string? Posthitslave { get; set; }

            public string? EachTurnScript { get; set; }

            public int Autofight { get; set; }
            public string? Features { get; set; }
            public ResistancesBlock? Resistances { get; set; }

            public string? AdditionalInfo { get; set; } // Для всех неизвестных вложенных блоков

        }

        public class AttackBlock
        {
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty; // полное содержимое блока (включая отступы и фигурные скобки)
        }

        public class ResistancesBlock
        {
            public int Physical { get; set; }
            public int Magic { get; set; }                
            public int Fire { get; set; }
            public int Glacial { get; set; }
            public int Poison { get; set; }
            public int Astral { get; set; }
        }
        public static AtomInfo CloneAtomInfo(AtomInfo original)
        {
            if (original == null) return null;

            return new AtomInfo
            {
                AtomName = original.AtomName,
                Main = CloneMainBlock(original.Main),
                ArenaParams = CloneArenaParamsBlock(original.ArenaParams),
                OtherAtomData = original.OtherAtomData,
                ModelPaths = original.ModelPaths?.ToList()
            };
        }

        private static MainBlock CloneMainBlock(MainBlock original)
        {
            if (original == null) return null;

            return new MainBlock
            {
                Class = original.Class,
                Models = original.Models?.ToList(),
                Cullcat = original.Cullcat,
                AdditionalInfo = original.AdditionalInfo
            };
        }

        private static ArenaParamsBlock CloneArenaParamsBlock(ArenaParamsBlock original)
        {
            if (original == null) return null;


            var copy = new ArenaParamsBlock
            {
                FeaturesLabel = original.FeaturesLabel,
                FeaturesHints = original.FeaturesHints,
                Race = original.Race,
                Cost = original.Cost,
                Level = original.Level,
                Leadership = original.Leadership,
                Attack = original.Attack,
                Defense = original.Defense,
                Defenseup = original.Defenseup,
                Initiative = original.Initiative,
                Speed = original.Speed,
                Hitpoint = original.Hitpoint,
                Movetype = original.Movetype,
                Krit = original.Krit,
                Hitback = original.Hitback,
                Hitbackprotect = original.Hitbackprotect,
                //Attacks = original.Attacks,
                Posthitmaster = original.Posthitmaster,
                Posthitslave = original.Posthitslave,
                EachTurnScript = original.EachTurnScript,
                Autofight = original.Autofight,
                Features = original.Features,
                Resistances = CloneResistancesBlock(original.Resistances),
                AdditionalInfo = original.AdditionalInfo
            };

            var newAttacks = new List<string>();
            original.Attacks.ForEach((item) =>
            {
                newAttacks.Add((string)item.Clone());
            });
            copy.Attacks = newAttacks;


            var attacksInfo = new List<AttackBlock>();
            original.AttacksInfo.ForEach((item) =>
            {
                attacksInfo.Add(
                        new AttackBlock
                        {
                            Name = item.Name,
                            Content = item.Content
                        }
                    );
            });
            copy.AttacksInfo = attacksInfo;

            return copy;
        }

        private static ResistancesBlock CloneResistancesBlock(ResistancesBlock original)
        {
            if (original == null) return null;

            return new ResistancesBlock
            {
                Physical = original.Physical,
                Magic = original.Magic,
                Fire = original.Fire,
                Glacial = original.Glacial,
                Poison = original.Poison,
                Astral = original.Astral
            };
        }
    }
}
