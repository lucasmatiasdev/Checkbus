var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var checkbusDb = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("18.3")
    .WithDataVolume()
    .WithPgAdmin(pgAdmin => pgAdmin.WithImageTag("9.15.0"))
    .AddDatabase("checkbusdb");

var apiService = builder.AddProject<Projects.Checkbus_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(checkbusDb)
    .WaitFor(checkbusDb);

builder.AddProject<Projects.Checkbus_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
