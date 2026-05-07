using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.User
{
    public partial class PublicUserVM : ObservableObject, IJsonParseable, ILocalize
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string profilePicture;

        [ObservableProperty]
        private UserRole role;

        [ObservableProperty]
        private ImageSource profilePictureSource;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FollowButtonText))]
        private bool myFollowing; 

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FollowButtonText))]
        private bool myFollower;

        public string FollowButtonText
        {
            get
            {
                if (MyFollowing)
                    return LocalizationResourceManager.Instance["Unfollow"].ToString();

                if (MyFollower)
                    return LocalizationResourceManager.Instance["FollowBack"].ToString();

                return LocalizationResourceManager.Instance["Follow"].ToString();
            }
        }

        public PublicUserVM() { }

        public PublicUserVM(int id, string username, string profilePicture, UserRole role)
        {
            Id = id;
            Username = username;
            ProfilePicture = profilePicture;
            Role = role;
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Username = json.GetProperty("username").GetString()!;
            ProfilePicture = json.GetProperty("profilePicture").GetString();
            Role = (UserRole)json.GetProperty("role").GetInt32();

            if (!string.IsNullOrWhiteSpace(ProfilePicture))
            {
                try
                {
                    ProfilePictureSource = ImageSource.FromUri(new Uri(ProfilePicture));
                }
                catch
                {
                    ProfilePictureSource = null;
                }
            }
            else
            {
                ProfilePictureSource = null;
            }
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(FollowButtonText));
        }
    }
}
