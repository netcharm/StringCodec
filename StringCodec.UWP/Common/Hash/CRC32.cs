using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace StringCodec.UWP.Common
{
    public class CRC32 : HashAlgorithm
    {
        //public uint Polynomial { get; set; } = 0x04C11DB7u; //0xEDB88320u;
        private uint polynomial = 0xEDB88320u;
        public uint Polynomial
        {
            get
            {
                return (polynomial);
            }
            set
            {
                polynomial = value;
                MakeCRC32Table();
            }
        }

        public uint Seed { get; set; } = 0xFFFFFFFFu;

        protected uint[] Crc32Table = null;
        //生成CRC32码表
        private void MakeCRC32Table()
        {
            Crc32Table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint CRC = i;
                //for (j = 8; j > 0; j--)
                for (uint j = 0; j < 8; j++)
                {
                    if ((CRC & 1) == 1)
                        CRC = (CRC >> 1) ^ Polynomial;
                    else
                        CRC = CRC >> 1;
                }
                Crc32Table[i] = CRC;
            }
        }

        public new static CRC32 Create()
        {
            return (new CRC32());
        }

        public static CRC32 Create(uint polynomial)
        {
            return (new CRC32() { Polynomial = polynomial });
        }

        public static CRC32 Create(uint polynomial, uint seed)
        {
            return (new CRC32() { Polynomial = polynomial, Seed = seed });
        }

        public CRC32()
        {
            Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (Crc32Table == null) MakeCRC32Table();

            uint value = Seed;
            for (int i = 0; i < array.Length; i++)
            {
                value = (value >> 8) ^ Crc32Table[array[i] ^ value & 0xFF];
            }

            value = value ^ 0xFFFFFFFFu;
            HashValue = new byte[4] {
                (byte)((value & 0xFF000000u) >> 24),
                (byte)((value & 0x00FF0000u) >> 16),
                (byte)((value & 0x0000FF00u) >> 8),
                (byte)((value & 0x000000FFu))
            };
        }

        protected override byte[] HashFinal()
        {
            return (HashValue);
        }

        public override void Initialize()
        {
            if(Crc32Table ==  null) MakeCRC32Table();
            HashValue = null;
        }
    }
}
