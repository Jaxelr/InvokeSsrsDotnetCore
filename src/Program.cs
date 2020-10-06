﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using InvokeSsrsDotnetCore.Models.Config;
using SSRS;

namespace InvokeSsrsDotnetCore
{
    internal static class Program
    {
        //TODO: Change as needed
        private const string Language = "en-US";

        private static async Task Main(string[] args)
        {
            var stubSettings = new AppSettings();

            await Execute(stubSettings);
        }

        private static async Task Execute(AppSettings settings)
        {
            var binding = GetBinding();
            var endpointAddress = new EndpointAddress(settings.ReportConfig.Url);

            ReportExecutionServiceSoapClient client = GetClient(binding, endpointAddress, settings.ReportCredentials);
            var trustedHeader = new TrustedUserHeader();

            var response = await client.LoadReportAsync(trustedHeader, settings.ReportConfig.Path, string.Empty);

            //TODO: Your parameters go here
            var reportParams = new List<ParameterValue>();

            await client.SetExecutionParametersAsync(response.ExecutionHeader, trustedHeader, reportParams.ToArray(), Language);
            var renderReport = await RenderReportAsync(client, response.ExecutionHeader, trustedHeader, settings.ReportConfig);

            //TODO: Your output location goes here
            using var fs = File.OpenWrite("c:\\temp\\output.pdf");
            fs.Write(renderReport.Result);
        }

        private static Binding GetBinding()
        {
            var binding = new BasicHttpBinding();

            //TODO: Use your server configuration for SSRS here...
            binding.Security.Mode = BasicHttpSecurityMode.None;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;

            //This is a vintage wcf issue of max size that we can resolve by props
            binding.MaxReceivedMessageSize = 2_147_483_647;
            binding.MaxBufferPoolSize = 2_147_483_647;
            binding.MaxBufferSize = 2_147_483_647;

            return binding;
        }

        private static ReportExecutionServiceSoapClient GetClient(Binding binding, EndpointAddress endpoint, Credential credential)
        {
            var client = new ReportExecutionServiceSoapClient(binding, endpoint);

            //TODO: Use your own credential logic
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            client.ClientCredentials.Windows.ClientCredential = GetCredential(credential);

            return client;
        }

        private static NetworkCredential GetCredential(Credential credential) => new NetworkCredential(credential.User, credential.Password, credential.Domain);

        private static async Task<RenderResponse> RenderReportAsync
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
    }
}