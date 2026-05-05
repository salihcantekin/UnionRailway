using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;

namespace UnionRailway.Tests;

// Workaround for .NET 11 TestHost bug where ProblemDetails serialization fails because 
// the TestHost ResponseBodyPipeWriter does not implement PipeWriter.UnflushedBytes.
// This replaces the TestServer's un-flushable body with a standard MemoryStream body.
internal class TestHostBugWorkaroundStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
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

internal class StreamResponseBodyFeatureWrapper : IHttpResponseBodyFeature
{
    private readonly Stream _stream;
    public StreamResponseBodyFeatureWrapper(Stream stream) => _stream = stream;
    public Stream Stream => _stream;
    public System.IO.Pipelines.PipeWriter Writer => System.IO.Pipelines.PipeWriter.Create(_stream);
    public void DisableBuffering() { }
    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CompleteAsync() => Task.CompletedTask;
}
