using UnityEngine;

/// <summary>
/// Handles seller machine behavior. Sellers remove items from the game
/// and award credits to the player based on the item's sell value.
/// </summary>
public class SellerMachine : BaseMachine
{
    /// <summary>
    /// Get the component ID for logging purposes
    /// </summary>
    protected string ComponentId => $"Seller_{cellData.x}_{cellData.y}";
    
    public SellerMachine(CellData cellData, MachineDef machineDef) : base(cellData, machineDef)
    {
    }
    
    /// <summary>
    /// Update logic for seller - processes any items that arrive
    /// </summary>
    public override void UpdateLogic()
    {
        // Sellers immediately process any items that arrive
        // No waiting queue needed - items are sold immediately
        for (int i = cellData.items.Count - 1; i >= 0; i--)
        {
            ItemData item = cellData.items[i];
            if (item.state == ItemState.Idle)
            {
                SellItem(item);
            }
        }
    }
    
    /// <summary>
    /// Sells an item and awards credits to the player
    /// </summary>
    private void SellItem(ItemData item)
    {
        // Get item definition to determine sell value
        ItemDef itemDef = FactoryRegistry.Instance.GetItem(item.itemType);
        int sellValue = itemDef?.sellValue ?? 0;
        
        // Award credits to player
        if (sellValue > 0)
        {
            GameManager.Instance.AddCredits(sellValue);
        }
        
        // Remove item from cell
        cellData.items.Remove(item);
        
        // Destroy visual representation
        UIGridManager gridManager = UnityEngine.Object.FindAnyObjectByType<UIGridManager>();
        if (gridManager != null)
        {
            gridManager.DestroyVisualItem(item.id);
        }
    }
    
    /// <summary>
    /// Handles items arriving at the seller - sells them immediately
    /// </summary>
    public override void OnItemArrived(ItemData item)
    {
        // Sellers process items immediately when they arrive
        SellItem(item);
    }
    
    /// <summary>
    /// Processes an item by selling it
    /// </summary>
    public override void ProcessItem(ItemData item)
    {
        SellItem(item);
    }
}