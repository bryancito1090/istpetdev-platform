using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using IstpetDev.Application.Accounts;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Infrastructure.Identity;

// Credenciales: cadena por defecto del SDK (perfil de instancia en EC2, AWS_PROFILE en local). NFR-02.
// En pools con username_attributes = email, el Username interno es el sub: las llamadas Admin usan Subject.
public sealed class CognitoIdentityDirectory(IAmazonCognitoIdentityProvider cognito, CognitoPool pool, ILogger<CognitoIdentityDirectory> logger) : IIdentityDirectory
{
    public string Issuer => pool.Issuer;

    public async Task<DirectoryUser?> FindByEmailAsync(string email, CancellationToken ct) =>
        (await ListAsync($"email = \"{email}\"", ct)).SingleOrDefault();

    public async Task<DirectoryUser?> FindBySubjectAsync(string subject, CancellationToken ct) =>
        (await ListAsync($"sub = \"{subject}\"", ct)).SingleOrDefault();

    public Task<IReadOnlyList<DirectoryUser>> ListUsersAsync(CancellationToken ct) => ListAsync(null, ct);

    public async Task<DirectoryUser> CreateUserAsync(string email, CancellationToken ct)
    {
        try
        {
            var response = await Call(() => cognito.AdminCreateUserAsync(new AdminCreateUserRequest
            {
                UserPoolId = pool.UserPoolId,
                Username = email,
                DesiredDeliveryMediums = ["EMAIL"],
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "email_verified", Value = "true" },
                ],
            }, ct));
            return Map(response.User.Attributes, response.User.UserStatus, response.User.Enabled == true);
        }
        catch (UsernameExistsException)
        {
            return await FindByEmailAsync(email, ct)
                ?? throw new IdentityDirectoryUnavailableException("Cognito informó un usuario existente que no se pudo leer.");
        }
    }

    public Task DisableAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminDisableUserAsync(new AdminDisableUserRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task EnableAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminEnableUserAsync(new AdminEnableUserRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task SignOutEverywhereAsync(string subject, CancellationToken ct) =>
        Call(() => cognito.AdminUserGlobalSignOutAsync(new AdminUserGlobalSignOutRequest { UserPoolId = pool.UserPoolId, Username = subject }, ct));

    public Task ResendInvitationAsync(string email, CancellationToken ct) =>
        Call(() => cognito.AdminCreateUserAsync(new AdminCreateUserRequest
        {
            UserPoolId = pool.UserPoolId, Username = email, MessageAction = MessageActionType.RESEND, DesiredDeliveryMediums = ["EMAIL"],
        }, ct));

    private async Task<IReadOnlyList<DirectoryUser>> ListAsync(string? filter, CancellationToken ct)
    {
        var users = new List<DirectoryUser>();
        string? token = null;
        do
        {
            var page = await Call(() => cognito.ListUsersAsync(new ListUsersRequest
            {
                UserPoolId = pool.UserPoolId, Filter = filter, Limit = 60, PaginationToken = token,
            }, ct));
            users.AddRange((page.Users ?? []).Select(u => Map(u.Attributes, u.UserStatus, u.Enabled == true)));
            token = page.PaginationToken;
        } while (!string.IsNullOrEmpty(token));
        return users;
    }

    // SDK 4: las colecciones vacías llegan como null.
    private static DirectoryUser Map(List<AttributeType>? attributes, UserStatusType? status, bool enabled)
    {
        string Attribute(string name) => attributes?.FirstOrDefault(a => a.Name == name)?.Value ?? string.Empty;
        var state = status == UserStatusType.FORCE_CHANGE_PASSWORD ? DirectoryUserStatus.InvitationPending
            : status == UserStatusType.CONFIRMED ? DirectoryUserStatus.Active : DirectoryUserStatus.Other;
        return new DirectoryUser(Attribute("sub"), Attribute("email"), state, enabled);
    }

    private async Task<T> Call<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception ex) when (ex is AmazonServiceException and not UsernameExistsException or AmazonClientException or HttpRequestException)
        {
            logger.LogWarning("Cognito no respondió a una operación de cuentas: {ErrorType}", ex.GetType().Name);
            throw new IdentityDirectoryUnavailableException("Cognito no está disponible.", ex);
        }
    }
}
