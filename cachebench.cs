// cachebench.cs - memory hierarchy micro-benchmark (C# port of cachebench.c)
// Replication of the core experiments from U. Drepper,
// "What Every Programmer Should Know About Memory" (2007).
//
// Experiment 1 (latency): pointer chasing through 64-byte elements (one cache line each)
//   for working sets from 4 KiB to 256 MiB, in sequential and random (Sattolo) order.
// Experiment 2 (traversal): summing an N x N matrix of doubles row-wise vs column-wise.
//
// Uses raw pointers (unsafe code) so the measured loops are the same as in C.
// Build with the compiler that ships with Windows (.NET Framework 4.x):
//   csc /optimize+ /unsafe /platform:x64 /out:cachebench.exe cachebench.cs
// Run: cachebench.exe > results.csv     (cachebench.exe --quick for a short test)
using System;
using System.Diagnostics;
using System.Globalization;

unsafe static class CacheBench
{
    const int LINE = 64;                       // bytes per element = one cache line
    static long sink;                          // keeps the JIT from removing loops
    static double dsink;
    static ulong rngState = 0x9E3779B97F4A7C15UL;

    static ulong Rng()
    {
        unchecked
        {
            rngState ^= rngState >> 12;
            rngState ^= rngState << 25;
            rngState ^= rngState >> 27;
            return rngState * 0x2545F4914F6CDD1DUL;
        }
    }

    static double NowNs()
    {
        return Stopwatch.GetTimestamp() * (1e9 / Stopwatch.Frequency);
    }

    static string F(double v) { return v.ToString("F4", CultureInfo.InvariantCulture); }

    // builds the list: each element's first 8 bytes hold the address of the next element
    static void BuildList(byte* a, long n, bool random)
    {
        long[] idx = new long[n];
        for (long i = 0; i < n; i++) idx[i] = i;
        if (random)
        {
            // Sattolo's algorithm: random permutation forming one single cycle
            for (long i = n - 1; i > 0; i--)
            {
                long j = (long)(Rng() % (ulong)i);
                long t = idx[i]; idx[i] = idx[j]; idx[j] = t;
            }
            for (long i = 0; i < n; i++) *(byte**)(a + i * LINE) = a + idx[i] * LINE;
        }
        else
        {
            for (long i = 0; i < n; i++) *(byte**)(a + i * LINE) = a + ((i + 1) % n) * LINE;
        }
    }

    static double Chase(byte* start, long steps)
    {
        byte* p = start;
        double t0 = NowNs();
        for (long s = 0; s < steps; s++) p = *(byte**)p;
        double t1 = NowNs();
        sink = (long)p;
        return (t1 - t0) / steps;
    }

    static void Latency(bool quick)
    {
        int reps = quick ? 2 : 5;
        long steps = quick ? 2000000 : 20000000;
        long maxBytes = 256L << 20;
        string[] names = { "sequential", "random" };

        for (long b = 4096; b <= maxBytes; b *= 2)
        {
            long[] sizes = { b, b + b / 2 };
            foreach (long bytes in sizes)
            {
                if (bytes > maxBytes) continue;
                long n = bytes / LINE;
                byte[] raw = new byte[bytes + LINE];
                fixed (byte* rp = raw)
                {
                    byte* a = (byte*)(((long)rp + LINE - 1) & ~(long)(LINE - 1)); // align to cache line
                    for (int order = 0; order < 2; order++)
                    {
                        BuildList(a, n, order == 1);
                        Chase(a, n < 1000000 ? n * 2 : n);  // warm-up
                        for (int r = 0; r < reps; r++)
                        {
                            double ns = Chase(a, steps);
                            Console.WriteLine("latency," + names[order] + "," + bytes + "," + r + "," + F(ns));
                        }
                    }
                }
                Console.Error.WriteLine("latency: " + (bytes >> 10) + " KiB done");
                raw = null;
                GC.Collect();
            }
        }
    }

    static double SumRows(double* m, long n)
    {
        double s = 0;
        for (long i = 0; i < n; i++)
            for (long j = 0; j < n; j++) s += m[i * n + j];
        return s;
    }

    static double SumCols(double* m, long n)
    {
        double s = 0;
        for (long j = 0; j < n; j++)
            for (long i = 0; i < n; i++) s += m[i * n + j];
        return s;
    }

    static void Traversal(bool quick)
    {
        int reps = quick ? 2 : 5;
        long[] ns = { 256, 500, 512, 1000, 1024, 2000, 2048, 4000, 4096 };
        int count = quick ? 6 : ns.Length;
        for (int k = 0; k < count; k++)
        {
            long n = ns[k];
            double[] raw = new double[n * n + 8];
            fixed (double* rp = raw)
            {
                double* m = (double*)(((long)rp + LINE - 1) & ~(long)(LINE - 1));
                for (long i = 0; i < n * n; i++) m[i] = i % 7;
                for (int order = 0; order < 2; order++)
                {
                    string name = order == 1 ? "column" : "row";
                    dsink = order == 1 ? SumCols(m, n) : SumRows(m, n);   // warm-up
                    for (int r = 0; r < reps; r++)
                    {
                        double t0 = NowNs();
                        double s = order == 1 ? SumCols(m, n) : SumRows(m, n);
                        double t1 = NowNs();
                        dsink = s;
                        Console.WriteLine("traversal," + name + "," + n + "," + r + "," + F((t1 - t0) / (n * n)));
                    }
                }
            }
            Console.Error.WriteLine("traversal: N=" + n + " done");
            raw = null;
            GC.Collect();
        }
    }

    static void Main(string[] args)
    {
        bool quick = args.Length > 0 && args[0] == "--quick";
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { }
        Console.Out.Flush();
        Console.WriteLine("experiment,pattern,size,rep,ns_per_access");
        Latency(quick);
        Traversal(quick);
        Console.Error.WriteLine("finished");
    }
}
