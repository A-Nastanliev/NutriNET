using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.Messages.Recipes
{
    public class FollowChangedMessage : ValueChangedMessage<bool>
    {
        public PublicUserVM User { get; }
        public FollowChangedMessage(PublicUserVM user, bool isFollowing) : base(isFollowing)
        {
            User = user;
        }
    }
}
