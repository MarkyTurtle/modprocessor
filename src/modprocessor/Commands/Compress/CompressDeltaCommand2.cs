using System;
using System.Collections.Generic;

class FibonacciDeltaCompressor
{
    static readonly sbyte[] DeltaTable = new sbyte[]
    {
        -21, -13, -8, -5, -3, -2, -1, 0, 1, 2, 3, 5, 8, 13, 21, 34
    };

    public static byte[] CompressToFibonacciDelta(byte[] input)
    {
        if (input == null || input.Length < 2)
            throw new ArgumentException("Input must contain at least two samples.");

        List<byte> output = new List<byte>();
        byte previous = input[0];
        output.Add(previous); // first sample uncompressed

        for (int i = 1; i < input.Length; i += 2)
        {
            byte packed = 0;

            // First delta
            sbyte delta1 = (sbyte)(input[i] - previous);
            int index1 = FindClosestDeltaIndex(delta1);
            previous = (byte)Math.Clamp(previous + DeltaTable[index1], 0, 255);
            packed = (byte)(index1 << 4);

            // Second delta
            if (i + 1 < input.Length)
            {
                sbyte delta2 = (sbyte)(input[i + 1] - previous);
                int index2 = FindClosestDeltaIndex(delta2);
                previous = (byte)Math.Clamp(previous + DeltaTable[index2], 0, 255);
                packed |= (byte)(index2 & 0x0F);
            }

            output.Add(packed);
        }

        return output.ToArray();
    }

    public static byte[] DecompressFibonacciDelta(byte[] input)
    {
        if (input == null || input.Length < 1)
            throw new ArgumentException("Compressed input is too short.");

        List<byte> output = new List<byte>();
        byte previous = input[0];
        output.Add(previous);

        for (int i = 1; i < input.Length; i++)
        {
            byte packed = input[i];
            int index1 = (packed >> 4) & 0x0F;
            previous = (byte)Math.Clamp(previous + DeltaTable[index1], 0, 255);
            output.Add(previous);

            if (output.Count >= input.Length * 2 - 1) break;

            int index2 = packed & 0x0F;
            previous = (byte)Math.Clamp(previous + DeltaTable[index2], 0, 255);
            output.Add(previous);
        }

        return output.ToArray();
    }

    private static int FindClosestDeltaIndex(sbyte delta)
    {
        int bestIndex = 0;
        int bestDistance = Math.Abs(delta - DeltaTable[0]);
        for (int i = 1; i < DeltaTable.Length; i++)
        {
            int dist = Math.Abs(delta - DeltaTable[i]);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }


}
