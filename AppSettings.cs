using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBAtomCreator
{
    internal class AppSettings
    {    // Класс для хранения настроек приложения

        public string AtomsFolder { get; set; } = string.Empty;
        //public DateTime LastSaved { get; set; } = DateTime.Now;

        // Можно добавить другие настройки в будущем
        // public string OtherSetting { get; set; }
        // public int SomeNumber { get; set; }
     
    }
}
