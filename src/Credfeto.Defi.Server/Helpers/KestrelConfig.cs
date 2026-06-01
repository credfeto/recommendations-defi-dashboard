using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Credfeto.Defi.Server.Helpers;

internal static class KestrelConfig
{
    private const int HTTP_PORT = 8080;
    private const int HTTPS_PORT = 8081;
    private const string CERT_FILENAME = "server.pfx";

    public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder webHostBuilder)
    {
        return webHostBuilder.UseKestrel(ConfigureKestrelOptions);
    }

    private static void ConfigureKestrelOptions(KestrelServerOptions options)
    {
        options.DisableStringReuse = false;
        options.AllowSynchronousIO = false;
        options.AddServerHeader = false;
        options.Limits.MinResponseDataRate = null;
        options.Limits.MinRequestBodyDataRate = null;

        string certPath = Path.Combine(path1: AppContext.BaseDirectory, path2: CERT_FILENAME);

        if (File.Exists(certPath))
        {
            options.Listen(
                address: IPAddress.IPv6Any,
                port: HTTPS_PORT,
                configure: o => ConfigureHttpsEndpoint(listenOptions: o, certFile: certPath)
            );
        }

        // Plain HTTP on loopback only — used by the health check
        options.Listen(address: IPAddress.Loopback, port: HTTP_PORT, configure: ConfigureHttpEndpoint);
    }

    private static void ConfigureHttpEndpoint(ListenOptions listenOptions)
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    }

    private static void ConfigureHttpsEndpoint(ListenOptions listenOptions, string certFile)
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        _ = listenOptions.UseHttps(fileName: certFile, password: null);
    }
}
