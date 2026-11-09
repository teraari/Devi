using System.Threading.Tasks;

namespace Proofreader.Core.Interfaces;

public interface IDictionaryService
{
    // Проверяет, есть ли слово в пользовательском словаре
    bool IsWordKnown(string word);
    
    // Добавляет слово в словарь и сохраняет на диск
    Task AddWordAsync(string word);
}