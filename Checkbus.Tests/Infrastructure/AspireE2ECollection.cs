namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Forces every Aspire-hosted end-to-end test class onto this single collection so xUnit
/// never runs them in parallel with each other. They all share one Postgres data volume
/// (<c>WithDataVolume()</c> in Checkbus.AppHost), and concurrent Postgres startups against
/// the same volume corrupt its WAL, cascading timeouts across the whole suite.
/// </summary>
[CollectionDefinition(Name)]
public class AspireE2ECollection
{
    public const string Name = "Aspire E2E";
}
