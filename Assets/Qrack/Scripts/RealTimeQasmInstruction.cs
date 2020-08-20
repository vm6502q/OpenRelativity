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
        I = 0, RAND,
        X, Y, Z, H, S, T, ADJS, ADJT, U,
        EXP, RX, RY, RZ,
        MCX, MCY, MCZ, MCH, MCS, MCT, MCADJS, MCADJT, MCU,
        MCEXP, MCRX, MCRY, MCRZ,
        M, QSET, QRESET, SET, RESET, FLOAD,
        NOT, AND, OR, XOR, NAND, NOR, XNOR,
        IF, ELSE, ENDIF
    }

    public class RealTimeQasmInstruction
    {
        public int LineNumber { get; set; }
        public float Time { get; set; }
        public QasmInstruction Gate { get; set; }
        public uint Target { get; set; }
        public uint ClassicalTarget { get; set; }
        public float FloatValue { get; set; }
        public uint[] Controls { get; set; }
        public uint[] FloatIndices { get; set; }
    }
}
