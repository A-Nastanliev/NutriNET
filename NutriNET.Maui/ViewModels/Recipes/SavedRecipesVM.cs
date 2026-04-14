using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Views.Recipes;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class SavedRecipesVM : PagedLoadingVM, IRecipient<RecipeDeletedMessage>, IRecipient<RecipeListItemAddedMessage>
    {
        [ObservableProperty]
        ObservableCollection<RecipeListLoaderVM> recipeLists = new();

        readonly RecipeClient _recipeClient;

        public SavedRecipesVM(RecipeClient recipeClient)
        {
            _recipeClient = recipeClient;
            UserClient.OnLogout += Clear;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public override async Task Load()
        {
            if (!CanStartLoading())
                return;

            BeginLoading();

            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var (result, recipeLists) = await _recipeClient.GetAllRecipeListsAsync();

                if (result.Success)
                {
                    RecipeLists.Clear();
                    foreach (var rl in recipeLists)
                    {
                        RecipeLists.Add(new RecipeListLoaderVM(_recipeClient, rl));
                    }

                    Loading = false;
                }
                else
                {
                    Loading = false;
                    string message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                Loading = false;
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public Task Clear()
        {
            Loading = false;
            RecipeLists.Clear();
            CursorDate = null;
            CursorId = null;
            CanLoadMore = true;
            return Task.CompletedTask;
        }

        [RelayCommand]
        public override async Task Refresh()
        {
            try
            {
                await Clear();
                await Load();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task AddList()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string title = LocalizationResourceManager.Instance["AddListTitle"].ToString();
            string message = LocalizationResourceManager.Instance["AddListMessage"].ToString();
            string cancel = LocalizationResourceManager.Instance["Cancel"].ToString();
            string? name = await Shell.Current.DisplayPromptAsync( title,  message, ok, cancel,  maxLength: 24, keyboard: Keyboard.Text);

            if (string.IsNullOrWhiteSpace(name)) return;

            name = name.Trim();
            name = Regex.Replace(name, @" {2,}", " ");

            if (name.Length < 1)
            {
                error = LocalizationResourceManager.Instance["Error"].ToString();
                string tooShort = LocalizationResourceManager.Instance["ListNameTooShort"].ToString();
                await Shell.Current.DisplayAlertAsync(error, tooShort, ok);
                return;
            }

            try 
            {
                RecipeListVM recipeList = new RecipeListVM();
                recipeList.Name = name;
                var result = await _recipeClient.CreateRecipeListAsync(recipeList);
                if (result.Success)
                {
                    RecipeLists.Add(new RecipeListLoaderVM(_recipeClient, recipeList));
                }
                else
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
            }
            catch(Exception ex) 
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task DeleteList(RecipeListLoaderVM recipeList)
        {
            string alertTitle = String.Format(LocalizationResourceManager.Instance["DeleteName"].ToString(), recipeList.RecipeList.Name);
            string message = String.Format(LocalizationResourceManager.Instance["DeleteNameMessage"].ToString(), recipeList.RecipeList.Name);
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(alertTitle, message, accept, decline);

            if (!confirm)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _recipeClient.DeleteRecipeListAsync(recipeList.RecipeList.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                RecipeLists.Remove(recipeList);
                message = String.Format(LocalizationResourceManager.Instance["DeletedRecipeList"].ToString(), recipeList.RecipeList.Name);
                _ = Toast.Make(message, ToastDuration.Short).Show();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public void Receive(RecipeDeletedMessage message)
        {
            foreach(var loader in RecipeLists)
            {
                var existing = loader.Recipes.FirstOrDefault(r=>r.Id == message.Value.Id);
                if (existing != null)
                {
                    loader.Recipes.Remove(existing);
                }
            }
        }

        public void Receive(RecipeListItemAddedMessage message)
        {
            var list = RecipeLists.FirstOrDefault(r => r.RecipeList.Id == message.ListId);
            if (list != null && list?.Recipes?.Any(r => r.Id == message.Value.Id) == false)
            {
                list.Recipes.Insert(0,message.Value);
            }
        }
    }
}
