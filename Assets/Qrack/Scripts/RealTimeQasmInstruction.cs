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
        M, QSET, QRESET, SET, RESET, FLOAD,
        NOT, AND, OR, XOR, NAND, NOR, XNOR,
        IF, ELSE, ENDIF, FOR, WHILE, DO, LOOP
    }

    public class RealTimeQasmInstruction
    {
        public int LineNumber { get; set; }
        public bool IsRelativeTime { get; set; }
        public float Time { get; set; }
        public QasmInstruction Gate { get; set; }
        public uint Target { get; set; }
        public uint ClassicalTarget { get; set; }
        public float FloatValue { get; set; }
        public uint[] Controls { get; set; }
        public uint[] FloatIndices { get; set; }
    }
}
