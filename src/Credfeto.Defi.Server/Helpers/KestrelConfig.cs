using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Credfeto.Defi.Server.Helpers;

internal static class KestrelConfig
{
    private const int HTTPS_PORT = 8081;

    public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder webHostBuilder, string certPath)
    {
        return webHostBuilder.UseKestrel(options => ConfigureKestrelOptions(options: options, certPath: certPath));
    }

    private static void ConfigureKestrelOptions(KestrelServerOptions options, string certPath)
    {
        options.DisableStringReuse = false;
        options.AllowSynchronousIO = false;
        options.AddServerHeader = false;
        options.Limits.MinResponseDataRate = null;
        options.Limits.MinRequestBodyDataRate = null;

        if (File.Exists(certPath))
        {
            options.Listen(
                address: IPAddress.Any,
                port: HTTPS_PORT,
                configure: o => ConfigureHttpsEndpoint(listenOptions: o, certFile: certPath)
            );
        }
    }

    [SuppressMessage(
        category: "Microsoft.Reliability",
        checkId: "CA2000:DisposeObjectsBeforeLosingScope",
        Justification = "Lives for program lifetime"
    )]
    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007:DisposeObjectsBeforeLosingScope",
        Justification = "Lives for program lifetime"
    )]
    private static void ConfigureHttpsEndpoint(ListenOptions listenOptions, string certFile)
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        X509Certificate2 cert = X509CertificateLoader.LoadPkcs12FromFile(
            path: certFile,
            password: null,
            keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet
        );
        listenOptions.UseHttps(serverCertificate: cert);
    }
}
