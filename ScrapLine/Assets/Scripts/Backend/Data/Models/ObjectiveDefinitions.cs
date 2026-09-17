using System;
using System.Collections.Generic;

[Serializable]
public sealed class ObjectiveDefinitionList
{
    public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();
}

[Serializable]
public sealed class ObjectiveDefinition
{
    public string id;
    public string title;
    public string description;
    public string type;
    public string targetId;
    public int targetValue;
    public ObjectiveRewardDefinition reward = new ObjectiveRewardDefinition();
    public string prerequisiteObjectiveId;
    public string recommendedNextCapability;
    public ObjectivePresentationMetadata presentation = new ObjectivePresentationMetadata();
}

[Serializable]
public sealed class ObjectiveRewardDefinition
{
    public string type;
    public int amount;
    public string targetId;
}

[Serializable]
public sealed class ObjectivePresentationMetadata
{
    public int sortOrder;
    public string accent;
}

public static class ObjectiveTypes
{
    public const string ItemSoldCount = "item_sold_count";
    public const string ItemSoldValue = "item_sold_value";
    public const string MachinePlaced = "machine_placed";
    public const string MachineLicense = "machine_license";
    public const string RecipeCompleted = "recipe_completed";
    public const string ScrapDelivery = "scrap_delivery_ordered";
    public const string GridExpanded = "grid_expanded";

    public static readonly HashSet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        ItemSoldCount,
        ItemSoldValue,
        MachinePlaced,
        MachineLicense,
        RecipeCompleted,
        ScrapDelivery,
        GridExpanded
    };
}

public static class ObjectiveRewardTypes
{
    public const string Credits = "credits";
    public const string MachineLicense = "machine_license";
}
