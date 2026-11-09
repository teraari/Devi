using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proofreader.Language.Spelling;

public static class LayoutMapper
{
    // Таблица соответствия клавиш EN -> RU
    private static readonly Dictionary<char, char> EnToRu = new()
    {
        ['q']='й', ['w']='ц', ['e']='у', ['r']='к', ['t']='е', ['y']='н', ['u']='г', ['i']='ш', ['o']='щ', ['p']='з', ['[']='х', [']']='ъ',
        ['a']='ф', ['s']='ы', ['d']='в', ['f']='а', ['g']='п', ['h']='р', ['j']='о', ['k']='л', ['l']='д', [';']='ж', ['\'']='э',
        ['z']='я', ['x']='ч', ['c']='с', ['v']='м', ['b']='и', ['n']='т', ['m']='ь', [',']='б', ['.']='ю',
        ['Q']='Й', ['W']='Ц', ['E']='У', ['R']='К', ['T']='Е', ['Y']='Н', ['U']='Г', ['I']='Ш', ['O']='Щ', ['P']='З', ['{']='Х', ['}']='Ъ',
        ['A']='Ф', ['S']='Ы', ['D']='В', ['F']='А', ['G']='П', ['H']='Р', ['J']='О', ['K']='Л', ['L']='Д', [':']='Ж', ['"']='Э',
        ['Z']='Я', ['X']='Ч', ['C']='С', ['V']='М', ['B']='И', ['N']='Т', ['M']='Ь', ['<']='Б', ['>']='Ю'
    };

    public static string EnToRuTranslate(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(EnToRu.TryGetValue(c, out var ru) ? ru : c);
        }
        return sb.ToString();
    }
}