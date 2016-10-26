using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Collections.Generic;
using System;

public class MSManufacturerWindow : BaseMachineWindow
{
    public const string InterfaceName = "steveman0.MSManufacturerWindow";
    public const string InterfaceSetItemToAssemble = "SetItemToAssemble";

    public bool dirty = false;

    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        MSManufacturer port = targetEntity as MSManufacturer;

        if (port == null || port.CurrentMachine != MSManufacturer.AttachedMachine.ResearchAssembler)
        {
            //GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        //UIUtil.UIdelay = 0;
        //UIUtil.UILock = true;

        this.manager.SetTitle("Mass Storage Manufacturer");

        this.manager.AddBigLabel("Infotext", "Select a Pod to Assemble", Color.white, 0, 0);

        int offset = 40;
        int spacing = 60;

        for (int n = 0; n < 7; n++)
        {
            //Lol code win :P
            string itemname = ItemManager.GetItemName(350 + n);
            string itemicon = ItemManager.GetItemIcon(350 + n);
            //switch (n)
            //{
            //    case 0:
            //        itemname = ItemManager.GetItemName(350);
            //        itemicon = ItemManager.GetItemIcon(350);
            //        break;
            //    case 1:
            //        itemname = ItemManager.GetItemName(351);
            //        itemicon = ItemManager.GetItemIcon(351);
            //        break;
            //    case 2:
            //        itemname = ItemManager.GetItemName(352);
            //        itemicon = ItemManager.GetItemIcon(352);
            //        break;
            //    case 3:
            //        itemname = ItemManager.GetItemName(353);
            //        itemicon = ItemManager.GetItemIcon(353);
            //        break;
            //    case 4:
            //        itemname = ItemManager.GetItemName(354);
            //        itemicon = ItemManager.GetItemIcon(354);
            //        break;
            //    case 5:
            //        itemname = ItemManager.GetItemName(355);
            //        itemicon = ItemManager.GetItemIcon(355);
            //        break;
            //    case 6:
            //        itemname = ItemManager.GetItemName(356);
            //        itemicon = ItemManager.GetItemIcon(356);
            //        break;
            //    default:
            //        itemname = "empty";
            //        break;
            //}

            this.manager.AddIcon("podicon" + n, itemicon, Color.white, 0, offset + (n * spacing));
            this.manager.AddBigLabel("podtext" + n, itemname, Color.white, 60, offset + (n * spacing));
        }

        if (port.PodType != null)
            this.dirty = true;
    }

    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        MSManufacturer port = targetEntity as MSManufacturer;

        if (port == null || port.CurrentMachine != MSManufacturer.AttachedMachine.ResearchAssembler)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        //UIUtil.UIdelay = 0;

        ItemBase item = port.PodType;

        if (item != null)
        {
            string itemname = ItemManager.GetItemName(item);
            string iconname = ItemManager.GetItemIcon(item);
            int slot = item.mnItemID - 350;
            this.manager.UpdateLabel("podtext" + slot, itemname, Color.cyan);
        }
    }

    public override bool ButtonClicked(string name, SegmentEntity targetEntity)
    {
        MSManufacturer port = targetEntity as MSManufacturer;

        if (name == "currentpod")
        {
            if (port.PodType != null)
            {
                MSManufacturerWindow.SetItemToAssemble(WorldScript.mLocalPlayer, port, null);
                this.manager.RedrawWindow();
            }
            return true;
        }
        else if (name.Contains("podicon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("podicon", ""), out slotNum); //Get slot name as number
            if (slotNum > -1)
            {
                MSManufacturerWindow.SetItemToAssemble(WorldScript.mLocalPlayer, port, ItemManager.SpawnItem(350 + slotNum));
                GenericMachinePanelScript.instance.Hide();
            }
        }
        return false;
    }

    public override void OnClose(SegmentEntity targetEntity)
    {
        base.OnClose(targetEntity);
    }

    public static bool SetItemToAssemble(Player player, MSManufacturer port, ItemBase itemtoassemble)
    {
        port.PodType = itemtoassemble;
        if (player.mbIsLocalPlayer && itemtoassemble != null)
            FloatingCombatTextManager.instance.QueueText(port.mnX, port.mnY + 1L, port.mnZ, 1f, "Assembling: " + ItemManager.GetItemName(itemtoassemble), ItemColor(itemtoassemble), 1.5f);
        else
            FloatingCombatTextManager.instance.QueueText(port.mnX, port.mnY + 1L, port.mnZ, 1f, "Assembler recipe cleared!", Color.blue, 1.5f);
        port.MarkDirtyDelayed();
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetItemToAssemble, (string)null, itemtoassemble, (SegmentEntity)port, 0.0f);
        return true;
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        MSManufacturer port = nic.target as MSManufacturer;
        string key = nic.command;
        if (key != null)
        {
            if (key == InterfaceSetItemToAssemble)
            {
                MSManufacturerWindow.SetItemToAssemble(player, port, nic.itemContext);
            }
        }
        return new NetworkInterfaceResponse()
        {
            entity = (SegmentEntity)port,
            inventory = player.mInventory
        };
    }

    private static Color ItemColor(ItemBase itemBase)
    {
        Color lCol = Color.green;
        if (itemBase.mType == ItemType.ItemCubeStack)
        {
            ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
            if (CubeHelper.IsGarbage(itemCubeStack.mCubeType))
                lCol = Color.red;
            if (CubeHelper.IsSmeltableOre(itemCubeStack.mCubeType))
                lCol = Color.green;
        }
        if (itemBase.mType == ItemType.ItemStack)
            lCol = Color.cyan;
        if (itemBase.mType == ItemType.ItemSingle)
            lCol = Color.white;
        if (itemBase.mType == ItemType.ItemCharge)
            lCol = Color.magenta;
        if (itemBase.mType == ItemType.ItemDurability)
            lCol = Color.yellow;
        if (itemBase.mType == ItemType.ItemLocation)
            lCol = Color.gray;

        return lCol;
    }
}

