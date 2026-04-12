using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Food
{
    public partial class FoodRequestVM : ObservableObject, IJsonParseable, ILocalize
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        string name;

        [ObservableProperty]
        string brand;

        [ObservableProperty]
        string barcode;

        [ObservableProperty]
        RequestStatus status;

        [ObservableProperty]
        PublicUserVM sender;

        [ObservableProperty]
        PublicUserVM actionUser;

        [ObservableProperty]
        DateTime dateSent;

        [ObservableProperty]
        DateTime? actionDate;

        public FoodRequestVM()
        {
            Sender = new PublicUserVM();
            ActionUser = new PublicUserVM();
        }

        public FoodRequestVM(PublicUserVM sender)
        {
            Sender = sender;
            ActionUser = new PublicUserVM();
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(DateSent));
            OnPropertyChanged(nameof(ActionDate));
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Name = json.GetProperty("name").GetString() ?? string.Empty;
            Brand = json.GetProperty("brand").GetString() ?? string.Empty;
            Barcode = json.GetProperty("barcode").GetString() ?? string.Empty;
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

            if (json.TryGetProperty("sender", out var senderJson) && senderJson.ValueKind == JsonValueKind.Object
                && senderJson.EnumerateObject().Any())
            {
                Sender ??= new PublicUserVM();
                Sender.FromJson(senderJson);
            }

            if (json.TryGetProperty("actionUser", out var actionJson)  && actionJson.ValueKind == JsonValueKind.Object
                && actionJson.EnumerateObject().Any()) 
            {
                ActionUser ??= new PublicUserVM();
                ActionUser.FromJson(actionJson);
            }
        }
    }
}
