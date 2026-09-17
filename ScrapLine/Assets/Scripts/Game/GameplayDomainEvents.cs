using System;

public sealed class GameplayDomainEvent
{
    public string EventId { get; set; }
    public string Type { get; set; }
    public string TargetId { get; set; }
    public int Count { get; set; } = 1;
    public int Value { get; set; }
    public string Source { get; set; }
    public int PriorWidth { get; set; }
    public int PriorHeight { get; set; }
    public int NewWidth { get; set; }
    public int NewHeight { get; set; }
    public int PaidCost { get; set; }
}

/// <summary>
/// Small domain-event bridge for progression, tutorial, analytics, and notifications.
/// Publishers report completed domain actions; this bridge never authorizes or gates them.
/// </summary>
public static class GameplayDomainEvents
{
    public static event Action<GameplayDomainEvent> EventPublished;

    public static void Publish(GameplayDomainEvent gameplayEvent)
    {
        if (gameplayEvent == null || string.IsNullOrWhiteSpace(gameplayEvent.EventId))
            return;

        Delegate[] subscribers = EventPublished?.GetInvocationList();
        if (subscribers == null)
            return;
        foreach (Action<GameplayDomainEvent> subscriber in subscribers)
        {
            try
            {
                subscriber(gameplayEvent);
            }
            catch (Exception exception)
            {
                // Advisory listeners must never break the authoritative gameplay action that emitted the event.
                GameLogger.LogError(LoggingManager.LogCategory.Debug,
                    $"Gameplay event listener failed for '{gameplayEvent.EventId}': {exception.Message}");
            }
        }
    }

    public static void PublishItemSold(string itemInstanceId, string itemId, int count, int value, string source)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = $"item-sold:{itemInstanceId}",
            Type = ObjectiveTypes.ItemSoldCount,
            TargetId = itemId,
            Count = count,
            Value = value,
            Source = source
        });
    }

    public static void PublishMachinePlaced(string eventId, string machineId)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = eventId,
            Type = ObjectiveTypes.MachinePlaced,
            TargetId = machineId,
            Count = 1,
            Source = "credit_placement"
        });
    }

    public static void PublishMachineLicense(string machineId, string unlockSource)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = $"machine-license:{machineId}",
            Type = ObjectiveTypes.MachineLicense,
            TargetId = machineId,
            Count = 1,
            Source = unlockSource
        });
    }

    public static void PublishRecipeCompleted(string operationId, string recipeId, int outputCount)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = $"recipe-completed:{operationId}",
            Type = ObjectiveTypes.RecipeCompleted,
            TargetId = recipeId,
            Count = 1,
            Value = Math.Max(0, outputCount),
            Source = "machine_processing"
        });
    }

    public static void PublishScrapDelivery(string eventId, string crateId, string source)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = eventId,
            Type = ObjectiveTypes.ScrapDelivery,
            TargetId = crateId,
            Count = 1,
            Source = source
        });
    }

    public static void PublishGridExpanded(
        string eventId,
        string expansionType,
        int priorWidth,
        int priorHeight,
        int newWidth,
        int newHeight,
        int paidCost)
    {
        Publish(new GameplayDomainEvent
        {
            EventId = eventId,
            Type = ObjectiveTypes.GridExpanded,
            TargetId = expansionType,
            Count = 1,
            Value = paidCost,
            Source = "credit_purchase",
            PriorWidth = priorWidth,
            PriorHeight = priorHeight,
            NewWidth = newWidth,
            NewHeight = newHeight,
            PaidCost = paidCost
        });
    }
}
