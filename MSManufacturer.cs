using UnityEngine;
using System.IO;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Linq;
using UnityEngine.Rendering;

public class MSManufacturer : MachineEntity //, CommunityItemInterface
{
    public MSManufacturer.eState meState;
    public MSManufacturer.eState meUnityState;
    private GameObject DigiTransmit;
    private MassStorageCrate mAttachedCrate;
    private MassStorageCrate mSourceCrate;
    public ItemBase Exemplar;
    public ItemBase mCarriedItem;
    public float mrCarryTimer;
    public float mrCarryDistance;
    private Vector3 mForwards;
    private float mrDroneHeight;
    private float mrDroneTargetHeight;
    private bool mbHoloPreviewDirty;
    public float mrStateTimer;
    private bool mbLinkedToGO;
    private GameObject CarryDrone;
    private GameObject CarryDroneClamp;
    private GameObject Thrust_Particles;
    private GameObject mCarriedObjectItem;
    private GameObject HoloPreview;
    private GameObject HoloCubePreview;
    private GameObject NoItem;
    private GameObject OutputHopperObject;
    private Segment mDroneSegment;
    private bool mbCarriedCubeNeedsConfiguring;
    private Vector3 mUnityDronePos;
    private Vector3 mUnityDroneTarget;
    private Vector3 mUnityDroneRestPos;
    private bool mbConfiguredDroneTarget;
    public MSManufacturer.AttachedMachine CurrentMachine = AttachedMachine.None;
    public ManufacturingPlant AttachedManfacturingPlant;
    public GenericAutoCrafterNew AttachedGAC;
    public Laboratory AttachedLab;
    public ResearchAssembler AttachedAssembler;
    public CraftData mRecipe;
    public ResearchDataEntry ResearchProject;
    public int RecipeIndex = 0;
    public ItemBase[] ItemInventory;
    public MachineInventory LocalInventory;
    public MachineInventory DroneInventory;
    public MachineInventory OverflowInventory;
    public int StorageCapacity = 400;
    public List<int> OverflowList;
    private float CycleTimer = 0.0f;
    private float PopupTimer = 0.0f;
    public int PopupIndex = 0;
    private bool LocalCrateLockOut = false;
    private float CrateLockTimer = 0.0f;
    private int[] IndexedDistances;
    private static ushort MK1_Value = ModManager.mModMappings.CubesByKey["steveman0.MSManufacturer"].ValuesByKey["steveman0.MSManufacturerMK1"].Value;
    private static ushort MK2_Value = ModManager.mModMappings.CubesByKey["steveman0.MSManufacturer"].ValuesByKey["steveman0.MSManufacturerMK2"].Value;
    private static ushort MK3_Value = ModManager.mModMappings.CubesByKey["steveman0.MSManufacturer"].ValuesByKey["steveman0.MSManufacturerMK3"].Value;
    private int Tier;
    //public MSManufacturerWindow MachineWindow = new MSManufacturerWindow();
    public ItemBase PodType;


    public MSManufacturer(ModCreateSegmentEntityParameters parameters)
      : base(parameters)
    {
        this.meState = MSManufacturer.eState.Unknown;
        this.CurrentMachine = AttachedMachine.None;
        this.mbNeedsLowFrequencyUpdate = true;
        this.mbNeedsUnityUpdate = true;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(parameters.Flags) * Vector3.forward;
        this.mForwards.Normalize();
        //large enough to hold all the items to craft any single craftable list-based and no nulls recorded for serialization efficiency
        this.LocalInventory = new MachineInventory(this, 1000);
        this.OverflowInventory = new MachineInventory(this, 1000);
        ushort lValue = parameters.Value;
        if (lValue == MK1_Value)
            this.Tier = 1;
        else if (lValue == MK2_Value)
            this.Tier = 2;
        else if (lValue == MK3_Value)
            this.Tier = 3;


        switch (this.Tier)
        {
            case 1:
                this.DroneInventory = new MachineInventory(this, 5);
                break;
            case 2:
                this.DroneInventory = new MachineInventory(this, 12);
                break;
            case 3:
                this.DroneInventory = new MachineInventory(this, 25);
                break;
            default:
                Debug.LogWarning("Unrecognized tier of MSManufacturer port!");
                break;
        }
    }

    public override void SpawnGameObject()
    {
        SpawnableObjectEnum modelType = SpawnableObjectEnum.MassStorageOutputPort;
        if (this.Tier == 1)
            modelType = SpawnableObjectEnum.MassStorageOutputPort;
        if (this.Tier == 2)
            modelType = SpawnableObjectEnum.MassStorageOutputPortMK2;
        if (this.Tier == 3)
            modelType = SpawnableObjectEnum.MassStorageOutputPortMK3;
        this.mWrapper = SpawnableObjectManagerScript.instance.SpawnObject(eGameObjectWrapperType.StaticModel, modelType, this.mnX, this.mnY, this.mnZ, this.mFlags, (object)null);
    }

    public override string GetPopupText()
    {
        //if (this.CurrentMachine != AttachedMachine.ResearchAssembler)
        //    UIUtil.EscapeUI();
        //else
        //    UIUtil.HandleThisMachineWindow(this, MachineWindow);
        this.PopupTimer -= Time.deltaTime;

        string lstr1 = "";
        switch (this.Tier)
        {
            case 1: lstr1 = "Mass Storage Manufacturer Port MK1\n"; break;
            case 2: lstr1 = "Mass Storage Manufacturer Port MK2\n"; break;
            case 3: lstr1 = "Mass Storage Manufacturer Port MK3\n"; break;
        }
        
        switch (this.meState)
        {
            case MSManufacturer.eState.LookingForAttachedStorage:
                lstr1 += "Looking for Machines\n";
                break;
            case MSManufacturer.eState.SendingDrone:
                lstr1 += "Sending Drone\n";
                break;
            case MSManufacturer.eState.RetrievingDrone:
                lstr1 += "Retrieving Drone\n";
                break;
            case MSManufacturer.eState.DroppingOff:
                lstr1 += "Dropping Off\n";
                break;
            default:
                lstr1 += this.meState + "\n";
                break;
        }

        string lstr2 = "";
        string lstr3 = "";
        string lstr4 = "";

        switch (this.CurrentMachine)
        {
            case AttachedMachine.GAC:
                lstr2 = "Attached to GAC: " + this.AttachedGAC.mMachine.FriendlyName + "\n";
                break;
            case AttachedMachine.MPlant:
                lstr2 = "Attached to Manufacturing Plant\n";
                break;
            case AttachedMachine.Lab:
                lstr2 = "Attached to Laboratory\n";
                break;
            case AttachedMachine.ResearchAssembler:
                lstr2 = "Attached to Research Assembler\n";
                break;
            case AttachedMachine.None:
                lstr2 = "Looking for attached crafting machine...\n";
                break;
        }

        //else if (this.AttachedGAC != null)
        //    lstr2 = "Attached to GAC: " + this.AttachedGAC.mMachine.FriendlyName + "\n";
        //else if (this.AttachedManfacturingPlant != null)
        //    lstr2 = "Attached to Manufacturing Plant\n";
        //else if (this.AttachedLab != null)
        //    lstr2 = "Attached to Laboratory\n";

        if (this.mAttachedCrate == null)
            lstr2 += "Looking for attached mass storage crate...\n";
        else if (this.IndexedDistances == null)
            lstr2 += "Surveying mass storage crate distances...\n";

        if (this.PopupTimer < 0)
        {
            this.PopupTimer = 1.5f;
            this.PopupIndex++;
        }

        if (this.mRecipe != null)
        {
            int itemcount = 0;
            ItemBase ingredient;
            lstr3 = "Recipe: " + this.mRecipe.CraftedName + "\n";
            if (this.PopupIndex >= this.mRecipe.Costs.Count)
                this.PopupIndex = 0;
            ingredient = GetCostAsItemBase(this.mRecipe.Costs[PopupIndex]);
            if (this.LocalInventory.Inventory != null && !this.LocalInventory.IsEmpty())
                itemcount = this.LocalInventory.Inventory.GetItemCount(ingredient);
            else
                itemcount = 0;
            lstr4 = this.mRecipe.Costs[PopupIndex].Name + ": " + itemcount + "/" + this.mRecipe.Costs[PopupIndex].Amount + "\n";

            /////////////// Debug.Log
            //if (this.Exemplar != null)
            //    lstr4 += "Exemplar: " + this.Exemplar.ToString() + ", count: " + this.LocalInventory.Inventory.GetItemCount(this.Exemplar).ToString() + "\n recipe index: " + this.RecipeIndex.ToString() + " recipe cost " + this.mRecipe.Costs[RecipeIndex].Name + " cost item " + this.mRecipe.Costs[RecipeIndex].CubeValue + "\n";
            //else
            //    lstr4 += "Exemplar null " + "\n";

        }

        if (this.ResearchProject != null && this.AttachedLab.mCurrentProject != null && this.AttachedLab.mRemainingItemRequirements != null && this.AttachedLab.mRemainingItemRequirements.Count > 0)
        {
            lstr3 = "Project: " + this.AttachedLab.mCurrentProject.Name + "\n";
            if (this.PopupIndex >= this.AttachedLab.mRemainingItemRequirements.Count)
                this.PopupIndex = 0;
            ItemBase item = this.GetResearchItemCost(this.PopupIndex);
            if (item != null)
                lstr4 = "Research requires: \n" + item.ToString() + "\n";
        }
        else if (this.ResearchProject != null && this.AttachedLab.mCurrentProject != null && this.AttachedLab.mRemainingItemRequirements != null && this.AttachedLab.mRemainingItemRequirements.Count == 0)
            lstr3 = "Project: " + this.AttachedLab.mCurrentProject.Name + " Complete!\n";

        if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
        {
            if (this.PodType == null)
                lstr3 = "Press E to configure pod to manufacture\n";
            else
            {
                lstr3 = ItemManager.GetItemName(this.PodType) + "\n";
                if (this.mAttachedCrate != null && this.IndexedDistances != null)
                {
                    lstr2 = lstr2.Remove(lstr2.Length - 1, 1) + ", Assembling:\n";
                }
            }
            if (this.Exemplar != null)
                lstr4 = "Currently collecting " + ItemManager.GetItemName(this.Exemplar) + "\n" + this.LocalInventory.Inventory.GetItemCount(this.Exemplar) + " " + ItemManager.GetItemName(this.Exemplar) + " in storage\n";
        }

        //Debug stuff
        //string lstr6 = "";
        //if (this.AttachedManfacturingPlant != null)
        //    lstr6 = this.AttachedManfacturingPlant.mState.ToString() + "\n";
        //if (this.AttachedGAC != null)
        //    lstr6 += this.AttachedGAC.meState.ToString() + "\n";

        int localcount = 0;
        int dronecount = 0;
        if (this.LocalInventory.Inventory != null && !this.LocalInventory.IsEmpty())
            localcount = this.LocalInventory.ItemCount();
        if (this.DroneInventory.Inventory != null && !this.DroneInventory.IsEmpty())
            dronecount = this.DroneInventory.ItemCount();


            string lstr7 = "Local inventory: " + localcount.ToString("N0") + "\nDrone inventory: " + dronecount.ToString("N0");
        string lstr5 = string.Concat(new object[5]
            {
                lstr1,
                lstr2,
                lstr3,
                lstr4,
                lstr7
            });
        return (lstr5);
    }

    private void SendDroneToCrate(MassStorageCrate lCrate)
    {
        this.mSourceCrate = lCrate;
        if ((double)this.mSourceCrate.mrOutputLockTimer > 0.0)
            Debug.LogError((object)"Cannot send drone to create, already locked by another!");
        this.mSourceCrate.mrOutputLockTimer = 5f;
        this.mrCarryTimer = -1f;
        this.mrDroneTargetHeight = (float)((double)this.mSourceCrate.mnRequestedStackHeight - 1.0 + 0.25);
        this.SetNewState(MSManufacturer.eState.SendingDrone);
        this.mbConfiguredDroneTarget = false;
    }

    public void SetExemplar(ItemBase lItem)
    {
        this.mbHoloPreviewDirty = true;
        if (this.mSourceCrate != null && this.mSourceCrate.mrOutputLockTimer > 0.0 && this.CurrentMachine != AttachedMachine.Lab)
            Debug.LogWarning((object)"Warning, changing exemplar whilst we have a SourceCrate locked!");
        //if (this.Exemplar == null || lItem == null)
        //{
        //    this.mbHoloPreviewDirty = true;
        //    //if (lItem == null)
        //        //Debug.Log((object)"MSOP Cleared Exemplar");
        //}
        //else if (lItem.mType == ItemType.ItemCubeStack)
        //{
        //    if (this.Exemplar.mType == ItemType.ItemCubeStack)
        //    {
        //        if ((int)(lItem as ItemCubeStack).mCubeType == (int)(this.Exemplar as ItemCubeStack).mCubeType)
        //            return;
        //    }
        //}
        //else
        //{
        //    if (lItem.mnItemID == this.Exemplar.mnItemID)
        //        return;
        //}
        this.Exemplar = lItem;
        this.MarkDirtyDelayed();
    }

    private void UpdateIdling()
    {
        if (!this.DroneInventory.IsEmpty())
        {
            this.SetNewState(MSManufacturer.eState.RetrievingDrone);
        }
        else
        {
            //Check for missing machines and null appropriately
            this.CheckMissingMachines();
            //Changed to add in my code to set the examplar
            if (this.Exemplar == null || (this.mRecipe == null && (this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.Lab)))
            {
                if (this.AttachedGAC != null)
                {
                    this.mRecipe = this.AttachedGAC.mRecipe;
                    if (this.mRecipe != null)
                    {
                        this.GetRecipeIngredient();
                        this.BuildOverflowList();
                    }
                    else
                    {
                        Debug.LogWarning("MSManufacturer attached to GAC with null recipe!?");
                        this.SetExemplar(null);
                        this.BuildOverflowList();
                    }
                }
                if (this.AttachedManfacturingPlant != null)
                {
                    this.mRecipe = this.AttachedManfacturingPlant.mSelectedRecipe;
                    if (this.mRecipe != null)
                    {
                        this.GetRecipeIngredient();
                        this.BuildOverflowList();
                    }
                }
                if (this.AttachedLab != null)
                {
                    this.ResearchProject = this.AttachedLab.mCurrentProject;
                    if (this.ResearchProject != null)
                    {
                        this.GetRecipeIngredient();
                        this.BuildOverflowList();
                    }
                }
                if (this.AttachedAssembler != null && this.PodType != null)
                {
                    this.GetRecipeIngredient();
                    this.BuildOverflowList();
                }
                if (this.Exemplar == null)
                    return;
            }
            MassStorageCrate center = this.mAttachedCrate.GetCenter();
            if (center == null)
                return;
            bool founditem = false;
            //Check if we have enough of the current ingredient and get a new one if we do
            if (this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.MPlant)
            {
                if (!this.Exemplar.Compare(this.GetCostAsItemBase(this.mRecipe.Costs[this.RecipeIndex])))
                {
                    Debug.LogWarning("MSManufacturer exemplar doesn't match current recipe cost! Exemplar: " + this.Exemplar.ToString());
                    this.SetExemplar(null);
                }
                if (this.CheckIngredientCount())
                {
                    this.RecipeIndex++;
                    this.GetRecipeIngredient();
                    if (this.CheckAllIngredients())
                        this.InitiateCrafting();
                    return;
                }
            }
            else if (this.CurrentMachine == AttachedMachine.Lab)
            {
                ItemBase item;
                if (this.CheckResearchReady(out item))
                {
                    if (item != null)
                        this.InitiateResearch(item.NewInstance().SetAmount(1));
                    else
                        Debug.LogWarning("MSManufacturer tried to initiate research with a null item!?");
                    return;
                }
            }
            else if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
            {
                if (this.CheckIngredientCount())
                {
                    this.RecipeIndex++;
                    this.GetRecipeIngredient();
                    if (this.CheckAssemblerReady())
                        this.InitiateAssembling();
                    return;
                }
                else if (this.PodType == null)
                    return;

            }
            if ((this.CurrentMachine == AttachedMachine.MPlant || this.CurrentMachine == AttachedMachine.GAC) && this.CheckIngredientCount())
            {
                Debug.LogWarning("MSManufacturer tried to search for crate with full ingredient: " + this.Exemplar.ToString() + " for recipe: " + this.mRecipe.ToString() + " cycle timer is: " + this.CycleTimer.ToString() + " Stack trace: " + System.Environment.StackTrace);
                return;
            }
            int count = this.mAttachedCrate.GetCenter().mConnectedCrates.Count;
            int crateindex;
            for (int index = 0; index < count + 1; ++index)
            {
                try
                {
                    crateindex = IndexedDistances[index];
                }
                catch (System.IndexOutOfRangeException)
                {
                    this.SetNewState(eState.Idling);
                    IndexedDistances = null;
                    return;
                }
                if (crateindex == count)
                {
                    if (center.mrOutputLockTimer <= 0.0 && center.GetItemSlot(this.Exemplar) != -1)
                    {
                        this.SendDroneToCrate(this.mAttachedCrate.GetCenter());
                        founditem = true;
                        break;
                    }
                }
                else
                {
                    if (center.mConnectedCrates[crateindex].mrOutputLockTimer <= 0.0 && center.mConnectedCrates[crateindex].GetItemSlot(this.Exemplar) != -1)
                    {
                        this.SendDroneToCrate(this.mAttachedCrate.GetCenter().mConnectedCrates[crateindex]);
                        founditem = true;
                        break;
                    }
                }

            }
            //if the item isn't found check for another item in the recipe
            if (!founditem)
            {
                this.CycleTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
                if (this.CycleTimer < 0)
                {
                    this.CycleTimer = 3.0f;
                    if ((this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.MPlant) && this.mRecipe == null)
                        return;
                    this.RecipeIndex++;
                    this.GetRecipeIngredient();
                }
            }
        }
    }

    public override void LowFrequencyUpdate()
    {
        if (GameState.PlayerSpawnedAndHadUpdates)
        {
            long x;
            long y;
            long z;
            WorldScript.instance.mPlayerFrustrum.GetCoordsFromUnityWithRoundingFix(this.mUnityDronePos, out x, out y, out z);
            this.mDroneSegment = this.AttemptGetSegment(x, y, z);
        }

        if (this.meState == MSManufacturer.eState.Unknown)
            this.SetNewState(MSManufacturer.eState.LookingForAttachedStorage);
        if (this.meState == MSManufacturer.eState.LookingForAttachedStorage)
        {
            if (this.mAttachedCrate == null)
                this.LookForAttachedStorage();
            else
            if (this.CurrentMachine == AttachedMachine.None)
                this.LookForAttachedCrafter();
        }
        if (this.mAttachedCrate == null || this.CurrentMachine == AttachedMachine.None)
        {
            this.SetNewState(MSManufacturer.eState.LookingForAttachedStorage);
        }
        else
        {
            if (IndexedDistances == null)
            {
                if (this.HandleCrateLockOut())
                    return;
            }
            else if (IndexedDistances.Count() != mAttachedCrate.GetCenter().mConnectedCrates.Count + 1 || LocalCrateLockOut)
            {
                if (!LocalCrateLockOut)
                    this.IndexedDistances = null;
                if (this.HandleCrateLockOut())
                    return;
            }

            if (this.meState == MSManufacturer.eState.Idling)
                this.UpdateIdling();
            if (this.meState == MSManufacturer.eState.SendingDrone)
                this.UpdateSendingDrone();
            if (this.meState == MSManufacturer.eState.RetrievingDrone)
            {
                this.mrCarryTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
                if (this.mrCarryTimer < 0.0)
                {
                    this.SetNewState(MSManufacturer.eState.DroppingOff);
                    return;
                }
            }
            if (this.meState == MSManufacturer.eState.DroppingOff)
            {
                if (this.DroneInventory.IsEmpty())
                {
                    Debug.LogWarning((object)"Warning, attempting to Drop Off EMPTY cargo");
                    this.SetNewState(MSManufacturer.eState.Idling);
                }
                if (this.LocalInventory.SpareCapacity() > 0)
                {
                    this.LocalInventory.Fill(ref this.DroneInventory.Inventory);
                    if (this.CurrentMachine == AttachedMachine.Lab)
                        this.SetExemplar(null);
                    this.SetNewState(MSManufacturer.eState.Idling);
                    return;
                }
            }
            this.mrStateTimer += LowFrequencyThread.mrPreviousUpdateTimeStep;
        }
        this.AttemptToOffloadOverflow();
    }

    private void UpdateSendingDrone()
    {
        if (this.mSourceCrate == null)
            Debug.LogError((object)"Error, trying to send drone, but mSourceCrate is null!");
        if ((this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.MPlant) && this.CheckIngredientCount())
        {
            Debug.LogWarning("MSManufacturer sending drone but doesn't need item.  Exemplar: " + this.Exemplar.ToString() + " for recipe: " + this.mRecipe.ToString());
            this.mSourceCrate.mrOutputLockTimer = 0f;
            this.mSourceCrate = null;
            this.SetNewState(eState.Idling);
        }
        this.mSourceCrate.mrOutputLockTimer = 5f;
        if ((double)this.mrCarryTimer == -1.0)
        {
            this.mrCarryDistance = 5f;
            this.mrCarryTimer = !this.mbLinkedToGO ? this.mrCarryDistance + 2f : 5f;
        }
        this.mrCarryTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
        if ((double)this.mrCarryTimer >= 0.0)
            return;
        int itemSlot = this.mSourceCrate.GetItemSlot(this.Exemplar);
        if (itemSlot == -1)
        {
            Debug.LogError((object)("Error, SourceCrate no longer contains item?! (LockTimer was " + (object)this.mSourceCrate.mrOutputLockTimer + ")"));
            this.SetNewState(MSManufacturer.eState.RetrievingDrone);
        }
        else
        {
            if (this.mSourceCrate.mMode == MassStorageCrate.CrateMode.SingleStack)
            {
                ItemBase itemBase = this.mSourceCrate.mItem;
                if (itemBase == null)
                {
                    Debug.LogError((object)("Error, SourceCrate no longer contains item?! (LockTimer was " + (object)this.mSourceCrate.mrOutputLockTimer + ")"));
                    this.SetNewState(MSManufacturer.eState.RetrievingDrone);
                    return;
                }
                if (itemBase.IsStack())
                {
                    if (this.CurrentMachine != AttachedMachine.Lab)
                    {
                        if (itemBase.GetAmount() <= this.DroneInventory.SpareCapacity())
                        {
                            this.DroneInventory.AddItem(ItemBaseUtil.NewInstance(itemBase));
                            this.mSourceCrate.mItem = (ItemBase)null;
                        }
                        else
                        {
                            itemBase.SetAmount(itemBase.GetAmount() - this.DroneInventory.SpareCapacity());
                            this.DroneInventory.AddItem(ItemBaseUtil.NewInstance(itemBase).SetAmount(this.DroneInventory.SpareCapacity()));
                        }
                    }
                    else
                    {
                        if (this.mSourceCrate.mItem.GetAmount() > 1)
                        {
                            this.DroneInventory.AddItem(ItemBaseUtil.NewInstance(itemBase).SetAmount(1));
                            this.mSourceCrate.mItem.DecrementStack(1);
                        }
                        else if (this.mSourceCrate.mItem.GetAmount() == 1)
                        {
                            this.DroneInventory.AddItem(ItemBaseUtil.NewInstance(itemBase));
                            this.mSourceCrate.mItem = (ItemBase)null;
                        }
                    }
                }
                else
                {
                    this.DroneInventory.AddItem(itemBase);
                    this.mSourceCrate.mItem = (ItemBase)null;
                }
                this.mSourceCrate.mrOutputLockTimer = 0.0f;
                this.mSourceCrate.CountUpFreeStorage(false);
                this.SetNewState(MSManufacturer.eState.RetrievingDrone);
                this.mrCarryTimer = !this.mbLinkedToGO ? this.mrCarryDistance + 2f : 5f;
            }
            else
            {
                if (this.mSourceCrate.mItems[itemSlot] == null)
                {
                    Debug.LogWarning("MSManufacturer tried to collect null item!");
                    if (this.Exemplar == null)
                        this.SetNewState(eState.RetrievingDrone);
                }
                this.DroneInventory.AddItem(this.mSourceCrate.mItems[itemSlot]);
                this.DepositCleanup(itemSlot);
                if (this.CurrentMachine != AttachedMachine.Lab)
                {
                    for (int index = 0; index < this.mSourceCrate.mnLocalUsedStorage; index++)
                    {
                        int slot = this.mSourceCrate.GetItemSlot(this.Exemplar);
                        if (slot != -1 && !this.DroneInventory.IsFull() && this.mSourceCrate.mItems[slot] != null)
                        {
                            this.DroneInventory.AddItem(this.mSourceCrate.mItems[slot]);
                            this.DepositCleanup(slot);
                        }
                        else if (this.DroneInventory.IsFull())
                            break;
                    }
                }
            }
        }
    }


    public void DepositCleanup(int itemSlot)
    {
        if (this.mSourceCrate == null || this.mSourceCrate.mItems[itemSlot] == null)
        {
            if (this.mSourceCrate == null)
                Debug.LogWarning("MSManufacturer Deposit cleanup attempted with null crate");
            else
                Debug.LogWarning("MSManufacturer Deposit cleanup attempted with null itemslot");
            return;
        }
        if (this.mSourceCrate.mItems[itemSlot].mnItemID == -1 && this.mSourceCrate.mItems[itemSlot].mType != ItemType.ItemCubeStack)
        {
            Debug.LogError((object)"Error, MSOP grabbed back ItemID -1!");
            this.DroneInventory.RemoveItem(this.mSourceCrate.mItems[itemSlot]);
            this.mSourceCrate.mrOutputLockTimer = 0.0f;
            if (this.mSourceCrate.mMode != MassStorageCrate.CrateMode.SingleStack)
                this.mSourceCrate.mItems[itemSlot] = (ItemBase)null;
            this.mSourceCrate.CountUpFreeStorage(false);
            this.SetNewState(MSManufacturer.eState.RetrievingDrone);
        }
        else
        {
            this.mSourceCrate.mrOutputLockTimer = 0.0f;
            if (this.mSourceCrate.mMode != MassStorageCrate.CrateMode.SingleStack)
                this.mSourceCrate.mItems[itemSlot] = (ItemBase)null;
            this.mSourceCrate.CountUpFreeStorage(false);
            this.SetNewState(MSManufacturer.eState.RetrievingDrone);
            if (this.mbLinkedToGO)
                this.mrCarryTimer = 5f;
            else
                this.mrCarryTimer = this.mrCarryDistance + 2f;
        }
    }

    private bool AttemptToOffloadOverflow()
    {
        if (this.OverflowInventory.IsEmpty())
            return false;
        ItemBase offload = this.OverflowInventory.RemoveAnySingle(1);
        if (this.GiveToSurrounding(offload))
            return true;
        else
            this.OverflowInventory.AddItem(offload);
        return false;
    }

    //private bool AttemptToOffloadOverflow()
    //{

    //    if (this.OverflowList == null || this.OverflowList.Count == 0)
    //    {
    //        return false;
    //    }
    //    for (int index = 0; index < 6; ++index)
    //    {
    //        long x = this.mnX;
    //        long y = this.mnY;
    //        long z = this.mnZ;
    //        if (index % 6 == 0)
    //            --x;
    //        if (index % 6 == 1)
    //            ++x;
    //        if (index % 6 == 2)
    //            --y;
    //        if (index % 6 == 3)
    //            ++y;
    //        if (index % 6 == 4)
    //            --z;
    //        if (index % 6 == 5)
    //            ++z;
    //        Segment segment = this.AttemptGetSegment(x, y, z);
    //        if (segment != null)
    //        {
    //            ushort cube = segment.GetCube(x, y, z);
    //            if ((int)cube == 505)
    //            {
    //                StorageHopper storageHopper = segment.FetchEntity(eSegmentEntity.StorageHopper, x, y, z) as StorageHopper;
    //                if (storageHopper != null && storageHopper.mPermissions != StorageHopper.ePermissions.Locked && storageHopper.mPermissions != StorageHopper.ePermissions.RemoveOnly)
    //                {
    //                    if (storageHopper.AddItem(this.ItemInventory[OverflowList[0]]))
    //                    {
    //                        this.ItemInventory[OverflowList[0]] = (ItemBase)null;
    //                        OverflowList.Remove(OverflowList[0]);
    //                        return true;
    //                    }
    //                }
    //                else
    //                    continue;
    //            }
    //            if ((int)cube == 513)
    //            {
    //                ConveyorEntity conveyorEntity = segment.FetchEntity(eSegmentEntity.Conveyor, x, y, z) as ConveyorEntity;
    //                if (conveyorEntity != null && conveyorEntity.mbReadyToConvey && (double)conveyorEntity.mrLockTimer == 0.0)
    //                {
    //                    conveyorEntity.AddItem(this.ItemInventory[OverflowList[0]]);
    //                    this.ItemInventory[OverflowList[0]] = (ItemBase)null;
    //                    OverflowList.Remove(OverflowList[0]);
    //                    return true;
    //                }
    //            }
    //        }
    //    }
    //    return false;
    //}

    private void SetNewState(MSManufacturer.eState lNewState)
    {
        if (this.meState != lNewState)
            this.RequestImmediateNetworkUpdate();
        this.meState = lNewState;
        if (this.meState == eState.Idling)
            this.mSourceCrate = null;
        this.mrStateTimer = 0.0f;
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
    }

    public override void UnitySuspended()
    {
        this.CarryDrone = (GameObject)null;
        if ((Object)this.mCarriedObjectItem != (Object)null)
            Object.Destroy((Object)this.mCarriedObjectItem);
        if ((Object)this.HoloPreview != (Object)null)
            Object.Destroy((Object)this.HoloPreview);
        this.HoloPreview = (GameObject)null;
        this.mCarriedObjectItem = (GameObject)null;
        this.CarryDroneClamp = (GameObject)null;
        this.Thrust_Particles = (GameObject)null;
    }

    private void SetTier(GameObject lObject)
    {
        Renderer[] componentsInChildren = lObject.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Color color = Color.white;
        if (this.Tier == 1)
            color = Color.green;
        if (this.Tier == 2)
            color = Color.cyan;
        if (this.Tier == 3)
            color = Color.magenta;
        properties.SetColor("_TintColor", color);
        for (int index = 0; index < componentsInChildren.Length; ++index)
        {
            componentsInChildren[index].material = PrefabHolder.instance.HoloPreviewMaterial;
            componentsInChildren[index].shadowCastingMode = ShadowCastingMode.Off;
            componentsInChildren[index].receiveShadows = false;
            componentsInChildren[index].SetPropertyBlock(properties);
        }
    }

    public override void UnityUpdate()
    {
        //UIUtil.DisconnectUI(this);
        if (!this.mbLinkedToGO)
        {
            if (this.mWrapper == null || this.mWrapper.mGameObjectList == null)
                return;
            if ((UnityEngine.Object)this.mWrapper.mGameObjectList[0].gameObject == (UnityEngine.Object)null)
                Debug.LogError((object)"MSIP missing game object #0 (GO)?");
            this.CarryDrone = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "CarryDrone").gameObject;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            Color color = Color.white;
            if (this.Tier == 1)
                color = Color.green;
            if (this.Tier == 2)
                color = Color.cyan;
            if (this.Tier == 3)
                color = Color.magenta;
            properties.SetColor("_GlowColor", color);
            this.CarryDrone.GetComponent<Renderer>().SetPropertyBlock(properties);
            this.CarryDroneClamp = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "ClampPoint").gameObject;
            this.OutputHopperObject = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "Output Hopper").gameObject;
            this.Thrust_Particles = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "Thrust_Particles").gameObject;
            this.HoloCubePreview = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "HoloCube").gameObject;
            this.NoItem = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "NoItemSet").gameObject;
            this.DigiTransmit = Extensions.Search(this.CarryDrone.transform, "DigiTransmit").gameObject;
            this.HoloCubePreview.SetActive(false);
            this.mUnityDroneRestPos = this.CarryDrone.transform.position;
            this.mUnityDronePos = this.CarryDrone.transform.position;
            this.mbCarriedCubeNeedsConfiguring = true;
            this.mbLinkedToGO = true;
        }
        else
        {
            if (this.meUnityState != this.meState)
            {
                if (this.meUnityState == MSManufacturer.eState.SendingDrone && this.meState == MSManufacturer.eState.RetrievingDrone)
                {
                    this.DigiTransmit.SetActive(true);
                    this.DigiTransmit.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }
                this.meUnityState = this.meState;
            }
            if (this.mbHoloPreviewDirty || (this.Exemplar != null && this.NoItem.activeSelf))
            {
                if ((Object)this.HoloPreview != (Object)null)
                {
                    Object.Destroy((Object)this.HoloPreview);
                    this.HoloPreview = null;
                }
                if (this.Exemplar != null)
                {
                    this.NoItem.SetActive(false);
                    if (this.Exemplar.mType == ItemType.ItemCubeStack)
                    {
                        this.HoloCubePreview.SetActive(true);
                        this.SetTier(this.HoloCubePreview);
                    }
                    else
                    {
                        int index = (int)ItemEntry.mEntries[this.Exemplar.mnItemID].Object;
                        this.HoloPreview = (GameObject)Object.Instantiate((Object)SpawnableObjectManagerScript.instance.maSpawnableObjects[index], this.mWrapper.mGameObjectList[0].gameObject.transform.position + new Vector3(0.0f, 1.5f, 0.0f), Quaternion.identity);
                        this.HoloPreview.transform.parent = this.mWrapper.mGameObjectList[0].gameObject.transform;
                        this.SetTier(this.HoloPreview);
                        this.HoloPreview.gameObject.AddComponent<RotateConstantlyScript>();
                        this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().YRot = 1f;
                        this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().XRot = 0.35f;
                        this.HoloPreview.SetActive(true);
                        this.HoloCubePreview.SetActive(false);
                    }
                }
                else
                {
                    this.HoloCubePreview.SetActive(false);
                    this.NoItem.SetActive(true);
                }
                this.mbHoloPreviewDirty = false;
            }
            if (this.meState == MSManufacturer.eState.Idling || this.meState == MSManufacturer.eState.DroppingOff)
            {
                Vector3 vector3 = this.mForwards;
                vector3.x += 0.1f;
                vector3.Normalize();
                this.CarryDrone.transform.forward += (vector3 - this.CarryDrone.transform.forward) * Time.deltaTime * 0.5f;

                ////My stuff for drifting back home
                //Vector3 vectorhome = this.mUnityDroneRestPos - this.mUnityDronePos;
                //float magnitude2 = vectorhome.magnitude;
                //vectorhome /= magnitude2;
                //if ((double)magnitude2 < 0.100000001490116)
                //{
                //    this.mUnityDronePos = this.mUnityDroneRestPos;
                //}
                //else
                //{
                //    this.CarryDrone.transform.position += vectorhome * Time.deltaTime * 0.5f;
                //}

                if (this.Thrust_Particles.activeSelf)
                    this.Thrust_Particles.SetActive(false);
            }
            else if (!this.Thrust_Particles.activeSelf)
                this.Thrust_Particles.SetActive(true);
            if (this.mbCarriedCubeNeedsConfiguring)
                this.ConfigureCarry();
            this.UnityUpdateSendingDrone();
            this.UnityUpdateRetrievingDrone();

            if (this.mbWellBehindPlayer || this.mSegment.mbOutOfView)
            {
                if ((Object)this.HoloPreview != (Object)null && this.HoloPreview.activeSelf)
                    this.HoloPreview.SetActive(false);
                if (this.OutputHopperObject.activeSelf)
                    this.OutputHopperObject.SetActive(false);
                if (this.Exemplar == null)
                {
                    if (this.NoItem.activeSelf)
                        this.NoItem.SetActive(false);
                }
                else if (this.Exemplar.mType == ItemType.ItemCubeStack)
                {
                    if (this.HoloCubePreview.activeSelf)
                        this.HoloCubePreview.SetActive(false);
                }
                else if (this.HoloPreview.activeSelf)
                    this.HoloPreview.SetActive(false);
            }
            else
            {
                if ((Object)this.HoloPreview != (Object)null && !this.HoloPreview.activeSelf)
                    this.HoloPreview.SetActive(true);
                if (!this.OutputHopperObject.activeSelf)
                    this.OutputHopperObject.SetActive(true);
                if (this.Exemplar == null)
                {
                    if (!this.NoItem.activeSelf)
                        this.NoItem.SetActive(true);
                }
                else if (this.Exemplar.mType == ItemType.ItemCubeStack)
                {
                    if (!this.HoloCubePreview.activeSelf)
                        this.HoloCubePreview.SetActive(true);
                }
                else if (!this.HoloPreview.activeSelf)
                    this.HoloPreview.SetActive(true);
            }
            if (this.mDroneSegment != null)
            {
                if (this.mDroneSegment.mbOutOfView)
                {
                    if (!this.CarryDrone.activeSelf)
                        return;
                    this.CarryDrone.SetActive(false);
                }
                else
                {
                    if (this.CarryDrone.activeSelf)
                        return;
                    this.CarryDrone.SetActive(true);
                }
            }
            else
            {
                if (!this.CarryDrone.activeSelf)
                    return;
                this.CarryDrone.SetActive(false);
            }
        }
    }

    private void ConfigureCarry()
    {
        this.mbCarriedCubeNeedsConfiguring = false;
        if ((UnityEngine.Object)this.mCarriedObjectItem != (UnityEngine.Object)null)
            UnityEngine.Object.Destroy((UnityEngine.Object)this.mCarriedObjectItem);
        if (this.mCarriedItem == null)
            return;
        if (this.mCarriedItem.mnItemID == -1)
        {
            if (this.mCarriedItem.mType == ItemType.ItemCubeStack)
                return;
            Debug.LogError((object)("Error, Mass Storage carrying illegal item ID of " + (object)this.mCarriedItem.mnItemID));
        }
        else
        {
            int index = (int)ItemEntry.mEntries[this.DroneInventory.Inventory[0].mnItemID].Object;
            this.mCarriedObjectItem = (GameObject)UnityEngine.Object.Instantiate((UnityEngine.Object)SpawnableObjectManagerScript.instance.maSpawnableObjects[index], this.CarryDroneClamp.transform.position, this.CarryDroneClamp.transform.rotation);
            this.mCarriedObjectItem.transform.parent = this.CarryDroneClamp.transform;
            this.mCarriedObjectItem.transform.localPosition = Vector3.zero;
            this.mCarriedObjectItem.transform.rotation = Quaternion.identity;
            this.mCarriedObjectItem.SetActive(true);
        }
    }

    private void UnityUpdateRetrievingDrone()
    {
        if (this.meState != MSManufacturer.eState.RetrievingDrone && this.meState != MSManufacturer.eState.LookingForAttachedStorage && (this.meState != MSManufacturer.eState.Idling && this.meState != MSManufacturer.eState.DroppingOff))
            return;
        Vector3 vector3_1 = this.mUnityDroneRestPos - this.mUnityDronePos;
        float magnitude = vector3_1.magnitude;
        Vector3 vector3_2 = vector3_1 / magnitude;
        if ((double)magnitude < 0.100000001490116)
        {
            this.mrDroneHeight -= Time.deltaTime;
            if ((double)this.mrDroneHeight < 0.0)
            {
                this.mrDroneHeight = 0.0f;
                this.mUnityDronePos = this.mUnityDroneRestPos;
                this.mrCarryTimer = 0.0f;
            }
            else
                this.mrCarryTimer = 1f;
        }
        else
        {
            this.mrCarryTimer = 1f;
            if (this.mrDroneHeight < this.mrDroneTargetHeight)
            {
                this.mrDroneHeight += Time.deltaTime;
                this.CarryDrone.transform.Rotate(0.0f, 1f, 0.0f);
            }
            else
                this.mUnityDronePos += vector3_2 * Time.deltaTime;
        }
        this.UpdateDronePos(this.mUnityDronePos);
        Vector3 vector3_3 = vector3_2 - this.CarryDrone.transform.forward;
        vector3_3.y = 0.0f;
        if (!(vector3_3 != Vector3.zero))
            return;
        this.CarryDrone.transform.forward += vector3_3 * Time.deltaTime * 2.5f;
    }

    private void UnityUpdateSendingDrone()
    {
        if (this.meState != MSManufacturer.eState.SendingDrone)
            return;
        if (!this.mbConfiguredDroneTarget)
        {
            this.mbConfiguredDroneTarget = true;
            this.mUnityDroneTarget = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mSourceCrate.mnX, this.mSourceCrate.mnY, this.mSourceCrate.mnZ) + new Vector3(0.5f, 1.5f, 0.5f);
        }
        if (this.mUnityDroneTarget == Vector3.zero)
            return;
        Vector3 vector3_1 = this.mUnityDroneTarget - this.mUnityDronePos;
        float magnitude = vector3_1.magnitude;
        vector3_1 /= magnitude;
        if ((double)magnitude < 0.100000001490116)
        {
            this.mrDroneHeight -= Time.deltaTime;
            this.mrCarryTimer = (double)this.mrDroneHeight >= -0.75 ? 1f : 0.0f;
        }
        else
        {
            this.mrCarryTimer = 1f;
            if (this.mrDroneHeight < this.mrDroneTargetHeight)
            {
                this.mrDroneHeight += Time.deltaTime;
                this.CarryDrone.transform.Rotate(0.0f, 1f, 0.0f);
            }
            else
                this.mUnityDronePos += vector3_1 * Time.deltaTime;
        }
        this.UpdateDronePos(this.mUnityDronePos);
        Vector3 vector3_2 = vector3_1 - this.CarryDrone.transform.forward;
        vector3_2.y = 0.0f;
        this.CarryDrone.transform.forward += vector3_2 * Time.deltaTime * 2.5f;
    }

    private void UpdateDronePos(Vector3 lPos)
    {
        this.CarryDrone.transform.position += (lPos + new Vector3(0.0f, this.mrDroneHeight, 0.0f) - this.CarryDrone.transform.position) * Time.deltaTime;
    }

    private void LookForAttachedStorage()
    {
        bool nullseg;
        List<MassStorageCrate> crates = CommunityUtil.CheckSurrounding<MassStorageCrate>(this, out nullseg);
        if (crates.Count == 0)
            return;
        MassStorageCrate crate = crates[0];
        if (crate != null && (crate.mbIsCenter || crate.GetCenter() != null))
        {
            this.mAttachedCrate = crate;
            if (this.CurrentMachine != AttachedMachine.None)
                this.SetNewState(MSManufacturer.eState.Idling);
        }
    }

    //private void LookForAttachedStorage()
    //{
    //    for (int index = 0; index < 4; ++index)
    //    {
    //        long x = this.mnX;
    //        long y = this.mnY;
    //        long z = this.mnZ;
    //        if (index == 0)
    //            --x;
    //        if (index == 1)
    //            ++x;
    //        if (index == 2)
    //            --z;
    //        if (index == 3)
    //            ++z;
    //        Segment segment = this.AttemptGetSegment(x, y, z);
    //        if (segment == null)
    //        {
    //            segment = WorldScript.instance.GetSegment(x, y, z);
    //            if (segment == null)
    //            {
    //                Debug.Log((object)"LookForAttachedStorage did not find segment");
    //                continue;
    //            }
    //        }
    //        //Debug.Log("Checking for crates");
    //        if ((int)segment.GetCube(x, y, z) == 527)
    //        {
    //            MassStorageCrate massStorageCrate = segment.FetchEntity(eSegmentEntity.MassStorageCrate, x, y, z) as MassStorageCrate;
    //            if (massStorageCrate != null && (massStorageCrate.mbIsCenter || massStorageCrate.GetCenter() != null))
    //            {
    //                this.mAttachedCrate = massStorageCrate;
    //                if (this.AttachedGAC != null || this.AttachedManfacturingPlant != null)
    //                    this.SetNewState(MSManufacturer.eState.Idling);
    //                break;
    //            }
    //        }
    //    }
    //}

    private void LookForAttachedCrafter()
    {
        for (int index = 0; index < 5; ++index)
        {
            long x = this.mnX;
            long y = this.mnY;
            long z = this.mnZ;
            if (index == 0)
                --x;
            if (index == 1)
                ++x;
            if (index == 2)
                --z;
            if (index == 3)
                ++z;
            if (index == 4)
                --y;
            Segment segment = this.AttemptGetSegment(x, y, z);
            if (segment == null)
            {
                segment = WorldScript.instance.GetSegment(x, y, z);
                if (segment == null)
                {
                    Debug.Log((object)"LookForAttachedCrafter did not find segment");
                    continue;
                }
            }
            //Debug.Log("Checking for crafters");
            if ((int)segment.GetCube(x, y, z) == 520)
            {
                ManufacturingPlant manufacturingplant = segment.FetchEntity(eSegmentEntity.ManufacturingPlant, x, y, z) as ManufacturingPlant;
                if (manufacturingplant != null)
                {
                    this.AttachedManfacturingPlant = manufacturingplant;
                    this.CurrentMachine = AttachedMachine.MPlant;
                    if (this.mAttachedCrate != null)
                        this.SetNewState(MSManufacturer.eState.Idling);
                    break;
                }
            }
            SegmentEntity testentity = segment.SearchEntity(x, y, z);
            if (testentity != null)
            {
                if (testentity.mType == eSegmentEntity.GenericCraftingStationNew)
                {
                    GenericAutoCrafterNew GAC = segment.FetchEntity(eSegmentEntity.GenericCraftingStationNew, x, y, z) as GenericAutoCrafterNew;
                    if (GAC != null)
                    {
                        this.AttachedGAC = GAC;
                        this.CurrentMachine = AttachedMachine.GAC;
                        if (this.mAttachedCrate != null)
                            this.SetNewState(MSManufacturer.eState.Idling);
                        break;
                    }
                }
            }
            if ((int)segment.GetCube(x, y, z) == 535)
            {
                Laboratory lab = segment.FetchEntity(eSegmentEntity.Laboratory, x, y, z) as Laboratory;
                lab = lab.mLinkedCenter;
                if (lab != null)
                {
                    this.AttachedLab = lab;
                    this.CurrentMachine = AttachedMachine.Lab;
                    if (this.mAttachedCrate != null)
                        this.SetNewState(MSManufacturer.eState.Idling);
                    break;
                }
            }
            if ((int)segment.GetCube(x, y, z) == 536)
            {
                ResearchAssembler assembler = segment.FetchEntity(eSegmentEntity.ResearchAssembler, x, y, z) as ResearchAssembler;
                if (assembler != null)
                {
                    this.AttachedAssembler = assembler;
                    this.CurrentMachine = AttachedMachine.ResearchAssembler;
                    if (this.mAttachedCrate != null)
                        this.SetNewState(MSManufacturer.eState.Idling);
                    break;
                }
            }
        }
    }

    private void CheckMissingMachines()
    {
        if (this.mAttachedCrate != null)
        {
            if (this.mAttachedCrate.mbDelete)
            {
                this.mAttachedCrate = null;
                if (CrateLockTimer < 0 && this.LocalCrateLockOut && CrateDistanceLockOut.CheckLock)
                {
                    this.LocalCrateLockOut = false;
                    CrateDistanceLockOut.CheckLock = false;
                }
            }
        }
        if (this.AttachedGAC != null)
        {
            if (this.AttachedGAC.mbDelete)
            {
                this.AttachedGAC = null;
                this.mRecipe = null;
                this.Exemplar = null;
                this.SetExemplar(this.Exemplar);
                if (this.CurrentMachine == AttachedMachine.GAC)
                    this.CurrentMachine = AttachedMachine.None;
            }
        }
        if (this.AttachedManfacturingPlant != null)
        {
            if (this.AttachedManfacturingPlant.mbDelete)
            {
                this.AttachedManfacturingPlant = null;
                this.mRecipe = null;
                this.Exemplar = null;
                this.SetExemplar(this.Exemplar);
                if (this.CurrentMachine == AttachedMachine.MPlant)
                    this.CurrentMachine = AttachedMachine.None;
            }
            else if (this.mRecipe != this.AttachedManfacturingPlant.mSelectedRecipe)
            {
                this.Exemplar = null;
                this.mRecipe = null;
                this.SetExemplar(this.Exemplar);
            }
        } 
        if (this.AttachedLab != null)
        {
            if (this.AttachedLab.mbDelete)
            {
                this.AttachedLab = null;
                this.ResearchProject = null;
                this.Exemplar = null;
                this.SetExemplar(this.Exemplar);
                if (this.CurrentMachine == AttachedMachine.Lab)
                    this.CurrentMachine = AttachedMachine.None;
            }
            else if (this.ResearchProject != this.AttachedLab.mCurrentProject)
            {
                this.ResearchProject = null;
                this.Exemplar = null;
                this.SetExemplar(null);
            }
        }
        if (this.AttachedAssembler != null)
        {
            if (this.AttachedAssembler.mbDelete)
            {
                this.AttachedAssembler = null;
                this.PodType = null;
                this.Exemplar = null;
                this.SetExemplar(this.Exemplar);
                if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
                    this.CurrentMachine = AttachedMachine.None;
            }
        }
    }

    public ItemBase GetCostAsItemBase(CraftCost craftCost)
    {
        if (craftCost.CubeType != 0)
        {
            ItemBase itembase = new ItemCubeStack(craftCost.CubeType, craftCost.CubeValue, (int)craftCost.Amount);
            return itembase;
        }
        else
        {
            ItemBase itembase = new ItemStack(craftCost.ItemType, (int)craftCost.Amount);
            return itembase;
        }
    }

    private ItemBase GetResearchItemCost(int n)
    {
        ItemBase item;
        if (this.AttachedLab.mRemainingItemRequirements.Count == 0 || this.AttachedLab.mState == Laboratory.OperationalState.Finished)
            return null;
        ProjectItemRequirement projectItemRequirement = this.AttachedLab.mRemainingItemRequirements[n];
        if (projectItemRequirement.Amount == 0)
        {
            Debug.LogWarning("MSManufacturer tried to get research project item with zero count!");
            n = n < this.AttachedLab.mRemainingItemRequirements.Count - 1 ? n + 1 : 0;
            projectItemRequirement = this.AttachedLab.mRemainingItemRequirements[n];
        }
        if (projectItemRequirement.ItemID == -1)
            item = ItemManager.SpawnCubeStack(projectItemRequirement.CubeType, projectItemRequirement.CubeValue, projectItemRequirement.Amount);
        else
            item = ItemManager.SpawnItem(projectItemRequirement.ItemID).SetAmount(projectItemRequirement.Amount);
        return item;
    }

    private ItemBase GetAssemblerCost(int n)
    {
        if (PodType == null)
            return null;
        switch (PodType.mnItemID)
        {
            case 350:    //Basic - copper
                if (n == 0)
                    return ItemManager.SpawnItem(380);
                return ItemManager.SpawnItem(400);
            case 351:    //Primary - tin
                if (n == 0)
                    return ItemManager.SpawnItem(381);
                return ItemManager.SpawnItem(401);
            case 352:    //Primary - iron
                if (n == 0)
                    return ItemManager.SpawnItem(382);
                return ItemManager.SpawnItem(402);
            case 353:    //Primary - lithium
                if (n == 0)
                    return ItemManager.SpawnItem(384);
                return ItemManager.SpawnItem(404);
            case 354:    //Primary - gold
                if (n == 0)
                    return ItemManager.SpawnItem(383);
                return ItemManager.SpawnItem(403);
            case 355:    //Primary - nickle
                if (n == 0)
                    return ItemManager.SpawnItem(385);
                return ItemManager.SpawnItem(405);
            case 356:    //Primary - titanium
                if (n == 0)
                    return ItemManager.SpawnItem(386);
                return ItemManager.SpawnItem(406);
            default:
                Debug.LogWarning("MSManufacturer tried to retrieve item cost for research pod of unknown itemid");
                return null;
        }
    }

    private void GetRecipeIngredient()
    {
        if (this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.MPlant)
        {
            if (this.mRecipe == null)
            {
                string machine;
                if (this.AttachedGAC != null)
                    machine = this.AttachedGAC.mFriendlyName.ToString();
                else if (this.AttachedManfacturingPlant != null)
                    machine = "Manufacturing plant";
                else
                    machine = "Attached machines are null or attached to something else!";
                Debug.LogWarning("Why is MSManufacturuer trying to get ingredient for null recipe!? Attached to: " + machine + " with CurrentMachine state: " + this.CurrentMachine.ToString());
                return;
            }
            if (RecipeIndex >= this.mRecipe.Costs.Count)
                RecipeIndex = 0;
            this.SetExemplar(GetCostAsItemBase(this.mRecipe.Costs[this.RecipeIndex]));
        }
        else if (this.CurrentMachine == AttachedMachine.Lab)
        {
            if (RecipeIndex >= this.AttachedLab.mRemainingItemRequirements.Count)
                RecipeIndex = 0;
            this.SetExemplar(GetResearchItemCost(this.RecipeIndex));
        }
        else if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
        {
            if (RecipeIndex >= 2)
                RecipeIndex = 0;
            this.SetExemplar(GetAssemblerCost(this.RecipeIndex));
        }
    }

    private bool CheckIngredientCount()
    {
        if (this.CurrentMachine == AttachedMachine.MPlant || this.CurrentMachine == AttachedMachine.GAC)
        {
            if (this.mRecipe == null || this.LocalInventory == null)
            {
                Debug.LogWarning("MSManufacturer tried to check ingredient count but found null recipe/inventory");
                return false;
            }
            ItemBase ingredient = GetCostAsItemBase(this.mRecipe.Costs[this.RecipeIndex]);
            if (ingredient == null)
            {
                Debug.LogWarning("MSManufacturer tried to get recipe ingredient count but found null cost?");
                return false;
            }
            if (this.LocalInventory.Inventory.GetItemCount(ingredient) >= this.mRecipe.Costs[this.RecipeIndex].Amount)
                return true;
        }
        else if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
        {
            if (PodType == null)
                return false;
            switch (PodType.mnItemID)
            {
                case 350:    //Basic - copper
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(380) >= 6 : this.LocalInventory.Inventory.GetItemCount(400) >= 2;
                case 351:    //Primary - tin
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(381) >= 6 : this.LocalInventory.Inventory.GetItemCount(401) >= 2;
                case 352:    //Primary - iron
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(382) >= 6 : this.LocalInventory.Inventory.GetItemCount(402) >= 2;
                case 353:    //Primary - lithium
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(384) >= 6 : this.LocalInventory.Inventory.GetItemCount(404) >= 2;
                case 354:    //Primary - gold
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(383) >= 6 : this.LocalInventory.Inventory.GetItemCount(403) >= 2;
                case 355:    //Primary - nickle
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(385) >= 6 : this.LocalInventory.Inventory.GetItemCount(405) >= 2;
                case 356:    //Primary - titanium
                    return this.RecipeIndex == 0 ? this.LocalInventory.Inventory.GetItemCount(386) >= 6 : this.LocalInventory.Inventory.GetItemCount(406) >= 2;
                default:
                    Debug.LogWarning("MSManufacturer tried to check ingredient count on unknown pod ID");
                    return false;
            }
        }
        return false;
    }

    private bool CheckAllIngredients()
    {
        for (int index = 0; index < this.mRecipe.Costs.Count; ++index)
        {
            if (this.LocalInventory.Inventory.GetItemCount(GetCostAsItemBase(this.mRecipe.Costs[index])) < this.mRecipe.Costs[index].Amount)
                return false;
        }
        return true;
    }

    private bool CheckResearchReady(out ItemBase item)
    {
        int count = this.AttachedLab.mRemainingItemRequirements.Count;
        for (int n = 0; n < count; n++)
        {
            item = this.GetResearchItemCost(n);
            if (this.LocalInventory.Inventory.GetItemCount(item) > 0)
                return true;
        }
        item = null;
        return false;
    }

    private bool CheckAssemblerReady()
    {
        if (PodType == null)
            return false;
        switch (PodType.mnItemID)
        {
            case 350:    //Basic - copper
                return this.LocalInventory.Inventory.GetItemCount(380) >= 6 && this.LocalInventory.Inventory.GetItemCount(400) >= 2;
            case 351:    //Primary - tin
                return this.LocalInventory.Inventory.GetItemCount(381) >= 6 && this.LocalInventory.Inventory.GetItemCount(401) >= 2;
            case 352:    //Primary - iron
                return this.LocalInventory.Inventory.GetItemCount(382) >= 6 && this.LocalInventory.Inventory.GetItemCount(402) >= 2;
            case 353:    //Primary - lithium
                return this.LocalInventory.Inventory.GetItemCount(384) >= 6 && this.LocalInventory.Inventory.GetItemCount(404) >= 2;
            case 354:    //Primary - gold
                return this.LocalInventory.Inventory.GetItemCount(383) >= 6 && this.LocalInventory.Inventory.GetItemCount(403) >= 2;
            case 355:    //Primary - nickle
                return this.LocalInventory.Inventory.GetItemCount(385) >= 6 && this.LocalInventory.Inventory.GetItemCount(405) >= 2;
            case 356:    //Primary - titanium
                return this.LocalInventory.Inventory.GetItemCount(386) >= 6 && this.LocalInventory.Inventory.GetItemCount(406) >= 2;
            default:
                Debug.LogWarning("MSManufacturer tried to check assembly ready on unknown pod ID");
                return false;
        }
    }

    private void InitiateAssembling()
    {
        if (PodType == null || this.AttachedAssembler.meState != ResearchAssembler.eState.eLookingForResources)
            return;
        int plate;
        int pcb;
        switch (PodType.mnItemID)
        {
            case 350:    //Basic - copper
                plate = 380;
                pcb = 400;
                break;
            case 351:    //Primary - tin
                plate = 381;
                pcb = 401;
                break;
            case 352:    //Primary - iron
                plate = 382;
                pcb = 402;
                break;
            case 353:    //Primary - lithium
                plate = 384;
                pcb = 404;
                break;
            case 354:    //Primary - gold
                plate = 383;
                pcb = 403;
                break;
            case 355:    //Primary - nickle
                plate = 385;
                pcb = 405;
                break;
            case 356:    //Primary - titanium
                plate = 386;
                pcb = 406;
                break;
            default:
                Debug.LogWarning("MSManufacturer tried to initiate assembly on unknown pod ID");
                return;
        }
        ItemBase outplate;
        ItemBase outpcb;
        outplate = this.LocalInventory.RemoveItem(ItemManager.SpawnItem(plate).SetAmount(6));
        outpcb = this.LocalInventory.RemoveItem(ItemManager.SpawnItem(pcb).SetAmount(2));
        if (outplate.GetAmount() == 6 && outpcb.GetAmount() == 2)
        {
            this.AttachedAssembler.meState = ResearchAssembler.eState.eCrafting;
            this.AttachedAssembler.mTargetCreation = PodType.NewInstance().SetAmount(1);
            this.AttachedAssembler.mrCraftingTimer = 15f;
            if (!DifficultySettings.mbCasualResource)
                return;
            this.AttachedAssembler.mrCraftingTimer = 5f;
        }
        else
        {
            this.LocalInventory.AddItem(outplate);
            this.LocalInventory.AddItem(outpcb);
            Debug.LogWarning("MSManufacturer Tried to take items out to assemble but didn't find all ingredients!");
        }
    }

    private void InitiateResearch(ItemBase itemBase1)
    {
        if (this.LocalInventory.RemoveItem(itemBase1) == null)
            return;
        if (this.AttachedLab != null && this.AttachedLab.mCurrentProcessingItem == null && this.AttachedLab.mMode == Laboratory.OperationalMode.Project && (this.AttachedLab.mState == Laboratory.OperationalState.NoInventoriesFound || this.AttachedLab.mState == Laboratory.OperationalState.NoItemsFound))
        {
            this.AttachedLab.mCurrentProcessingItem = itemBase1;
            for (int index = 0; index < this.AttachedLab.mRemainingItemRequirements.Count; ++index)
            {
                ProjectItemRequirement projectItemRequirement = this.AttachedLab.mRemainingItemRequirements[index];
                if (projectItemRequirement.ItemID >= 0 && itemBase1.mnItemID >= 0 && projectItemRequirement.ItemID == itemBase1.mnItemID)
                {
                    --projectItemRequirement.Amount;
                    if (projectItemRequirement.Amount == 0)
                    {
                        this.AttachedLab.mRemainingItemRequirements.RemoveAt(index);
                        break;
                    }
                    break;
                }
                if (projectItemRequirement.ItemID < 0 && itemBase1.mnItemID < 0)
                {
                    ItemCubeStack itemCubeStack = itemBase1 as ItemCubeStack;
                    if (projectItemRequirement.CubeType == itemCubeStack.mCubeType && projectItemRequirement.CubeValue == itemCubeStack.mCubeValue)
                    {
                        --projectItemRequirement.Amount;
                        if (projectItemRequirement.Amount == 0)
                        {
                            this.AttachedLab.mRemainingItemRequirements.RemoveAt(index);
                            break;
                        }
                        break;
                    }
                }
            }
            if (itemBase1.mnItemID == -1)
            {
                ItemCubeStack itemCubeStack = itemBase1 as ItemCubeStack;
                this.AttachedLab.FriendlyState = TerrainData.GetNameForValue(itemCubeStack.mCubeType, itemCubeStack.mCubeValue);
            }
            else
                this.AttachedLab.FriendlyState = ItemEntry.GetNameFromID(itemBase1.mnItemID);
            this.SetExemplar(null);
            this.AttachedLab.mrCurrentRequirementProgress = 0.0f;
            this.AttachedLab.mState = Laboratory.OperationalState.Processing;
            this.AttachedLab.MarkDirtyDelayed();
            this.AttachedLab.RequestImmediateNetworkUpdate();
        }
        else
            this.LocalInventory.AddItem(itemBase1);
    }
    
    public void InitiateCrafting()
    {
        if (this.AttachedGAC != null)
        {
            if (this.AttachedGAC.meState == GenericAutoCrafterNew.eState.eLookingForResources)
            {
                if (!this.DeductIngredients())
                {
                    Debug.LogWarning("MSManufacturer tried to initiate crafting but couldn't deduct ingredients!");
                    return;
                }
                this.AttachedGAC.RequestImmediateNetworkUpdate();
                this.AttachedGAC.meState = GenericAutoCrafterNew.eState.eCrafting;
                this.AttachedGAC.mrCraftingTimer = this.AttachedGAC.mMachine.CraftTime;
                this.AttachedGAC.MarkDirtyDelayed();
            }
        }
        else if (this.AttachedManfacturingPlant != null)
        {
            if (this.AttachedManfacturingPlant.mState == ManufacturingPlant.State.SearchingIngredients)
            {
                if (!this.DeductIngredients())
                {
                    Debug.Log("Tried to initiate crafting but didn't have all the ingredients???");
                    return;
                }
                this.AttachedManfacturingPlant.mCurrentRecipe = this.AttachedManfacturingPlant.mSelectedRecipe;
                this.AttachedManfacturingPlant.mProgressTimer = this.AttachedManfacturingPlant.mCurrentRecipe.CraftTime;
                this.AttachedManfacturingPlant.mState = ManufacturingPlant.State.Crafting;
                this.AttachedManfacturingPlant.MarkDirtyDelayed();
            }
        }
    }

    public bool DeductIngredients()
    {
        for (int index = 0; index < this.mRecipe.Costs.Count; index++)
        {
            this.LocalInventory.RemoveItem(this.GetCostAsItemBase(this.mRecipe.Costs[index]));
        }
        return true;
    }

    //public bool DeductIngredients()
    //{
    //    //Debug.Log("Deducting ingredients");
    //    ItemBase[] items = this.ItemInventory;
    //    int[] itemcount = new int[this.mRecipe.Costs.Count];

    //    for (int index = 0; index < this.mRecipe.Costs.Count; index++)
    //    {
    //        for (int index2 = 0; index2 < this.StorageCapacity; index2++)
    //        {
    //            if (items[index2] != null)
    //            {
    //                //Debug.Log("Found item to deduct " + items[index2]);
    //                //Debug.Log("Recipe costs: " + GetCostAsItemBase(this.mRecipe.Costs[index]));
    //                if (items[index2].Compare(GetCostAsItemBase(this.mRecipe.Costs[index])))
    //                {
    //                    itemcount[index]++;
    //                    //Debug.Log("index2: " + index2 + " item count: " + itemcount[index]);
    //                    items[index2] = null;
    //                }
    //            }
    //            if (itemcount[index] >= this.mRecipe.Costs[index].Amount)
    //                break;
    //            else if (index2 == this.StorageCapacity - 1)
    //                return false;
    //        }
    //    }
    //    //Debug.Log("is it finished?");
    //    this.ItemInventory = items;
    //    return true;
    //}

    //public int CountHowManyOfItem(int itemID, ItemType type)
    //{
    //    int num = 0;
    //    for (int index = 0; index < this.StorageCapacity; ++index)
    //    {
    //        ItemBase itemBase = this.ItemInventory[index];
    //        if (itemBase != null && itemBase.mnItemID == itemID)
    //        {
    //            if (type == ItemType.ItemStack)
    //            {
    //                ItemStack itemStack = itemBase as ItemStack;
    //                if (itemStack != null)
    //                    num += itemStack.mnAmount;
    //                else
    //                    ++num;
    //            }
    //            else
    //                ++num;
    //        }
    //    }
    //    return num;
    //}
    
    //public int CountHowManyOfType(ushort lType, ushort lValue)
    //{
    //    int num = 0;
    //    for (int index = 0; index < this.StorageCapacity; ++index)
    //    {
    //        if (this.ItemInventory[index] != null && this.ItemInventory[index].mType == ItemType.ItemCubeStack)
    //        {
    //            ItemCubeStack itemCubeStack = this.ItemInventory[index] as ItemCubeStack;
    //            if ((int)lValue == (int)ushort.MaxValue && (int)itemCubeStack.mCubeType == (int)lType)
    //                num += itemCubeStack.mnAmount;
    //            if ((int)itemCubeStack.mCubeType == (int)lType && (int)itemCubeStack.mCubeValue == (int)lValue)
    //                num += itemCubeStack.mnAmount;
    //        }
    //    }
    //    return num;
    //}

    //public int CountHowMany(ItemBase itembase)
    //{
    //    int num = 0;
    //    if (itembase.mType == ItemType.ItemCubeStack)
    //    {
    //        ItemCubeStack itemcubestack = itembase as ItemCubeStack;
    //        num = this.CountHowManyOfType(itemcubestack.mCubeType, itemcubestack.mCubeValue);
    //    }
    //    else
    //        num = this.CountHowManyOfItem(itembase.mnItemID, itembase.mType);
    //    return num;
    //}

    public void BuildOverflowList()
    {
        List<ItemBase> blacklist = new List<ItemBase>();
        if ((this.CurrentMachine == AttachedMachine.GAC || this.CurrentMachine == AttachedMachine.MPlant) && this.mRecipe != null)
        {
            for (int index = 0; index < this.mRecipe.Costs.Count; index++)
                blacklist.Add(this.GetCostAsItemBase(this.mRecipe.Costs[index]));
        }
        else if (this.CurrentMachine == AttachedMachine.Lab)
        {
            int count = this.AttachedLab.mRemainingItemRequirements.Count;
            for (int n = 0; n < count; n++)
            {
                blacklist.Add(this.GetResearchItemCost(n));
            }
        }
        else if (this.CurrentMachine == AttachedMachine.ResearchAssembler)
        {
            //Nothing blacklisted as the recipes are mutually exclusive
        }
        this.OverflowInventory.FillBlackList(ref this.LocalInventory.Inventory, blacklist);
    }

    //public void BuildOverflowList()
    //{
    //    List<int> OverflowList = new List<int>();

    //    //Debug.Log("Entering build overflow");
    //    for (int index = 0; index < this.ItemInventory.Length; ++index)
    //    {
    //        bool matchfound = false;
    //        ItemBase itembase = this.ItemInventory[index];
    //        if (itembase == null)
    //            continue;
    //        for (int index2 = 0; index2 < this.mRecipe.Costs.Count; ++index2)
    //        {
    //            CraftCost craftcost = this.mRecipe.Costs[index2];
    //            if (itembase.mType == ItemType.ItemCubeStack)
    //            {
    //                ItemCubeStack itemcubestack = itembase as ItemCubeStack;
    //                if (itemcubestack.mCubeType == craftcost.CubeType && itemcubestack.mCubeValue == craftcost.CubeValue)
    //                    matchfound = true;
    //            }
    //            else
    //            {
    //                if (itembase.mnItemID == craftcost.ItemType)
    //                    matchfound = true;
    //            }
    //        }
    //        //Debug.Log("Overflow item: " + itembase.ToString() + " match: " + matchfound);
    //        if (!matchfound)
    //            OverflowList.Add(index);
    //    }
    //    this.OverflowList = OverflowList;
    //    //Debug.Log("Overflow list length: " + this.OverflowList.Count);
    //}

    public override void OnDelete()
    {
        this.LocalInventory.DropOnDelete();
        this.DroneInventory.DropOnDelete();
        this.OverflowInventory.DropOnDelete();
        base.OnDelete();
    }

    private bool HandleCrateLockOut()
    {
        CrateLockTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
        if (this.LocalCrateLockOut && !CrateDistanceLockOut.CheckLock)
        {
            Debug.LogWarning("Local crate distance scan locked out but global lock isn't?");
            this.LocalCrateLockOut = false;
            return true;
        }
        //We're holding the global lock and our timer has expired
        if (CrateLockTimer < 0 && this.LocalCrateLockOut && CrateDistanceLockOut.CheckLock)
        {
            this.LocalCrateLockOut = false;
            CrateDistanceLockOut.CheckLock = false;
            return true;
        }
        //Still awaiting lockout time
        if (this.CrateLockTimer > 0 || this.LocalCrateLockOut || CrateDistanceLockOut.CheckLock)
            return true;
        //Lockout is free so claim it and scan the storage for crate distances
        else
        {
            this.LocalCrateLockOut = true;
            CrateDistanceLockOut.CheckLock = true;
            this.MapCrateDistances();
            this.CrateLockTimer = 0.3f;
            return false;
        }
    }

    private void MapCrateDistances()
    {
        List<KeyValuePair<float, int>> DistanceMap = new List<KeyValuePair<float, int>>();
        if (mAttachedCrate.GetCenter() == null)
            return;
        MassStorageCrate lCrate = mAttachedCrate.GetCenter();
        this.IndexedDistances = new int[lCrate.mConnectedCrates.Count + 1];

        for (int index = 0; index < lCrate.mConnectedCrates.Count + 1; ++index)
        {
            if (index == lCrate.mConnectedCrates.Count) //Center crate!
            {
                if (lCrate != null)
                    DistanceMap.Add(new KeyValuePair<float, int>(new Vector3(this.mnX - lCrate.mnX, this.mnY - lCrate.mnY, this.mnZ - lCrate.mnZ).sqrMagnitude, index));
            }
            else if (lCrate.mConnectedCrates[index] != null)
            {
                DistanceMap.Add(new KeyValuePair<float, int>(new Vector3(this.mnX - lCrate.mConnectedCrates[index].mnX, this.mnY - lCrate.mConnectedCrates[index].mnY, this.mnZ - lCrate.mConnectedCrates[index].mnZ).sqrMagnitude, index));
            }
        }
        DistanceMap = DistanceMap.OrderBy(x => x.Key).ToList();
        for (int i = 0; i < DistanceMap.Count; i++)
        {
            this.IndexedDistances[i] = DistanceMap[i].Value;
        }
    }

    public override bool ShouldNetworkUpdate()
    {
        return true;
    }

    public override void ReadNetworkUpdate(BinaryReader reader)
    {
        base.ReadNetworkUpdate(reader);
        MSManufacturer.eState eState = (MSManufacturer.eState)reader.ReadByte();
        switch (eState)
        {
            case MSManufacturer.eState.RetrievingDrone:
            case MSManufacturer.eState.Idling:
            case MSManufacturer.eState.LookingForAttachedStorage:
            case MSManufacturer.eState.DroppingOff:
                this.meState = eState;
                break;
        }
    }

    public override void WriteNetworkUpdate(BinaryWriter writer)
    {
        base.WriteNetworkUpdate(writer);
        writer.Write((byte)this.meState);
    }

    //public bool HasItems()
    //{
    //    return false;
    //}

    //public bool HasItem(ItemBase item)
    //{
    //    return false;
    //}

    //public bool HasItems(ItemBase item, out int amount)
    //{
    //    amount = 0;
    //    return false;
    //}

    //public bool HasFreeSpace(uint amount)
    //{
    //    return GetFreeSpace() >= amount;
    //}

    //public int GetFreeSpace()
    //{
    //    return this.StorageCapacity - this.ItemInventory.GetItemCount();
    //}

    //public bool GiveItem(ItemBase item)
    //{
    //    //return this.AddItem(item);
    //    return false;
    //}

    //public ItemBase TakeItem(ItemBase item)
    //{
    //    return null;
    //}

    //public ItemBase TakeAnyItem()
    //{
    //    return null;
    //}


    public override int GetVersion()
    {
        return 2;
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override void Write(BinaryWriter writer)
    {
        this.LocalInventory.WriteInventory(writer);
        this.DroneInventory.WriteInventory(writer);
        this.OverflowInventory.WriteInventory(writer);
        ItemFile.SerialiseItem(this.Exemplar, writer);
        if (this.PodType != null)
        {
            writer.Write(true);
            ItemFile.SerialiseItem(this.PodType, writer);
        }
        else
            writer.Write(false);
    }

    public override void Read(BinaryReader reader, int entityVersion)
    {
        switch (entityVersion)
        {
            case 0:
                int itemcount = reader.ReadInt32();
                for (int index = 0; index < itemcount; ++index)
                    this.LocalInventory.AddItem(ItemFile.DeserialiseItem(reader));
                double num3 = (double)reader.ReadSingle();
                double num4 = (double)reader.ReadSingle();
                double num5 = (double)reader.ReadSingle();
                double num6 = (double)reader.ReadSingle();
                double num7 = (double)reader.ReadSingle();
                double num8 = (double)reader.ReadSingle();
                double num9 = (double)reader.ReadSingle();
                this.Exemplar = ItemFile.DeserialiseItem(reader);
                this.DroneInventory.AddItem(ItemFile.DeserialiseItem(reader));
                this.meState = MSManufacturer.eState.Unknown;
                this.mbHoloPreviewDirty = true;
                if (this.DroneInventory.Inventory[0] == null || this.DroneInventory.Inventory[0].mnItemID != -1 || this.DroneInventory.Inventory[0].mType == ItemType.ItemCubeStack)
                    return;
                Debug.LogWarning("Error, saved MSOP had illegal item!");
                this.DroneInventory.RemoveAnySingle();
                break;
            case 1:
                this.LocalInventory.ReadInventory(reader);
                this.DroneInventory.ReadInventory(reader);
                this.OverflowInventory.ReadInventory(reader);
                this.Exemplar = ItemFile.DeserialiseItem(reader);
                this.meState = MSManufacturer.eState.Unknown;
                break;
            case 2:
                this.LocalInventory.ReadInventory(reader);
                this.DroneInventory.ReadInventory(reader);
                this.OverflowInventory.ReadInventory(reader);
                this.Exemplar = ItemFile.DeserialiseItem(reader);
                this.meState = MSManufacturer.eState.Unknown;
                if (reader.ReadBoolean())
                {
                    ItemBase item = ItemFile.DeserialiseItem(reader);
                    if (item == null || item.mnItemID == -1)
                        Debug.LogError("MSManufacturer loaded in with bad pod type");
                    else
                        this.PodType = item;
                    if (this.Exemplar.mnItemID > 399 && this.Exemplar.mnItemID < 407)
                        this.RecipeIndex = 1;
                }
                break;
        }
    }

    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters((SegmentEntity)this);
        parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.yellow;
        return holobase.CreateHolobaseEntity(parameters);
    }

    public enum eState
    {
        Unknown,
        LookingForAttachedStorage,
        Idling,
        SendingDrone,
        RetrievingDrone,
        DroppingOff,
    }

    public enum AttachedMachine
    {
        None,
        GAC,
        MPlant,
        Lab,
        ResearchAssembler,
    }
}

public static class CrateDistanceLockOut
{
    public static bool CheckLock { get; set; }
}
