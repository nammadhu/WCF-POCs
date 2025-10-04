# WCF POCs - .NET Framework to .NET 8 Migration with Logging

A practical demonstration of migrating WCF services from .NET Framework 4.8 to .NET 8 using CoreWCF, with comprehensive request/response logging.

## ?? Project Goal

Learning resource for beginners to understand:
- WCF migration from .NET Framework to .NET 8
- SOAP request/response logging implementation
- WCF testing with SoapUI

## ?? Repository Structure

**Branch-based learning approach** - each WCF concept in separate branches:

### Current (main branch)
- ? Basic WCF Service Migration (.NET Framework 4.8 ? .NET 8)
- ? Request/Response Logging with SoapUI-compatible output

### Upcoming Concepts (separate branches)
- ?? Security (Authentication & Authorization)
- ?? Custom Bindings (HTTP, TCP, Named Pipes)
- ?? Fault Handling & Error Management
- ?? Session Management & State
- ?? Performance Optimization
- ?? Unit Testing Strategies

### Learning with Branches
```bash
git branch -r                    # List all branches
git checkout feature/security    # Switch to security branch
git diff main feature/security   # See differences
```

## ?? Quick Start

### Prerequisites
- .NET 8 SDK
- SoapUI (for testing)

### Run the Service
```bash
git clone https://github.com/nammadhu/WCF-POCs
cd WCF_POCs
dotnet run
```

### Access Points
- **Service**: `https://localhost:7012/Service1.svc`
- **WSDL**: `https://localhost:7012/Service1.svc?wsdl`

?? **Common Error**: Don't access `https://localhost:7012/` (shows error page)

## ?? Testing with SoapUI

1. **New SOAP Project** ? WSDL: `https://localhost:7012/Service1.svc?wsdl`
2. **Test Operations**:
   - `GetData(1)` ? Returns: "You entered: 1"
   - `GetDataUsingDataContract` with `BoolValue=true`, `StringValue=madhu`

### Sample Request
```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:tem="http://tempuri.org/">
   <soapenv:Body>
      <tem:GetData>
         <tem:value>1</tem:value>
      </tem:GetData>
   </soapenv:Body>
</soapenv:Envelope>
```

## ?? Logging Features

### What's Logged
- Complete SOAP request/response messages
- Operation timing and correlation IDs
- SoapUI-compatible XML format

### Log Location
- **Files**: `{ProjectDirectory}/WCFLogs/` 
- **Console**: Colorized output (green=request, blue=response)

### Configuration (Environment Variables)
```
EnableWcfMessageLogging=true
SoapXmlConsoleLogging=true
SoapXmlFileLogging=true
```

## ?? Using the Logger in Your Project

1. **Copy files**:
   - `Logging/WcfMessageLoggingExtension.cs`
   - `LoggingUsageByCaller/WcfLoggingIntegration.cs` (optional)

2. **Add to Program.cs**:
   ```csharp
   builder.Services.AddSingleton<IServiceBehavior, WcfMessageLoggingExtension>();
   ```

3. **Custom logger** (optional):
   ```csharp
   WcfLoggerFactory.SetLogger(new YourLogger());
   ```

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| "localhost page can't be found" | Use full URL: `/Service1.svc` |
| WSDL not accessible | Check `serviceMetadataBehavior.HttpsGetEnabled = true` |
| No log files | Check directory permissions, verify logging is enabled |

## ?? Key Migration Points

- **Service Contracts**: Mostly unchanged
- **Hosting**: IIS/Windows Service ? ASP.NET Core
- **Configuration**: web.config ? Dependency Injection
- **Message Inspection**: `IDispatchMessageInspector` for logging

## ?? Contributing

- Submit issues for questions
- Add screenshots to help beginners
- Propose new WCF concepts for branches

**Branch naming**: `feature/concept-name`, `docs/improvement`

## ?? References

- [CoreWCF Documentation](https://github.com/CoreWCF/CoreWCF)
- [.NET 8 Migration Guide](https://docs.microsoft.com/en-us/dotnet/core/porting/)

---

**Note**: Complete migration example with production-ready logging. Each WCF concept developed in separate branches for focused learning.