﻿using NodeEditorFramework.Utilities;
using UnityEngine;

namespace NodeEditorFramework.Standard
{
    [Node(false, "Actions/Set Part Drop Rate")]
    public class SetPartDropRateNode : Node
    {
        public override string GetName
        {
            get { return "SetPartDropRate"; }
        }

        public override string Title
        {
            get { return "Set Part Drop Rate"; }
        }

        public override bool AllowRecursion
        {
            get { return true; }
        }

        public override bool AutoLayout
        {
            get { return true; }
        }

        [ConnectionKnob("Output", Direction.Out, "TaskFlow", NodeSide.Right)]
        public ConnectionKnob output;

        [ConnectionKnob("Input", Direction.In, "TaskFlow", NodeSide.Left)]
        public ConnectionKnob input;

        public float dropRate;
        public string sectorName;
        public bool restoreOld;
        public static SectorManager.SectorLoadDelegate del;

        public override void NodeGUI()
        {
            GUILayout.BeginHorizontal();
            input.DisplayLayout();
            output.DisplayLayout();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (!(restoreOld = GUILayout.Toggle(restoreOld, "Restore old drop rate")))
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                dropRate = RTEditorGUI.FloatField("Drop Rate: ", dropRate);
                if (dropRate < 0 || dropRate > 1)
                {
                    dropRate = RTEditorGUI.FloatField("Drop Rate: ", 0);
                    Debug.LogWarning("Can't register this number!");
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Sector Name:");
                GUILayout.BeginHorizontal();
                sectorName = RTEditorGUI.TextField(sectorName, GUILayout.MinWidth(50));
            }

            GUILayout.EndHorizontal();
        }

        public override int Traverse()
        {
            if (!restoreOld)
            {
                if (del != null)
                {
                    SectorManager.OnSectorLoad -= del;
                    del = null;
                }

                Entity.partDropRate = dropRate;
                del = RestoreOldValue;
                SectorManager.OnSectorLoad += del;
            }
            else if (del != null)
            {
                SectorManager.OnSectorLoad -= del;
                del = null;
                Entity.partDropRate = Entity.DefaultPartRate;
            }

            return 0;
        }

        public void RestoreOldValue(string sectorName)
        {
            if (sectorName != this.sectorName)
            {
                if (Entity.partDropRate != Entity.DefaultPartRate) Debug.Log("Left part drop rate sector");
                Entity.partDropRate = Entity.DefaultPartRate;
            }
            else
            {
                Debug.Log("Entering part drop rate sector");
                Entity.partDropRate = dropRate;
            }
        }
    }
}
