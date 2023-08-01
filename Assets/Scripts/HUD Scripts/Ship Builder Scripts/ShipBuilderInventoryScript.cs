﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;



public class ShipBuilderInventoryScript : ShipBuilderInventoryBase
{
    public GameObject SBPrefab;
    public ShipBuilderCursorScript cursor;
    public BuilderMode mode;

    int count;

    protected override void Start()
    {
        base.Start();
        val.text = count + "";
        val.enabled = (mode == BuilderMode.Yard || mode == BuilderMode.Workshop);
        if (mode == BuilderMode.Workshop)
        {
            int size = ResourceManager.GetAsset<PartBlueprint>(part.partID).size;
            var active = (ShipBuilder.instance.GetDroneWorkshopSelectPhase() && part.abilityID == 10) || 
                (!ShipBuilder.instance.GetDroneWorkshopSelectPhase() && size == 0 && part.abilityID != 10);
            gameObject.SetActive(active);
        }
        // button border size is handled specifically by the grid layout components
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
#if UNITY_EDITOR
#endif
        }

        var dwNotSelectionPhase = mode == BuilderMode.Workshop && !ShipBuilder.instance.GetDroneWorkshopSelectPhase();
        var minCount = dwNotSelectionPhase ? -1 : ShipBuilder.instance.GetDronePartCount();
        if (count > minCount)
        {
            if (mode == BuilderMode.Workshop)
            {
                if (ShipBuilder.instance.GetDroneWorkshopSelectPhase())
                {
                    if (string.IsNullOrEmpty(part.playerGivenName))
                    {
                        ShipBuilder.instance.OpenNameWindow(this);
                    }
                    else
                    {
                        var spawnData = DroneUtilities.GetDroneSpawnDataByShorthand(part.secondaryData);
                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            var existingParts = SectorManager.TryGettingEntityBlueprint(spawnData.drone).parts;
                            var parts = DroneUtilities.GetDefaultBlueprint(spawnData.type).parts;
                            if (ShipBuilder.instance.ContainsParts(parts, existingParts))
                            {
                                ShipBuilder.instance.SwapDroneParts(parts, existingParts, this, spawnData.type);
                            }
                        }
                        else if (Input.GetKey(KeyCode.LeftShift))
                        {
                            var p = ShipBuilder.CullSpatialValues(part);
                            p.secondaryData = DroneUtilities.GetDefaultSecondaryDataByType(spawnData.type);
                            p.playerGivenName = "";
                            var existingParts = SectorManager.TryGettingEntityBlueprint(spawnData.drone).parts;
                            var defaultParts = DroneUtilities.GetDefaultBlueprint(spawnData.type).parts;
                            if (
                                ShipBuilder.instance.ContainsParts(new List<EntityBlueprint.PartInfo>() {p}) &&
                                ShipBuilder.instance.ContainsParts(existingParts, defaultParts))
                            {
                                ShipBuilder.instance.SwapDroneParts(existingParts, defaultParts, this, spawnData.type, true);
                            }
                        }
                        else 
                            ShipBuilder.instance.InitializeDronePart(part);
                    }
                    return;
                }
            }


            var builderPart = InstantiatePart();
            DecrementCount(false, dwNotSelectionPhase);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (mode == BuilderMode.Yard && cursor.builder.GetMode() == BuilderMode.Trader)
                {
                    cursor.builder.DispatchPart(builderPart, ShipBuilder.TransferMode.Sell);
                    return;
                }
                else if (mode == BuilderMode.Trader)
                {
                    cursor.buildCost += EntityBlueprint.GetPartValue(builderPart.info);
                    cursor.builder.DispatchPart(builderPart, ShipBuilder.TransferMode.Buy);
                    return;
                }
            }

            ShipBuilderPart symmetryPart = count > minCount && cursor.symmetryMode != ShipBuilderCursorScript.SymmetryMode.Off ? InstantiatePart() : null;
            if (symmetryPart)
            {
                //if(cursor.symmetryMode == ShipBuilderCursorScript.SymmetryMode.X)
                symmetryPart.info.mirrored = !builderPart.info.mirrored;
                if (cursor.symmetryMode == ShipBuilderCursorScript.SymmetryMode.Y)
                {
                    symmetryPart.info.rotation = 180;
                }
            }

            cursor.GrabPart(builderPart, symmetryPart);
            if (symmetryPart)
            {
                DecrementCount(false, dwNotSelectionPhase);
            }

            cursor.buildValue += EntityBlueprint.GetPartValue(part);
            if (symmetryPart)
            {
                cursor.buildValue += EntityBlueprint.GetPartValue(part);
            }

            if (mode == BuilderMode.Trader)
            {
                cursor.buildCost += EntityBlueprint.GetPartValue(part);
                if (symmetryPart)
                {
                    cursor.buildCost += EntityBlueprint.GetPartValue(part);
                }
            }
        }
    }

    private ShipBuilderPart InstantiatePart()
    {
        var builderPart = Instantiate(SBPrefab, cursor.transform.parent).GetComponent<ShipBuilderPart>();
        builderPart.info = part;
        builderPart.cursorScript = cursor;
        builderPart.mode = mode;
        builderPart.Initialize();
        cursor.parts.Add(builderPart);
        return builderPart;
    }

    public void IncrementCount(bool obeyDroneCount = false)
    {
        if (obeyDroneCount)
        {
            for (int i = 0; i < ShipBuilder.instance.GetDronePartCount(); i++)
            {
                count++;
            }
        }
        else count++;
    }

    public void DecrementCount(bool destroyIfZero = false, bool obeyDroneCount = false)
    {
        if (obeyDroneCount)
        {
            for (int i = 0; i < ShipBuilder.instance.GetDronePartCount(); i++)
            {
                count--;
            }
        }
        else count--;
        if (destroyIfZero && count <= 0)
        {
            ShipBuilder.instance.RemoveKeyFromPartDict(part);
            Destroy(gameObject);
        }
    }

    public int GetCount()
    {
        return count;
    }

    void Update()
    {
        val.text = count.ToString();
        image.color = count > 0 ? activeColor : Color.gray;
        if (shooter)
        {
            shooter.color = count > 0 ? activeColor : Color.gray;
        }
    }
}
