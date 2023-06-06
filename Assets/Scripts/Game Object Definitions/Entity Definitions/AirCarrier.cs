﻿using System.Collections.Generic;
using UnityEngine;

public interface ICarrier : IOwner
{
    Vector3 GetSpawnPoint();
    bool GetIsInitialized();
    bool GetIsDead();
}

public class AirCarrier : AirConstruct, ICarrier
{
    private float coreAlertThreshold;
    private float shellAlertThreshold;

    int intrinsicCommandLimit = 0;
    public List<IOwnable> unitsCommanding = new List<IOwnable>();
    BattleZoneManager BZManager;

    public bool GetIsInitialized()
    {
        return initialized;
    }

    public Vector3 GetSpawnPoint()
    {
        var tmp = transform.position;
        tmp.y -= 3;
        return tmp;
    }

    protected override void Start()
    {
        category = EntityCategory.Station;
        base.Start();
        initialized = true;
        coreAlertThreshold = maxHealth[1] * 0.8f;
        shellAlertThreshold = maxHealth[0] * 0.8f;
        BZManager = GameObject.Find("SectorManager").GetComponent<BattleZoneManager>();
    }

    public List<IOwnable> GetUnitsCommanding()
    {
        return unitsCommanding;
    }

    public int GetTotalCommandLimit()
    {
        if (sectorMngr)
        {
            return intrinsicCommandLimit + sectorMngr.GetExtraCommandUnits(faction);
        }
        else
        {
            return intrinsicCommandLimit;
        }
    }

    public SectorManager GetSectorManager()
    {
        return sectorMngr;
    }


    protected override void OnDeath()
    {
        if (!MasterNetworkAdapter.lettingServerDecide)
        {
            if (MasterNetworkAdapter.mode != MasterNetworkAdapter.NetworkMode.Off && !MasterNetworkAdapter.lettingServerDecide
                && lastDamagedBy is ShellCore core && core.networkAdapter && core.networkAdapter.isPlayer.Value)
                {
                    HUDScript.AddScore(core.networkAdapter.playerName, 10);
                }
        
        }
        base.OnDeath();
    }

    protected override void Update()
    {
        if (initialized)
        {
            TickAbilitiesAsStation();
            base.Update();
            TargetManager.Enqueue(targeter);

            if (!SectorManager.instance.carriers.ContainsKey(faction)
                || (SectorManager.instance.carriers[faction] == null || SectorManager.instance.carriers[faction].Equals(null))
                || SectorManager.instance.carriers[faction].GetIsDead())
            {
                if (!GetIsDead())
                {
                    SectorManager.instance.carriers[faction] = this;
                }
            }
        }
    }

    public Draggable GetTractorTarget()
    {
        return null;
    }

    public int GetIntrinsicCommandLimit()
    {
        return intrinsicCommandLimit;
    }

    public void SetIntrinsicCommandLimit(int val)
    {
        intrinsicCommandLimit = val;
    }

    public override void TakeCoreDamage(float amount)
    {
        base.TakeCoreDamage(amount);
        if (currentHealth[1] < coreAlertThreshold && currentHealth[1] > 0)
        {
            int temp = (int)(Mathf.Floor((currentHealth[1] / maxHealth[1]) * 5) + 1) * 20;
            coreAlertThreshold -= (maxHealth[1] * 0.2f);
            if (BZManager) BZManager.AttemptAlertPlayers(faction, $"Carrier is at {temp}% core", "clip_alert");
        }
    }

    public override float TakeShellDamage(float amount, float shellPiercingFactor, Entity lastDamagedBy)
    {
        float residue = base.TakeShellDamage(amount, shellPiercingFactor, lastDamagedBy);
        if (currentHealth[0] < shellAlertThreshold && currentHealth[0] > 0)
        {
            int temp = (int)(Mathf.Floor((currentHealth[0] / maxHealth[0]) * 5) + 1) * 20;
            shellAlertThreshold -= (maxHealth[0] * 0.2f);
            if (BZManager) BZManager.AttemptAlertPlayers(faction, $"Carrier is at {temp}% shell", "clip_alert");
        }

        return residue;
    }
}
