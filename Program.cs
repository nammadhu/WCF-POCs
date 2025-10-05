using System.Diagnostics;
using WCF_POCs.Logging;
using WCF_POCs.LoggerCustomOfOwnChoice;

var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

if (IsLocalhostEnvironment())
{
    //AddCustomLoggerAsOptionalChoice();//Even if not added default Console & File logger works perfectly

    builder.Services.AddSingleton<IServiceBehavior, WcfMessageLoggingExtension>();
}
builder.Services.AddSingleton<IServiceBehavior, WcfMessageLoggingExtension>();


var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<Service1>();
    serviceBuilder.AddServiceEndpoint<Service1, IService1>(new BasicHttpBinding(BasicHttpSecurityMode.Transport), "/Service1.svc");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

app.Run();



static bool IsLocalhostEnvironment()
{
    var serverUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? string.Empty;
    var machineName = Environment.MachineName?.ToLowerInvariant() ?? string.Empty;
    var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    var isDebuggerAttached = Debugger.IsAttached;

    return isDebuggerAttached || isDevelopment ||
           serverUrls.Contains("localhost", StringComparison.OrdinalIgnoreCase) || serverUrls.Contains("127.0.0.1") ||
           machineName.Contains("localhost") || machineName.Contains("dev");
}

static void AddCustomLoggerAsOptionalChoice()
{
    try
    {
        // Try to initialize User logging integration
        LoggerCustomSelectionInsteadOfDefaultLogger.Initialize();
    }
    catch
    {
        // If User setup fails, WcfMessageLoggingExtension will use defaults
        Console.WriteLine("[INFO] Using default WCF logging configuration");
    }
}