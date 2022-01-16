using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using InvokeSsrsDotnetCore.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SSRS;

//TODO: Change language as needed
const string Language = "en-US";
var settings = new AppSettings();

var builder = Host.CreateDefaultBuilder(args).ConfigureServices((ctx, services) =>
{
    //Extract the AppSettings information from the appsettings config.
    ctx.Configuration.GetSection(nameof(AppSettings)).Bind(settings);
    services.AddSingleton(settings);
});

var app = builder.Build();

await Execute(app.Services.GetRequiredService<AppSettings>());

static async Task Execute(AppSettings settings)
{
    var binding = GetBinding();
    var endpointAddress = new EndpointAddress(settings.ReportSettings.Url);

    ReportExecutionServiceSoapClient client = GetClient(binding, endpointAddress, settings.CredentialSettings);
    var trustedHeader = new TrustedUserHeader();

    var response = await client.LoadReportAsync(trustedHeader, settings.ReportSettings.Path, null);

    //TODO: Your parameters go here
    var reportParams = new List<ParameterValue>();

    await client.SetExecutionParametersAsync(response.ExecutionHeader, trustedHeader, reportParams.ToArray(), Language);
    var renderReport = await RenderReportAsync(client, response.ExecutionHeader, trustedHeader, settings.ReportSettings);

    //TODO: Your output location goes here
    using var fs = File.OpenWrite("c:\\temp\\output.pdf");
    fs.Write(renderReport.Result);
}

static Binding GetBinding()
{
    var binding = new BasicHttpBinding();

    //TODO: Use your server configuration for SSRS here...
    binding.Security.Mode = BasicHttpSecurityMode.None;
    binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;

    //This is a vintage wcf issue of max size that we can resolve by maxxing props, use wisely
    binding.MaxReceivedMessageSize = 2_147_483_647;
    binding.MaxBufferPoolSize = 2_147_483_647;
    binding.MaxBufferSize = 2_147_483_647;

    return binding;
}

static ReportExecutionServiceSoapClient GetClient(Binding binding, EndpointAddress endpoint, Credential credential)
{
    var client = new ReportExecutionServiceSoapClient(binding, endpoint);

    //TODO: Use your own credential logic
    client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
    client.ClientCredentials.Windows.ClientCredential = GetCredential(credential);

    return client;
}

static NetworkCredential GetCredential(Credential credential) => new(credential.User, credential.Password, credential.Domain);

static async Task<RenderResponse> RenderReportAsync
(
    ReportExecutionServiceSoapClient client,
    ExecutionHeader header,
    TrustedUserHeader userHeader, Report report
)
{
    string deviceInfo = $"<DeviceInfo><PageHeight>{report.Height}</PageHeight><PageWidth>{report.Width}</PageWidth><PrintDpiX>300</PrintDpiX><PrintDpiY>300</PrintDpiY></DeviceInfo>";

    var request = new RenderRequest(header, userHeader, report.Format, deviceInfo);

    return await client.RenderAsync(request);
}
