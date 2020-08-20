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

        private uint ifDepth;
        private bool hasElse;

        public enum QasmInstruction
        {
            I = 0,
            X, Y, Z, H, S, T, U,
            MCX, MCY, MCZ, MCH, MCS, MCT, MCU,
            M, QSET, QRESET, SET, RESET,
            NOT, AND, OR, XOR, NAND, NOR, XNOR,
            IF, ELSE, ENDIF
        }

        public class RealTimeQasmInstruction
        {
            public float Time { get; set; }
            public QasmInstruction Gate { get; set; }
            public uint Target { get; set; }
            public uint ClassicalTarget { get; set; }
            public uint[] Controls { get; set; }
            public float[] Args { get; set; }
        }

        public List<RealTimeQasmInstruction> InstructionList { get; set; }

        // Start is called before the first frame update
        void Awake()
        {
            ifDepth = 0;
            hasElse = false;

            InstructionList = ParseBlock();
        }

        public void ResetBlock(TextAsset nProgram)
        {
            ifDepth = 0;
            hasElse = false;
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

                if (programLines[0] == "#")
                {
                    // Comment
                    continue;
                }

                string[] words = programLines[i].Trim().Split(' ');
                RealTimeQasmInstruction instruction = new RealTimeQasmInstruction();

                instruction.Time = float.Parse(words[0]);

                bool isControlled = false;
                bool isClassical = false;
                int tailArgs = 0;

                switch (words[1].Trim().ToUpperInvariant())
                {
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
                    case "U":
                        instruction.Gate = QasmInstruction.U;
                        tailArgs = 3;
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
                    case "MCU":
                        instruction.Gate = QasmInstruction.MCU;
                        isControlled = true;
                        tailArgs = 3;
                        break;
                    case "M":
                        instruction.Gate = QasmInstruction.M;
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
                    case "IF":
                        instruction.Gate = QasmInstruction.IF;
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
                    case "I":
                        instruction.Gate = QasmInstruction.I;
                        lrtqi.Add(instruction);
                        continue;
                    default:
                        throw new FormatException("Could not parse RealTimeQasmText, line " + (i + 1).ToString());
                }

                int targetIndex = words.Length - (tailArgs + 1);
                uint targetValue = uint.Parse(words[targetIndex]);

                if (isClassical)
                {
                    instruction.ClassicalTarget = targetValue;
                }
                else
                {
                    instruction.Target = targetValue;
                }

                if (instruction.Gate == QasmInstruction.M)
                {
                    instruction.ClassicalTarget = uint.Parse(words[targetIndex + 1]);
                    lrtqi.Add(instruction);
                    continue;
                }

                if (isControlled)
                {
                    instruction.Controls = (targetIndex < 2) ? new uint[targetIndex - 2] : null;
                    for (int j = 2; j < targetIndex; j++)
                    {
                        instruction.Controls[j - 2] = uint.Parse(words[j]);
                    }
                }

                instruction.Args = (tailArgs > 0) ? new float[tailArgs] : null;
                for (int j = 0; j < tailArgs; j++)
                {
                    instruction.Args[j] = float.Parse(words[targetIndex + j + 1]);
                }

                lrtqi.Add(instruction);
            }

            return lrtqi;
        }
    }
}