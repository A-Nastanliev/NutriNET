using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Recipes
{
    public partial class RecipeCommentVM : ObservableObject, IJsonParseable, ILocalize
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string comment = string.Empty;

        [ObservableProperty]
        private PublicUserVM user;

        [ObservableProperty]
        private DateTime date;

        public RecipeCommentVM()
        {
            User = new PublicUserVM();
        }

        public RecipeCommentVM(int id, string comment, PublicUserVM user,  DateTime date)
        {
            Id = id;
            Comment = comment;
            User = user;
            Date = date;
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Comment = json.GetProperty("comment").GetString() ?? string.Empty;
            Date = json.GetProperty("date").GetDateTime();

            if (json.TryGetProperty("user", out var userJson) &&
                userJson.ValueKind == JsonValueKind.Object &&
                userJson.EnumerateObject().Any())
            {
                User ??= new PublicUserVM();
                User.FromJson(userJson);
            }
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(Date));
        }
    }
}
