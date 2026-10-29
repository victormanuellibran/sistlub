namespace SistLubri.Api.Auth;

public static class Roles
{
    public const string Administrator = "Administrador";
    public const string Operator = "Operador";

    public static readonly string[] All = [Administrator, Operator];
}
