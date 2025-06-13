using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modprocessor.libs
{
    public class ProtrackerSample
    {

        public string Name { get; set; } = string.Empty;
        public int StartOffset { get; set; }
        public Int16 Length { get; set; }
        public byte FineTune { get; set; }
        public byte Volume { get; set; }
        public Int16 RepeatOffset { get; set; }
        public Int16 RepeatLength { get; set; }
        public byte[] SampleData { get; set; } = new byte[0];
        public byte[] PackedSample { get; set; } = new byte[0]; 


        public static ProtrackerSample Create(int sampleIndex, BigEndianBinaryReader reader, int sampleDataOffset)
        {
            var sample = new ProtrackerSample();
            reader.BaseStream.Position = 20 + (30 * sampleIndex);

            sample.StartOffset = sampleDataOffset;
            sample.Name = reader.ReadAscii(22);
            sample.Length = (short)(reader.ReadInt16() * 2);
            sample.FineTune = reader.ReadByte();
            sample.Volume = reader.ReadByte();
            sample.RepeatOffset = reader.ReadInt16();
            sample.RepeatLength = reader.ReadInt16();

            reader.BaseStream.Position = sampleDataOffset;
            sample.SampleData = reader.ReadBytes(sample.Length);

            return sample;

        }

        public bool IsPacked => PackedSample.Length > 0;

        public bool IsEmpty => SampleData.Length == 0;

        public ProtrackerSample()
        {

        }

    }
}
