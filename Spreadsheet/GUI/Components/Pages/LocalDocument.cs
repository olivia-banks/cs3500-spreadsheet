namespace GUI.Components.Pages;

using System;
using System.Globalization;

/// <summary>
///     <para>
///         Represents a spreadsheet entry persisted in browser local storage.
///     </para>
/// </summary>
public sealed record LocalDocument
{
    /// <summary>
    ///     <para>
    ///         The persisted document name.
    ///     </para>
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         The persisted save timestamp in ISO-style text.
    ///     </para>
    /// </summary>
    public string SavedAt { get; init; } = string.Empty;

    /// <summary>
    ///     <para>
    ///         A user-friendly local-time rendering of <see cref="SavedAt"/>.
    ///     </para>
    /// </summary>
    public string SavedAtText
    {
        get
        {
            if (DateTimeOffset.TryParse(SavedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
            }

            return "Unknown";
        }
    }
}
