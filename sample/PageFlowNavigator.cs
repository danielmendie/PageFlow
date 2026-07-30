using Blazored.LocalStorage;
using CountryApp.Abstractions.Constants;
using CountryApp.Abstractions.Enums;
using CountryApp.Abstractions.Helpers;
using CountryApp.Abstractions.Models;
using CountryApp.Abstractions.Services.Data;
using CountryApp.Pages.About;
using CountryApp.Pages.Dashboard;
using CountryApp.Pages.Profile;
using CountryApp.Pages.Profile.Pages;
using CountryApp.Pages.Search;
using CountryApp.Pages.Support;
using CountryApp.Pages.Support.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using PageFlow.Blazor;

namespace CountryApp
{
    public class PageFlowNavigator : BlazorPageFlowBase
    {
        [Inject]
        public ISnackbar SnackBar { get; set; } = default!;

        [Inject]
        public ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        public ISyncLocalStorageService SyncLocalStorage { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public IJSRuntime AppJSRuntime { get; set; } = default!;

        [Inject]
        public IDocumentDataService DocumentDataService { get; set; } = default!;

        [Inject]
        public DataModule DataModule { get; set; } = default!;

        [Inject]
        public PageFlowRegistry PageFlowRegistry { get; set; } = default!;

        [Inject]
        public IPageFlowNavigator Navigator { get; set; } = default!;

        protected void ShowNotification(string message, Severity severity)
        {
            SnackBar.Clear();
            SnackBar.Add(message, severity);
        }

        public async Task SignOut(bool clearUserState = false)
        {
            var user = CurrentUser;
            user.IsLoggedIn = false;
            var activities = UserActivities;
            activities.Add(new Activity { ActivityType = EnumActivity.SignOut, CreatedOn = DateTime.UtcNow, Description = $"You signed out of your account.", CreatedBy = CurrentUser.Email });
            await LocalStorage.SetItemAsync(ConfigType.UserActivitiesStore, activities);
            await LocalStorage.SetItemAsync(ConfigType.IdentityUserStore, clearUserState ? null : user);
            await NavigateToAsync(nameof(StartPage));
            NavigationManager.NavigateTo(NavigationManager.BaseUri, true);
        }

        public List<LocationSearchItem> UserFavorites
        {
            get => SyncLocalStorage.GetItem<List<LocationSearchItem>>(ConfigType.UserFavoritesStore) ?? new List<LocationSearchItem>();
            set
            {
                InvokeAsync(StateHasChanged);
            }
        }

        public List<Activity> UserActivities
        {
            get => SyncLocalStorage.GetItem<List<Activity>>(ConfigType.UserActivitiesStore) ?? new List<Activity>();
            set
            {
                InvokeAsync(StateHasChanged);
            }
        }

        public List<Profile> Profiles
        {
            get => SyncLocalStorage.GetItem<List<Profile>>(ConfigType.UserProfilesStore) ?? new List<Profile>();
            set
            {
                InvokeAsync(StateHasChanged);
            }
        }

        public List<SearchQuota> UserQuota
        {
            get => SyncLocalStorage.GetItem<List<SearchQuota>>(ConfigType.UserQuotaStore) ?? new List<SearchQuota>();
            set
            {
                InvokeAsync(StateHasChanged);
            }
        }

        public Profile CurrentUser
        {
            get => SyncLocalStorage.GetItem<Profile>(ConfigType.IdentityUserStore) ?? new Profile();
            set
            {
                InvokeAsync(StateHasChanged);
            }
        }

        public async Task UpdateUserData(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return;
            var updatedUserData = ObjectPropertyHelper.UpdateProperty(CurrentUser, key, value);
            await LocalStorage.SetItemAsync(ConfigType.IdentityUserStore, updatedUserData);
            var currentUser = CurrentUser;
            var profiles = Profiles;
            if (profiles.Count > 0)
            {
                var profileIndex = profiles.IndexOf(profiles.First(f => f.Email == currentUser.Email));
                profiles[profileIndex] = currentUser;
            }
            else
            {
                profiles.Add(currentUser);
            }

            await LocalStorage.SetItemAsync(ConfigType.UserProfilesStore, profiles);
        }

        protected override async Task OnAppFlowSetup()
        {
            PageFlowRegistry.Routes.Clear();
            PageFlowRegistry.Routes.AddRange(new List<PageFlowInfo>() { new PageFlowInfo { AppId = 0, PageName = "About App", Component = nameof(AboutStart), ComponentType = typeof(AboutStart), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Overview", Component = nameof(StartPage), ComponentType = typeof(StartPage), IsDefault = true, }, new PageFlowInfo { AppId = 0, PageName = "Switch Apps", Component = nameof(SwitchPage), ComponentType = typeof(SwitchPage), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Account Setting", Component = nameof(AccountStart), ComponentType = typeof(AccountStart), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Country Search", Component = nameof(CountryStart), ComponentType = typeof(CountryStart), IsDefault = false, Params = new() { { "Favorite", "" } } }, new PageFlowInfo { AppId = 0, PageName = "Your Favorites", Component = nameof(FavoriteStart), ComponentType = typeof(FavoriteStart), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Support", Component = nameof(SupportStart), ComponentType = typeof(SupportStart), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Account Deletion", Component = nameof(AccountDelete), ComponentType = typeof(AccountDelete), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Profile Update", Component = nameof(AccountProfile), ComponentType = typeof(AccountProfile), IsDefault = false, }, new PageFlowInfo { AppId = 0, PageName = "Help Article", Component = nameof(ArticleView), ComponentType = typeof(ArticleView), IsDefault = false, Params = new() { { "ParentId", "" } } } });
            await base.OnAppFlowSetup();
        }
    }
}