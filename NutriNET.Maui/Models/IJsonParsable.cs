using System.Text.Json;

namespace NutriNET.Maui.Models
{
    public interface IJsonParseable
    {
        void FromJson(JsonElement json);
    }
}
