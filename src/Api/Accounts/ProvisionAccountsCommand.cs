using IstpetDev.Application.Accounts;

namespace IstpetDev.Api.Accounts;

// H3 de 013: crea la Gerencia inicial y las cuentas de demo. Cognito genera y envía la contraseña temporal;
// este comando nunca la ve ni la imprime, y tampoco imprime correos.
public static class ProvisionAccountsCommand
{
    public const string Argument = "--provision-accounts";

    public static async Task<int> RunAsync(IServiceProvider services, string path, TextWriter output, CancellationToken ct = default)
    {
        ProvisioningInput input;
        using (var reader = File.OpenText(path)) input = ProvisioningInput.Parse(reader);
        foreach (var error in input.Errors) await output.WriteLineAsync($"línea {error.Line}: {error.Message}");
        if (input.Errors.Count > 0) return 2;

        using var scope = services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<AccountProvisioning>();
        var failures = 0;
        foreach (var row in input.Rows)
        {
            try
            {
                var result = await provisioning.ProvisionAsync(row.Email, row.DisplayName, row.Role, ct);
                await output.WriteLineAsync($"línea {row.Line}: {Describe(result.Outcome)} ({row.Role})");
            }
            catch (Exception ex) when (ex is AccountRuleException or IdentityDirectoryUnavailableException)
            {
                failures++;
                await output.WriteLineAsync($"línea {row.Line}: rechazada: {ex.Message}");
            }
        }
        return failures == 0 ? 0 : 1;
    }

    private static string Describe(ProvisionOutcome outcome) => outcome switch
    {
        ProvisionOutcome.Created => "creada; Cognito envió la invitación",
        ProvisionOutcome.Reconciled => "creada en la base; el usuario de Cognito ya existía",
        _ => "ya existía; sin cambios",
    };
}
