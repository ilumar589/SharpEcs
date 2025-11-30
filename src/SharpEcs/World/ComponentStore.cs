namespace SharpEcs.World;

/// <summary>
/// Type-safe, array-based storage for components using real generic arrays.
/// Unlike Java's Object[], C# allows true T[] storage with value type support.
/// </summary>
/// <typeparam name="T">The component type, must be a struct for value type semantics.</typeparam>
/// <remarks>
/// <para>
/// ComponentStore provides high-performance storage for ECS components using
/// C#'s true generic arrays. This eliminates the boxing/unboxing overhead
/// present in Java's type-erased generics.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Real generic arrays (T[] instead of Object[])</description></item>
/// <item><description>Zero-copy ref returns via <see cref="Get(int)"/></description></item>
/// <item><description>High-performance iteration via <see cref="AsSpan()"/></description></item>
/// <item><description>Automatic capacity growth with configurable growth factor</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new ComponentStore&lt;Position&gt;(initialCapacity: 100);
/// 
/// // Add components
/// int index = store.Add(new Position { X = 10, Y = 20, Z = 0 });
/// 
/// // Zero-copy access via ref return
/// ref Position pos = ref store.Get(index);
/// pos.X = 15; // Modifies in-place, no copy
/// 
/// // High-performance iteration via Span
/// foreach (ref Position p in store.AsSpan())
/// {
///     p.Y += 1.0f; // Update all positions in-place
/// }
/// </code>
/// </example>
public sealed class ComponentStore<T> where T : struct
{
    /// <summary>
    /// The default initial capacity for new component stores.
    /// </summary>
    private const int DefaultInitialCapacity = 16;

    /// <summary>
    /// The growth factor used when resizing the internal array.
    /// </summary>
    private const double GrowthFactor = 1.5;

    /// <summary>
    /// The internal array storing components.
    /// </summary>
    private T[] _data;

    /// <summary>
    /// The current number of components in the store.
    /// </summary>
    private int _size;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentStore{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the store. Defaults to 16.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when initialCapacity is less than 1.</exception>
    public ComponentStore(int initialCapacity = DefaultInitialCapacity)
    {
        if (initialCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be at least 1.");
        }

        _data = new T[initialCapacity];
        _size = 0;
    }

    /// <summary>
    /// Gets the current number of components in the store.
    /// </summary>
    public int Size => _size;

    /// <summary>
    /// Gets the current capacity of the internal array.
    /// </summary>
    public int Capacity => _data.Length;

    /// <summary>
    /// Adds a component to the store and returns its index.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <returns>The index at which the component was stored.</returns>
    /// <remarks>
    /// If the store is at capacity, the internal array is grown by the growth factor.
    /// </remarks>
    public int Add(T component)
    {
        EnsureCapacity(_size + 1);
        _data[_size] = component;
        return _size++;
    }

    /// <summary>
    /// Gets a reference to the component at the specified index for zero-copy access.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>A reference to the component at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    /// <remarks>
    /// This method returns a reference to the component, allowing in-place modification
    /// without copying. This is a key performance advantage over Java's type-erased generics.
    /// </remarks>
    public ref T Get(int index)
    {
        if (index < 0 || index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Size: {_size}");
        }

        return ref _data[index];
    }

    /// <summary>
    /// Sets the component at the specified index.
    /// </summary>
    /// <param name="index">The index at which to set the component.</param>
    /// <param name="component">The component value to set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public void Set(int index, T component)
    {
        if (index < 0 || index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Size: {_size}");
        }

        _data[index] = component;
    }

    /// <summary>
    /// Removes the component at the specified index using the swap-and-pop technique.
    /// </summary>
    /// <param name="index">The index of the component to remove.</param>
    /// <returns>The component that was moved to fill the gap (the former last element), 
    /// or default if removing the last element.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    /// <remarks>
    /// This method uses the swap-and-pop technique for O(1) removal:
    /// the element at the specified index is replaced with the last element,
    /// and the size is decremented. This maintains a contiguous array but
    /// does not preserve element order.
    /// </remarks>
    public T RemoveSwapPop(int index)
    {
        if (index < 0 || index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Size: {_size}");
        }

        T movedComponent = default;
        int lastIndex = _size - 1;

        if (index != lastIndex)
        {
            movedComponent = _data[lastIndex];
            _data[index] = movedComponent;
        }

        _data[lastIndex] = default;
        _size--;

        return movedComponent;
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> over the stored components for high-performance iteration.
    /// </summary>
    /// <returns>A span containing all stored components.</returns>
    /// <remarks>
    /// Using Span enables bounds check elimination and SIMD optimization by the JIT compiler.
    /// The returned span is valid only while the component store is not modified.
    /// </remarks>
    public Span<T> AsSpan()
    {
        return _data.AsSpan(0, _size);
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> over the stored components for safe read-only iteration.
    /// </summary>
    /// <returns>A read-only span containing all stored components.</returns>
    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        return _data.AsSpan(0, _size);
    }

    /// <summary>
    /// Clears all components from the store.
    /// </summary>
    /// <remarks>
    /// This method resets the size to zero but does not deallocate the internal array.
    /// </remarks>
    public void Clear()
    {
        Array.Clear(_data, 0, _size);
        _size = 0;
    }

    /// <summary>
    /// Ensures the internal array has at least the specified capacity.
    /// </summary>
    /// <param name="requiredCapacity">The minimum required capacity.</param>
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _data.Length)
        {
            return;
        }

        int newCapacity = Math.Max(requiredCapacity, (int)(_data.Length * GrowthFactor));
        Array.Resize(ref _data, newCapacity);
    }
}
