using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microcharts;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.ViewModels.Settings;
using NutriNET.Maui.Views.Recipes;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class RecipeDetailVM : PagedLoadingVM, IQueryAttributable, ILocalize, IRecipient<RecipeUpdatedMessage>, IRecipient<RecipeDeletedMessage>
    {
        [ObservableProperty]
        ObservableCollection<RecipeCommentVM> comments = new();

        [ObservableProperty]
        FoodVM navigationRecipeFood = new();

        [ObservableProperty]
        RecipeVM recipe = new();

        [ObservableProperty]
        byte? myRating;

        [ObservableProperty]
        int ratingCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplaybleAverageRating))]
        double averageRating;

        public double DisplaybleAverageRating => Math.Round(AverageRating, 1, MidpointRounding.AwayFromZero);

        [ObservableProperty]
        PieChart macroChart;

        [ObservableProperty]
        string privacyLevel;

        [ObservableProperty]
        string newComment;

        [ObservableProperty]
        string editComment;

        [ObservableProperty]
        bool isEditingComment;

        [ObservableProperty]
        RecipeCommentVM selectedComment = new();

        [ObservableProperty]
        bool isRestricting;

        [ObservableProperty]
        string restrictionReason;

        [ObservableProperty]
        ObservableCollection<KeyValuePair<string, TimeSpan?>> restrictionOptions = new();

        [ObservableProperty]
        KeyValuePair<string, TimeSpan?> selectedRestrictionOption;

        public Func<Task> OpenBottomSheet;
        public Func<Task> CloseBottomSheet;

        readonly RecipeClient _recipeClient;
        readonly UserClient _userClient;
        readonly UserVM _user;

        bool _canManageRating;

        public RecipeDetailVM(RecipeClient recipeClient, UserClient userClient, UserVM user)
        {
            MacroChart = new PieChart
            {
                Entries = new[]
             {
                    new ChartEntry(0),
                    new ChartEntry(0),
                    new ChartEntry(0),
                },
                AnimationDuration = TimeSpan.FromSeconds(0.5),
                LabelMode = LabelMode.None,
                BackgroundColor = SKColor.Empty
            };
            _recipeClient = recipeClient;
            _userClient = userClient;
            _user = user;
            WeakReferenceMessenger.Default.RegisterAll(this);
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
        }

        void UpdateChart()
        {
            var protein = (float)(Recipe?.TotalProteins ?? 0);
            var carbs = (float)(Recipe?.TotalCarbohydrates ?? 0);
            var fat = (float)(Recipe?.TotalFats ?? 0);

            var total = protein + carbs + fat;

            if (total == 0)
            {
                protein = carbs = fat = 1;
            }

            var entries = new[]
            {
                new ChartEntry(protein)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["ProteinColor"]).ToHex())
                },
                new ChartEntry(carbs)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["CarbsColor"]).ToHex())
                },
                new ChartEntry(fat)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["FatColor"]).ToHex())
                },
            };

            MacroChart.Entries = entries;
            OnPropertyChanged(nameof(MacroChart));
        }

        [RelayCommand]
        public async Task AddToRecipeList()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;
            List<RecipeListVM> lists = new();
            try
            {
                (var result, lists) = await _recipeClient.GetAllRecipeListsAsync();

                if (!result.Success || lists == null)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            var nameCounts = lists.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var seen = new Dictionary<string, int>();
            var displayNames = new string[lists.Count];

            for (int i = 0; i < lists.Count; i++)
            {
                var name = lists[i].Name;
                if (nameCounts.Contains(name))
                {
                    seen.TryGetValue(name, out int count);
                    seen[name] = count + 1;
                    displayNames[i] = $"{name} ({count + 1})";
                }
                else
                {
                    displayNames[i] = name;
                }
            }

            string cancel = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["PickListTitle"].ToString();

            string? chosen = await Shell.Current.DisplayActionSheetAsync(title, cancel, null, displayNames);

            if (chosen == null || chosen == cancel) return;

            int chosenIndex = Array.IndexOf(displayNames, chosen);
            if (chosenIndex < 0) return;

            var pickedList = lists[chosenIndex];

            try
            {
                var result = await _recipeClient.CreateRecipeListItemAsync(pickedList.Id, Recipe.Id);

                if (!result.Success)
                {
                    message = String.Format(LocalizationResourceManager.Instance[result.Error].ToString(), Recipe.Name, pickedList.Name);
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["AddedToList"].ToString(), Recipe.Name, pickedList.Name);
                if(string.IsNullOrWhiteSpace(NavigationRecipeFood.ExtraInfo))
                {
                    NavigationRecipeFood.ExtraInfo = Recipe.Creator.Username;
                    NavigationRecipeFood.Calories = Recipe.NormalizedCalories;
                    NavigationRecipeFood.Image = Recipe.Image;
                    NavigationRecipeFood.ImageSource = Recipe.ImageSource;
                    NavigationRecipeFood.Name = Recipe.Name;
                }
                WeakReferenceMessenger.Default.Send(new RecipeListItemAddedMessage(NavigationRecipeFood, pickedList.Id));
                _ = Toast.Make(message, ToastDuration.Short).Show();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

        }

        [RelayCommand]
        private async Task CreateComment()
        {
            if (string.IsNullOrWhiteSpace(NewComment))
            {
                return;
            }
            else if (!string.IsNullOrWhiteSpace(NewComment))
            {
                NewComment = Regex.Replace(NewComment, @"[ ]{2,}", " ");
                NewComment = Regex.Replace(NewComment, @"(\r?\n){2,}", "\n");
                NewComment = NewComment.Trim();
            }

            string message;
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            if ((_user?.CurrentRestriction?.EndDate == null || _user?.CurrentRestriction?.EndDate > DateTime.UtcNow) 
                && _user?.CurrentRestriction?.Id != 0)
            {
                string title = LocalizationResourceManager.Instance["RestrictedTitle"].ToString();
                message = _user.CurrentRestriction?.EndDate == null
                    ? LocalizationResourceManager.Instance["RestrictedMessageWithoutEndDate"].ToString()
                    : string.Format(
                        LocalizationResourceManager.Instance["RestrictedMessageWithEndDate"].ToString(),
                        TimeZoneInfo.ConvertTimeFromUtc(
                            _user.CurrentRestriction.EndDate.Value,
                            TimeZoneInfo.Local)
                        .ToString("HH:mm d MMMM yyyy", CultureInfo.CurrentCulture));
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            const int minLength = 4;
            if (NewComment?.Length < minLength)
            {
                message = String.Format(LocalizationResourceManager.Instance["CommentTooShort"].ToString(), minLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            try
            {
                var (result, id) = await _recipeClient.CreateRecipeCommentAsync(Recipe.Id, NewComment);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                RecipeCommentVM myComment = new RecipeCommentVM(id.Value, NewComment, _user.PublicUser, DateTime.UtcNow);
                Comments.Insert(0, myComment);
                NewComment = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task SelectComment(RecipeCommentVM comment)
        {
            if ((_user.PublicUser.Role == UserRole.User && comment.User.Id != _user.PublicUser.Id) || 
               (_user.PublicUser.Role != UserRole.User && comment.User.Role != UserRole.User && comment.User.Id != _user.PublicUser.Id))
                return;

            string[] options;
            string edit = LocalizationResourceManager.Instance["Edit"].ToString();
            string delete = LocalizationResourceManager.Instance["Delete"].ToString();
            string restrict = LocalizationResourceManager.Instance["RestrictUser"].ToString();
            if (_user.PublicUser.Id == comment.User.Id && _user.PublicUser.Role == UserRole.User &&
                ((_user.CurrentRestriction?.EndDate == null || _user.CurrentRestriction?.EndDate > DateTime.UtcNow) && _user.CurrentRestriction?.Id != 0))
            {
                options = new string[] { delete };
            }
            else if(_user.PublicUser.Id == comment.User.Id)
            {
                options = new string[] { delete, edit };
            }
            else
            {
                options = new string[] { delete, restrict };
            }

            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["SelectCommentAction"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected))
                return;

            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            if (selected == delete)
            {
                bool confirm = await Shell.Current.DisplayAlertAsync(
                     LocalizationResourceManager.Instance["Confirm"].ToString(),
                     LocalizationResourceManager.Instance["DeleteCommentMessage"].ToString(),
                     LocalizationResourceManager.Instance["Yes"].ToString(),
                     LocalizationResourceManager.Instance["No"].ToString());
                if (!confirm) return;

                try
                {
                    RequestResult result ;
                    if (comment.User.Id != _user.PublicUser.Id)
                    { 
                        result = await _recipeClient.DeleteRecipeCommentAsync(comment.Id); 
                    }
                    else
                    {
                        result = await _recipeClient.DeleteOwnRecipeCommentAsync(comment.Id);
                    }
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        return;
                    }
                    Comments.Remove(comment);
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            else if (selected == edit)
            {
                IsEditingComment = true;
                EditComment = comment.Comment;
                SelectedComment = comment;
                await OpenBottomSheet.Invoke();      
            }
            else if (selected == restrict)
            {
                SelectedComment = comment;
                IsRestricting = true;
                LoadRestrictionOptions();
                SelectedRestrictionOption = RestrictionOptions.FirstOrDefault();
                await OpenBottomSheet.Invoke();
            }
        }

        [RelayCommand]
        private async Task UpdateComment()
        {
            if (string.IsNullOrWhiteSpace(EditComment))
            {
                return;
            }
            else if (!string.IsNullOrWhiteSpace(EditComment))
            {
                EditComment = Regex.Replace(EditComment, @"[ ]{2,}", " ");
                EditComment = Regex.Replace(EditComment, @"(\r?\n){2,}", "\n");
                EditComment = EditComment.Trim();
            }

            string message;
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();


            if ((_user?.CurrentRestriction?.EndDate == null || _user?.CurrentRestriction?.EndDate > DateTime.UtcNow)
                && _user?.CurrentRestriction?.Id != 0)
            {
                string title = LocalizationResourceManager.Instance["RestrictedTitle"].ToString();
                message = _user.CurrentRestriction?.EndDate == null
                    ? LocalizationResourceManager.Instance["RestrictedMessageWithoutEndDate"].ToString()
                    : string.Format(
                        LocalizationResourceManager.Instance["RestrictedMessageWithEndDate"].ToString(),
                        TimeZoneInfo.ConvertTimeFromUtc(
                            _user.CurrentRestriction.EndDate.Value,
                            TimeZoneInfo.Local)
                        .ToString("HH:mm d MMMM yyyy", CultureInfo.CurrentCulture));
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            const int minLength = 4;
            if (EditComment?.Length < minLength)
            {
                message = String.Format(LocalizationResourceManager.Instance["CommentTooShort"].ToString(), minLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            try
            {
                var result = await _recipeClient.UpdateRecipeCommentAsync(SelectedComment.Id, EditComment);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                SelectedComment.Comment = EditComment;
                await CloseBottomSheet.Invoke();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        private void LoadRestrictionOptions()
        {
            RestrictionOptions.Clear();

            if (_user.PublicUser.Role == UserRole.User)
                return;

            string hours = LocalizationResourceManager.Instance["hours"].ToString();
            string days = LocalizationResourceManager.Instance["days"].ToString();
            string weeks = LocalizationResourceManager.Instance["weeks"].ToString();
            string oneHour = LocalizationResourceManager.Instance["oneHour"].ToString();
            string oneDay = LocalizationResourceManager.Instance["oneDay"].ToString();
            string oneWeek = LocalizationResourceManager.Instance["oneWeek"].ToString();

            RestrictionOptions.Add(new(oneHour, TimeSpan.FromHours(1)));
            RestrictionOptions.Add(new(string.Format(hours, 6), TimeSpan.FromHours(6)));
            RestrictionOptions.Add(new(string.Format(hours, 12), TimeSpan.FromHours(12)));
            RestrictionOptions.Add(new(oneDay, TimeSpan.FromDays(1)));
            RestrictionOptions.Add(new(string.Format(days, 3), TimeSpan.FromDays(3)));
            RestrictionOptions.Add(new(oneWeek, TimeSpan.FromDays(7)));
            RestrictionOptions.Add(new(string.Format(weeks, 2), TimeSpan.FromDays(14)));
            RestrictionOptions.Add(new(string.Format(days, 30), TimeSpan.FromDays(30)));

            if (_user.PublicUser.Role == UserRole.Administrator)
                RestrictionOptions.Add(new(LocalizationResourceManager.Instance["NoEndDate"].ToString(), null));
        }

        [RelayCommand]
        public async Task SelectRestrictionDuration()
        {
            var options = RestrictionOptions.Select(x => x.Key).ToArray();
            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["Duration"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected) || selected == cancelText)
                return;

            SelectedRestrictionOption = RestrictionOptions.First(x => x.Key == selected);
        }

        [RelayCommand]
        public async Task RestrictUser()
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            try
            {
                DateTime? endDate = SelectedRestrictionOption.Value.HasValue ? DateTime.UtcNow.Add(SelectedRestrictionOption.Value.Value) : null;
                if (!string.IsNullOrWhiteSpace(RestrictionReason))
                    RestrictionReason = Regex.Replace(RestrictionReason.Trim(), @"\s{2,}", " ");
                else
                    RestrictionReason = null;
                var result = await _userClient.CreateCommentRestrictionAsync(SelectedComment.User.Id, endDate , RestrictionReason);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                message = string.Format(LocalizationResourceManager.Instance["UserRestricted"].ToString(), SelectedComment.User.Username);
                _ = Toast.Make(message).Show();
                await CloseBottomSheet.Invoke();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }
        }

        [RelayCommand]
        private async Task CreateRating()
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            try
            {
                var result = await _recipeClient.CreateRecipeRatingAsync(Recipe.Id, MyRating.Value);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    _canManageRating = false;
                    MyRating = 0;
                    _canManageRating = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                _canManageRating = false;
                MyRating = 0;
                _canManageRating = true;
                return;
            }

            double totalRating = AverageRating * RatingCount;
            totalRating += (double)MyRating;
            RatingCount += 1;
            AverageRating = totalRating / RatingCount;
        }

        [RelayCommand]
        private async Task UpdateRating(byte oldRating)
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            try
            {
                var result = await _recipeClient.UpdateRecipeRatingAsync(Recipe.Id, MyRating.Value);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    _canManageRating = false;
                    MyRating = 0;
                    _canManageRating = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                _canManageRating = false;
                MyRating = 0;
                _canManageRating = true;
                return;
            }

            double totalRating = AverageRating * RatingCount;
            totalRating += (double)MyRating - (double)oldRating;
            AverageRating = totalRating / RatingCount;
        }

        [RelayCommand]
        public async Task ClearRating()
        {
            if (MyRating != null && MyRating != 0)
            {
                string message;
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                try
                {
                    var result = await _recipeClient.DeleteRecipeRatingAsync(Recipe.Id);
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message , ok);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message , ok);
                    return;
                }

                double totalRating = AverageRating * RatingCount;
                totalRating -= (double)MyRating;
                RatingCount -= 1;
                if (RatingCount == 0)
                {
                    AverageRating = 0;
                }
                else
                {
                    AverageRating = totalRating / RatingCount;
                }
                MyRating = 0;
            }
        }

        partial void OnMyRatingChanged(byte? oldValue, byte? newValue)
        {
            if (_canManageRating)
            {
                if (newValue != 0 && newValue != null && (oldValue == 0 || oldValue == null))
                    CreateRatingCommand.Execute(null);
                else if (newValue != 0 && newValue != null && oldValue != 0 && oldValue != null)
                    UpdateRatingCommand.Execute(oldValue);
            }
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            if (query.TryGetValue($"{nameof(NavigationRecipeFood)}", out var recipeFood) && recipeFood is FoodVM food)
            {
                NavigationRecipeFood = food; 
                try
                {
                    var (result, detailedRecipe, ratingCount, averageRating, myRating) = await _recipeClient.GetRecipeDetailsAsync(food.Id);
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await Shell.Current.GoToAsync("..");
                        return;
                    }

                    Recipe = detailedRecipe;
                    PrivacyLevel = LocalizationResourceManager.Instance[$"PrivacyLevel{Recipe.PrivacyLevel}"].ToString();
                    UpdateChart();
                    RatingCount = ratingCount;
                    AverageRating = averageRating;
                    MyRating = (byte?)myRating;
                    _canManageRating = true;
                    result = await _userClient.GetMyContextAsync();
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await Shell.Current.GoToAsync("..");
                        return;
                    }
                    await Load();
                    query.Clear();
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            else if (query.TryGetValue("deepLinkToken", out var tokenObj)) 
            {

                var token = tokenObj?.ToString();
                if (string.IsNullOrWhiteSpace(token)) return;

                query.Remove("deepLinkToken");
                try
                {
                    var (result, detailedRecipe, ratingCount, averageRating, myRating) = await _recipeClient.GetSharedRecipeAsync(token);
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await Shell.Current.GoToAsync("..");
                        return;
                    }
                    NavigationRecipeFood = new FoodVM{ FoodType = FoodType.RecipeFood, Id = detailedRecipe.Id };
                    Recipe = detailedRecipe;
                    PrivacyLevel = LocalizationResourceManager.Instance[$"PrivacyLevel{Recipe.PrivacyLevel}"].ToString();
                    UpdateChart();
                    RatingCount = ratingCount;
                    AverageRating = averageRating;
                    MyRating = (byte?)myRating;
                    _canManageRating = true;
                    result = await _userClient.GetMyContextAsync();
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await Shell.Current.GoToAsync("..");
                        return;
                    }
                    await Load();
                    query.Clear();
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }

            }
        }

        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading()) return;

            BeginLoading();

            try
            {
                var (result, comments, cursorDate, cursorId) = 
                    await _recipeClient.GetNextRecipeCommentsAsync( NavigationRecipeFood.Id ,BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var c in comments)
                    {
                        Comments.Add(c);
                    }

                    if (comments.Any())
                    {
                        EndLoading(comments.Count, cursorDate, cursorId);
                        return;
                    }

                    EndLoading(0, null, null);
                }
                else
                {
                    Loading = false;
                    string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                    string error = LocalizationResourceManager.Instance["Error"].ToString();
                    string message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                Loading = false;
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public void OnLocalize()
        {
            PrivacyLevel = LocalizationResourceManager.Instance[$"PrivacyLevel{Recipe.PrivacyLevel}"].ToString();
            foreach (var c in Comments) 
            {
                c.OnLocalize();
            }
            if(_user.PublicUser.Role != UserRole.User)
            {
                var option = SelectedRestrictionOption;
                LoadRestrictionOptions();
                SelectedRestrictionOption = RestrictionOptions.FirstOrDefault(ro => ro.Value == option.Value);
            }
        }

        [RelayCommand]
        public async Task VisitProfile(PublicUserVM user)
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true,
             new Dictionary<string, object> { [nameof(ProfileVM.User)] = user});
        }

        [RelayCommand]
        public override async Task Refresh()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;
            try
            {
                var (result, detailedRecipe, ratingCount, averageRating, myRating) = await _recipeClient.GetRecipeDetailsAsync(Recipe.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                Recipe = detailedRecipe;
                PrivacyLevel = LocalizationResourceManager.Instance[$"PrivacyLevel{Recipe.PrivacyLevel}"].ToString();
                UpdateChart();
                RatingCount = ratingCount;
                AverageRating = averageRating;
                MyRating = (byte?)myRating;
                _canManageRating = true;
                result = await _userClient.GetMyContextAsync();
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                await Load();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public async void Receive(RecipeUpdatedMessage message)
        {
            if (message.Value.Id == Recipe.Id)
            {
                RefreshCommand.Execute(null);
            }
        }

        public async void Receive(RecipeDeletedMessage message)
        {
            if (message.Value.Id == Recipe.Id)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var myTab = Shell.Current.Items
                        .SelectMany(i => i.Items)
                        .FirstOrDefault(tab => tab.Items
                            .Any(content => content.Navigation.NavigationStack
                                .Any(p => p?.BindingContext == this)));

                    if (myTab != null)
                        foreach (var content in myTab.Items)
                            await content.Navigation.PopToRootAsync();
                });
            }
        }


        [RelayCommand]
        private async Task ShareRecipe()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;
            try
            {
                var(result, token) = await _recipeClient.GetShareTokenAsync(Recipe.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                var link = $"https:///recipe/{token}";

                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = Recipe.Name,
                    Text = string.Format(LocalizationResourceManager.Instance["ShareRecipeText"].ToString(), Recipe.Name),
                    Uri = link
                });
            }
            catch(Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            } 
        }
    }
}
