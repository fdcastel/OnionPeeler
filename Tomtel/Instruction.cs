namespace OnionPeeler.Tomtel
{
    public enum Instruction
    {
        Halt = 0x01,

        Out = 0x02,

        Jez = 0x21,
        Jnz = 0x22,

        Mv = 0x40,
        Mv32 = 0x80,

        Cmp = 0xC1,
        Add = 0xC2,
        Sub = 0xC3,
        Xor = 0xC4,

        Aptr = 0xE1
    }
}
