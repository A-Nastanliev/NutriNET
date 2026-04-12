using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using NutriNET.Maui.Models;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;

namespace NutriNET.Maui.Models.User
{
    public partial class ModeratorRequestVM : ObservableObject, IJsonParseable, ILocalize
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        string description;

        [ObservableProperty]
        RequestStatus status;

        [ObservableProperty]
        PublicUserVM publicUser;

        [ObservableProperty]
        PublicUserVM actionUser;

        [ObservableProperty]
        DateTime dateSent;

        [ObservableProperty]
        DateTime? actionDate;

        public ModeratorRequestVM() 
        {
            publicUser = new PublicUserVM();
            actionUser = new PublicUserVM();
        }

        public ModeratorRequestVM(PublicUserVM publicUser)
        {
            PublicUser = publicUser;
            actionUser = new PublicUserVM();
        }

        public ModeratorRequestVM(int id,  string description, RequestStatus status, PublicUserVM publicUser, DateTime dateSent)
        {
            Id = id;
            Description = description;
            Status = status;
            PublicUser = publicUser;
            DateSent = dateSent;
            actionUser = new PublicUserVM();
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Description = json.GetProperty("description").GetString()!;
            Status = (RequestStatus)json.GetProperty("status").GetInt32();
            DateSent = json.GetProperty("dateSent").GetDateTime();

            if (json.TryGetProperty("actionDate", out var actionDate) && actionDate.ValueKind != JsonValueKind.Null)
            {
                ActionDate = actionDate.GetDateTime();
            }
            else
            {
                ActionDate = null;
            }

            if (json.TryGetProperty("publicUser", out var publicUserJson) && publicUserJson.ValueKind == JsonValueKind.Object
                && publicUserJson.EnumerateObject().Any())
            {
                PublicUser ??= new PublicUserVM();
                PublicUser.FromJson(publicUserJson);
            }

            if (json.TryGetProperty("actionUser", out var actionUser) && actionUser.ValueKind != JsonValueKind.Null)
            {
                ActionUser ??= new PublicUserVM();
                ActionUser.FromJson(actionUser);
            }
            else
            {
                ActionUser = null;
            }
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(DateSent));
            OnPropertyChanged(nameof(ActionDate));
        }
    }
}
