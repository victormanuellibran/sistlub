using SistLubri.Domain.Common;

namespace SistLubri.Domain.Tests.Architecture;

public sealed class DomainDependencyTests
{
    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var forbiddenAssemblies = new[]
        {
            "SistLubri.Application",
            "SistLubri.Infrastructure",
            "SistLubri.Api"
        };

        var references = typeof(Entity).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(forbiddenAssemblies, references.Contains);
    }
}
