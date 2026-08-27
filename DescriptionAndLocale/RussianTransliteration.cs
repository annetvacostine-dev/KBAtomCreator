using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBAtomCreator.DescriptionAndLocale
{
    using System.Text;

    public static class RussianTransliteration
    {
        public static string Transliterate(string russianText)
        {
            StringBuilder latinText = new StringBuilder();
            foreach (char c in russianText)
            {
                switch (char.ToLower(c)) // Handle both upper and lower case
                {
                    case 'а': latinText.Append('a'); break;
                    case 'б': latinText.Append('b'); break;
                    case 'в': latinText.Append('v'); break;
                    case 'г': latinText.Append('g'); break;
                    case 'д': latinText.Append('d'); break;
                    case 'е': latinText.Append('e'); break;
                    case 'ё': latinText.Append("yo"); break; // 'yo' for 'ё'
                    case 'ж': latinText.Append("zh"); break;
                    case 'з': latinText.Append('z'); break;
                    case 'и': latinText.Append('i'); break;
                    case 'й': latinText.Append('y'); break;
                    case 'к': latinText.Append('k'); break;
                    case 'л': latinText.Append('l'); break;
                    case 'м': latinText.Append('m'); break;
                    case 'н': latinText.Append('n'); break;
                    case 'о': latinText.Append('o'); break;
                    case 'п': latinText.Append('p'); break;
                    case 'р': latinText.Append('r'); break;
                    case 'с': latinText.Append('s'); break;
                    case 'т': latinText.Append('t'); break;
                    case 'у': latinText.Append('u'); break;
                    case 'ф': latinText.Append('f'); break;
                    case 'х': latinText.Append("kh"); break;
                    case 'ц': latinText.Append("ts"); break;
                    case 'ч': latinText.Append("ch"); break;
                    case 'ш': latinText.Append("sh"); break;
                    case 'щ': latinText.Append("shch"); break;
                    case 'ъ': latinText.Append(""); break; // Hard sign often omitted
                    case 'ы': latinText.Append('y'); break;
                    case 'ь': latinText.Append(""); break; // Soft sign often omitted
                    case 'э': latinText.Append('e'); break;
                    case 'ю': latinText.Append("yu"); break;
                    case 'я': latinText.Append("ya"); break;
                    case ' ': latinText.Append('_'); break; //убираем пробелы
                    default: latinText.Append(c); break; // Keep non-Cyrillic characters as is
                }
            }
            return latinText.ToString();
        }
    }
}
