# Replicating Drepper's Cache Latency Experiments on a Laptop and a Xeon Desktop

Supporting material for the research paper by **Mikita Karavatski** (Student ID 73441, Vistula University).

The paper replicates two experiments from U. Drepper, *What Every Programmer Should Know About Memory* (2007):

1. **Latency vs. working-set size** – pointer chasing through a linked list of 64-byte elements (one cache line each), 4 KiB to 256 MiB, in sequential and random order (random = one cycle built with Sattolo's algorithm).
2. **Matrix traversal** – summing an N × N matrix of doubles row-wise and column-wise for N = 256, 500, 512, 1000, 1024, 2000, 2048, 4000, 4096.

## Files

| File | Description |
|---|---|
| `cachebench.cs` | Benchmark source code (C#, unsafe pointers) |
| `run.bat` | Compiles the benchmark, collects system information and runs it twice |
| `plot_compare.py` | Creates the figures and the summary table from the results |
| `laptop_results.csv` | Platform A (Intel Core i5-11400H laptop): raw results of both runs (repetitions 0–4 = run 1, 5–9 = run 2) |
| `laptop_sysinfo.txt` | Platform A: hardware information collected by `run.bat` |
| `xeon_results.csv` | Platform B (Intel Xeon E5-2630 v3 desktop): raw results of both runs (repetitions 0–4 = run 1, 5–9 = run 2) |
| `xeon_sysinfo.txt` | Platform B: hardware information collected by `run.bat` |

## Data format

Each results file has the columns:

`experiment, pattern, size, rep, ns_per_access`

- `experiment` – `latency` or `traversal`
- `pattern` – `sequential` / `random` (latency) or `row` / `column` (traversal)
- `size` – working-set size in bytes (latency) or matrix dimension N (traversal)
- `rep` – repetition number (0–9; 0–4 come from the first run and 5–9 from the second run)
- `ns_per_access` – average time per access (latency) or per matrix element (traversal) in nanoseconds

## How to reproduce

Requirements: Windows with .NET Framework 4 (included in Windows 10/11). Nothing else needs to be installed.

1. Put `cachebench.cs` and `run.bat` in one folder.
2. Connect the charger (on laptops) and close other programs.
3. Double-click `run.bat`. It compiles the program with the C# compiler included in Windows
   (`csc /optimize+ /unsafe /platform:x64`), saves hardware information to `sysinfo.txt`
   and runs the full benchmark twice (`results1.csv`, `results2.csv`). This takes about 10–15 minutes.

A short test run: `cachebench.exe --quick > test.csv`

## Creating the figures

Requires Python 3 with matplotlib (`pip install matplotlib`):

```
python plot_compare.py laptop_results.csv xeon_results.csv
```

This writes `fig1_latency.png`, `fig2_slowdown.png` (Figures 1 and 2 of the paper) and `summary_both.csv` to the current folder.
