using System;
using System.IO;

namespace CAN_Tool.Libs
{
    // Loads an Intel HEX file for the ПУ-28 panel's own main application (the program the
    // panel's GD32 bootloader jumps to - NOT the OmniProtocol/type-123 firmware handled by
    // BootloaderDeviceViewModel, which is a different device on the bus entirely).
    //
    // Mirrors the original Delphi tool's LoadHexFile/downloadOneDataLine: builds a flat,
    // 0xFF-filled memory image and pokes hex data records into it at their absolute address.
    //
    // If the image starts at exactly the panel's app start address (0x0800C000, i.e. offset
    // 0xC000 from the internal flash base) and patchLengthAndCrc is true (the default), it also
    // patches in a self-referential length+CRC16 at flash offset 0x1C000 - the bootloader's own
    // startup check (calcCrc() in main.c) reads this back to decide whether the just-flashed
    // application is valid, so without this patch the panel would refuse to run the new
    // firmware. Turn it off if the .hex was already built with this checksum baked in (e.g. by
    // the compiler's own build step) and you don't want it silently overwritten.
    public static class PanelFirmwareHex
    {
        public const int AppStartAddress = 0xC000; // offset from internal flash base 0x08000000
        private const int MemorySize = 0x40000;
        private const int CrcPatchOffset = 0x1C000;

        public static byte[] LoadAndPatch(string hexPath, out int appStart, out int appEnd, bool patchLengthAndCrc = true)
        {
            var mem = new byte[MemorySize];
            for (var i = 0; i < mem.Length; i++) mem[i] = 0xFF;

            var highAddr = 0;
            var start = int.MaxValue;
            var end = -1;

            foreach (var rawLine in File.ReadLines(hexPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] != ':') continue;

                var byteCount = (line.Length - 1) / 2;
                var bytes = new byte[byteCount];
                for (var i = 0; i < byteCount; i++)
                    bytes[i] = Convert.ToByte(line.Substring(1 + i * 2, 2), 16);

                var count = bytes[0];
                var addr = (bytes[1] << 8) | bytes[2];
                var type = bytes[3];

                if (type == 0x04)
                {
                    // Hex files for this chip commonly use absolute addresses (0x08xxxxxx,
                    // the internal flash's mapped base) in extended linear address records.
                    // Masking off bit 27 strips exactly the 0x08000000 flash-base bit, turning
                    // it into an offset from that base - matches the original Delphi tool
                    // exactly ("and $7ffffff").
                    highAddr = (((bytes[4] << 8) | bytes[5]) << 16) & 0x07FFFFFF;
                }
                else if (type == 0x00)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var a = highAddr + addr + i;
                        if (a >= mem.Length)
                            throw new InvalidDataException($"Hex file address 0x{a:X} exceeds the expected flash size");
                        mem[a] = bytes[4 + i];
                        if (a > end) end = a;
                        if (a < start) start = a;
                    }
                }
            }

            if (end < 0)
                throw new InvalidDataException("No data records found in hex file");

            appStart = start;
            appEnd = end;

            if (patchLengthAndCrc && start == AppStartAddress)
            {
                var crc = (ushort)0xFFFF;
                for (var a = start; a <= end; a++)
                {
                    byte b = (a >= CrcPatchOffset && a <= CrcPatchOffset + 5) ? (byte)0xFF : mem[a];
                    for (var j = 0; j < 8; j++)
                    {
                        var bit = crc & 1;
                        crc = (ushort)(crc >> 1);
                        if ((b & 1) != bit) crc ^= 0xA001;
                        b = (byte)(b >> 1);
                    }
                }

                var length = end - start + 1;
                mem[CrcPatchOffset + 0] = (byte)(length & 0xFF);
                mem[CrcPatchOffset + 1] = (byte)((length >> 8) & 0xFF);
                mem[CrcPatchOffset + 2] = (byte)((length >> 16) & 0xFF);
                mem[CrcPatchOffset + 3] = (byte)((length >> 24) & 0xFF);
                mem[CrcPatchOffset + 4] = (byte)(crc & 0xFF);
                mem[CrcPatchOffset + 5] = (byte)((crc >> 8) & 0xFF);
            }

            return mem;
        }
    }
}
