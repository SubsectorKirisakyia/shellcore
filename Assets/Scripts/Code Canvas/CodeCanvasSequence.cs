using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CodeTraverser;

public class CodeCanvasSequence : MonoBehaviour
{
    public enum InstructionCommand
    {
        SetInteraction,
        StartCutscene,
        EndCutscene,
        Log,
        Call
    }
    public struct Instruction
    {
        public InstructionCommand command;
        public string arguments;
        public string GetArgument(string key)
        {
            var args = arguments.Split(";");
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == key) 
                {
                    return args[i+1];
                }
            }
            return null;
        }

        // TODO: escape semicolons
        public void AddArgument(string key, string value)
        {
            if (arguments == null) arguments = $"{key};{value}";
            arguments += $";{key};{value}";
        }
    }


    public struct Sequence
    {
        public List<Instruction> instructions;
    }

    private struct MissionTrigger
    {
        public string name;
        public string rank;
        public List<string> prerequisites;
        public Sequence sequence;
    }

    public static void RunSequence (Sequence seq, CodeTraverser traverser, Context context)
    {
        foreach (var inst in seq.instructions)
        {
            switch (inst.command)
            {
                case InstructionCommand.SetInteraction:
                    InteractAction action = new InteractAction();
                    var entityID = inst.GetArgument("entityID");
                    action.action = new UnityEngine.Events.UnityAction(() =>
                        {
                            switch (context.type)
                            {
                                case TriggerType.Mission:
                                    TaskManager.Instance.SetSpeakerID(entityID);
                                    break;
                                default:
                                    DialogueSystem.Instance.SetSpeakerID(entityID);
                                    break;
                            }
                            
                            DialogueSystem.StartDialogue(traverser.dialogues[inst.GetArgument("dialogueID")], null, context);
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
                    break;
                case InstructionCommand.Call:
                    var s = traverser.GetFunction(inst.GetArgument("name"));
                    RunSequence(s, traverser, context);
                    break;
            }
        }
    }

    public static Sequence ParseSequence(int index, string line)
    {
        var seq = new Sequence();
        seq.instructions = new List<Instruction>();

        List<string> stx = null;
        bool skipToComma = false;
        int brax = 0;

        index = CodeTraverser.GetNextOccurenceInScope(index, line, stx, ref brax, ref skipToComma, '(', ')');
        for (int i = index; i < line.Length; i = CodeTraverser.GetNextOccurenceInScope(i, line, stx, ref brax, ref skipToComma, '(', ')'))
        {
            skipToComma = true;
            var lineSubstr = line.Substring(i);
            Debug.LogWarning(lineSubstr);
            if (lineSubstr.StartsWith("SetInteraction"))
            {
                seq.instructions.Add(ParseSetInteraction(i, line));
            }
            else if (lineSubstr.StartsWith("Call"))
            {
                var funcName = lineSubstr.Substring(5);
                funcName = funcName.Substring(0, funcName.IndexOf(")"));
                var inst = new Instruction();
                inst.AddArgument("name", funcName);
                inst.command = InstructionCommand.Call;
                seq.instructions.Add(inst);
                // TODO: Function call recursion
            }
        }

        return seq;
    }

    private static Instruction ParseSetInteraction(int index, string line)
    {
        var inst = new Instruction();
        inst.command = InstructionCommand.SetInteraction;
        inst.arguments = "";

        List<string> stx = new List<string>()
        {
            "entityID=",
            "dialogueID=",
        };
        bool skipToComma = false;
        int brax = 0;

        index = CodeTraverser.GetNextOccurenceInScope(index, line, stx, ref brax, ref skipToComma, '(', ')');
        for (int i = index; i < line.Length; i = CodeTraverser.GetNextOccurenceInScope(i, line, stx, ref brax, ref skipToComma, '(', ')'))
        {
            skipToComma = true;
            var lineSubstr = line.Substring(i);
            var val = lineSubstr.Split(",")[0].Split("=")[1];

            if (lineSubstr.StartsWith("entityID="))
            {
                inst.AddArgument("entityID", val);
            }
            else if (lineSubstr.StartsWith("dialogueID="))
            {
                inst.AddArgument("dialogueID", val);
            }

        }
        return inst;
    }

}
