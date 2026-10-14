namespace IstpetDev.Application.Accounts;

// Regla de negocio de cuentas con código estable para Problem Details (spec 013).
public sealed class AccountRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
