using Microsoft.AspNetCore.Mvc;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Global configuration for how <see cref="Rail{T}"/> values are translated to
/// HTTP responses. Register via <c>builder.Services.AddRailway()</c> or
/// <c>builder.Services.Configure&lt;RailwayOptions&gt;()</c>.
/// </summary>
public sealed class RailwayOptions
{
    /// <summary>
    /// A global callback applied to every <see cref="ProblemDetails"/> response
    /// produced by UnionRailway. Use this to inject trace IDs, strip sensitive
    /// details in production, or enrich extensions.
    /// <para>
    /// This is applied <em>after</em> the default or custom mapping and
    /// <em>after</em> any per-call <c>configureProblem</c> callback.
    /// </para>
    /// </summary>
    public Action<ProblemDetails>? ConfigureProblem { get; set; }
}
