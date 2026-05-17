using Halen.Application.Common;
using Halen.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Halen.Application.Profile.Commands;

public class ChangePasswordCommandHandler(
    UserManager<User> userManager
) : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
            return new ChangePasswordResult(false, "User not found", ErrorKind.NotFound);

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return new ChangePasswordResult(false, result.Errors.First().Description);

        return new ChangePasswordResult(true);
    }
}
