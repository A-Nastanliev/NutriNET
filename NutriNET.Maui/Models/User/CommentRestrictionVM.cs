using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.User
{
    public partial class CommentRestrictionVM : ObservableObject, IJsonParseable, ILocalize
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        DateTime startDate;

        [ObservableProperty]
        DateTime? endDate;

        [ObservableProperty]
        string reason;
         
        [ObservableProperty] 
        PublicUserVM publicUser;

        public CommentRestrictionVM()
        {
            PublicUser = new PublicUserVM();
        }

        public CommentRestrictionVM(PublicUserVM publicUser)
        {
            PublicUser = publicUser;
        }

        public CommentRestrictionVM(int id, DateTime startDate, DateTime? endDate, string reason, PublicUserVM publicUser)
        {
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            Reason = reason;
            PublicUser = publicUser;
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            StartDate = json.GetProperty("startDate").GetDateTime();
            EndDate = json.TryGetProperty("endDate", out var end) && end.ValueKind != JsonValueKind.Null ? end.GetDateTime() : null;
            Reason = json.GetProperty("reason").GetString()!;

            if (json.TryGetProperty("publicUser", out var publicUserJson) && publicUserJson.ValueKind == JsonValueKind.Object
                && publicUserJson.EnumerateObject().Any())
            {
                PublicUser ??= new PublicUserVM();
                PublicUser.FromJson(publicUserJson);
            }
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
        }
    }
}
