using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public class RealTimeQasmProgram : MonoBehaviour
    {
        public TextAsset RealTimeQasmText;
        public float SecondsOffset = 0;
        public bool isRepeating = false;

        public List<RealTimeQasmInstruction> InstructionList { get; set; }

        // Start is called before the first frame update
        void Awake()
        {
            InstructionList = ParseBlock();
        }

        public void ResetBlock(TextAsset nProgram)
        {
            SecondsOffset = Time.time;

            RealTimeQasmText = nProgram;
            InstructionList = ParseBlock();
        }

        List<RealTimeQasmInstruction> ParseBlock()
        {
            List<RealTimeQasmInstruction> lrtqi = new List<RealTimeQasmInstruction>();

            if (RealTimeQasmText == null)
            {
                return lrtqi;
            }

            string[] programLines = RealTimeQasmText.text.Split('\n');

            for (int i = 0; i < programLines.Length; i++)
            {
                programLines[i] = programLines[i].Trim();
                if (string.IsNullOrEmpty(programLines[i]))
                {
                    continue;
                }

                string[] words = programLines[i].Trim().Split(' ');

                if (words[0] == "#")
                {
                    // Comment
                    continue;
                }

                RealTimeQasmInstruction instruction = new RealTimeQasmInstruction();

                instruction.LineNumber = i;

                if (words[0][0] == '+')
                {
                    instruction.IsRelativeTime = true;
                    words[0] = words[0].Substring(1);
                } else
                {
                    instruction.IsRelativeTime = false;
                }

                instruction.Time = float.Parse(words[0]);

                bool isControlled = false;
                bool isClassical = false;
                int tailArgs = 0;

                switch (words[1].Trim().ToUpperInvariant())
                {
                    case "RAND":
                        instruction.Gate = QasmInstruction.RAND;
                        break;
                    case "SETCLOCK":
                        instruction.Gate = QasmInstruction.SETCLOCK;
                        isClassical = true;
                        tailArgs = 1;
                        break;
                    case "X":
                        instruction.Gate = QasmInstruction.X;
                        break;
                    case "Y":
                        instruction.Gate = QasmInstruction.Y;
                        break;
                    case "Z":
                        instruction.Gate = QasmInstruction.Z;
                        break;
                    case "H":
                        instruction.Gate = QasmInstruction.H;
                        break;
                    case "S":
                        instruction.Gate = QasmInstruction.S;
                        break;
                    case "T":
                        instruction.Gate = QasmInstruction.T;
                        break;
                    case "ADJS":
                        instruction.Gate = QasmInstruction.ADJS;
                        break;
                    case "ADJT":
                        instruction.Gate = QasmInstruction.ADJT;
                        break;
                    case "U":
                        instruction.Gate = QasmInstruction.U;
                        tailArgs = 3;
                        break;
                    case "EXP":
                        instruction.Gate = QasmInstruction.EXP;
                        tailArgs = 1;
                        break;
                    case "RX":
                        instruction.Gate = QasmInstruction.RX;
                        tailArgs = 1;
                        break;
                    case "RY":
                        instruction.Gate = QasmInstruction.RY;
                        tailArgs = 1;
                        break;
                    case "RZ":
                        instruction.Gate = QasmInstruction.RZ;
                        tailArgs = 1;
                        break;
                    case "MCX":
                        instruction.Gate = QasmInstruction.MCX;
                        isControlled = true;
                        break;
                    case "MCY":
                        instruction.Gate = QasmInstruction.MCY;
                        isControlled = true;
                        break;
                    case "MCZ":
                        instruction.Gate = QasmInstruction.MCZ;
                        isControlled = true;
                        break;
                    case "MCH":
                        instruction.Gate = QasmInstruction.MCH;
                        isControlled = true;
                        break;
                    case "MCS":
                        instruction.Gate = QasmInstruction.MCS;
                        isControlled = true;
                        break;
                    case "MCT":
                        instruction.Gate = QasmInstruction.MCT;
                        isControlled = true;
                        break;
                    case "MCADJS":
                        instruction.Gate = QasmInstruction.MCADJS;
                        isControlled = true;
                        break;
                    case "MCADJT":
                        instruction.Gate = QasmInstruction.MCADJT;
                        isControlled = true;
                        break;
                    case "MCU":
                        instruction.Gate = QasmInstruction.MCU;
                        isControlled = true;
                        tailArgs = 3;
                        break;
                    case "MCEXP":
                        instruction.Gate = QasmInstruction.MCEXP;
                        isControlled = true;
                        tailArgs = 1;
                        break;
                    case "MCRX":
                        instruction.Gate = QasmInstruction.MCRX;
                        isControlled = true;
                        tailArgs = 1;
                        break;
                    case "MCRY":
                        instruction.Gate = QasmInstruction.MCRY;
                        isControlled = true;
                        tailArgs = 1;
                        break;
                    case "MCRZ":
                        instruction.Gate = QasmInstruction.MCRZ;
                        isControlled = true;
                        tailArgs = 1;
                        break;
                    case "M":
                        instruction.Gate = QasmInstruction.M;
                        tailArgs = 1;
                        break;
                    case "QSET":
                        instruction.Gate = QasmInstruction.QSET;
                        break;
                    case "QRESET":
                        instruction.Gate = QasmInstruction.QRESET;
                        break;
                    case "SET":
                        instruction.Gate = QasmInstruction.SET;
                        isClassical = true;
                        break;
                    case "RESET":
                        instruction.Gate = QasmInstruction.RESET;
                        isClassical = true;
                        break;
                    case "FLOAD":
                        instruction.Gate = QasmInstruction.FLOAD;
                        isClassical = true;
                        tailArgs = 1;
                        break;
                    case "ALOAD":
                        instruction.Gate = QasmInstruction.ALOAD;
                        isClassical = true;
                        tailArgs = 1;
                        break;
                    case "NOT":
                        instruction.Gate = QasmInstruction.NOT;
                        isClassical = true;
                        break;
                    case "AND":
                        instruction.Gate = QasmInstruction.AND;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "OR":
                        instruction.Gate = QasmInstruction.OR;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "XOR":
                        instruction.Gate = QasmInstruction.XOR;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "NAND":
                        instruction.Gate = QasmInstruction.NAND;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "NOR":
                        instruction.Gate = QasmInstruction.NOR;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "XNOR":
                        instruction.Gate = QasmInstruction.XNOR;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "ADD":
                        instruction.Gate = QasmInstruction.ADD;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "SUB":
                        instruction.Gate = QasmInstruction.SUB;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "MUL":
                        instruction.Gate = QasmInstruction.MUL;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "DIV":
                        instruction.Gate = QasmInstruction.DIV;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "CMPEQ":
                        instruction.Gate = QasmInstruction.CMPEQ;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "CMPGR":
                        instruction.Gate = QasmInstruction.CMPGR;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "IF":
                        instruction.Gate = QasmInstruction.IF;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "ELSE":
                        instruction.Gate = QasmInstruction.ELSE;
                        lrtqi.Add(instruction);
                        continue;
                    case "ENDIF":
                        instruction.Gate = QasmInstruction.ENDIF;
                        lrtqi.Add(instruction);
                        continue;
                    case "FOR":
                        instruction.Gate = QasmInstruction.FOR;
                        isClassical = true;
                        tailArgs = 2;
                        break;
                    case "WHILE":
                        instruction.Gate = QasmInstruction.WHILE;
                        isControlled = true;
                        isClassical = true;
                        break;
                    case "DO":
                        instruction.Gate = QasmInstruction.DO;
                        lrtqi.Add(instruction);
                        continue;
                    case "LOOP":
                        instruction.Gate = QasmInstruction.LOOP;
                        lrtqi.Add(instruction);
                        continue;
                    case "I":
                        instruction.Gate = QasmInstruction.I;
                        lrtqi.Add(instruction);
                        continue;
                    default:
                        throw new FormatException("Could not parse RealTimeQasmText, line " + (i + 1).ToString());
                }

                int targetIndex;

                if ((instruction.Gate == QasmInstruction.IF)
                    || (instruction.Gate == QasmInstruction.WHILE)
                    || (instruction.Gate == QasmInstruction.SETCLOCK))
                {
                    targetIndex = words.Length;
                }
                else
                {
                    targetIndex = words.Length - (tailArgs + 1);

                    if (words[targetIndex][0] == '$')
                    {
                        instruction.IsIndirectTarget = true;
                        words[targetIndex] = words[targetIndex].Substring(1);
                    } else
                    {
                        instruction.IsIndirectTarget = false;
                    }

                    uint targetValue = uint.Parse(words[targetIndex]);
                    if (isClassical)
                    {
                        instruction.ClassicalTarget = targetValue;
                    }
                    else
                    {
                        instruction.Target = targetValue;
                    }
                }

                if (instruction.Gate == QasmInstruction.M)
                {
                    instruction.ClassicalTarget = uint.Parse(words[targetIndex + 1]);
                    lrtqi.Add(instruction);
                    continue;
                } else if ((instruction.Gate == QasmInstruction.FLOAD)
                    || (instruction.Gate == QasmInstruction.SETCLOCK))
                {
                    instruction.FloatValue = float.Parse(words[targetIndex + 1]);
                    lrtqi.Add(instruction);
                    continue;
                } else if (instruction.Gate == QasmInstruction.ALOAD)
                {
                    instruction.IntValue = int.Parse(words[targetIndex + 1]);
                    lrtqi.Add(instruction);
                    continue;
                }

                if (isControlled)
                {
                    if (targetIndex > 2)
                    {
                        instruction.Controls = new uint[targetIndex - 2];
                        instruction.IsIndirectControls = new bool[targetIndex - 2];
                    }
                    for (int j = 2; j < targetIndex; j++)
                    {
                        if (words[j][0] == '$')
                        {
                            instruction.IsIndirectControls[j - 2] = true;
                            words[j] = words[j].Substring(1);
                        }
                        else
                        {
                            instruction.IsIndirectControls[j - 2] = false;
                        }

                        instruction.Controls[j - 2] = uint.Parse(words[j]);
                    }
                }

                if (tailArgs > 0)
                {
                    instruction.FloatIndices = new uint[tailArgs];
                    instruction.IsIndirectFloatIndices = new bool[tailArgs];
                }

                int wordOffset;
                for (int j = 0; j < tailArgs; j++)
                {
                    wordOffset = targetIndex + j + 1;
                    if (words[wordOffset][0] == '$')
                    {
                        instruction.IsIndirectControls[j] = true;
                        words[wordOffset] = words[wordOffset].Substring(1);
                    }
                    else
                    {
                        instruction.IsIndirectControls[j] = false;
                    }

                    instruction.FloatIndices[j] = uint.Parse(words[wordOffset]);
                }

                lrtqi.Add(instruction);
            }

            return lrtqi;
        }
    }
}