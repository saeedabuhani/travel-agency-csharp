using Microsoft.AspNetCore.Identity;

public class UserRegistrationHandler
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserRegistrationHandler(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task AssignUserRoleAsync(IdentityUser user)
    {
        await _userManager.AddToRoleAsync(user, "User");
    }
}
