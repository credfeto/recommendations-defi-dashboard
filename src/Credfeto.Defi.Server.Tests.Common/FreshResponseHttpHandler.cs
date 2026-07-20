using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.Defi.Server.Tests.Common;

public sealed class FreshResponseHttpHandler : HttpMessageHandler
{
    private readonly string _json;

    public FreshResponseHttpHandler(string json) => this._json = json;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(this._json, Encoding.UTF8, mediaType: "application/json"),
            }
        );
    }
}
