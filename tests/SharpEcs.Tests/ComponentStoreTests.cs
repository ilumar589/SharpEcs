using SharpEcs.Component;
using SharpEcs.World;
using Xunit;

namespace SharpEcs.Tests;

/// <summary>
/// Unit tests for <see cref="ComponentStore{T}"/>.
/// </summary>
public class ComponentStoreTests
{
    [Fact]
    public void Add_SingleComponent_ReturnsZeroIndex()
    {
        var store = new ComponentStore<Position>();
        
        int index = store.Add(new Position { X = 1, Y = 2, Z = 3 });
        
        Assert.Equal(0, index);
        Assert.Equal(1, store.Size);
    }

    [Fact]
    public void Add_MultipleComponents_ReturnsSequentialIndices()
    {
        var store = new ComponentStore<Position>();
        
        int index0 = store.Add(new Position { X = 0, Y = 0, Z = 0 });
        int index1 = store.Add(new Position { X = 1, Y = 1, Z = 1 });
        int index2 = store.Add(new Position { X = 2, Y = 2, Z = 2 });
        
        Assert.Equal(0, index0);
        Assert.Equal(1, index1);
        Assert.Equal(2, index2);
        Assert.Equal(3, store.Size);
    }

    [Fact]
    public void Get_ValidIndex_ReturnsComponent()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 10, Y = 20, Z = 30 });
        
        ref Position pos = ref store.Get(0);
        
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
        Assert.Equal(30, pos.Z);
    }

    [Fact]
    public void Get_RefReturn_AllowsInPlaceModification()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        
        ref Position pos = ref store.Get(0);
        pos.X = 100;
        pos.Y = 200;
        
        ref Position retrieved = ref store.Get(0);
        Assert.Equal(100, retrieved.X);
        Assert.Equal(200, retrieved.Y);
    }

    [Fact]
    public void Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Get(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Get(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Get(100));
    }

    [Fact]
    public void Set_ValidIndex_UpdatesComponent()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        
        store.Set(0, new Position { X = 50, Y = 60, Z = 70 });
        
        ref Position pos = ref store.Get(0);
        Assert.Equal(50, pos.X);
        Assert.Equal(60, pos.Y);
        Assert.Equal(70, pos.Z);
    }

    [Fact]
    public void Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var store = new ComponentStore<Position>();
        
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Set(0, new Position()));
    }

    [Fact]
    public void RemoveSwapPop_LastElement_DecreasesSize()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 1, Y = 1, Z = 1 });
        store.Add(new Position { X = 2, Y = 2, Z = 2 });
        
        store.RemoveSwapPop(1);
        
        Assert.Equal(1, store.Size);
    }

    [Fact]
    public void RemoveSwapPop_MiddleElement_SwapsWithLast()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 1, Y = 1, Z = 1 });
        store.Add(new Position { X = 2, Y = 2, Z = 2 });
        store.Add(new Position { X = 3, Y = 3, Z = 3 });
        
        var moved = store.RemoveSwapPop(0);
        
        Assert.Equal(2, store.Size);
        // The last element (3,3,3) should now be at index 0
        ref Position pos = ref store.Get(0);
        Assert.Equal(3, pos.X);
        // The moved component should be the one that was at the last position
        Assert.Equal(3, moved.X);
    }

    [Fact]
    public void RemoveSwapPop_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position());
        
        Assert.Throws<ArgumentOutOfRangeException>(() => store.RemoveSwapPop(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.RemoveSwapPop(1));
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 1, Y = 1, Z = 1 });
        store.Add(new Position { X = 2, Y = 2, Z = 2 });
        store.Add(new Position { X = 3, Y = 3, Z = 3 });
        
        Span<Position> span = store.AsSpan();
        
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
        Assert.Equal(3, span[2].X);
    }

    [Fact]
    public void AsSpan_AllowsInPlaceModification()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        store.Add(new Position { X = 0, Y = 0, Z = 0 });
        
        Span<Position> span = store.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            span[i].X = i * 10;
        }
        
        Assert.Equal(0, store.Get(0).X);
        Assert.Equal(10, store.Get(1).X);
    }

    [Fact]
    public void AsReadOnlySpan_ReturnsCorrectSpan()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 5, Y = 10, Z = 15 });
        
        ReadOnlySpan<Position> span = store.AsReadOnlySpan();
        
        Assert.Equal(1, span.Length);
        Assert.Equal(5, span[0].X);
    }

    [Fact]
    public void Capacity_GrowsWhenNeeded()
    {
        var store = new ComponentStore<Position>(2);
        
        Assert.Equal(2, store.Capacity);
        
        store.Add(new Position());
        store.Add(new Position());
        store.Add(new Position()); // This should trigger growth
        
        Assert.True(store.Capacity > 2);
        Assert.Equal(3, store.Size);
    }

    [Fact]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ComponentStore<Position>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ComponentStore<Position>(-1));
    }

    [Fact]
    public void Clear_RemovesAllComponents()
    {
        var store = new ComponentStore<Position>();
        store.Add(new Position { X = 1, Y = 1, Z = 1 });
        store.Add(new Position { X = 2, Y = 2, Z = 2 });
        
        store.Clear();
        
        Assert.Equal(0, store.Size);
    }

    [Fact]
    public void Health_Component_WorksCorrectly()
    {
        var store = new ComponentStore<Health>();
        store.Add(new Health { Current = 100, Maximum = 100 });
        
        ref Health health = ref store.Get(0);
        Assert.True(health.IsAlive);
        Assert.True(health.IsFullHealth);
        Assert.Equal(1.0f, health.GetPercentage());
        
        health.Current = 50;
        Assert.Equal(0.5f, health.GetPercentage());
    }

    [Fact]
    public void Velocity_Component_WorksCorrectly()
    {
        var store = new ComponentStore<Velocity>();
        store.Add(new Velocity { X = 1.5f, Y = 2.5f, Z = 3.5f });
        
        ref Velocity vel = ref store.Get(0);
        Assert.Equal(1.5f, vel.X);
        Assert.Equal(2.5f, vel.Y);
        Assert.Equal(3.5f, vel.Z);
    }
}
