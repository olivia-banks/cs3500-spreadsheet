namespace GUI.Components.Pages;

/// <summary>
///     <para>
///         Display model for a selectable row in file-like modal lists.
///     </para>
/// </summary>
/// <param name="Name">Primary row label.</param>
/// <param name="MetaText">Optional secondary metadata text shown on the right.</param>
public sealed record FileRowOption(string Name, string? MetaText = null);