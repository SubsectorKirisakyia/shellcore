﻿using NodeEditorFramework.Utilities;
using UnityEngine;

namespace NodeEditorFramework.Standard
{
    [Node(false, "Dialogue/End Dialogue")]
    public class EndDialogue : Node
    {
        public override string GetName
        {
            get { return "EndDialogue"; }
        }

        public override string Title
        {
            get { return "End Dialogue"; }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(200f, 100f); }
        }

        public override bool AutoLayout
        {
            get { return true; }
        }

        public override bool AllowRecursion
        {
            get { return true; }
        }

        ConnectionKnobAttribute OutStyle = new ConnectionKnobAttribute("Output", Direction.Out, "TaskFlow", ConnectionCount.Single, NodeSide.Right);

        [ConnectionKnob("Input", Direction.In, "Dialogue", NodeSide.Left)]
        public ConnectionKnob input;

        public ConnectionKnob output;

        public bool jumpToStart = false; // This is now necessary :)
        public bool openBuilder = false;
        public bool openTrader = false;
        public string traderJSON = null;
        public NodeEditorGUI.NodeEditorState state;

        public override void NodeGUI()
        {
            if (NodeEditorGUI.state == NodeEditorGUI.NodeEditorState.Dialogue && outputKnobs.Count > 0)
            {
                DeleteConnectionPort(outputKnobs[0]);
                output = null;
            }
            else if ((NodeEditorGUI.state != NodeEditorGUI.NodeEditorState.Dialogue) && (output == null) && !jumpToStart)
            {
                if (outputKnobs.Count > 0) 
		{
		    output = outputKnobs[0];
		}
		else
	    	{
		    output = CreateConnectionKnob(OutStyle);
		}
            }

            GUILayout.BeginHorizontal();
            input.DisplayLayout();
	    if (output)
	    {
		output.DisplayLayout();
	    }
            GUILayout.EndHorizontal();
            jumpToStart = RTEditorGUI.Toggle(jumpToStart, "Jump to start");
            if (jumpToStart && outputKnobs.Count > 0)
            {
                DeleteConnectionPort(outputKnobs[0]);
                output = null;
            }

            GUILayout.BeginHorizontal();
            openBuilder = RTEditorGUI.Toggle(openBuilder, "Open Yard");
            if (openBuilder == true)
            {
                openTrader = false;
            }
            GUILayout.EndHorizontal();
            if (openTrader = RTEditorGUI.Toggle(openTrader, "Open Trader"))
            {
                openBuilder = false;
                GUILayout.Label("Trader Inventory JSON");
                GUILayout.BeginHorizontal();
                traderJSON = GUILayout.TextArea(traderJSON);
                GUILayout.EndHorizontal();
            }
        }

        public override int Traverse()
        {
            IDialogueOverrideHandler handler = null;
            if (state != NodeEditorGUI.NodeEditorState.Dialogue)
            {
                handler = TaskManager.Instance;
            }
            else
            {
                handler = DialogueSystem.Instance;
            }

            if (handler as TaskManager)
            {
                if (!TaskManager.objectiveLocations.ContainsKey((Canvas as QuestCanvas).missionName))
                {
                    Debug.LogWarning($"{(Canvas as QuestCanvas).missionName} does not have objective locations");
                    return -1;
                }
                foreach (var objectiveLocation in TaskManager.objectiveLocations[(Canvas as QuestCanvas).missionName])
                {
                    if (objectiveLocation.followEntity &&
                        objectiveLocation.followEntity.ID == StartDialogueNode.missionCanvasNode?.EntityID)
                    {
                        TaskManager.objectiveLocations[(Canvas as QuestCanvas).missionName].Remove(objectiveLocation);
                        TaskManager.DrawObjectiveLocations();
                        break;
                    }
                }
            }

            var node = state != NodeEditorGUI.NodeEditorState.Dialogue
                ? StartDialogueNode.missionCanvasNode
                : StartDialogueNode.dialogueCanvasNode;

            if (jumpToStart)
            {
                handler.SetNode(node);
                if (!handler.GetInteractionOverrides().ContainsKey(node.EntityID))
                {
                    Debug.LogWarning($"{node.EntityID} is not in the interaction dictionary despite dialogue being started on it.");
                    return -1;
                }
                handler.GetInteractionOverrides()[node.EntityID].Pop();
                if (handler is DialogueSystem || (!output || !output.connected()))
                {
                    DialogueSystem.Instance.DialogueViewTransitionOut();
                }

                if (openBuilder)
                {
                    DialogueSystem.Instance.OpenBuilder(SectorManager.instance.GetEntity(node.EntityID).transform.position);
                }

                if (openTrader)
                {
                    DialogueSystem.Instance.OpenTrader(SectorManager.instance.GetEntity(node.EntityID).transform.position,
                        JsonUtility.FromJson<ShipBuilder.TraderInventory>(traderJSON).parts);
                }

                return -1;
            }
            else
            {
                if (node && !string.IsNullOrEmpty(node.EntityID))
                {                
                    if (!handler.GetInteractionOverrides().ContainsKey(node.EntityID))
                    {
                        Debug.LogWarning($"{node.EntityID} is not in the interaction dictionary despite dialogue being started on it.");
                        return -1;
                    }
                    handler.GetInteractionOverrides()[node.EntityID].Pop();
                    DialogueSystem.Instance.DialogueViewTransitionOut();
                    if (node == StartDialogueNode.missionCanvasNode)
                    {
                        StartDialogueNode.missionCanvasNode = null;
                    }
                    else
                    {
                        StartDialogueNode.dialogueCanvasNode = null;
                    }
                }

                if (openBuilder)
                {
                    DialogueSystem.Instance.OpenBuilder(SectorManager.instance.GetEntity(node.EntityID).transform.position);
                }

                if (openTrader)
                {
                    DialogueSystem.Instance.OpenTrader(SectorManager.instance.GetEntity(node.EntityID).transform.position,
                        JsonUtility.FromJson<ShipBuilder.TraderInventory>(traderJSON).parts);
                }

                return outputKnobs.Count > 0 ? 0 : -1;
            }
        }
    }
}
