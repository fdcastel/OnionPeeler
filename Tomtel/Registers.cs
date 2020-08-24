namespace OnionPeeler.Tomtel
{
    public class Registers
    {
        public byte A; // Accumulation register -- Used to store the result of various instructions
        public byte B; // Operand register -- This is 'right hand side' of various operations
        public byte C; // Count/offset register -- Holds an offset or index value that is used when reading memory
        public byte D; // General purpose register
        public byte E; // General purpose register
        public byte F; // Flags register -- Holds the result of the comparison instruction (CMP), and is used by conditional jump instructions (JEZ, JNZ)

        public uint La; // General purpose register
        public uint Lb; // General purpose register
        public uint Lc; // General purpose register
        public uint Ld; // General purpose register

        public uint Ptr; // Pointer to memory -- holds a memory address which is used by instructions that read or write memory

        public uint Pc; // Program counter -- holds a memory address that points to the next instruction to be executed
    }
}