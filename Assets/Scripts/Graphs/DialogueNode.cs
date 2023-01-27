﻿using System.Collections.Generic;
using NodeEditorFramework.Utilities;
using UnityEngine;

namespace NodeEditorFramework.Standard
{
    [Node(false, "Dialogue/Dialogue Node")]
    public class DialogueNode : Node
    {
        public override string GetName
        {
            get { return "DialogueNode"; }
        }

        public override string Title
        {
            get { return "Dialogue"; }
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

        [ConnectionKnob("Input Left", Direction.In, "Dialogue", NodeSide.Left)]
        public ConnectionKnob input;

        [ConnectionKnob("Cancel", Direction.Out, "TaskFlow", NodeSide.Right)]
        public ConnectionKnob cancel;

        ConnectionKnobAttribute outputAttribute = new ConnectionKnobAttribute("Output ", Direction.Out, "Dialogue", ConnectionCount.Single, NodeSide.Right);

        public bool useEntityColor = true;
        public Color textColor = Color.white;
        public string text;
        public List<string> answers = new List<string>();
        public bool customDialogueSpeed;

        // deprecated speed field
        public double speed;
        private double oldSpeed;
        public float floatSpeed;

        public NodeEditorGUI.NodeEditorState state;

        public override void NodeGUI()
        {
            GUILayout.Label("Text:");
            text = GUILayout.TextArea(text, GUILayout.ExpandHeight(false));
            if (!(useEntityColor = GUILayout.Toggle(useEntityColor, "Use entity color")))
            {
                GUILayout.Label("Text Color:");
                float r, g, b;
                GUILayout.BeginHorizontal();
                r = RTEditorGUI.FloatField(textColor.r);
                if (textColor.r < 0 || textColor.r > 1)
                {
                    r = RTEditorGUI.FloatField(textColor.r = 1);
                    Debug.LogWarning("Can't register this numbers!");
                }
                g = RTEditorGUI.FloatField(textColor.g);
                if (textColor.g < 0 || textColor.g > 1)
                {
                    g = RTEditorGUI.FloatField(textColor.g = 1);
                    Debug.LogWarning("Can't register this numbers!");
                }
                b = RTEditorGUI.FloatField(textColor.b);
                if (textColor.b < 0 || textColor.b > 1)
                {
                    b = RTEditorGUI.FloatField(textColor.b = 1);
                    Debug.LogWarning("Can't register this numbers!");
                }
                GUILayout.EndHorizontal();
                textColor = new Color(r, g, b);
            }

            if (customDialogueSpeed = GUILayout.Toggle(customDialogueSpeed, "Use custom typing speed", GUILayout.MinWidth(40f)))
            {
                floatSpeed = RTEditorGUI.FloatField("Typing speed (seconds)", floatSpeed);
                if (speed != -2)
                {
                    floatSpeed = (float)speed;
                    speed = -2;
                }
            }

            GUILayout.Label("Answers:");
            for (int i = 0; i < answers.Count; i++)
            {
                RTEditorGUI.Seperator();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("x", GUILayout.ExpandWidth(false)))
                {
                    DeleteConnectionPort(outputPorts[i + 1]);
                    answers.RemoveAt(i);
                    i--;
                    if (i == -1)
                    {
                        break;
                    }

                    continue;
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                answers[i] = RTEditorGUI.TextField(answers[i]);
                outputKnobs[i + 1].DisplayLayout();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", GUILayout.ExpandWidth(false), GUILayout.MinWidth(100f)))
            {
                CreateConnectionKnob(outputAttribute);
                answers.Add("");
            }

            GUILayout.EndHorizontal();
            cancel.DisplayLayout();
        }

        public void OnClick(int index)
        {
            DialogueSystem.OnDialogueCancel -= OnCancel;
            DialogueSystem.OnDialogueEnd -= OnClick;
            if (customDialogueSpeed)
            {
                DialogueSystem.Instance.timeBetweenCharacters = oldSpeed;
            }

            if (outputPorts[index].connected())
            {
                TaskManager.Instance.setNode(outputPorts[index]);
            }
            else
            {
                TaskManager.Instance.setNode(state != NodeEditorGUI.NodeEditorState.Dialogue
                    ? StartDialogueNode.missionCanvasNode
                    : StartDialogueNode.dialogueCanvasNode);
            }
        }

        public void OnCancel()
        {
            DialogueSystem.OnDialogueCancel -= OnCancel;
            DialogueSystem.OnDialogueEnd -= OnClick;
            if (customDialogueSpeed)
            {
                DialogueSystem.Instance.timeBetweenCharacters = oldSpeed;
            }

            if (cancel.connected())
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

                var node = state != NodeEditorGUI.NodeEditorState.Dialogue
                    ? StartDialogueNode.missionCanvasNode
                    : StartDialogueNode.dialogueCanvasNode;
                if (node && !string.IsNullOrEmpty(node.EntityID))
                {
                    handler.GetInteractionOverrides()[node.EntityID].Pop();
                    // DialogueSystem.Instance.DialogueViewTransitionOut();
                    if (node == StartDialogueNode.missionCanvasNode)
                    {
                        StartDialogueNode.missionCanvasNode = null;
                    }
                    else
                    {
                        StartDialogueNode.dialogueCanvasNode = null;
                    }
                }
                TaskManager.Instance.setNode(cancel);
            }
        }

        public override int Traverse()
        {
            oldSpeed = DialogueSystem.Instance.timeBetweenCharacters;
            if (customDialogueSpeed)
            {
                DialogueSystem.Instance.timeBetweenCharacters = floatSpeed;
            }

            if (state != NodeEditorGUI.NodeEditorState.Dialogue)
            {
                DialogueSystem.ShowDialogueNode(this, TaskManager.GetSpeaker());
            }
            else
            {
                DialogueSystem.ShowDialogueNode(this, DialogueSystem.GetSpeaker());
            }

            DialogueSystem.OnDialogueEnd += OnClick;
            DialogueSystem.OnDialogueCancel += OnCancel;
            return -1;
        }
    }
}
