using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles scrap delivery purchases for individual spawners.
/// </summary>
public class WasteSupplyManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CreditsManager creditsManager;
    public GridManager gridManager;

    private string ComponentId => $"WasteSupplyManager_{GetEntityId()}";

    public bool PurchaseWasteCrate(string crateId, SpawnerMachine targetSpawner)
    {
        if (targetSpawner == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy,
                "A scrap order must target a spawner.", ComponentId);
            return false;
        }

        WasteCrateDef crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy,
                $"Cannot find waste crate definition for '{crateId}'.", ComponentId);
            return false;
        }

        WasteCrateQueueStatus queue = targetSpawner.GetDeliveryQueueStatus();
        if (!queue.canAddToQueue)
        {
            GameLogger.LogWarning(LoggingManager.LogCategory.Economy,
                $"Selected spawner delivery queue is full ({queue.queuedCrateIds.Count}/{queue.maxQueueSize}).",
                ComponentId);
            return false;
        }

        int crateCost = GetWasteCrateCost(crateId);
        if (creditsManager == null || !creditsManager.TrySpendCredits(crateCost))
            return false;

        if (!targetSpawner.TryEnqueueDelivery(crateId))
        {
            creditsManager.AddCredits(crateCost);
            GameLogger.LogError(LoggingManager.LogCategory.Economy,
                $"Could not add '{crateDef.displayName}' to the selected spawner; refunded {crateCost} credits.",
                ComponentId);
            return false;
        }

        GameManager.Instance?.RequestAutosave();
        GameLogger.LogEconomy(
            $"Ordered '{crateDef.displayName}' for {crateCost} credits for the selected spawner.", ComponentId);
        return true;
    }

    public bool TryDeliverStarterCrate(SpawnerMachine targetSpawner)
    {
        GameData data = GameManager.Instance?.gameData;
        if (targetSpawner == null || data == null || !data.starterDeliveryAvailable)
            return false;

        if (!targetSpawner.TryEnqueueDelivery("starter_crate"))
            return false;

        data.starterDeliveryAvailable = false;
        GameManager.Instance?.RequestAutosave();
        GameLogger.LogEconomy("Delivered the free starter Can Bale to the first spawner.", ComponentId);
        return true;
    }

    public int RefundQueuedDeliveries(CellData cellData)
    {
        if (cellData?.wasteDeliveryQueue == null || cellData.wasteDeliveryQueue.Count == 0)
            return 0;

        if (creditsManager == null)
        {
            GameLogger.LogError(LoggingManager.LogCategory.Economy,
                "Cannot refund unopened scrap deliveries without a CreditsManager.", ComponentId);
            return 0;
        }

        int refund = 0;
        foreach (string crateId in cellData.wasteDeliveryQueue)
            refund += GetWasteCrateCost(crateId);

        cellData.wasteDeliveryQueue.Clear();
        if (refund > 0)
            creditsManager.AddCredits(refund);

        GameLogger.LogEconomy($"Refunded {refund} credits for unopened scrap deliveries.", ComponentId);
        return refund;
    }

    public WasteCrateQueueStatus GetQueueStatus(SpawnerMachine targetSpawner)
    {
        return targetSpawner?.GetDeliveryQueueStatus() ?? new WasteCrateQueueStatus
        {
            queuedCrateIds = new List<string>(),
            maxQueueSize = SpawnerMachine.DeliveryQueueCapacity,
            canAddToQueue = false
        };
    }

    public List<WasteCrateDef> GetAvailableWasteCrates()
    {
        return FactoryRegistry.Instance?.GetAllWasteCrates() ?? new List<WasteCrateDef>();
    }

    public bool CanAffordWasteCrate(string crateId)
    {
        return creditsManager != null && creditsManager.CanAfford(GetWasteCrateCost(crateId));
    }

    public int GetWasteCrateCost(string crateId)
    {
        WasteCrateDef crateDef = FactoryRegistry.Instance?.GetWasteCrate(crateId);
        if (crateDef == null)
            return 0;
        return crateDef.cost > 0 ? crateDef.cost : SpawnerMachine.CalculateWasteCrateCost(crateDef);
    }
}
