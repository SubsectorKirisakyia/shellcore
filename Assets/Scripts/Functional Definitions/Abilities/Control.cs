﻿using System.Collections.Generic;
using UnityEngine;

public class Control : PassiveAbility
{
    const float healthAddition = 200;
    public const float damageFactor = 0.05F;
    public const float baseControlFractionBoost = 0.2F;
    List<Entity> boosted = new List<Entity>();

    protected override void Awake()
    {
        base.Awake();
        ID = AbilityID.Control;
    }

    public override void Deactivate()
    {
        for (int i = 0; i < boosted.Count; i++)
        {
            var entity = boosted[i];
            if (!entity)
            {
                continue;
            }

            entity.ControlStacks -= abilityTier;
        }

        base.Deactivate();
        Entity.OnEntitySpawn -= EntitySpawn;
    }

    protected override void Execute()
    {
        for (int i = 0; i < AIData.entities.Count; i++)
        {
            if (!AIData.entities[i].GetIsDead())
            {
                Enhance(AIData.entities[i]);
            }
        }

        Entity.OnEntitySpawn += EntitySpawn;
    }

    void EntitySpawn(Entity entity)
    {
        if (!entity.GetIsDead())
        {
            Enhance(entity);
        }
    }

    void Enhance(Entity entity)
    {
        if (entity.faction.factionID == Core.faction.factionID && entity != Core && (Core is IOwner owner) 
        && (entity is IOwnable ownable) 
        && ownable.GetOwner() != null 
        && !ownable.GetOwner().Equals(null) 
        && ownable.GetOwner().Equals(owner) 
        && !boosted.Contains(entity))
        {
            entity.ControlStacks += abilityTier;
            boosted.Add(entity);
        }
    }
}
