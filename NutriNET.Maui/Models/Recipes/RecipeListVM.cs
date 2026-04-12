using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Recipes
{
    public partial class RecipeListVM : ObservableObject, IJsonParseable
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name;

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Name = json.GetProperty("name").GetString() ?? string.Empty;
        }
    }
}
