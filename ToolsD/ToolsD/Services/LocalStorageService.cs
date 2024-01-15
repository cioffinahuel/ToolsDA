using Microsoft.JSInterop;
using System.Text.Json;

namespace ToolsD.Services;
public class LocalStorageService : ILocalStorageService
{


    public LocalStorageService()
    {
    }

    public async Task<T> GetItem<T>(string key)
    {
        var json = Preferences.Default.Get<string>(key, null); 
        if (json == null)
            return default;

        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetItem<T>(string key, T value)
    {
       Preferences.Default.Set(key, JsonSerializer.Serialize(value));
    }

    public async Task RemoveItem(string key)
    {
        Preferences.Default.Remove(key);
    }
}
