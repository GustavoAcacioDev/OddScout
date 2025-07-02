using OddScout.Application.Common.Interfaces;
using OddScout.Application.DTOs;

namespace OddScout.Application.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserDto>;