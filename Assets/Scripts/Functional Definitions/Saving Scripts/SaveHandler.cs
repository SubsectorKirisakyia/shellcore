﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
public class SaveHandler : MonoBehaviour {

	public PlayerCore player;
	PlayerSave save;
	// Use this for initialization
	void Awake() {
		string currentPath;
		if(!File.Exists(Application.persistentDataPath + "\\CurrentSavePath")) {
			currentPath = Application.persistentDataPath + "\\TestSave";
		}
		else currentPath = File.ReadAllLines(Application.persistentDataPath + "\\CurrentSavePath")[0];
		if(File.Exists(currentPath)) {
			string json = File.ReadAllText(currentPath);
			save = JsonUtility.FromJson<PlayerSave>(json);
			player.blueprint = ScriptableObject.CreateInstance<EntityBlueprint>();
			player.blueprint.name = "Player Save Blueprint";
			if(save.currentPlayerBlueprint != null && save.currentPlayerBlueprint != "") {
				JsonUtility.FromJsonOverwrite(save.currentPlayerBlueprint, player.blueprint);
			} else {
				player.blueprint.baseRegen = new float[] {60,0,60};
				player.blueprint.shellHealth = new float[] {1000,250,500};
				player.blueprint.coreSpriteID = "core1_light";
				player.blueprint.coreShellSpriteID = "core1_shell";
			}
			player.abilityCaps = save.abilityCaps;
			player.shards = save.shards;
			player.cursave = save;
			player.credits = save.credits;
			if(save.presetBlueprints.Length != 5) {
				save.presetBlueprints = new string[5];
			}
		} else {
			save = new PlayerSave();
			save.presetBlueprints = new string[5];
			save.currentHealths = new float[] {1000,250,500};
			save.partInventory = new List<EntityBlueprint.PartInfo>();

			player.blueprint = ScriptableObject.CreateInstance<EntityBlueprint>();
			player.blueprint.name = "Player Save Blueprint";
			player.blueprint.baseRegen = new float[] {60,0,60};
			player.blueprint.shellHealth = new float[] {1000,250,500};
			player.blueprint.parts = new List<EntityBlueprint.PartInfo>();
			player.blueprint.coreSpriteID = "core1_light";
			player.blueprint.coreShellSpriteID = "core1_shell";
			player.cursave = save;
			player.abilityCaps = new int[] {10, 10, 10, 10};
		}
	}
	
	public void Save() {
		save.timePlayed += Time.timeSinceLevelLoad / 60;
		string currentPath = File.ReadAllLines(Application.persistentDataPath + "\\CurrentSavePath")[0];
		save.position = player.transform.position;
		save.currentHealths = player.currentHealth;
		if(player.currentHealth[1] <= 0) save.currentHealths = player.GetMaxHealth();
		save.currentPlayerBlueprint = JsonUtility.ToJson(player.blueprint);
		save.credits = player.credits;
		save.abilityCaps = player.abilityCaps;
		save.shards = player.shards;
		string saveJson = JsonUtility.ToJson(save);
		File.WriteAllText(currentPath, saveJson);
	}
}
