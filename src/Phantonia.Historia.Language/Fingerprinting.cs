using System;
using System.Collections.Generic;
using System.Numerics;

namespace Phantonia.Historia.Language;

public static class Fingerprinting
{
    const ulong PrimeA = 646644388857619157;
    const ulong PrimeB = 675658988204990189;
    const ulong PrimeC = 752989082444509271;
    const ulong PrimeD = 148981401512231593;
    const ulong PrimeE = 577207077317580517;

    public static ulong Jumble(ulong x)
    {
        ulong temp = x * PrimeA + PrimeB;
        return BitOperations.RotateLeft(temp, 17);
    }

    public static ulong Combine(ulong x, ulong y)
    {
        ulong temp = x * PrimeA + y * PrimeB + PrimeC;
        return BitOperations.RotateLeft(temp, 23);
    }

    public static ulong Combine(ulong x, ulong y, ulong z)
    {
        ulong temp = x * PrimeA + y * PrimeB + z * PrimeC + PrimeD;
        return BitOperations.RotateLeft(temp, 29);
    }

    public static ulong Combine(IEnumerable<ulong> values)
    {
        ulong temp = PrimeE;

        Span<ulong> primes = [PrimeA, PrimeB, PrimeC, PrimeD, PrimeE];
        int i = 0;
        
        foreach (ulong x in values)
        {
            if (x is 0)
            {
                continue;
            }

            temp += x * primes[i % 5];
            temp = BitOperations.RotateLeft(temp, 19);
            i++;
        }

        return temp;
    }

    public static ulong HashString(string str)
    {
        ulong temp = PrimeE;

        Span<ulong> primes = [PrimeA, PrimeB, PrimeC, PrimeD, PrimeE];
        int i = 0;

        foreach (char x in str)
        {
            temp += x * primes[i % 5];
            temp = BitOperations.RotateLeft(temp, 31);
            i++;
        }

        return temp;
    }
}
