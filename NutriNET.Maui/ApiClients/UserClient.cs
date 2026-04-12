using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Views.Authentication;
using System;
using NutriNET.Maui.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public class UserClient
    {
        public static event Func<Task>? OnLogout;

        private readonly HttpClient _http;
        private readonly ITokenStore _tokenStore;

        readonly UserVM _user;

        public UserClient(HttpClient http, ITokenStore tokenStore, UserVM user)
        {
            _http = http;
            _tokenStore = tokenStore;
            _user = user;
        }

        public async Task Logout()
        {
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            foreach (var item in Shell.Current.Items)
            {
                if (item is TabBar tabBar)
                {
                    foreach (var tab in tabBar.Items)
                    {
                        foreach (var content in tab.Items)
                        {
                            await content.Navigation.PopToRootAsync();
                        }
                    }
                }
            }

            _user.EmailAddress = null;
            _user.CreatedAt = default(DateTime);
            _user.PublicUser.Id = 0;
            _user.PublicUser.ProfilePicture = null;
            _user.PublicUser.ProfilePictureSource = null;
            _user.PublicUser.Username = null;
            _user.PublicUser.Role = UserRole.User;
            _user.FollowerIds.Clear();
            _user.FollowingIds.Clear();

            _user.CurrentRestriction = new CommentRestrictionVM(_user.PublicUser);

            _user.ModeratorRequest = new ModeratorRequestVM(_user.PublicUser);

            _tokenStore?.Clear();
            if (OnLogout != null)
            {
                foreach (Func<Task> handler in OnLogout.GetInvocationList())
                {
                    try
                    {
                        await handler();
                    }
                    catch (Exception ex)
                    {
                        string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                        string error = LocalizationResourceManager.Instance["Error"].ToString();
                        string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                    }
                }
            }
        }

        public async Task<RequestResult> SignUpAsync(string username, string email, string password, string imagePath)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(username), "Username");
            content.Add(new StringContent(email), "EmailAddress");
            content.Add(new StringContent(password), "Password");

            var stream = File.OpenRead(imagePath);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            content.Add(streamContent, "ProfilePicture", Path.GetFileName(imagePath));

            var response = await _http.PostAsync("/api/users/signup", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response, false));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> EmailLoginAsync(string email, string password)
        {
            var payload = new { Email = email, Password = password };
            var json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/users/email_login", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response, false));
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("accessToken").GetString();
            var refreshToken = root.GetProperty("refreshToken").GetString();
            _user.FromJson(root.GetProperty("user"));
            await _tokenStore.SetAccessTokenAsync(accessToken);
            await _tokenStore.SetRefreshTokenAsync(refreshToken);

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> TokenLoginAsync()
        {
            var token = await _tokenStore.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return new RequestResult(false, null);

            var response = await _http.GetAsync("/api/users/me");

            if (!response.IsSuccessStatusCode)
            {
                _tokenStore.Clear();
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _user.FromJson(root.GetProperty("user"));

            if (root.TryGetProperty("tokenUpdate", out var tokenProp) && tokenProp.ValueKind != JsonValueKind.Null)
            {
                if (tokenProp.TryGetProperty("accessToken", out var at) && tokenProp.TryGetProperty("refreshToken", out var rt))
                {
                    await _tokenStore.SetAccessTokenAsync(at.GetString());
                    await _tokenStore.SetRefreshTokenAsync(rt.GetString());
                }
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> GetMyContextAsync()
        {
            var response = await _http.GetAsync("/api/users/me/context");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("role", out var roleProp) &&  roleProp.ValueKind != JsonValueKind.Null)
            {
                _user.PublicUser.Role = (UserRole)roleProp.GetInt32();
            }

            if (root.TryGetProperty("commentRestriction", out var crProp) &&
                crProp.ValueKind != JsonValueKind.Null)
            {
                _user.CurrentRestriction.FromJson(crProp);
            }
            else
            {
                _user.CurrentRestriction.Id = 0;
                _user.CurrentRestriction.StartDate = default;
                _user.CurrentRestriction.EndDate = null;
                _user.CurrentRestriction.Reason = null;
            }

            if (root.TryGetProperty("moderatorRequest", out var mrProp) && mrProp.ValueKind != JsonValueKind.Null)
            {
                _user.ModeratorRequest.FromJson(mrProp);
            }
            else
            {
                _user.ModeratorRequest.Status = Models.RequestStatus.Pending;
                _user.ModeratorRequest.Description = null;
                _user.ModeratorRequest.Id = 0;
                _user.ModeratorRequest.ActionDate = null;
                _user.ModeratorRequest.DateSent = default;
            }

            if (root.TryGetProperty("tokenUpdate", out var tokenProp) && tokenProp.ValueKind != JsonValueKind.Null)
            {
                if (tokenProp.TryGetProperty("accessToken", out var at) && tokenProp.TryGetProperty("refreshToken", out var rt))
                {
                    await _tokenStore.SetAccessTokenAsync(at.GetString());
                    await _tokenStore.SetRefreshTokenAsync(rt.GetString());
                }
            }

            return new RequestResult(true, null);
        }

        public ImageSource GetProfilePicture(string path)
        {
            return ImageSource.FromUri(new Uri($"{path}"));
        }

        public async Task<(RequestResult,List<PublicUserVM> Users, DateTime? CursorDate)> GetUsersAsync(UserRole role, int count, DateTime? cursorDate, int? cursorId)
        {
            var url = $"/api/users" + PaginationQuery.Build(count, cursorDate, cursorId)+ $"&role={role}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false,await ApiErrorParser.ParseAsync(response)), null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<PublicUserVM>();
            foreach (var item in root.GetProperty("users").EnumerateArray())
            {
                var vm = new PublicUserVM();
                vm.FromJson(item);
                list.Add(vm);
            }

            DateTime? nextCursorDate = root.GetProperty("cursorDate").ValueKind != JsonValueKind.Null
                ? root.GetProperty("cursorDate").GetDateTime()
                : null;

            return (new RequestResult(true, null),list, nextCursorDate);
        }

        public async Task<RequestResult> UpdateUsernameAsync(string username)
        {
            var payload = new
            {
                Username = username,
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync("/api/users/me", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            _user.PublicUser.Username = username;

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UpdateEmailAsync(string newEmail, string currentPassword)
        {
            var payload = new
            {
                NewEmail = newEmail,
                CurrentPassword = currentPassword
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync("/api/users/me/email", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            _user.EmailAddress = newEmail;

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UpdatePasswordAsync(string currentPassword, string newPassword)
        {
            var payload = new
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync("/api/users/me/password", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UpdateProfilePictureAsync(string imagePath)
        {
            using var content = new MultipartFormDataContent();
            using var stream = File.OpenRead(imagePath);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            content.Add(streamContent, "picture", Path.GetFileName(imagePath));        

            var response = await _http.PutAsync("/api/users/me/profile-picture", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var newPath = doc.RootElement.GetProperty("profilePicture").GetString();

            _user.PublicUser.ProfilePicture = newPath;

            return new RequestResult(true, null);
        }


        public async Task<RequestResult> UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            var payload = new
            {
                NewRole = newRole
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"/api/users/{userId}/role", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteSelfAsync()
        {
            var response = await _http.DeleteAsync($"/api/users");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteUserAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/users/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> FollowAsync(int userId)
        {
            var response = await _http.PostAsync($"/api/users/{userId}/follow", null);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            _user.FollowingIds.Add(userId);
            _user.FollowingSet.Add(userId);
            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UnfollowAsync(int userId)
        {
            var response = await _http.DeleteAsync($"/api/users/{userId}/unfollow");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            _user.FollowingIds.Remove(userId);
            _user.FollowingSet.Remove(userId);
            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, int FollowersCount, int FollowingCount)> GetUserStatsAsync(PublicUserVM user)
        {
            var response = await _http.GetAsync($"/api/users/{user.Id}/stats");

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), 0, 0);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var followersCount = root.GetProperty("followersCount").GetInt32();
            var followingCount = root.GetProperty("followingCount").GetInt32();
            user.MyFollower = _user.FollowerIds.Contains(user.Id);
            user.MyFollowing = _user.FollowingIds.Contains(user.Id);

            return (new RequestResult(true, null), followersCount, followingCount);
        }

        public async Task<(RequestResult Result,List<PublicUserVM> Users, DateTime? CursorDate, int? CursorId)> GetMyFollowersAsync
            (int count, DateTime? cursorDate, int? cursorId)
        {
            var url = "/api/users/me/followers" + PaginationQuery.Build(count, cursorDate, cursorId);

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null,null); 

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<PublicUserVM>();

            foreach (var item in root.GetProperty("followers").EnumerateArray())
            {
                var user = new PublicUserVM();
                user.FromJson(item);
                user.MyFollower = true;
                user.MyFollowing = _user.FollowingSet.Contains(user.Id);
                list.Add(user);
            }

            DateTime? nextCursorDate = root.GetProperty("cursorDate").ValueKind != JsonValueKind.Null
                ? root.GetProperty("cursorDate").GetDateTime()
                : null;

            int? nextCursorId = root.GetProperty("cursorId").ValueKind != JsonValueKind.Null
                ? root.GetProperty("cursorId").GetInt32()
                : null;

            return (new RequestResult(true, null),list, nextCursorDate, nextCursorId); ;
        }

        public async Task<(RequestResult Result,List<PublicUserVM> Users, DateTime? CursorDate, int? CursorId)> GetMyFollowingAsync
            (int count, DateTime? cursorDate, int? cursorId)
        {
            var url = "/api/users/me/following" + PaginationQuery.Build(count, cursorDate, cursorId);

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<PublicUserVM>();
            foreach (var item in root.GetProperty("followings").EnumerateArray())
            {
                var user = new PublicUserVM();
                user.FromJson(item);
                user.MyFollowing = true;
                user.MyFollower = _user.FollowerSet.Contains(user.Id);
                list.Add(user);
            }

            DateTime? nextCursorDate = root.GetProperty("cursorDate").ValueKind != JsonValueKind.Null
                ? root.GetProperty("cursorDate").GetDateTime()
                : null;

            int? nextCursorId = root.GetProperty("cursorId").ValueKind != JsonValueKind.Null
                ? root.GetProperty("cursorId").GetInt32()
                : null;

            return (new RequestResult(true, null),list, nextCursorDate, nextCursorId);
        }

        public async Task<RequestResult> CreateModeratorRequestAsync(string description)
        {
            var dto = new
            {
                Description = description
            };

            var json = JsonSerializer.Serialize(dto);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/users/moderator-requests", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var id = doc.RootElement.GetProperty("id").GetInt32();

            _user.ModeratorRequest.Id = id;
            _user.ModeratorRequest.Description = description;
            _user.ModeratorRequest.DateSent = DateTime.UtcNow;

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult,List<ModeratorRequestVM>)> GetModeratorRequestsAsync(RequestStatus status, int count, DateTime? cursorDate, int? cursorId)
        {
            var query = PaginationQuery.Build(count, cursorDate, cursorId);

            var url = $"/api/users/moderator-requests{query}&status={status}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<ModeratorRequestVM>();
            foreach (var item in root.EnumerateArray())
            {
                var vm = new ModeratorRequestVM();
                vm.FromJson(item);
                list.Add(vm);
            }

            return (new RequestResult(true, null),list);
        }

        public async Task<RequestResult> UpdateModeratorRequestAsync(  int requestId, RequestStatus status)
        {
            var payload = new
            {
                Status = status
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"/api/users/moderator-requests/{requestId}", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> CreateCommentRestrictionAsync(int userId, DateTime? endDate, string reason)
        {
            var url = $"/api/users/{userId}/comment-restriction";

            var body = new
            {
                endDate,
                reason
            };

            var response = await _http.PostAsJsonAsync(url, body);

            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult,List<CommentRestrictionVM>)> GetCommentRestrictionsInternalAsync
            (RestrictionStatus status, int count, DateTime? cursorDate,int? cursorId)
        {
            var query = PaginationQuery.Build(count, cursorDate, cursorId);

            var url = $"/api/users/comment-restrictions{query}&status={status}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false,await ApiErrorParser.ParseAsync(response)), null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var list = new List<CommentRestrictionVM>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var vm = new CommentRestrictionVM();
                vm.FromJson(element);
                list.Add(vm);
            }

            return (new RequestResult(true,null),list);
        }

        public async Task<RequestResult> EndCommentRestrictionAsync(int restrictionId)
        {
            var url = $"/api/users/comment-restriction/{restrictionId}/end";

            var request = new HttpRequestMessage(HttpMethod.Put, url);

            var response = await _http.SendAsync(request);              

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> ForgotPasswordAsync(string email, string language = "en-US")
        {
            var payload = new { Email = email, Language = language };
            var json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/users/forgot-password", content);

            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var payload = new { Email = email, Code = code, NewPassword = newPassword };
            var json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/users/reset-password", content);

            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }
    }
}
