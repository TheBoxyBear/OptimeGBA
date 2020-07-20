using static OptimeGBA.Bits;
using System;

namespace OptimeGBA
{
    public class LCD
    {
        GBA Gba;
        public LCD(GBA gba)
        {
            Gba = gba;
        }

        public enum LCDEnum
        {
            Drawing, HBlank, VBlank
        }


        // DISPCNT
        uint Mode;
        bool CgbMode;
        bool DisplayFrameSelect;
        bool HBlankIntervalFree;
        bool ObjCharacterVramMapping;
        bool ForcedBlank;
        bool ScreenDisplayBg0;
        bool ScreenDisplayBg1;
        bool ScreenDisplayBg2;
        bool ScreenDisplayBg3;
        bool ScreenDisplayObj;
        bool Window0DisplayFlag;
        bool Window1DisplayFlag;
        bool ObjWindowDisplayFlag;

        // DISPSTAT
        public bool VBlank;
        public bool HBlank;
        public bool VCounterMatch;
        public bool VBlankIrqEnable;
        public bool HBlankIrqEnable;
        public bool VCounterIrqEnable;
        public byte VCountSetting;

        // RGB, 24-bit
        public byte[] Screen = new byte[240 * 160 * 3];

        public byte[] Palettes = new byte[1024];
        public byte[] Vram = new byte[98304];
        public byte[] Oam = new byte[1024];


        public byte Read8(uint addr)
        {
            if (addr >= 0x05000000 && addr <= 0x050003FF)
            {
                return Palettes[addr - 0x05000000];
            }
            else if (addr >= 0x06000000 && addr <= 0x06017FFF)
            {
                return Vram[addr - 0x06000000];
            }
            else if (addr >= 0x07000000 && addr <= 0x070003FF)
            {
                return Oam[addr - 0x07000000];
            }
            return 0;
        }

        public void Write8(uint addr, byte val)
        {
            if (addr >= 0x05000000 && addr <= 0x050003FF)
            {
                Palettes[addr - 0x05000000] = val;
            }
            else if (addr >= 0x06000000 && addr <= 0x06017FFF)
            {
                Vram[addr - 0x06000000] = val;
            }
            else if (addr >= 0x07000000 && addr <= 0x070003FF)
            {
                Oam[addr - 0x07000000] = val;
            }
        }

        public byte ReadHwio8(uint addr)
        {
            byte val = 0;
            switch (addr)
            {
                case 0x4000000: // DISPCNT B0
                    val |= (byte)(Mode & 0b111);
                    if (CgbMode) val = BitSet(val, 3);
                    if (DisplayFrameSelect) val = BitSet(val, 4);
                    if (HBlankIntervalFree) val = BitSet(val, 5);
                    if (ObjCharacterVramMapping) val = BitSet(val, 6);
                    if (ForcedBlank) val = BitSet(val, 7);
                    break;
                case 0x4000001: // DISPCNT B1
                    if (ScreenDisplayBg0) val = BitSet(val, 8 - 8);
                    if (ScreenDisplayBg1) val = BitSet(val, 9 - 8);
                    if (ScreenDisplayBg2) val = BitSet(val, 10 - 8);
                    if (ScreenDisplayBg3) val = BitSet(val, 11 - 8);
                    if (ScreenDisplayObj) val = BitSet(val, 12 - 8);
                    if (Window0DisplayFlag) val = BitSet(val, 13 - 8);
                    if (Window1DisplayFlag) val = BitSet(val, 14 - 8);
                    if (ObjWindowDisplayFlag) val = BitSet(val, 15 - 8);
                    break;

                case 0x4000004: // DISPSTAT B0
                    if (VBlank) val = BitSet(val, 0);
                    if (HBlank) val = BitSet(val, 1);
                    if (VCounterMatch) val = BitSet(val, 2);
                    if (VBlankIrqEnable) val = BitSet(val, 3);
                    if (HBlankIrqEnable) val = BitSet(val, 4);
                    if (VCounterIrqEnable) val = BitSet(val, 5);
                    break;
                case 0x4000005: // DISPSTAT B1
                    val |= VCountSetting;
                    break;
            }

            return val;
        }

        public void WriteHwio8(uint addr, byte val)
        {
            switch (addr)
            {
                case 0x4000000: // DISPCNT B0
                    Mode = (uint)(val & 0b111);
                    CgbMode = BitTest(val, 3);
                    DisplayFrameSelect = BitTest(val, 4);
                    HBlankIntervalFree = BitTest(val, 5);
                    ObjCharacterVramMapping = BitTest(val, 6);
                    ForcedBlank = BitTest(val, 7);
                    break;
                case 0x4000001: // DISPCNT B1
                    ScreenDisplayBg0 = BitTest(val, 8 - 8);
                    ScreenDisplayBg1 = BitTest(val, 9 - 8);
                    ScreenDisplayBg2 = BitTest(val, 10 - 8);
                    ScreenDisplayBg3 = BitTest(val, 11 - 8);
                    ScreenDisplayObj = BitTest(val, 12 - 8);
                    Window0DisplayFlag = BitTest(val, 13 - 8);
                    Window1DisplayFlag = BitTest(val, 14 - 8);
                    ObjWindowDisplayFlag = BitTest(val, 15 - 8);
                    break;

                case 0x4000004: // DISPSTAT B0
                    VBlankIrqEnable = BitTest(val, 3);
                    HBlankIrqEnable = BitTest(val, 4);
                    VCounterIrqEnable = BitTest(val, 5);
                    break;
                case 0x4000005: // DISPSTAT B1
                    VCountSetting = val;
                    break;
            }
        }

        public uint TotalFrames;

        public uint VCount;

        public uint CycleCount;
        public LCDEnum lcdEnum;
        public void Tick(uint cycles)
        {
            CycleCount += cycles;
            switch (lcdEnum)
            {
                case LCDEnum.Drawing:
                    {
                        if (CycleCount >= 960)
                        {
                            lcdEnum = LCDEnum.HBlank;
                            HBlank = true;
                            RenderScanline();
                        }
                    }
                    break;
                case LCDEnum.HBlank:
                    {
                        if (CycleCount >= 1232)
                        {
                            CycleCount = 0;

                            HBlank = false;

                            if (VCount != 227)
                            {
                                VCount++;
                                if (VCount > 159)
                                {
                                    lcdEnum = LCDEnum.VBlank;
                                    VBlank = true;
                                }
                                else
                                {
                                    lcdEnum = LCDEnum.Drawing;
                                }
                            }
                            else
                            {
                                VCount = 0;
                                lcdEnum = LCDEnum.Drawing;
                                VBlank = false;

                                TotalFrames++;
                            }
                        }
                    }
                    break;
                case LCDEnum.VBlank:
                    {
                        if (CycleCount >= 960)
                        {
                            HBlank = true;
                            lcdEnum = LCDEnum.HBlank;
                            RenderScanline();
                        }
                    }
                    break;

            }
        }

        public void RenderScanline()
        {
            switch (Mode)
            {
                case 4:
                    RenderMode4();
                    return;
            }
        }

        public void RenderMode4()
        {

        }
    }
}