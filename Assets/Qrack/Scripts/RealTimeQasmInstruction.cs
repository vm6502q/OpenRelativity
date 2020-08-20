namespace Qrack
{
    public enum QasmInstruction
    {
        I = 0,
        X, Y, Z, H, S, T, ADJS, ADJT, U,
        MCX, MCY, MCZ, MCH, MCS, MCT, MCADJS, MCADJT, MCU,
        M, QSET, QRESET, SET, RESET, FLOAD,
        NOT, AND, OR, XOR, NAND, NOR, XNOR,
        IF, ELSE, ENDIF
    }

    public class RealTimeQasmInstruction
    {
        public float Time { get; set; }
        public QasmInstruction Gate { get; set; }
        public uint Target { get; set; }
        public uint ClassicalTarget { get; set; }
        public float FloatValue { get; set; }
        public uint[] Controls { get; set; }
        public uint[] FloatIndices { get; set; }
    }
}
