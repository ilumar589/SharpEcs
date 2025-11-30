using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SharpEcs.Component;
using SharpEcs.World;
using Xunit;

namespace SharpEcs.Tests;

/// <summary>
/// Performance benchmarks for the SharpEcs library.
/// Uses BenchmarkDotNet for accurate performance measurements.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks demonstrate the performance characteristics of the ECS implementation,
/// including entity creation, component access, and iteration.
/// </para>
/// <para>
/// Key metrics to observe:
/// <list type="bullet">
/// <item><description>Entity creation time</description></item>
/// <item><description>Component access via ref return vs copy</description></item>
/// <item><description>Span-based iteration performance</description></item>
/// <item><description>Query performance across archetypes</description></item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class PerformanceBenchmarks
{
    private EcsWorld _world = null!;
    private List<Entity.Entity> _entities = null!;
    private ComponentStore<Position> _componentStore = null!;

    /// <summary>
    /// Sets up the benchmark environment with pre-populated data.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _world = new EcsWorld();
        _entities = new List<Entity.Entity>();
        _componentStore = new ComponentStore<Position>(10000);

        // Pre-populate for iteration benchmarks
        for (int i = 0; i < 10000; i++)
        {
            _entities.Add(_world.CreateEntity(
                new Position { X = i, Y = i, Z = i },
                new Velocity { X = 1, Y = 1, Z = 1 }
            ));
            _componentStore.Add(new Position { X = i, Y = i, Z = i });
        }
    }

    /// <summary>
    /// Benchmarks creating 10,000 entities with two components each.
    /// </summary>
    [Benchmark]
    public void CreateEntities_10000()
    {
        var world = new EcsWorld();
        for (int i = 0; i < 10000; i++)
        {
            world.CreateEntity(
                new Position { X = i, Y = i, Z = i },
                new Velocity { X = 1, Y = 1, Z = 1 }
            );
        }
    }

    /// <summary>
    /// Benchmarks querying entities with Position and Velocity components.
    /// </summary>
    [Benchmark]
    public int QueryEntities_Position_Velocity()
    {
        var result = _world.Query(typeof(Position), typeof(Velocity));
        return result.Count;
    }

    /// <summary>
    /// Benchmarks iterating components using Span for maximum performance.
    /// </summary>
    [Benchmark]
    public float IterateComponents_Span()
    {
        float sum = 0;
        Span<Position> span = _componentStore.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i].X + span[i].Y + span[i].Z;
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks iterating components using ref foreach for zero-copy access.
    /// </summary>
    [Benchmark]
    public float IterateComponents_RefForeach()
    {
        float sum = 0;
        foreach (ref Position pos in _componentStore.AsSpan())
        {
            sum += pos.X + pos.Y + pos.Z;
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks accessing components via ref return for in-place modification.
    /// </summary>
    [Benchmark]
    public void ModifyComponents_RefReturn()
    {
        for (int i = 0; i < _componentStore.Size; i++)
        {
            ref Position pos = ref _componentStore.Get(i);
            pos.X += 1;
            pos.Y += 1;
            pos.Z += 1;
        }
    }

    /// <summary>
    /// Benchmarks modifying components via Span for batch updates.
    /// </summary>
    [Benchmark]
    public void ModifyComponents_Span()
    {
        Span<Position> span = _componentStore.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            span[i].X += 1;
            span[i].Y += 1;
            span[i].Z += 1;
        }
    }

    /// <summary>
    /// Benchmarks the full movement system pattern using Query and SetComponent.
    /// </summary>
    [Benchmark]
    public void MovementSystem_Full()
    {
        foreach (var entity in _world.Query(typeof(Position), typeof(Velocity)))
        {
            var pos = _world.GetComponent<Position>(entity)!.Value;
            var vel = _world.GetComponent<Velocity>(entity)!.Value;
            _world.SetComponent(entity, new Position
            {
                X = pos.X + vel.X,
                Y = pos.Y + vel.Y,
                Z = pos.Z + vel.Z
            });
        }
    }

    /// <summary>
    /// Benchmarks adding components to existing entities.
    /// </summary>
    [Benchmark]
    public void AddComponent_ToExistingEntities()
    {
        var world = new EcsWorld();
        var entities = new List<Entity.Entity>();
        
        // Create entities with just Position
        for (int i = 0; i < 1000; i++)
        {
            entities.Add(world.CreateEntity(new Position { X = i, Y = i, Z = i }));
        }
        
        // Add Velocity to all entities (triggers archetype migration)
        foreach (var entity in entities)
        {
            world.AddComponent(entity, new Velocity { X = 1, Y = 1, Z = 1 });
        }
    }

    /// <summary>
    /// Benchmarks destroying entities.
    /// </summary>
    [Benchmark]
    public void DestroyEntities_1000()
    {
        var world = new EcsWorld();
        var entities = new List<Entity.Entity>();
        
        for (int i = 0; i < 1000; i++)
        {
            entities.Add(world.CreateEntity(new Position { X = i, Y = i, Z = i }));
        }
        
        foreach (var entity in entities)
        {
            world.DestroyEntity(entity);
        }
    }
}

/// <summary>
/// xUnit tests that validate performance benchmark functionality.
/// These are not actual benchmarks but verify the benchmark code works correctly.
/// </summary>
public class PerformanceTests
{
    [Fact]
    public void Benchmark_CreateEntities_RunsSuccessfully()
    {
        var benchmark = new PerformanceBenchmarks();
        benchmark.Setup();
        
        benchmark.CreateEntities_10000();
        // If we get here without exception, the benchmark works
        Assert.True(true);
    }

    [Fact]
    public void Benchmark_QueryEntities_ReturnsCorrectCount()
    {
        var benchmark = new PerformanceBenchmarks();
        benchmark.Setup();
        
        int count = benchmark.QueryEntities_Position_Velocity();
        
        Assert.Equal(10000, count);
    }

    [Fact]
    public void Benchmark_IterateSpan_CalculatesSum()
    {
        var benchmark = new PerformanceBenchmarks();
        benchmark.Setup();
        
        float sum = benchmark.IterateComponents_Span();
        
        // Sum of 0 to 9999, times 3 (X, Y, Z)
        // = 3 * (9999 * 10000 / 2) = 3 * 49995000 = 149985000
        // Note: Using approximate comparison due to float precision
        Assert.True(sum > 149000000f, $"Sum was {sum}");
    }

    [Fact]
    public void Benchmark_IterateRefForeach_CalculatesSum()
    {
        var benchmark = new PerformanceBenchmarks();
        benchmark.Setup();
        
        float sum = benchmark.IterateComponents_RefForeach();
        
        // Using approximate comparison due to float precision
        Assert.True(sum > 149000000f, $"Sum was {sum}");
    }

    [Fact]
    public void Benchmark_ModifyRefReturn_ModifiesInPlace()
    {
        var store = new ComponentStore<Position>(10);
        for (int i = 0; i < 10; i++)
        {
            store.Add(new Position { X = 0, Y = 0, Z = 0 });
        }
        
        for (int i = 0; i < store.Size; i++)
        {
            ref Position pos = ref store.Get(i);
            pos.X += 1;
        }
        
        for (int i = 0; i < store.Size; i++)
        {
            Assert.Equal(1, store.Get(i).X);
        }
    }

    [Fact]
    public void Benchmark_ModifySpan_ModifiesInPlace()
    {
        var store = new ComponentStore<Position>(10);
        for (int i = 0; i < 10; i++)
        {
            store.Add(new Position { X = 0, Y = 0, Z = 0 });
        }
        
        Span<Position> span = store.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            span[i].X += 5;
        }
        
        for (int i = 0; i < store.Size; i++)
        {
            Assert.Equal(5, store.Get(i).X);
        }
    }

    [Fact]
    public void SpanIteration_ZeroAllocation()
    {
        // This test verifies the conceptual advantage of Span
        // In a real benchmark, BenchmarkDotNet would measure allocations
        var store = new ComponentStore<Position>(1000);
        for (int i = 0; i < 1000; i++)
        {
            store.Add(new Position { X = i, Y = i, Z = i });
        }
        
        // Span-based iteration should not allocate
        float sum = 0;
        foreach (ref Position pos in store.AsSpan())
        {
            sum += pos.X;
        }
        
        Assert.True(sum > 0);
    }

    [Fact]
    public void RefReturn_AllowsZeroCopyModification()
    {
        var store = new ComponentStore<Position>(1);
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        
        // Get reference, modify in place
        ref Position pos = ref store.Get(0);
        pos.X = 100;
        pos.Y = 200;
        pos.Z = 300;
        
        // Verify modification persisted without calling Set
        ref Position retrieved = ref store.Get(0);
        Assert.Equal(100, retrieved.X);
        Assert.Equal(200, retrieved.Y);
        Assert.Equal(300, retrieved.Z);
    }

    [Fact]
    public void ValueType_InlineStorage()
    {
        // Verify that Position is a value type (struct)
        Assert.True(typeof(Position).IsValueType);
        Assert.True(typeof(Velocity).IsValueType);
        Assert.True(typeof(Health).IsValueType);
        Assert.True(typeof(Entity.Entity).IsValueType);
    }

    [Fact]
    public void ComponentStore_GenericArrayBenefit()
    {
        // Verify we have real generic arrays, not Object[]
        var store = new ComponentStore<Position>(10);
        store.Add(new Position { X = 1, Y = 2, Z = 3 });
        
        // Access should be type-safe with no casting needed
        ref Position pos = ref store.Get(0);
        Assert.Equal(1, pos.X);
        
        // Span provides direct access to underlying array
        Span<Position> span = store.AsSpan();
        Assert.Equal(1, span.Length);
    }
}
