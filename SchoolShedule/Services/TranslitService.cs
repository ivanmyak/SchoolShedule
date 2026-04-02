using SchoolShedule.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace SchoolShedule.Services
{
    public class TranslitService : ITranslit
    {
        private readonly Dictionary<string, string> _cache = new();

        private static readonly Dictionary<char, string> _map = new()
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "g",
            ['д'] = "d",
            ['е'] = "e",
            ['ё'] = "e",
            ['ж'] = "zh",
            ['з'] = "z",
            ['и'] = "i",
            ['й'] = "y",
            ['к'] = "k",
            ['л'] = "l",
            ['м'] = "m",
            ['н'] = "n",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "h",
            ['ц'] = "c",
            ['ч'] = "ch",
            ['ш'] = "sh",
            ['щ'] = "sch",
            ['ы'] = "y",
            ['э'] = "e",
            ['ю'] = "yu",
            ['я'] = "ya"
        };

        public string Transliterate(string text)
        {
            if (_cache.TryGetValue(text, out var cached))
                return cached;

            var sb = new StringBuilder();

            foreach (var ch in text.ToLower())
                sb.Append(_map.TryGetValue(ch, out var t) ? t : ch);

            var result = sb.ToString();
            _cache[text] = result;

            return result;
        }

        public string Normalize(string text)
        {
            text = text.Trim().ToLower();

            // пробелы и дефисы → точки
            text = Regex.Replace(text, @"[\s\-]+", ".");

            // убрать повторяющиеся точки
            text = Regex.Replace(text, @"\.{2,}", ".");

            // убрать недопустимые символы
            text = Regex.Replace(text, @"[^a-z0-9\.]", "");

            return text;
        }
    }
}
