using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Models.Food;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
namespace NutriNET.Maui.Models.User
{
    public partial class UserVM : ObservableObject, IJsonParseable
    {
        [ObservableProperty]
        PublicUserVM publicUser;

        [ObservableProperty]
        string emailAddress;

        [ObservableProperty]
        DateTime createdAt;

        [ObservableProperty]
        ObservableCollection<int> followerIds = new();

        [ObservableProperty]
        ObservableCollection<int> followingIds = new();

        private HashSet<int>? _followerSet;
        private HashSet<int>? _followingSet;

        public HashSet<int> FollowerSet => _followerSet ??= new HashSet<int>(FollowerIds);
        public HashSet<int> FollowingSet => _followingSet ??= new HashSet<int>(FollowingIds);

        [ObservableProperty]
        CommentRestrictionVM currentRestriction;

        [ObservableProperty]
        ModeratorRequestVM moderatorRequest;

        [ObservableProperty]
        ObservableCollection<FoodRequestVM> pendingFoodRequests = new();

        public UserVM()
        {
            PublicUser = new PublicUserVM();
            CurrentRestriction = new CommentRestrictionVM(PublicUser);
            ModeratorRequest = new ModeratorRequestVM(PublicUser);
        }

        public void FromJson(JsonElement json)
        {
            EmailAddress = json.GetProperty("emailAddress").GetString()!;
            CreatedAt = json.GetProperty("createdAt").GetDateTime();

            if (json.TryGetProperty("publicUser", out var publicUserJson))
            {
                PublicUser ??= new PublicUserVM();
                PublicUser.FromJson(publicUserJson);
            }

            if (json.TryGetProperty("followerIds", out var followers))
            {
                FollowerIds.Clear();
                foreach (var id in followers.EnumerateArray())
                    FollowerIds.Add(id.GetInt32());
                _followerSet = null;
            }

            if (json.TryGetProperty("followingIds", out var following))
            {
                FollowingIds.Clear();
                foreach (var id in following.EnumerateArray())
                    FollowingIds.Add(id.GetInt32());
                _followingSet = null;
            }
        }
    }
}
