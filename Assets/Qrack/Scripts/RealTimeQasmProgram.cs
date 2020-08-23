using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qrack
{
    public class RealTimeQasmProgram : MonoBehaviour
    {
        public TextAsset RealTimeQasmText;
        public bool doRepeat = false;

        public List<List<List<RealTimeQasmInstruction>>> ClockBlocks { get; private set; }

        // Start is called before the first frame update
        void Awake()
        {
            ResetBlock(RealTimeQasmText);
        }

        public void ResetBlock(TextAsset nProgram)
        {
            RealTimeQasmText = nProgram;
            ClockBlocks = SortTimes(ParseBlock());
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

                if (words[0][0] == '=')
                {
                    instruction.IsForcedSerial = true;
                }
                else
                {
                    instruction.IsForcedSerial = false;
                    instruction.Time = float.Parse(words[0]);
                }

                bool isControlled = false;

                int tailArgs = 0;

                switch (words[1].Trim().ToUpperInvariant())
                {
                    case "RAND":
                        instruction.Gate = QasmInstruction.RAND;
                        break;
                    case "SETCLOCK":
                        instruction.Gate = QasmInstruction.SETCLOCK;
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
                        break;
                    case "RESET":
                        instruction.Gate = QasmInstruction.RESET;
                        break;
                    case "FLOAD":
                        instruction.Gate = QasmInstruction.FLOAD;
                        tailArgs = 1;
                        break;
                    case "ALOAD":
                        instruction.Gate = QasmInstruction.ALOAD;
                        tailArgs = 1;
                        break;
                    case "NOT":
                        instruction.Gate = QasmInstruction.NOT;
                        break;
                    case "AND":
                        instruction.Gate = QasmInstruction.AND;
                        isControlled = true;
                        break;
                    case "OR":
                        instruction.Gate = QasmInstruction.OR;
                        isControlled = true;
                        break;
                    case "XOR":
                        instruction.Gate = QasmInstruction.XOR;
                        isControlled = true;
                        break;
                    case "NAND":
                        instruction.Gate = QasmInstruction.NAND;
                        isControlled = true;
                        break;
                    case "NOR":
                        instruction.Gate = QasmInstruction.NOR;
                        isControlled = true;
                        break;
                    case "XNOR":
                        instruction.Gate = QasmInstruction.XNOR;
                        isControlled = true;
                        break;
                    case "QAND":
                        instruction.Gate = QasmInstruction.QAND;
                        isControlled = true;
                        break;
                    case "QOR":
                        instruction.Gate = QasmInstruction.QOR;
                        isControlled = true;
                        break;
                    case "QXOR":
                        instruction.Gate = QasmInstruction.QXOR;
                        isControlled = true;
                        break;
                    case "QNAND":
                        instruction.Gate = QasmInstruction.QNAND;
                        isControlled = true;
                        break;
                    case "QNOR":
                        instruction.Gate = QasmInstruction.QNOR;
                        isControlled = true;
                        break;
                    case "QXNOR":
                        instruction.Gate = QasmInstruction.QXNOR;
                        isControlled = true;
                        break;
                    case "CQAND":
                        instruction.Gate = QasmInstruction.CQAND;
                        isControlled = true;
                        break;
                    case "CQOR":
                        instruction.Gate = QasmInstruction.CQOR;
                        isControlled = true;
                        break;
                    case "CQXOR":
                        instruction.Gate = QasmInstruction.CQXOR;
                        isControlled = true;
                        break;
                    case "CQNAND":
                        instruction.Gate = QasmInstruction.CQNAND;
                        isControlled = true;
                        break;
                    case "CQNOR":
                        instruction.Gate = QasmInstruction.CQNOR;
                        isControlled = true;
                        break;
                    case "CQXNOR":
                        instruction.Gate = QasmInstruction.CQXNOR;
                        isControlled = true;
                        break;
                    case "ADD":
                        instruction.Gate = QasmInstruction.ADD;
                        isControlled = true;
                        break;
                    case "SUB":
                        instruction.Gate = QasmInstruction.SUB;
                        isControlled = true;
                        break;
                    case "MUL":
                        instruction.Gate = QasmInstruction.MUL;
                        isControlled = true;
                        break;
                    case "DIV":
                        instruction.Gate = QasmInstruction.DIV;
                        isControlled = true;
                        break;
                    case "CMPEQ":
                        instruction.Gate = QasmInstruction.CMPEQ;
                        isControlled = true;
                        break;
                    case "CMPGR":
                        instruction.Gate = QasmInstruction.CMPGR;
                        isControlled = true;
                        break;
                    case "IF":
                        instruction.Gate = QasmInstruction.IF;
                        isControlled = true;
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
                        tailArgs = 2;
                        break;
                    case "WHILE":
                        instruction.Gate = QasmInstruction.WHILE;
                        isControlled = true;
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

                    instruction.TargetIndex = uint.Parse(words[targetIndex]);
                }

                if (instruction.Gate == QasmInstruction.M)
                {
                    instruction.TargetIndex = uint.Parse(words[targetIndex + 1]);
                    lrtqi.Add(instruction);
                    continue;
                } else if (instruction.Gate == QasmInstruction.FLOAD)
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

        private List<List<List<RealTimeQasmInstruction>>> SortTimes(List<RealTimeQasmInstruction> instructions)
        {
            int lineNumber = 0;
            float lastClock = 0;
            List<List<List<RealTimeQasmInstruction>>> setClockBlocks = new List<List<List<RealTimeQasmInstruction>>>();
            while (lineNumber < instructions.Count)
            {
                // Get the next set of instructions between SETCLOCK operations, or until EOF.
                List<RealTimeQasmInstruction> setClockBlock = new List<RealTimeQasmInstruction>();
                do
                {
                    setClockBlock.Add(instructions[lineNumber]);
                    lineNumber++;
                } while ((lineNumber < instructions.Count) &&
                    (instructions[lineNumber].Gate != QasmInstruction.SETCLOCK));

                if (setClockBlock[0].IsRelativeTime)
                {
                    setClockBlock[0].IsRelativeTime = false;
                    setClockBlock[0].Time += lastClock;
                }
                if (setClockBlock[0].IsForcedSerial)
                {
                    setClockBlock[0].IsForcedSerial = false;
                    setClockBlock[0].Time = lastClock;
                }

                int blockIndex = 0;

                List<List<RealTimeQasmInstruction>> absoluteTimeBlocks = new List<List<RealTimeQasmInstruction>>();
                do
                {
                    List<RealTimeQasmInstruction> absoluteTimeBlock = new List<RealTimeQasmInstruction>();
                    do
                    {
                        absoluteTimeBlock.Add(setClockBlock[blockIndex]);
                        blockIndex++;

                    } while ((blockIndex < setClockBlock.Count) &&
                        (setClockBlock[0].IsRelativeTime || setClockBlock[0].IsForcedSerial));

                    absoluteTimeBlocks.Add(absoluteTimeBlock);

                } while (blockIndex < setClockBlock.Count);

                absoluteTimeBlocks.Sort((x, y) => x[0].Time.CompareTo(y[0].Time));

                setClockBlocks.Add(absoluteTimeBlocks);

                if (instructions[lineNumber - 1].Gate == QasmInstruction.SETCLOCK)
                {
                    lastClock = instructions[lineNumber - 1].FloatValue;
                }
            }

            return setClockBlocks;
        }
    }
}