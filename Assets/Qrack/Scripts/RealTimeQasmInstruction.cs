namespace Qrack
{
    public enum Pauli
    {
        PauliI = 0,
        PauliX = 1,
        PauliY = 3,
        PauliZ = 2
    }

    public enum QasmInstruction
    {
        I = 0, RAND, SETCLOCK,
        X, Y, Z, H, S, T, ADJS, ADJT, U,
        EXP, RX, RY, RZ,
        MCX, MCY, MCZ, MCH, MCS, MCT, MCADJS, MCADJT, MCU,
        MCEXP, MCRX, MCRY, MCRZ,
        M, QSET, QRESET, SET, RESET, FLOAD, ALOAD,
        NOT, AND, OR, XOR, NAND, NOR, XNOR,
        QAND, QOR, QXOR, QNAND, QNOR, QXNOR,
        CQAND, CQOR, CQXOR, CQNAND, CQNOR, CQXNOR,
        ADD, SUB, MUL, DIV, CMPEQ, CMPGR,
        IF, ELSE, ENDIF, FOR, WHILE, DO, LOOP
    }

    public class RealTimeQasmInstruction
    {
        public int LineNumber { get; set; }
        public bool IsRelativeTime { get; set; }
        public bool IsForcedSerial { get; set; }
        public float Time { get; set; }
        public QasmInstruction Gate { get; set; }
        public uint TargetIndex { get; set; }
        public bool IsIndirectTarget { get; set; }
        public float FloatValue { get; set; }
        public int IntValue { get; set; }
        public uint[] Controls { get; set; }
        public bool[] IsIndirectControls { get; set; }
        public int[] TailArgs { get; set; }
        public bool[] IsIndirectFloatIndices { get; set; }
    }
}
