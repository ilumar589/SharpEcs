namespace SharpEcs.World;

/// <summary>
/// Non-generic interface for component stores to enable type-agnostic operations.
/// </summary>
/// <remarks>
/// This interface allows Archetype to perform operations on component stores
/// without reflection, improving performance in hot paths.
/// </remarks>
internal interface IComponentStore
{
    /// <summary>
    /// Gets the current number of components in the store.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Adds a component to the store and returns its index.
    /// </summary>
    /// <param name="component">The component to add (boxed).</param>
    /// <returns>The index at which the component was stored.</returns>
    int AddBoxed(object component);

    /// <summary>
    /// Gets a component from the store as a boxed object.
    /// </summary>
    /// <param name="index">The index of the component.</param>
    /// <returns>The component at the specified index (boxed).</returns>
    object GetBoxed(int index);

    /// <summary>
    /// Sets the component at the specified index using a boxed value.
    /// </summary>
    /// <param name="index">The index at which to set the component.</param>
    /// <param name="component">The component value (boxed).</param>
    void SetBoxed(int index, object component);

    /// <summary>
    /// Removes the component at the specified index using swap-and-pop.
    /// </summary>
    /// <param name="index">The index of the component to remove.</param>
    /// <returns>The component that was moved to fill the gap (boxed).</returns>
    object RemoveSwapPopBoxed(int index);

    /// <summary>
    /// Clears all components from the store.
    /// </summary>
    void Clear();
}
