using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriNET.Api.Dto;
using NutriNET.Api.Dto.User;
using NutriNET.Api.Mappers;
using NutriNET.Api.Services;
using NutriNET.Data.Enums;
using NutriNET.Data.Models;
using NutriNET.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NutriNET.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;
        private readonly IConfiguration _configuration;
        private readonly IImageStorageService _imageStorageService;
        private readonly IEmailService _emailService;

        private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private UserRole? Role => Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

        public UserController(IConfiguration configuration, UserService service, IImageStorageService imageStorageService, IEmailService emailService)
        {
            _service = service;
            _configuration = configuration;
            _imageStorageService = imageStorageService;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var existing = await _service.GetRefreshTokenAsync(req.RefreshToken);
            if (existing == null || existing.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { error = "SessionExpired" });

            await _service.RevokeRefreshTokenAsync(existing);
            var newRefresh = await _service.CreateRefreshTokenAsync(existing.UserId);

            var secret = _configuration["Jwt:Secret"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var user = await _service.GetByIdAsync(existing.UserId);
            var newAccess = JwtService.GenerateAccessToken(user.Id, user.Role, secret, issuer, audience);

            return Ok(new { AccessToken = newAccess, RefreshToken = newRefresh.Token });
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromForm] SignUpRequest req)
        {

            string profilePicturePath = null;

            try
            {
                profilePicturePath = await _imageStorageService.SaveImageAsync(req.ProfilePicture, "profile-pictures");

                var newUser = new User
                {
                    Username = req.Username,
                    EmailAddress = req.EmailAddress,
                    PasswordHash = req.Password,
                    ProfilePicture = profilePicturePath,
                };

                await _service.SignUpAsync(newUser);

                return StatusCode(201);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { error = e.Message });
            }
            catch (DbUpdateException)
            {
                if (profilePicturePath != null)
                    _imageStorageService.DeleteImage(profilePicturePath);
                return Conflict(new { error = "UsernameOrEmailInUse" });
            }
        }

        [AllowAnonymous]
        [HttpPost("email_login")]
        public async Task<IActionResult> EmailLogin([FromBody] EmailLoginRequest req)
        {
            var user = await _service.EmailPasswordLoginAsync(req.Email, req.Password);
            if (user == null)
                return Unauthorized(new { error = "InvalidCredentials" });

            var secret = _configuration["Jwt:Secret"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var baseUrl = _configuration["App:BaseUrl"];

            var accessToken = JwtService.GenerateAccessToken(user.Id, user.Role, secret, issuer, audience);
            var refreshToken = await _service.CreateRefreshTokenAsync(user.Id);

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                User = user.ToDto(baseUrl)
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> TokenLogin()
        {
            var user = await _service.GetByIdAsync(UserId.Value);
            var baseUrl = _configuration["App:BaseUrl"];

            if (user == null)
                return Unauthorized(new { error = "SessionExpired" });

            var response = new Dictionary<string, object?>
            {
                ["user"] = user.ToDto(baseUrl)
            };

            if (Role.HasValue && user.Role != Role.Value)
            {
                var secret = _configuration["Jwt:Secret"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                var newAccessToken = JwtService.GenerateAccessToken(UserId.Value, user.Role, secret, issuer, audience);

                var currentRefresh = await _service.GetActiveRefreshTokenAsync(UserId.Value);
                string? newRefreshToken = null;
                if (currentRefresh != null)
                {
                    var remainingDays = (int)Math.Max(1, (currentRefresh.ExpiresAt - DateTime.UtcNow).TotalDays);
                    await _service.RevokeRefreshTokenAsync(currentRefresh);
                    newRefreshToken = (await _service.CreateRefreshTokenAsync(UserId.Value, remainingDays)).Token;
                }

                response["tokenUpdate"] = new { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
            }

            return Ok(response);
        }

        [HttpGet("me/context")]
        public async Task<IActionResult> GetMyContext()
        {
            var userId = UserId.Value;
            var baseUrl = _configuration["App:BaseUrl"];

            var dbRole = await _service.GetRole(userId);
            var commentRestriction = await _service.GetActiveCommentRestrictionAsync(userId);
            var moderatorRequest = await _service.GetPendingModeratorRequestAsync(userId);

            var response = new Dictionary<string, object?>
            {
                ["commentRestriction"] = commentRestriction?.ToDto(baseUrl),
                ["moderatorRequest"] = moderatorRequest?.ToDto(baseUrl)
            };

            bool roleChanged = Role.Value != dbRole;
            if (roleChanged)
            {
                var secret = _configuration["Jwt:Secret"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                var newAccessToken = JwtService.GenerateAccessToken(userId, dbRole, secret, issuer, audience);

                var currentRefresh = await _service.GetActiveRefreshTokenAsync(userId);
                string? newRefreshToken = null;
                if (currentRefresh != null)
                {
                    var remainingDays = (int)Math.Max(1, (currentRefresh.ExpiresAt - DateTime.UtcNow).TotalDays);
                    await _service.RevokeRefreshTokenAsync(currentRefresh);
                    newRefreshToken = (await _service.CreateRefreshTokenAsync(userId, remainingDays)).Token;
                }

                response["role"] = dbRole;
                response["tokenUpdate"] = new { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
            }

            return Ok(response);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetUsers([FromQuery] CursorDto load, [FromQuery] UserRole role)
        {
            var (users, cursorDate) = await _service.GetNextUsersAsync(load.Count, load.CursorDate, role, load.CursorId);
            var baseUrl = _configuration["App:BaseUrl"];
            var dtos = users.Select(u => u.ToPublicDto(baseUrl)).ToList();
            return Ok(new
            {
                Users = dtos,
                CursorDate = cursorDate,
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest req)
        {
            try
            {
                var userToUpdate = new User
                {
                    Id = UserId.Value,
                    Username = req.Username,
                };

                await _service.UpdateAsync(userToUpdate);
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { error = "UsernameUse" });
            }
            catch (KeyNotFoundException)
            {
                return Unauthorized();
            }
        }

        [HttpPut("me/email")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest req)
        {
            try
            {
                await _service.UpdateEmailAsync(UserId.Value, req.NewEmail, req.CurrentPassword);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return BadRequest(new { error = "IncorrectPassword" });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { error = "EmailUse" });
            }
        }

        [HttpPut("me/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromForm] IFormFile picture)
        {
            if (picture == null || picture.Length == 0)
                return BadRequest();

            var user = await _service.GetByIdAsync(UserId.Value);
            if (user == null)
                return NotFound();

            string oldImagePath = user.ProfilePicture;

            string newPath;
            try
            {
                newPath = await _imageStorageService.SaveImageAsync(picture, "profile-pictures");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { error = e.Message });
            }

            try
            {
                await _service.UpdateProfilePictureAsync(new User { Id = UserId.Value, ProfilePicture = newPath });
                _imageStorageService.DeleteImage(oldImagePath);

                var baseUrl = _configuration["App:BaseUrl"].TrimEnd('/');
                var fullUrl = $"{baseUrl}/{newPath}";

                return Ok(new { ProfilePicture = fullUrl });
            }
            catch (KeyNotFoundException ex)
            {
                _imageStorageService.DeleteImage(newPath);
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest req)
        {
            try
            {
                await _service.UpdatePasswordAsync(UserId.Value, req.NewPassword, req.CurrentPassword);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/role")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest req)
        {
            try
            {
                await _service.UpdateRoleAsync(id, req.NewRole, UserId.Value);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException)
            {
                return BadRequest();
            }
        }


        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            try
            {
                var user = await _service.GetByIdAsync(UserId.Value);
                if (user == null)
                    return NotFound("UserNotFound");

                string profilePicture = user.ProfilePicture;

                await _service.DeleteAsync(UserId.Value, UserId.Value);

                _imageStorageService.DeleteImage(profilePicture);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _service.GetByIdAsync(id);
                if (user == null)
                    return NotFound("UserNotFound");

                if (user.Role == UserRole.Administrator && user.Id == id)
                    return BadRequest("AdminCannotDeleteSelf");

                if (user.Role == UserRole.Administrator)
                    return BadRequest("CannotDeleteOtherAdmin");

                string profilePicture = user.ProfilePicture;

                await _service.DeleteAsync(id, UserId.Value);

                _imageStorageService.DeleteImage(profilePicture);

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpPost("{id}/follow")]
        public async Task<IActionResult> FollowUser(int id)
        {
            try
            {
                await _service.FollowAsync(UserId.Value, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}/unfollow")]
        public async Task<IActionResult> UnfollowUser(int id)
        {
            try
            {
                await _service.UnfollowAsync(UserId.Value, id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetUserStats(int id)
        {
            try
            {
                var (followersCount, followingCount) = await _service.GetFollowStatsAsync(id);
                return Ok(new
                {
                    FollowersCount = followersCount,
                    FollowingCount = followingCount
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("me/followers")]
        public async Task<IActionResult> GetMyFollowers([FromQuery] CursorDto load)
        {
            var followers = await _service.GetNextFollowersAsync(UserId.Value, load.Count, load.CursorDate, load.CursorId);
            var baseUrl = _configuration["App:BaseUrl"];

            var vms = followers.Select(u => u.ToPublicDto(baseUrl)).ToList();

            DateTime? nextCursorDate = null;
            int? nextCursorId = null;

            if (followers.Any())
            {
                var last = followers.Last();
                nextCursorDate = last._FollowerDate;
                nextCursorId = last._FollowerId;
            }

            return Ok(new
            {
                Followers = vms,
                CursorDate = nextCursorDate,
                CursorId = nextCursorId
            });
        }

        [HttpGet("me/following")]
        public async Task<IActionResult> GetMyFollowing([FromQuery] CursorDto load)
        {
            var following = await _service.GetNextFollowingAsync(UserId.Value, load.Count, load.CursorDate, load.CursorId);
            var baseUrl = _configuration["App:BaseUrl"];
            var vms = following.Select(u => u.ToPublicDto(baseUrl)).ToList();

            DateTime? nextCursorDate = null;
            int? nextCursorId = null;

            if (following.Any())
            {
                var last = following.Last();
                nextCursorDate = last._FollowingDate;
                nextCursorId = last._FollowingId;
            }

            return Ok(new
            {
                Followings = vms,
                CursorDate = nextCursorDate,
                CursorId = nextCursorId
            });
        }

        [Authorize(Policy = "AdminOrModerator")]
        [HttpPost("{id}/comment-restriction")]
        public async Task<IActionResult> CreateCommentRestriction(int id, [FromBody] CommentRestrictionDto req)
        {
            var restriction = new CommentRestriction
            {
                UserId = id,
                Reason = req.Reason,
                EndDate = req.EndDate
            };

            try
            {
                await _service.CreateCommentRestrictionAsync(restriction, UserId.Value);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }

            return StatusCode(201, new { id = restriction.Id });
        }

        [HttpGet("comment-restrictions")]
        [Authorize(Policy = "AdminOrModerator")]
        public async Task<IActionResult> GetCommentRestrictions([FromQuery] RestrictionStatus status, [FromQuery] CursorDto load)
        {
            var restrictions = await _service.GetLatestRestrictionsAsync(load.Count, load.CursorDate, load.CursorId, status);

            var baseUrl = _configuration["App:BaseUrl"];

            return Ok(restrictions.Select(r => r.ToDto(baseUrl)));
        }

        [HttpPut("comment-restriction/{restrictionId}/end")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EndCommentRestriction(int restrictionId)
        {
            try
            {
                await _service.EndCommentRestrictionAsync(restrictionId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("moderator-requests")]
        public async Task<IActionResult> CreateModeratorRequest([FromBody] CreateModeratorRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest();

            var moderatorRequest = new ModeratorRequest
            {
                SenderId = UserId.Value,
                RequestDescription = dto.Description
            };

            try
            {
                await _service.CreateModeratorRequestAsync(moderatorRequest);

                return StatusCode(201, new { id = moderatorRequest.Id });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        [HttpGet("moderator-requests")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetRequestsByStatus([FromQuery] CursorDto load, [FromQuery] RequestStatus status)
        {
            var requests = await _service.GetNextModeratorRequestsAsync(load.Count, load.CursorDate, load.CursorId, status);
            var baseUrl = _configuration["App:BaseUrl"];

            return Ok(requests.Select(r => r.ToDto(baseUrl)));
        }


        [HttpPut("moderator-requests/{requestId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateModeratorRequest(int requestId, [FromBody] ModeratorRequestDto req)
        {
            try
            {
                await _service.UpdateModeratorRequestAsync(requestId, req.Status, UserId.Value);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            var code = await _service.CreatePasswordResetCodeAsync(req.Email);

            if (code != null)
                await _emailService.SendPasswordResetEmailAsync(req.Email, code, req.Language);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                await _service.ResetPasswordAsync(req.Email, req.Code, req.NewPassword);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { error = "InvalidOrExpiredCode" });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { error = "InvalidOrExpiredCode" });
            }
        }
    }
}
