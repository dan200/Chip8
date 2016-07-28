using System;

namespace Dan200
{
    /**
     * An emulator for the CHIP-8 system
     */
    public class Chip8
    {
        public const int DISPLAY_WIDTH = 64;
        public const int DISPLAY_HEIGHT = 32;

        private byte[] m_ram;
        private byte[] m_registers;
        private ushort[] m_stack;
        private byte m_stackSize;
        private ushort m_i;
        private ushort m_pc;
        private Random m_random;
        private byte m_delayTimer;
        private byte m_soundTimer;

        private bool[] m_keys;
        private int m_keyWaitingRegister;

        private bool[] m_display;
        private bool m_displayChanged;

        private string m_errorMessage;

        private static byte[] s_rom = new byte[] {
            // 0x0 - Font
			0xF0,
            0x90,
            0x90,
            0x90,
            0xF0,

            0x20,
            0x60,
            0x20,
            0x20,
            0x70,

            0xF0,
            0x10,
            0xF0,
            0x80,
            0xF0,

            0xF0,
            0x10,
            0xF0,
            0x10,
            0xF0,

            0x90,
            0x90,
            0xF0,
            0x10,
            0x10,

            0xF0,
            0x80,
            0xF0,
            0x10,
            0xF0,

            0xF0,
            0x80,
            0xF0,
            0x90,
            0xF0,

            0xF0,
            0x10,
            0x20,
            0x40,
            0x40,

            0xF0,
            0x90,
            0xF0,
            0x90,
            0xF0,

            0xF0,
            0x90,
            0xF0,
            0x10,
            0xF0,

            0xF0,
            0x90,
            0xF0,
            0x90,
            0x90,

            0xE0,
            0x90,
            0xE0,
            0x90,
            0xE0,

            0xF0,
            0x80,
            0x80,
            0x80,
            0xF0,

            0xE0,
            0x90,
            0x90,
            0x90,
            0xE0,

            0xF0,
            0x80,
            0xF0,
            0x80,
            0xF0,

            0xF0,
            0x80,
            0xF0,
            0x80,
            0x80,
        };

        /**
         * Returns a DISPLAY_WIDTH x DISPLAY_HEIGHT array of the current display contents
         */
        public bool[] Display
        {
            get
            {
                return m_display;
            }
        }

        /**
         * Returns whether the display contents have been changed by the emulator since startup or the last time DisplayChanged was set to false
         */
        public bool DisplayChanged
        {
            get
            {
                return m_displayChanged;
            }
            set
            {
                m_displayChanged = value;
            }
        }

        /**
         * Returns whether the game should currently emit sound
         */
        public bool Beep
        {
            get
            {
                return m_soundTimer >= 0;
            }
        }

        /**
         * Returns whether the emulator has errored
         */
        public bool Error
        {
            get
            {
                return m_errorMessage != null;
            }
        }

        /**
         * Returns the error message if the emulator has errored
         */
        public string ErrorMessage
        {
            get
            {
                return m_errorMessage;
            }
        }

        public Chip8()
        {
            m_ram = new byte[4096];
            m_registers = new byte[16];
            m_stack = new ushort[16];
            m_display = new bool[DISPLAY_WIDTH * DISPLAY_HEIGHT];
            m_displayChanged = false;
            m_random = new Random();
            m_keys = new bool[16];
            Reset();
        }

        /**
         * Resets the emulator to it's power-on state with no ROM loaded
         */
        public void Reset()
        {
            for (int i = 0; i < m_ram.Length; ++i)
            {
                if (i < s_rom.Length)
                {
                    m_ram[i] = s_rom[i];
                }
                else
                {
                    m_ram[i] = 0;
                }
            }
            for (int i = 0; i < m_registers.Length; ++i)
            {
                m_registers[i] = 0;
            }
            for (int i = 0; i < m_stack.Length; ++i)
            {
                m_stack[i] = 0;
            }
            m_stackSize = 0;
            m_delayTimer = 0;
            m_soundTimer = 0;
            m_i = 0;
            m_pc = 512;
            for (int i = 0; i < m_keys.Length; ++i)
            {
                m_keys[i] = false;
            }
            m_keyWaitingRegister = -1;
            Clear();
            m_errorMessage = null;
        }

        /**
         * Loads a ROM image into the emulator for execution
         */
        public void LoadRom(byte[] rom)
        {
            for (int i = 0; i < rom.Length; ++i)
            {
                m_ram[512 + i] = rom[i];
            }
        }

        /**
         * Advances the emulator timers by 1/60th of a second.
         * You must seperately call Step() at the desired clock rate of your system.
         */
        public void Tick()
        {
            if (m_delayTimer > 0)
            {
                --m_delayTimer;
            }
            if (m_soundTimer > 0)
            {
                --m_soundTimer;
            }
        }

        /**
         * Decodes and executes the next instruction.
         * You must call this as many times per second as your desired clock rate.
         * Some documentation suggests limiting this to 60hz, but many games will play better at a higher rate.
         */
        public void Step()
        {
            if (m_errorMessage != null)
            {
                return;
            }
            if (m_keyWaitingRegister >= 0)
            {
                for (int i = 0; i < m_keys.Length; ++i)
                {
                    var key = m_keys[i];
                    if (key)
                    {
                        m_registers[m_keyWaitingRegister] = (byte)i;
                        m_keyWaitingRegister = -1;
                        break;
                    }
                }
                if (m_keyWaitingRegister >= 0)
                {
                    return;
                }
            }

            var b1 = ReadByte(m_pc);
            var b2 = ReadByte((ushort)(m_pc + 1));
            ushort opcode = (ushort)((b1 << 8) + b2);
            m_pc += 2;

            var a = (byte)((opcode & 0xf000) >> 12);
            switch (a)
            {
                case 0:
                    switch (opcode)
                    {
                        case 0x00e0:
                            Clear();
                            break;
                        case 0x00ee:
                            if (m_stackSize > 0)
                            {
                                m_pc = m_stack[m_stackSize - 1];
                                m_stackSize--;
                            }
                            break;
                        default:
                            Unhandled(opcode);
                            break;
                    }
                    break;
                case 1:
                    {
                        var nnn = (ushort)(opcode & 0x0fff);
                        m_pc = nnn;
                        break;
                    }
                case 2:
                    {
                        var nnn = (ushort)(opcode & 0x0fff);
                        m_stack[m_stackSize] = m_pc;
                        m_pc = nnn;
                        m_stackSize++;
                        break;
                    }
                case 3:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var nn = (byte)(opcode & 0x00ff);
                        if (m_registers[x] == nn)
                        {
                            m_pc += 2;
                        }
                        break;
                    }
                case 4:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var nn = (byte)(opcode & 0x00ff);
                        if (m_registers[x] != nn)
                        {
                            m_pc += 2;
                        }
                        break;
                    }
                case 5:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var y = (byte)((opcode & 0x00f0) >> 4);
                        var op = (byte)(opcode & 0x000f);
                        switch (op)
                        {
                            case 0:
                                if (m_registers[x] == m_registers[y])
                                {
                                    m_pc += 2;
                                }
                                break;
                            default:
                                Unhandled(opcode);
                                break;
                        }
                        break;
                    }
                case 6:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var nn = (byte)(opcode & 0x00ff);
                        m_registers[x] = nn;
                        break;
                    }
                case 7:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var nn = (byte)(opcode & 0x00ff);
                        m_registers[x] += nn;
                        break;
                    }
                case 8:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var y = (byte)((opcode & 0x00f0) >> 4);
                        var op = (byte)(opcode & 0x000f);
                        switch (op)
                        {
                            case 0:
                                m_registers[x] = m_registers[y];
                                break;
                            case 1:
                                m_registers[x] = (byte)(m_registers[x] | m_registers[y]);
                                break;
                            case 2:
                                m_registers[x] = (byte)(m_registers[x] & m_registers[y]);
                                break;
                            case 3:
                                m_registers[x] = (byte)(m_registers[x] ^ m_registers[y]);
                                break;
                            case 4:
                                {
                                    var result = (int)m_registers[x] + (int)m_registers[y];
                                    m_registers[x] = (byte)result;
                                    m_registers[15] = (result > 255) ? (byte)1 : (byte)0;
                                    break;
                                }
                            case 5:
                                {
                                    var result = (int)m_registers[x] - (int)m_registers[y];
                                    m_registers[x] = (byte)result;
                                    m_registers[15] = (result < 0) ? (byte)0 : (byte)1;
                                    break;
                                }
                            case 6:
                                {
                                    var previous = m_registers[x];
                                    m_registers[x] = (byte)(m_registers[x] >> 1);
                                    m_registers[15] = (byte)(previous & 0x01);
                                    break;
                                }
                            case 7:
                                {
                                    var result = (int)m_registers[y] - (int)m_registers[x];
                                    m_registers[x] = (byte)result;
                                    m_registers[15] = (result < 0) ? (byte)0 : (byte)1;
                                    break;
                                }
                            case 0xe:
                                {
                                    var previous = m_registers[x];
                                    m_registers[x] = (byte)(m_registers[x] << 1);
                                    m_registers[15] = (byte)(previous & 0x80);
                                    break;
                                }
                            default:
                                Unhandled(opcode);
                                break;
                        }
                        break;
                    }
                case 9:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var y = (byte)((opcode & 0x00f0) >> 4);
                        var op = (byte)(opcode & 0x000f);
                        switch (op)
                        {
                            case 0:
                                if (m_registers[x] != m_registers[y])
                                {
                                    m_pc += 2;
                                }
                                break;
                            default:
                                Unhandled(opcode);
                                break;
                        }
                        break;
                    }
                case 0xa:
                    {
                        var nnn = (ushort)(opcode & 0x0fff);
                        m_i = nnn;
                        break;
                    }
                case 0xb:
                    {
                        var nnn = (ushort)(opcode & 0x0fff);
                        m_pc = (ushort)(nnn + m_registers[0]);
                        break;
                    }
                case 0xc:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var nn = (byte)(opcode & 0x00ff);
                        m_registers[x] = (byte)(nn & m_random.Next(256));
                        break;
                    }
                case 0xd:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var y = (byte)((opcode & 0x00f0) >> 4);
                        var n = (byte)(opcode & 0x000f);
                        var changed = Draw(m_registers[x], m_registers[y], m_i, n);
                        m_registers[15] = changed ? (byte)1 : (byte)0;
                        break;
                    }
                case 0xe:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var op = (byte)(opcode & 0x00ff);
                        switch (op)
                        {
                            case 0x9e:
                                if (ReadKey(m_registers[x]))
                                {
                                    m_pc += 2;
                                }
                                break;
                            case 0xa1:
                                if (!ReadKey(m_registers[x]))
                                {
                                    m_pc += 2;
                                }
                                break;
                            default:
                                Unhandled(opcode);
                                break;
                        }
                        break;
                    }
                case 0xf:
                    {
                        var x = (byte)((opcode & 0x0f00) >> 8);
                        var op = (byte)(opcode & 0x00ff);
                        switch (op)
                        {
                            case 0x07:
                                m_registers[x] = m_delayTimer;
                                break;
                            case 0x0a:
                                m_keyWaitingRegister = x;
                                break;
                            case 0x15:
                                m_delayTimer = m_registers[x];
                                break;
                            case 0x18:
                                m_soundTimer = m_registers[x];
                                break;
                            case 0x1e:
                                m_i += m_registers[x];
                                break;
                            case 0x29:
                                {
                                    var value = m_registers[x];
                                    m_i = (ushort)((value & 0x0f) * 5);
                                    break;
                                }
                            case 0x33:
                                {
                                    var value = m_registers[x];
                                    var hundreds = ((value / 100) % 10);
                                    var tens = ((value / 10) % 10);
                                    var ones = (value % 10);
                                    WriteByte(m_i, (byte)hundreds);
                                    WriteByte((ushort)(m_i + 1), (byte)tens);
                                    WriteByte((ushort)(m_i + 2), (byte)ones);
                                    break;
                                }
                            case 0x55:
                                for (int i = 0; i <= x; ++i)
                                {
                                    WriteByte((ushort)(m_i + i), m_registers[i]);
                                }
                                break;
                            case 0x65:
                                for (int i = 0; i <= x; ++i)
                                {
                                    m_registers[i] = ReadByte((ushort)(m_i + i));
                                }
                                break;
                            default:
                                Unhandled(opcode);
                                break;
                        }
                        break;
                    }
                default:
                    Unhandled(opcode);
                    break;
            }
        }

        /**
         * Sets whether a given key on the 4x4 keypad is currently pressed
         */
        public void SetKey(int key, bool value)
        {
            if (key >= 0 && key < m_keys.Length)
            {
                m_keys[key] = value;
            }
        }

        private bool ReadKey(byte key)
        {
            if (key < m_keys.Length)
            {
                return m_keys[key];
            }
            return false;
        }

        private byte ReadByte(ushort address)
        {
            if (address < m_ram.Length)
            {
                return m_ram[address];
            }
            return 0;
        }

        private void WriteByte(ushort address, byte value)
        {
            if (address < m_ram.Length)
            {
                m_ram[address] = value;
            }
        }

        private void Unhandled(ushort opcode)
        {
            m_errorMessage = string.Format("Unhandled opcode: {0:X4} @ {1}", opcode, (m_pc - 2));
        }

        private void Clear()
        {
            var anyPixelChanged = false;
            for (int i = 0; i < m_display.Length; ++i)
            {
                if (m_display[i])
                {
                    m_display[i] = false;
                    anyPixelChanged = true;
                }
            }
            m_displayChanged |= anyPixelChanged;
        }

        private bool Draw(byte sx, byte sy, ushort i, byte h)
        {
            var anyPixelUnset = false;
            var anyPixelChanged = false;
            for (ushort y = 0; y < h; ++y)
            {
                var b = ReadByte((ushort)(i + y));
                for (ushort x = 0; x < 8; ++x)
                {
                    var c = ((b >> (7 - x)) & 0x1) != 0;
                    var px = sx + x;
                    var py = sy + y;
                    if (px >= 0 && px < DISPLAY_WIDTH && py >= 0 && py < DISPLAY_HEIGHT)
                    {
                        var oldColour = m_display[px + py * DISPLAY_WIDTH];
                        var newColour = oldColour ^ c;
                        if (newColour != oldColour)
                        {
                            m_display[px + py * DISPLAY_WIDTH] = newColour;
                            anyPixelChanged = true;
                            if (!newColour)
                            {
                                anyPixelUnset = true;
                            }
                        }
                    }
                }
            }
            m_displayChanged |= anyPixelChanged;
            return anyPixelUnset;
        }
    }
}

