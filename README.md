# SharpEcs

High-performance C# ECS (Entity-Component-System) library with value types, ref returns, and zero-allocation design.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

SharpEcs is a C# port of [JEcs](https://github.com/ilumar589/JEcs) with significant performance improvements leveraging C#-specific features. While the Java version uses `Object[]` arrays due to type erasure limitations and awaits Project Valhalla for value type support, SharpEcs takes advantage of C#'s native capabilities for maximum performance.

## Why C# Instead of Java?

| Feature | Java (JEcs) | C# (SharpEcs) |
|---------|-------------|---------------|
| **Generic Arrays** | `Object[]` (type erasure) | `T[]` (real generics) |
| **Value Types** | Waiting for Valhalla | Structs available now |
| **Zero-Copy Access** | Not possible | `ref` returns |
| **High-Performance Iteration** | Manual optimization | `Span<T>` |
| **SIMD Support** | Limited | `System.Numerics` |
| **Memory Layout** | Object headers overhead | Inline storage |

## Key Features

- **Real Generic Arrays**: `T[]` instead of `Object[]` - no casting, full type safety
- **Value Types**: Components stored inline without object headers
- **Ref Returns**: `ref T Get(int index)` for zero-copy access
- **Span<T>**: High-performance iteration with bounds check elimination
- **Archetype-Based Storage**: Cache-friendly, contiguous memory layout
- **Entity Recycling**: Generation-based entity validation for safe references
- **Zero-Allocation Design**: Minimal GC pressure in hot paths

## Quick Start

### Installation

Add the SharpEcs project to your solution or reference the NuGet package (coming soon).

### Basic Usage

```csharp
using SharpEcs.World;
using SharpEcs.Component;

// Create the world
var world = new EcsWorld();

// Create entities with components
var entity = world.CreateEntity(
    new Position { X = 0, Y = 0, Z = 0 },
    new Velocity { X = 1, Y = 0.5f, Z = 0 }
);

// Query entities with specific components
foreach (var e in world.Query(typeof(Position), typeof(Velocity)))
{
    var pos = world.GetComponent<Position>(e)!.Value;
    var vel = world.GetComponent<Velocity>(e)!.Value;
    
    // Update position
    world.SetComponent(e, new Position 
    { 
        X = pos.X + vel.X, 
        Y = pos.Y + vel.Y, 
        Z = pos.Z + vel.Z 
    });
}

// Destroy entity when done
world.DestroyEntity(entity);
```

### High-Performance Component Access

```csharp
// Get component store for direct access
var archetype = world.GetArchetype(entity);
var positionStore = archetype?.GetComponentStore<Position>();

if (positionStore != null)
{
    // Zero-copy ref return for in-place modification
    ref Position pos = ref positionStore.Get(0);
    pos.X += 10; // Modifies in place, no copy
    
    // High-performance Span iteration
    foreach (ref Position p in positionStore.AsSpan())
    {
        p.Y += 1.0f; // Update all positions in-place
    }
}
```

### Component Management

```csharp
// Add components dynamically
world.AddComponent(entity, new Health { Current = 100, Maximum = 100 });

// Check for components
if (world.HasComponent<Health>(entity))
{
    var health = world.GetComponent<Health>(entity)!.Value;
    Console.WriteLine($"Health: {health.Current}/{health.Maximum}");
}

// Remove components
world.RemoveComponent<Health>(entity);
```

## Architecture

### Entity

A lightweight value type (struct) that uniquely identifies an entity:

```csharp
public readonly struct Entity : IEquatable<Entity>
{
    public int Id { get; }
    public int Generation { get; }
}
```

The `Generation` field enables safe entity references - when an entity is destroyed and its ID recycled, the new entity will have an incremented generation, allowing detection of stale references.

### ComponentStore<T>

Type-safe, array-based storage using real generic arrays:

```csharp
var store = new ComponentStore<Position>(initialCapacity: 100);

// Add component
int index = store.Add(new Position { X = 10, Y = 20, Z = 0 });

// Zero-copy access via ref return
ref Position pos = ref store.Get(index);
pos.X = 15; // Modifies in-place

// High-performance iteration via Span
foreach (ref Position p in store.AsSpan())
{
    p.Y += deltaTime;
}
```

### Archetype

Groups entities with identical component signatures for cache-friendly storage:

```csharp
// Entities with same components share an archetype
var entity1 = world.CreateEntity(new Position(), new Velocity()); // Archetype A
var entity2 = world.CreateEntity(new Position(), new Velocity()); // Same Archetype A
var entity3 = world.CreateEntity(new Position());                 // Archetype B
```

### EcsWorld

Central coordinator managing entities, components, and archetypes:

```csharp
var world = new EcsWorld();

// Entity lifecycle
var entity = world.CreateEntity(components...);
world.DestroyEntity(entity);

// Component operations
world.GetComponent<T>(entity);
world.SetComponent(entity, component);
world.AddComponent(entity, component);
world.RemoveComponent<T>(entity);

// Queries
var entities = world.Query(typeof(Position), typeof(Velocity));
world.ForEach(action, typeof(Position));
```

## Performance Comparison

### Benchmark Results (10,000 entities)

| Operation | Description |
|-----------|-------------|
| **Entity Creation** | Archetype-based allocation with component stores |
| **Query** | O(archetypes) scan with O(1) entity collection access |
| **Span Iteration** | Bounds check elimination, SIMD-friendly |
| **Ref Return** | Zero-copy component access |

### Memory Characteristics

- **Value Types**: Components stored inline (no object headers)
- **Contiguous Arrays**: Cache-friendly memory layout
- **Span<T>**: Stack-only, no allocations
- **Entity Recycling**: ID reuse with generation tracking

## Example Components

```csharp
// All components are structs for value type benefits
public struct Position
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public struct Velocity
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public struct Health
{
    public int Current { get; set; }
    public int Maximum { get; set; }
    
    public bool IsAlive => Current > 0;
    public float GetPercentage() => Maximum > 0 ? (float)Current / Maximum : 0f;
}
```

## Common ECS Patterns

### Movement System

```csharp
public void UpdateMovement(EcsWorld world, float deltaTime)
{
    foreach (var entity in world.Query(typeof(Position), typeof(Velocity)))
    {
        var pos = world.GetComponent<Position>(entity)!.Value;
        var vel = world.GetComponent<Velocity>(entity)!.Value;
        
        world.SetComponent(entity, new Position
        {
            X = pos.X + vel.X * deltaTime,
            Y = pos.Y + vel.Y * deltaTime,
            Z = pos.Z + vel.Z * deltaTime
        });
    }
}
```

### Damage System

```csharp
public void ApplyDamage(EcsWorld world, Entity entity, int damage)
{
    if (!world.HasComponent<Health>(entity)) return;
    
    var health = world.GetComponent<Health>(entity)!.Value;
    world.SetComponent(entity, new Health
    {
        Current = Math.Max(0, health.Current - damage),
        Maximum = health.Maximum
    });
}
```

### Entity Cleanup

```csharp
public void CleanupDeadEntities(EcsWorld world)
{
    var toDestroy = new List<Entity>();
    
    foreach (var entity in world.Query(typeof(Health)))
    {
        var health = world.GetComponent<Health>(entity)!.Value;
        if (!health.IsAlive)
        {
            toDestroy.Add(entity);
        }
    }
    
    foreach (var entity in toDestroy)
    {
        world.DestroyEntity(entity);
    }
}
```

## Migration from JEcs

If you're migrating from the Java implementation:

1. **Components**: Convert Java classes to C# structs
2. **Generic Arrays**: No changes needed - C# handles this automatically
3. **Iteration**: Use `Span<T>` for high-performance loops
4. **Component Access**: Use `ref` returns for zero-copy modification
5. **Entity References**: Same generation-based validation pattern

## Project Structure

```
SharpEcs/
├── src/
│   └── SharpEcs/
│       ├── SharpEcs.csproj
│       ├── Entity/
│       │   └── Entity.cs
│       ├── Component/
│       │   ├── Position.cs
│       │   ├── Velocity.cs
│       │   └── Health.cs
│       └── World/
│           ├── ComponentStore.cs
│           ├── Archetype.cs
│           ├── ArchetypeKey.cs
│           └── EcsWorld.cs
├── tests/
│   └── SharpEcs.Tests/
│       ├── SharpEcs.Tests.csproj
│       ├── ComponentStoreTests.cs
│       ├── ArchetypeTests.cs
│       ├── EcsWorldTests.cs
│       └── PerformanceTests.cs
├── SharpEcs.sln
├── LICENSE
└── README.md
```

## Building

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run benchmarks (Release mode recommended)
dotnet run -c Release --project tests/SharpEcs.Tests
```

## Requirements

- .NET 10.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## References

- Original Java implementation: [JEcs](https://github.com/ilumar589/JEcs)
- [C# Value Types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-types)
- [Span<T>](https://learn.microsoft.com/en-us/dotnet/api/system.span-1)
- [ref returns](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref#ref-returns)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
