using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace modprocessor.libs
{
    public class ProtrackerModule
    {

        public byte[] ModuleData { get; set; }

        public string ModuleName { get; set; } = string.Empty;

        public List<ProtrackerSample> Samples { get; set; } = new List<ProtrackerSample>();

        public int SampleCount { get; set; }

        public string SampleTag { get; set; } = string.Empty;

        public int PatternCount { get; set; }



        public static ProtrackerModule Create(byte[] moduleData)
        {
            var protrackerModule = new ProtrackerModule();

            protrackerModule.ModuleData = moduleData;

            using (var memoryStream = new MemoryStream(moduleData))
            using (var reader = new BigEndianBinaryReader(memoryStream))
            {

                // Module Type (15 or 31 samples)
                reader.BaseStream.Position = 1080;                      // Id Tag Position
                var tag = reader.ReadBytes(4);
                if ((tag[0] < 0x20 || tag[0] > 0x7e)
                    || (tag[1] < 0x20 || tag[1] > 0x7e)
                    || (tag[2] < 0x20 || tag[2] > 0x7e)
                    || (tag[3] < 0x20 || tag[3] > 0x7e))
                {
                    protrackerModule.SampleCount = 15;
                    protrackerModule.SampleTag = string.Empty;
                }
                else
                {
                    protrackerModule.SampleCount = 31;
                    protrackerModule.SampleTag = Encoding.ASCII.GetString(tag);
                }

                // Module Name
                reader.BaseStream.Position = 0;
                protrackerModule.ModuleName = reader.ReadAscii(20);

                // Find Number of Patterns By Finding the Highest Pattern Number Referenced
                reader.BaseStream.Position = 952;
                protrackerModule.PatternCount = 0;
                for (int i = 0; i < 128; i++)
                {
                    var patternNo = reader.ReadByte();
                    if (patternNo > protrackerModule.PatternCount)
                    {
                        protrackerModule.PatternCount = patternNo;
                    }
                }

                // Get Sample Data
                int sampleStartOffset = 1080 + (1024 * (protrackerModule.PatternCount + 1));
                if (!string.IsNullOrEmpty(protrackerModule.SampleTag))
                {
                    sampleStartOffset += 4;
                }
                for (int i = 0; i < protrackerModule.SampleCount; i++)
                {
                    reader.BaseStream.Position = 0;
                    var sample = ProtrackerSample.Create(i, reader, sampleStartOffset);
                    protrackerModule.Samples.Add(sample);
                    sampleStartOffset += sample.Length;
                }

            }

            return protrackerModule;
        }




        public ProtrackerModule()
        {

        }



        public void PackSamples()
        {
            foreach (var sample in Samples)
            {
                if (!sample.IsEmpty)
                {
                    sample.PackedSample = Pack4Bit(sample.SampleData);
                    Console.WriteLine($"Sample: '{sample.Name}' - packed.");
                }
                else
                {
                    Console.WriteLine($"Sample: '{sample.Name}' - is unused.");
                }
            }

            // write packed sample back over original sample data
            using (var memoryStream = new MemoryStream(ModuleData))
            using (var writer = new BigEndianBinaryWriter(memoryStream))
            {
                foreach (var sample in Samples)
                {
                    if (!sample.IsEmpty)
                    {
                        writer.BaseStream.Position = sample.StartOffset;
                        writer.ClearBytes(sample.Length);
                        writer.BaseStream.Position = sample.StartOffset;
                        writer.Write(sample.PackedSample);
                    }

                }
            }

        }

        public void DepackSamples()
        {
            foreach (var sample in Samples)
            {
                if (!sample.IsEmpty)
                {
                    sample.SampleData = Depack4Bit(sample.SampleData);
                    Console.WriteLine($"Sample: '{sample.Name}' - packed.");
                }
                else
                {
                    Console.WriteLine($"Sample: '{sample.Name}' - is unused.");
                }
            }

            // write packed sample back over original sample data
            using (var memoryStream = new MemoryStream(ModuleData))
            using (var writer = new BigEndianBinaryWriter(memoryStream))
            {
                foreach (var sample in Samples)
                {
                    if (!sample.IsEmpty)
                    {
                        // writer.BaseStream.Position = sample.StartOffset;
                        // writer.ClearBytes(sample.Length);
                        writer.BaseStream.Position = sample.StartOffset;
                        writer.Write(sample.SampleData);
                    }

                }
            }

        }




        private int[] DeltaTable = { 0, 1, 2, 4, 8, 16, 32, 64, 128, -64, -32, -16, -8, -4, -2, -1 };


        private byte[] Depack4Bit(byte[] source)
        {
            var depacked = new byte[source.Length];

            for (int i=0; i<source.Length/2; i++)
            {
                byte value = source[i];
                byte nibble1 = (byte)(value & 0x0f);
                byte nibble2 = (byte)((value >> 4) & 0x0f);
                depacked[i * 2] = (byte)DeltaTable[nibble1];
                depacked[(i*2)+1] = (byte)DeltaTable[nibble2];
            }
            return depacked;
        }

            private byte[] Pack4Bit(byte[] source)
        {
            // Make even number of bytes
            if ((source.Length & 0x1) == 1)
            {
                var temp = source.ToList();
                temp.Add(0x00);
                source = temp.ToArray();
            }

            var deltaSmp = 0;
            var delta4 = new byte[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var current = (int)(sbyte)source[i];                // current sample
                var deltaPos = DeltaPick(deltaSmp, current);

                deltaSmp = DeltaTable[deltaSmp];
                delta4[i] = (byte)deltaPos;
            }

            var final = new byte[source.Length / 2];
            var pos = 0;

            for (var i = 0; i < source.Length; i += 2)
            {
                var result = ((delta4[i] & 0xf) << 4) + (delta4[i + 1] & 0x0f);
                final[pos] = (byte)result;
                pos++;
            }

            return final;
        }



        private int DeltaPick(int deltaSmp, int current)
        {
            var deltaPos = -1;

            var ceiling = 127;
            for (var i = 0; i < 16; i++)
            {
                var temp = deltaSmp;
                temp = temp - DeltaTable[i];

                temp = (int)(sbyte)(temp & 0xff);
                temp -= current;

                if (temp < 0)
                {
                    temp = 0 - temp;
                }

                if (temp >= ceiling)
                {
                    continue;
                }
                else
                {
                    deltaPos = i;
                    ceiling = temp;
                }
            }
            return deltaPos;
        }




        public void PackSamplesDelta()
        {
            foreach (var sample in Samples)
            {
                if (!sample.IsEmpty)
                {
                    sample.PackedSample = Pack4BitDelta(sample.SampleData);
                    Console.WriteLine($"Sample: '{sample.Name}' - packed.");
                }
                else
                {
                    Console.WriteLine($"Sample: '{sample.Name}' - is unused.");
                }
            }

            // write packed sample back over original sample data
            using (var memoryStream = new MemoryStream(ModuleData))
            using (var writer = new BigEndianBinaryWriter(memoryStream))
            {
                foreach (var sample in Samples)
                {
                    if (!sample.IsEmpty)
                    {
                        writer.BaseStream.Position = sample.StartOffset;
                        writer.ClearBytes(sample.Length);
                        writer.BaseStream.Position = sample.StartOffset;
                        writer.Write(sample.PackedSample);
                    }

                }
            }

        }
        

        private byte[] Depack4BitDelta(byte[] source)
        {
            var depacked = new byte[source.Length];

            for (int i=0; i<source.Length/2; i++)
            {
                byte value = source[i];
                byte nibble1 = (byte)(value & 0x0f);
                byte nibble2 = (byte)((value >> 4) & 0x0f);

                if (i == 0)
                {
                    depacked[0] = 
                }
                depacked[i * 2] = (byte)DeltaTable[nibble1];
                depacked[(i*2)+1] = (byte)DeltaTable[nibble2];
            }
            return depacked;
        }
        

        private byte[] Pack4BitDelta(byte[] sampleData)
        {
            byte[] deltaIndexes = new byte[sampleData.Length-1];

            int lastSampleValue = (sbyte)sampleData[0];
            deltaIndexes[0] = (byte)lastSampleValue;
            for (int i = 1; i < sampleData.Length; i++)
            {
                int deltaValue = lastSampleValue - sampleData[i];
                int quantisedDeltaIndex = QuantiseDeltaValue(deltaValue);
                lastSampleValue = Math.Clamp(lastSampleValue + delttable[quantisedDeltaIndex], -128, 127);
                deltaIndexes[i] = (byte)quantisedDeltaIndex;
            }

            byte[] packedSample = new byte[(sampleData.Length / 2)+2];
            var pos = 1;
            for (var i = 1; i < sampleData.Length; i += 2)
            {
                var result = ((deltaIndexes[i] & 0xf) << 4) + (deltaIndexes[i + 1] & 0x0f);
                packedSample[pos] = (byte)result;
                pos++;
            }

            return packedSample;

        }
        
        int[] delttable = [2, 4, 8, 16, 32, 64, 128, 200, -2, -4, -8, -16, -32, -64, -128, -200];

        private int QuantiseDeltaValue(int deltaValue)
        {
            int deltaIndex = 0;
            int smallestDifference = 0;
            if (deltaValue >= 0)
            {
                deltaIndex = 7;
                smallestDifference = 255;
                for (int i = 0; i < 8; i++)
                {
                    var deltaDifference = delttable[i] - deltaValue;
                    if (smallestDifference > Math.Abs(deltaDifference))
                    {
                        deltaIndex = i;
                        smallestDifference = Math.Abs(deltaDifference);
                    }
                }
            }
            else
            {
                deltaIndex = 15;
                smallestDifference = 255;
                for (int i = 0; i < 8; i++)
                {
                    var deltaDifference = delttable[i + 8] - deltaValue;
                    if (smallestDifference > Math.Abs(deltaDifference))
                    {
                        deltaIndex = i;
                        smallestDifference = Math.Abs(deltaDifference);
                    }
                }
            }

            return deltaIndex;
        }

    }

}
