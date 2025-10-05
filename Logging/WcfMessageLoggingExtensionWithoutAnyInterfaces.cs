//-----------------------------------------------------------------------------------------
// WCF Message Logging Extension - Open Source Component
// A CoreWCF message inspector that logs incoming and outgoing SOAP messages.
// Used for capturing WCF service requests and responses for SOAPUI testing.
//-----------------------------------------------------------------------------------------
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace WCF_POCs.Logging;

/// <summary>
/// If no need of any custom logger then this WcfMessageLoggingExtensionWithoutAnyInterfaces can be used directly.
/// If need to use any custom logger then implement ICustomLogger interface and use WcfMessageLoggingExtension class.
/// </summary>
public class WcfMessageLoggingExtensionWithoutAnyInterfaces : IDispatchMessageInspector, IServiceBehavior
{
    private const string UrlDefaultInCaseOfExtractionFailed = "http://localhost/Service";

    private static readonly bool LoggingEnabled = GetConfigurationBool("EnableWcfMessageLogging", true);
    private static readonly string LoggingPath = GetConfigurationString("WcfMessageLoggingPath", Path.Combine(AppContext.BaseDirectory, "WCFLogs", DateTime.Today.ToString("ddMMMyyyy")));
    private static readonly int MaxMessageSize = GetConfigurationInt("WcfMessageLoggingMaxSize", 5242880);
    private static readonly bool RawXmlConsoleLogging = GetConfigurationBool("RawXmlConsoleLogging", false);
    private static readonly bool SoapXmlConsoleLogging = GetConfigurationBool("SoapXmlConsoleLogging", false);
    private static readonly bool RawXmlFileLogging = GetConfigurationBool("RawXmlFileLogging", false);
    private static readonly bool SoapXmlFileLogging = GetConfigurationBool("SoapXmlFileLogging", true);

    public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
    {
        if (!IsLoggingEnabled())
        {
            return null;
        }

        try
        {
            MessageBuffer buffer = request.CreateBufferedCopy(MaxMessageSize);
            Message requestCopy = buffer.CreateMessage();

            string operationName = GetOperationNameSafely(requestCopy);
            string correlationId = Guid.NewGuid().ToString();
            string endpointUrl = ExtractEndpointUrl(requestCopy);
            string rawXml = GetMessageContentSafely(requestCopy);

            ProcessMessageLogging(rawXml, operationName, correlationId, endpointUrl, "Request", true);

            request = buffer.CreateMessage();

            return new MessageLoggingCorrelationState
            {
                CorrelationId = correlationId,
                OperationName = operationName,
                Timestamp = DateTime.UtcNow,
                MessageContent = rawXml,
                EndpointUrl = endpointUrl
            };
        }
        catch (Exception ex)
        {
            LogError($"Error logging WCF request: {ex.Message}", ex);
            return null;
        }
    }

    public void BeforeSendReply(ref Message reply, object correlationState)
    {
        if (!IsLoggingEnabled())
        {
            return;
        }

        try
        {
            if (correlationState is not MessageLoggingCorrelationState state)
            {
                return;
            }

            MessageBuffer buffer = reply.CreateBufferedCopy(MaxMessageSize);
            Message replyCopy = buffer.CreateMessage();

            string rawXml = GetMessageContentSafely(replyCopy);

            ProcessMessageLogging(rawXml, state.OperationName, state.CorrelationId, state.EndpointUrl, "Response", false);

            TimeSpan elapsed = DateTime.UtcNow - state.Timestamp;
            LogDebug($"WCF operation {state.OperationName} completed in {elapsed.TotalMilliseconds}ms");

            reply = buffer.CreateMessage();
        }
        catch (Exception ex)
        {
            LogError($"Error logging WCF response: {ex.Message}", ex);
        }
    }

    public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase,
        Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
    {
    }

    public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
        try
        {
            if (serviceHostBase?.ChannelDispatchers == null)
            {
                LogWarning("Cannot apply WCF logging - serviceHostBase or ChannelDispatchers is null");
                return;
            }

            LogInfo($"WCF Logging: Adding message inspector to {serviceHostBase.ChannelDispatchers.Count} dispatchers");
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
                {
                    endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
                    LogInfo($"Added WCF logging inspector to endpoint: {endpointDispatcher.EndpointAddress}");
                }
            }

            EnsureLogDirectoryExists();
        }
        catch (Exception ex)
        {
            LogError($"Error applying WCF message inspector: {ex.Message}", ex);
        }
    }

    public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
    }

    private static bool IsLoggingEnabled()
    {
        return LoggingEnabled || Debugger.IsAttached;
    }

    private static void ProcessMessageLogging(string rawXml, string operationName, string correlationId, string endpointUrl, string messageType, bool isRequest)
    {
        string soapXml = CreateSoapUiCompatibleMessage(rawXml, operationName, endpointUrl, isRequest);

        WriteToFiles(rawXml, soapXml, operationName, correlationId, messageType);
        WriteToConsole(rawXml, soapXml, operationName, correlationId, endpointUrl, messageType, isRequest);
    }

    private static void WriteToFiles(string rawXml, string soapXml, string operationName, string correlationId, string messageType)
    {
        if (RawXmlFileLogging)
        {
            WriteToLogFile(rawXml, operationName, correlationId, $"raw{messageType}.RAW");
        }

        if (SoapXmlFileLogging)
        {
            WriteToLogFile(soapXml, operationName, correlationId, $"{messageType.ToLower()}");
        }
    }

    private static void WriteToConsole(string rawXml, string soapXml, string operationName, string correlationId, string endpointUrl, string messageType, bool isRequest)
    {
        if (!RawXmlConsoleLogging && !SoapXmlConsoleLogging)
        {
            return;
        }

        ConsoleColor headerColor = isRequest ? ConsoleColor.Green : ConsoleColor.Blue;
        Console.ForegroundColor = headerColor;
        Console.WriteLine($"\n========== WCF {messageType.ToUpper()} ==========");
        Console.WriteLine($"Operation: {operationName}");
        Console.WriteLine($"Correlation ID: {correlationId}");

        if (isRequest)
        {
            Console.WriteLine($"Timestamp: {DateTime.Now}");
            Console.WriteLine($"Endpoint: {endpointUrl}");
        }

        if (RawXmlConsoleLogging)
        {
            Console.WriteLine($"{messageType} XML (Raw):");
            Console.WriteLine(rawXml);
        }

        if (SoapXmlConsoleLogging)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nSOAPUI Compatible XML:");
            Console.WriteLine(soapXml);
        }

        Console.WriteLine("================================\n");
        Console.ResetColor();
    }

    private static string GetMessageContentSafely(Message message)
    {
        try
        {
            if (message == null)
            {
                return "<null message>";
            }

            MemoryStream ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document,
                OmitXmlDeclaration = false
            };

            using (XmlWriter writer = XmlWriter.Create(ms, settings))
            {
                MessageBuffer buffer = message.CreateBufferedCopy(MaxMessageSize);
                Message msgCopy = buffer.CreateMessage();
                try
                {
                    msgCopy.WriteMessage(writer);
                }
                catch (Exception ex)
                {
                    return $"<Error writing message: {ex.Message}>";
                }
            }

            ms.Position = 0;
            using (StreamReader reader = new StreamReader(ms, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            return $"<Error extracting message content: {ex.Message}>";
        }
    }

    private static string GetOperationNameSafely(Message message)
    {
        try
        {
            if (message.Headers.Action == null)
            {
                Uri to = message.Headers.To;
                if (to != null)
                {
                    string[] segments = to.Segments;
                    if (segments.Length > 0)
                    {
                        return segments[segments.Length - 1];
                    }
                }

                return "UnknownOperation";
            }

            string[] parts = message.Headers.Action.Split('/');
            return parts.Length > 0 ? parts[parts.Length - 1] : "UnknownOperation";
        }
        catch
        {
            return "UnknownOperation";
        }
    }

    private static string ExtractEndpointUrl(Message message)
    {
        try
        {
            Uri to = message.Headers.To;
            return to?.ToString() ?? UrlDefaultInCaseOfExtractionFailed;
        }
        catch
        {
            return UrlDefaultInCaseOfExtractionFailed;
        }
    }

    private static string CreateSoapUiCompatibleMessage(string originalXml, string operationName, string endpointUrl, bool isRequest)
    {
        try
        {
            XDocument doc;
            try
            {
                doc = XDocument.Parse(originalXml);
            }
            catch (Exception)
            {
                return isRequest ? CreateBasicSoapTemplate(operationName, endpointUrl) : originalXml;
            }

            XNamespace soapNs = doc.Root.Name.Namespace;
            XElement body = doc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
            if (body == null)
            {
                return isRequest ? CreateBasicSoapTemplate(operationName, endpointUrl) : originalXml;
            }

            XElement operation = body.Elements().FirstOrDefault();
            if (operation == null)
            {
                return isRequest ? CreateBasicSoapTemplate(operationName, endpointUrl) : originalXml;
            }

            XDocument newDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(soapNs + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "soapenv", soapNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "tem", "http://tempuri.org/"),
                    new XElement(soapNs + "Header"),
                    new XElement(soapNs + "Body", operation)));

            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false }))
            {
                newDoc.WriteTo(writer);
            }

            string result = sb.ToString();
            string commentType = isRequest ? "Request" : "Response";
            string comment = isRequest
                ? $"<!-- SOAPUI Compatible {commentType} for {operationName}\r\nEndpoint: {endpointUrl}\r\n-->\r\n"
                : $"<!-- SOAPUI Compatible {commentType} for {operationName} -->\r\n";

            return comment + result;
        }
        catch (Exception ex)
        {
            return $"<!-- Error creating SOAPUI compatible message: {ex.Message} -->\r\n{originalXml}";
        }
    }

    private static string CreateBasicSoapTemplate(string operationName, string endpointUrl)
    {
        return $@"<!-- SOAPUI Compatible Request for {operationName}
Endpoint: {endpointUrl}
-->
<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"">
  <soapenv:Header/>
  <soapenv:Body>
    <tem:{operationName}>
      <!-- Add parameters here -->
    </tem:{operationName}>
  </soapenv:Body>
</soapenv:Envelope>";
    }

    private static void WriteToLogFile(string content, string operationName, string correlationId, string type)
    {
        try
        {
            EnsureLogDirectoryExists();

            var fileName = Path.Combine(LoggingPath, $"{operationName}_{type}_{DateTime.Now:HHmmss}_{correlationId.Substring(0, 5)}....xml");

            File.WriteAllText(fileName, content, Encoding.UTF8);
            LogInfo($"WCF message logged to {fileName}");
        }
        catch (Exception ex)
        {
            LogError($"Failed to write WCF log to file: {ex.Message}", ex);
        }
    }

    private static void EnsureLogDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(LoggingPath))
            {
                Directory.CreateDirectory(LoggingPath);
                LogInfo($"Created WCF log directory: {LoggingPath}");

                if (RawXmlConsoleLogging || SoapXmlConsoleLogging)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nWCF logging enabled. Log files will be saved to: {LoggingPath}\n");
                    Console.ResetColor();
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to create log directory {LoggingPath}: {ex.Message}", ex);

            try
            {
                string fallbackPath = Path.Combine(AppContext.BaseDirectory, "WCFLogs");
                if (LoggingPath != fallbackPath && !Directory.Exists(fallbackPath))
                {
                    Directory.CreateDirectory(fallbackPath);
                    LogInfo($"Created fallback WCF log directory: {fallbackPath}");
                }
            }
            catch
            {
            }
        }
    }

    private static void LogError(string message, Exception ex = null)
    {
        WriteColoredLog("ERROR", ConsoleColor.Red, message);
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex}");
        }
    }

    private static void LogWarning(string message)
    => WriteColoredLog("WARN", ConsoleColor.Yellow, message);

    private static void LogInfo(string message)
    => WriteColoredLog("INFO", ConsoleColor.Cyan, message);

    private static void LogDebug(string message)
    => WriteColoredLog("DEBUG", ConsoleColor.Gray, message);

    private static void WriteColoredLog(string level, ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        Console.ResetColor();
    }

    private static string GetConfigurationString(string key, string defaultValue = null)
    => Environment.GetEnvironmentVariable(key) ??
               System.Configuration.ConfigurationManager.AppSettings[key] ??
               defaultValue;

    private static bool GetConfigurationBool(string key, bool defaultValue = false)
    => bool.TryParse(GetConfigurationString(key), out var result) ? result : defaultValue;

    private static int GetConfigurationInt(string key, int defaultValue = 0)
    => int.TryParse(GetConfigurationString(key), out var result) ? result : defaultValue;

    private class MessageLoggingCorrelationState
    {
        public string CorrelationId { get; set; }
        public string OperationName { get; set; }
        public DateTime Timestamp { get; set; }
        public string MessageContent { get; set; }
        public string EndpointUrl { get; set; }
    }
}