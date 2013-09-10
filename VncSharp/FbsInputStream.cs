using System;
using System.IO;
using System.Text;
using System.Threading;

namespace VncSharp
{
    public class FbsInputStream : BinaryReader
    {
        protected int BufferSize;
        protected byte[] Buffer;
        protected int NextBlockOffset;
        public int TimeOffset { get; set; }
        protected long StartTime;

        public string MinorVersion { get; set; }
        public string MajorVersion { get; set; }

        public int WaitingTime { get; set; }

        public FbsInputStream(Stream input)
            : base(input)
        {
            ReadVersion();
            Init();
        }

        public FbsInputStream(Stream input, Encoding encoding)
            : base(input, encoding)
        {
            ReadVersion();
            Init();
        }

        private void Init()
        {
            StartTime = CurrentMillis() + TimeOffset;
            BufferSize = 0;
            NextBlockOffset = 0;
        }

        private void ReadVersion()
        {
            var b = new byte[12];
            ReadFully(b);
            if (b[0] != 'F' || b[1] != 'B' || b[2] != 'S' || b[3] != ' ' ||
                b[4] != '0' || b[5] != '0' || b[6] != '1' || b[7] != '.' ||
                b[8] < '0' || b[8] > '9' || b[9] < '0' || b[9] > '9' ||
                b[10] < '0' || b[10] > '9' || b[11] != '\n')
            {
                throw new IOException("Incorrect FBS file signature");
            }

            var version = Encoding.ASCII.GetString(b);
            MajorVersion = version.Substring(4, 3);
            MinorVersion = version.Substring(8, 3);
        }

        public int MyReadInt32()
        {
            var bytes = ReadDataBlockToBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public byte[] ReadDataBlockToBytes()
        {
            long readResult = ReadUnsigned32();
            if (readResult < 0)
                return null;

            BufferSize = (int)readResult;
            var alignedSize = (BufferSize + 3) & 0xFFFFFFFC;

            if (NextBlockOffset > 0)
            {
                ReadBytes(NextBlockOffset);
                BufferSize -= NextBlockOffset;
                alignedSize -= NextBlockOffset;
                NextBlockOffset = 0;
            }

            if (BufferSize >= 0)
            {
                Buffer = new byte[alignedSize];
                ReadFully(Buffer);
                Array.Resize(ref Buffer, BufferSize);
                TimeOffset = (int) ReadUnsigned32();
                long current = CurrentMillis();
                WaitingTime = (int)(StartTime + TimeOffset - current);
                if (WaitingTime >= 0)
                    Thread.Sleep(WaitingTime);
            }

            if (BufferSize < 0 || TimeOffset < 0)
            {
                Buffer = null;
                BufferSize = 0;
                throw new IOException("Invalid FBS file data");
            }

            return Buffer;
        }

        private long CurrentMillis()
        {
            var utcNow = DateTime.UtcNow;
            var baseTime = new DateTime(1970, 1, 1, 0, 0, 0);
            long timeStamp = (utcNow - baseTime).Ticks / 10000;
            return timeStamp;
        }

        private long ReadUnsigned32()
        {
            var buf = new byte[4];
            if (!ReadFully(buf))
                return -1;

            var val = ((long)(buf[0] & 0xFF) << 24 |
                        (buf[1] & 0xFF) << 16 |
                        (buf[2] & 0xFF) << 8 |
                        (buf[3] & 0xFF));
            return val;
        }

        public bool ReadFully(byte[] b)
        {
            var off = 0;
            var len = b.Length;

            while (off != len)
            {
                var count = Read(b, off, len - off);
                if (count < 0)
                {
                    return false;
                }
                off += count;
            }

            return true;
        }
    }
}