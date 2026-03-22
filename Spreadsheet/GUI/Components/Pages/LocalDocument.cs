namespace GUI.Components.Pages;

using System;
using System.Globalization;

public sealed record LocalDocument
{
    public string Name { get; init; } = string.Empty;
    public string SavedAt { get; init; } = string.Empty;

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
