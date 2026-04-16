using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VintageTweaks.src.client;

internal static class ClientInventoryInput
{
    public static bool IsShiftDown(ICoreClientAPI capi)
    {
        return capi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft]
            || capi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
    }

    public static bool IsLeftMouseDown(ICoreClientAPI capi)
    {
        return capi.Input.MouseButton.Left;
    }

    public static bool IsMouseSlotEmpty(ICoreClientAPI capi)
    {
        ItemSlot mouseSlot = capi.World.Player.InventoryManager.MouseItemSlot;
        return mouseSlot == null || mouseSlot.Empty;
    }

    public static bool TryGetHoveredSlot(
        ICoreClientAPI capi,
        int backpackSlots,
        out ItemSlot hoveredSlot,
        out InventoryBase inventory,
        out int slotId
    )
    {
        hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
        inventory = hoveredSlot?.Inventory;
        slotId = inventory?.GetSlotId(hoveredSlot) ?? -1;

        return inventory != null
            && slotId >= 0
            && inventory.ClassName != GlobalConstants.mousecursorInvClassName
            && (inventory.ClassName != GlobalConstants.backpackInvClassName || slotId >= backpackSlots);
    }

    public static string GetSlotKey(InventoryBase inventory, int slotId)
    {
        return $"{inventory.InventoryID}:{slotId}";
    }
}
