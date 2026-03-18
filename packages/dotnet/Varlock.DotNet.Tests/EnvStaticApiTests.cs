using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Varlock.DotNet.Tests;

public sealed class EnvStaticApiTests
{
  [Fact]
  public void Env_is_static_class()
  {
    Assert.True(typeof(Env).IsAbstract && typeof(Env).IsSealed);
  }

  [Fact]
  public void Env_exposes_expected_static_load_overloads()
  {
    var loadMethods = typeof(Env)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method => method.Name == "Load")
      .OrderBy(method => method.GetParameters().Length)
      .ToArray();

    Assert.Equal(3, loadMethods.Length);

    // Load()
    Assert.Empty(loadMethods[0].GetParameters());
    Assert.Equal(typeof(VarlockResolvedGraph), loadMethods[0].ReturnType);

    // Load(VarlockLoadOptions)
    Assert.Single(loadMethods[1].GetParameters());
    Assert.Equal(typeof(VarlockLoadOptions), loadMethods[1].GetParameters()[0].ParameterType);
    Assert.Equal(typeof(VarlockResolvedGraph), loadMethods[1].ReturnType);

    // Load(Action<VarlockLoadOptions>)
    Assert.Single(loadMethods[2].GetParameters());
    Assert.Equal(typeof(Action<VarlockLoadOptions>), loadMethods[2].GetParameters()[0].ParameterType);
    Assert.Equal(typeof(VarlockResolvedGraph), loadMethods[2].ReturnType);
  }

  [Fact]
  public void Env_exposes_expected_static_loadasync_overloads()
  {
    var asyncMethods = typeof(Env)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method => method.Name == "LoadAsync")
      .OrderBy(method => method.GetParameters().Length)
      .ToArray();

    Assert.Equal(2, asyncMethods.Length);

    // LoadAsync(CancellationToken)
    Assert.Single(asyncMethods[0].GetParameters());
    Assert.Equal(typeof(CancellationToken), asyncMethods[0].GetParameters()[0].ParameterType);
    Assert.Equal(typeof(Task<VarlockResolvedGraph>), asyncMethods[0].ReturnType);

    // LoadAsync(VarlockLoadOptions, CancellationToken)
    Assert.Equal(2, asyncMethods[1].GetParameters().Length);
    Assert.Equal(typeof(VarlockLoadOptions), asyncMethods[1].GetParameters()[0].ParameterType);
    Assert.Equal(typeof(CancellationToken), asyncMethods[1].GetParameters()[1].ParameterType);
    Assert.Equal(typeof(Task<VarlockResolvedGraph>), asyncMethods[1].ReturnType);
  }

  [Fact]
  public void Load_with_options_passes_options_through()
  {
    var options = new VarlockLoadOptions
    {
      SchemaPath = ".env.schema",
      WorkingDirectory = System.IO.Path.Combine(TestPaths.RepositoryRoot, "examples", "dotnet-console"),
    };

    var graph = Env.Load(options);

    Assert.NotNull(graph);
    Assert.NotEmpty(graph.Items);
  }

  [Fact]
  public void Load_with_configure_applies_action()
  {
    var graph = Env.Load(options =>
    {
      options.SchemaPath = ".env.schema";
      options.WorkingDirectory = System.IO.Path.Combine(TestPaths.RepositoryRoot, "examples", "dotnet-console");
    });

    Assert.NotNull(graph);
    Assert.NotEmpty(graph.Items);
  }

  [Fact]
  public async Task LoadAsync_with_options_passes_options_through()
  {
    var options = new VarlockLoadOptions
    {
      SchemaPath = ".env.schema",
      WorkingDirectory = System.IO.Path.Combine(TestPaths.RepositoryRoot, "examples", "dotnet-console"),
    };

    var graph = await Env.LoadAsync(options);

    Assert.NotNull(graph);
    Assert.NotEmpty(graph.Items);
  }
}
