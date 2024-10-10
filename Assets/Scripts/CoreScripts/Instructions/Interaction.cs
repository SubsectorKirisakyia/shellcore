using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CoreScriptsManager;
using static CoreScriptsSequence;

public class Interaction : MonoBehaviour
{
    public static void ShowAlert(string text, string soundID)
    {
        SectorManager.instance.player.alerter.showMessage(text, soundID);
    }

    public static void ClearInteraction(Context context, string entityID)
    {
        switch (context.type)
        {
            case TriggerType.Mission:
                TaskManager.Instance.ClearInteractionOverrides(entityID);
                break;
            default:
                DialogueSystem.Instance.ClearInteractionOverrides(entityID);
                break;
        }
    }

    public static void StartDialogue(Context context, string dialogueID)
    {
        DialogueSystem.StartDialogue(CoreScriptsManager.instance.dialogues[dialogueID], null, context);
    }

    public static void SetInteraction(Context context, string entityID, string dialogueID, Sequence sequence)
    {
        InteractAction action = new InteractAction();
            action.action = new UnityEngine.Events.UnityAction(() =>
                {
                    if (sequence.instructions != null)
                    {
                        CoreScriptsSequence.RunSequence(sequence, context);
                    }
                    
                    switch (context.type)
                    {
                        case TriggerType.Mission:
                            TaskManager.Instance.SetSpeakerID(entityID);
                            break;
                        default:
                            DialogueSystem.Instance.SetSpeakerID(entityID);
                            break;
                    }
                    
                    if (dialogueID != null && CoreScriptsManager.instance.dialogues.ContainsKey(dialogueID))
                        DialogueSystem.StartDialogue(CoreScriptsManager.instance.dialogues[dialogueID], null, context);
                });

            switch (context.type)
            {
                case TriggerType.Mission:
                    TaskManager.Instance.PushInteractionOverrides(entityID, action, null, context);
                    break;
                default:
                    DialogueSystem.Instance.PushInteractionOverrides(entityID, action, null);
                    break;
            }
    }

    // TODO: Allow passive dialogue to inherit entity color
    public static void PassiveDialogue(string id, string text, string soundType, bool onlyShowIfInParty, bool useEntityColor)
    {
        if (!onlyShowIfInParty || (PartyManager.instance.partyMembers.Exists(sc => sc.ID == id)))
        {
            int soundIndex;
            bool success = int.TryParse(soundType, out soundIndex);
            if (!success)
            {
                soundIndex = 1;
            }

            PassiveDialogueSystem.Instance.PushPassiveDialogue(id, text, soundIndex, useEntityColor);
        }
        else
        {
            Debug.Log("Party member not found, not pushing dialogue");
        }
    }
}
