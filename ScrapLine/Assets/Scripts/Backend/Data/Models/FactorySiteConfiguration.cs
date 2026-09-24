using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One configurable factory site. Sites are planned capacity, not owned state: a site listed here
/// only becomes a real factory when a <see cref="GridData"/> carrying its ID exists in the save.
/// </summary>
[Serializable]
public sealed class FactorySiteDef
{
    public string id;
    public string displayName;
    public bool unlockedByDefault;
}

/// <summary>
/// Data-driven limits for the multi-site factory plan: how many sites may ever exist, the size a new
/// site starts at, and the largest size a site may be expanded to.
///
/// These are deliberately configuration rather than constants so the pacing pass can retune the plan
/// without a code change or a save migration. Every value has a built-in default, so the save system
/// (which runs in tests and before <see cref="Resources"/> have loaded) never depends on load order.
/// </summary>
[Serializable]
public sealed class FactorySiteSettings
{
    public int maxSiteCount = 4;
    public int initialWidth = 5;
    public int initialHeight = 7;
    public int maxWidth = 8;
    public int maxHeight = 10;
    public List<FactorySiteDef> sites = new List<FactorySiteDef>();

    /// <summary>Clamps hand-edited configuration into a usable shape rather than throwing.</summary>
    public void Normalize()
    {
        initialWidth = Mathf.Max(1, initialWidth);
        initialHeight = Mathf.Max(1, initialHeight);
        maxWidth = Mathf.Max(initialWidth, maxWidth);
        maxHeight = Mathf.Max(initialHeight, maxHeight);
        maxSiteCount = Mathf.Max(1, maxSiteCount);

        sites ??= new List<FactorySiteDef>();

        // Drop blanks, and duplicate IDs (which would make the default-factory lookup ambiguous),
        // keeping the first occurrence so configuration order stays meaningful.
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<FactorySiteDef> unique = new List<FactorySiteDef>(sites.Count);
        foreach (FactorySiteDef site in sites)
        {
            if (site == null || string.IsNullOrWhiteSpace(site.id))
                continue;
            if (seen.Add(site.id))
                unique.Add(site);
        }
        sites = unique;

        if (sites.Count == 0)
            sites.Add(new FactorySiteDef { id = FactorySiteConfiguration.FallbackSiteId, displayName = "Salvage Yard", unlockedByDefault = true });

        // The plan may not describe more sites than it allows.
        if (sites.Count > maxSiteCount)
            sites.RemoveRange(maxSiteCount, sites.Count - maxSiteCount);

        foreach (FactorySiteDef site in sites)
        {
            if (string.IsNullOrWhiteSpace(site.displayName))
                site.displayName = site.id;
        }
        sites[0].unlockedByDefault = true;
    }
}

/// <summary>
/// Global access to the active <see cref="FactorySiteSettings"/>. Defaults are always valid, so
/// callers never have to null-check or wait for content loading.
/// </summary>
public static class FactorySiteConfiguration
{
    /// <summary>Resource name (without extension) of the site plan.</summary>
    public const string ResourceName = "factorysites";

    /// <summary>
    /// Site ID given to the single pre-multi-site factory. Existing saves adopt this ID during the
    /// schema 3 to 4 migration, so it must never change.
    /// </summary>
    public const string DefaultFactoryId = "factory_primary";

    /// <summary>Used only when configuration supplies no sites at all.</summary>
    internal const string FallbackSiteId = DefaultFactoryId;

    private static FactorySiteSettings current;

    public static FactorySiteSettings Current
    {
        get
        {
            if (current == null)
                current = CreateDefault();
            return current;
        }
    }

    public static int MaxSiteCount => Current.maxSiteCount;
    public static int InitialWidth => Current.initialWidth;
    public static int InitialHeight => Current.initialHeight;
    public static int MaxWidth => Current.maxWidth;
    public static int MaxHeight => Current.maxHeight;

    /// <summary>The site ID for a given zero-based index, or null when the plan has no such site.</summary>
    public static string SiteIdAt(int siteIndex)
    {
        List<FactorySiteDef> sites = Current.sites;
        return siteIndex >= 0 && siteIndex < sites.Count ? sites[siteIndex].id : null;
    }

    public static FactorySiteDef FindSite(string siteId)
    {
        if (string.IsNullOrWhiteSpace(siteId))
            return null;
        foreach (FactorySiteDef site in Current.sites)
        {
            if (string.Equals(site.id, siteId, StringComparison.Ordinal))
                return site;
        }
        return null;
    }

    /// <summary>
    /// Whether a site may occupy the given dimensions. Expansion pricing and enforcement live with
    /// the expansion system; this only states the planned ceiling.
    /// </summary>
    public static bool IsWithinSiteLimits(int width, int height)
    {
        return width >= 1 && height >= 1 && width <= MaxWidth && height <= MaxHeight;
    }

    public static void LoadFromJson(string json)
    {
        FactorySiteSettings parsed = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                parsed = JsonUtility.FromJson<FactorySiteSettings>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"factorysites.json could not be parsed ({exception.Message}); using defaults.");
                parsed = null;
            }
        }

        current = parsed ?? CreateDefault();
        current.Normalize();
    }

    /// <summary>Restores built-in defaults. Used by tests and by a clean editor session.</summary>
    public static void ResetToDefaults()
    {
        current = CreateDefault();
    }

    private static FactorySiteSettings CreateDefault()
    {
        FactorySiteSettings settings = new FactorySiteSettings
        {
            sites = new List<FactorySiteDef>
            {
                new FactorySiteDef { id = DefaultFactoryId, displayName = "Salvage Yard", unlockedByDefault = true }
            }
        };
        settings.Normalize();
        return settings;
    }
}
