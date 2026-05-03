using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using System.IO;
using System.Threading.Tasks;

namespace UnionRailway.IntegrationTests;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });

        builder.ConfigureTestServices(services =>
        {
            // Registering the workaround filter for the TestHost bug
            services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, TestHostBugWorkaroundStartupFilter>();
        });

        builder.UseEnvironment("Development");
    }
}

// Workaround for .NET 8 TestHost bug where ProblemDetails serialization fails because 
// the TestHost ResponseBodyPipeWriter does not implement PipeWriter.UnflushedBytes.
// This replaces the TestServer's un-flushable body with a standard MemoryStream body.
public class TestHostBugWorkaroundStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
{
    public System.Action<IApplicationBuilder> Configure(System.Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.Use(async (context, nextMiddleware) =>
            {
                var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();
                if (originalBodyFeature != null)
                {
                    using var memoryStream = new MemoryStream();
                    context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeatureWrapper(memoryStream));

                    await nextMiddleware();

                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBodyFeature.Stream);
                }
                else
                {
                    await nextMiddleware();
                }
            });
            next(builder);
        };
    }
}

public class StreamResponseBodyFeatureWrapper : IHttpResponseBodyFeature
{
    private readonly Stream _stream;
    public StreamResponseBodyFeatureWrapper(Stream stream) => _stream = stream;
    public Stream Stream => _stream;
    public System.IO.Pipelines.PipeWriter Writer => System.IO.Pipelines.PipeWriter.Create(_stream);
    public void DisableBuffering() { }
    public Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StartAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CompleteAsync() => Task.CompletedTask;
}
