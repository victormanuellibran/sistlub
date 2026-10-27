namespace SistLubri.Application.Common.Security;

public interface ICurrentUser
{
    Guid Id { get; }
    string Nombre { get; }
}
