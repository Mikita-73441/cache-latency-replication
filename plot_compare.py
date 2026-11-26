"""
plot_compare.py - figures comparing two platforms.

Usage: python plot_compare.py laptop_results.csv vm_results.csv
Creates fig1_latency.png, fig2_slowdown.png and summary_both.csv in the current folder.
"""
import csv
import statistics
import sys
from collections import defaultdict

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

BLUE, ORANGE = "#257de0", "#f47230"
TEXT, GRID = "#333333", "#dddddd"
plt.rcParams.update({
    "font.family": "serif", "font.size": 10, "axes.edgecolor": "#888888",
    "axes.labelcolor": TEXT, "xtick.color": TEXT, "ytick.color": TEXT,
    "axes.spines.top": False, "axes.spines.right": False,
})


def load(path):
    d = defaultdict(list)
    with open(path, newline="") as f:
        for r in csv.DictReader(f):
            d[(r["experiment"], r["pattern"], int(r["size"]))].append(float(r["ns_per_access"]))
    return d


def series(d, exp, pat):
    keys = sorted(k for k in d if k[0] == exp and k[1] == pat)
    return [k[2] for k in keys], [statistics.median(d[k]) for k in keys]


def label(b):
    return f"{b // 2**20} MiB" if b >= 2**20 else f"{b // 2**10} KiB"


def main():
    a_path, b_path = sys.argv[1], sys.argv[2]
    plats = [("Laptop (Core i5-11400H)", load(a_path), BLUE, "o"),
             ("Desktop (Xeon E5-2630 v3)", load(b_path), ORANGE, "s")]

    # Figure 1: latency vs working set
    fig, ax = plt.subplots(figsize=(6.5, 4.0))
    for name, d, c, m in plats:
        x, y = series(d, "latency", "random")
        ax.plot(x, y, color=c, marker=m, markersize=4, linewidth=1.6, label=f"{name}, random")
        x, y = series(d, "latency", "sequential")
        ax.plot(x, y, color=c, marker=m, markersize=3, linewidth=1.2, linestyle="--",
                markerfacecolor="white", label=f"{name}, sequential")
    ax.set_xscale("log", base=2)
    ax.set_yscale("log")
    ticks = [2**k for k in range(12, 29, 2)]
    ax.set_xticks(ticks)
    ax.set_xticklabels([label(t) for t in ticks], rotation=45, ha="right")
    ax.set_xlabel("Working-set size")
    ax.set_ylabel("Time per access (ns, log scale)")
    ax.grid(True, color=GRID, linewidth=0.6)
    ax.legend(frameon=False, fontsize=8)
    fig.tight_layout()
    fig.savefig("fig1_latency.png", dpi=300)
    plt.close(fig)

    # Figure 2: column/row slowdown per N
    fig, ax = plt.subplots(figsize=(6.5, 3.6))
    w = 0.38
    for i, (name, d, c, m) in enumerate(plats):
        xr, yr = series(d, "traversal", "row")
        _, yc = series(d, "traversal", "column")
        ratio = [cc / rr for cc, rr in zip(yc, yr)]
        pos = [p + (i - 0.5) * (w + 0.02) for p in range(len(xr))]
        ax.bar(pos, ratio, w, color=c, hatch="//" if i else None, edgecolor="white", label=name)
    ax.set_xticks(range(len(xr)))
    ax.set_xticklabels([str(n) for n in xr])
    ax.axhline(1, color="#888888", linewidth=0.8)
    ax.set_xlabel("Matrix size N (N × N doubles)")
    ax.set_ylabel("Slowdown, column-wise / row-wise")
    ax.grid(True, axis="y", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    ax.legend(frameon=False)
    fig.tight_layout()
    fig.savefig("fig2_slowdown.png", dpi=300)
    plt.close(fig)

    with open("summary_both.csv", "w", newline="") as f:
        wr = csv.writer(f)
        wr.writerow(["platform", "experiment", "pattern", "size", "median_ns", "min_ns", "max_ns", "reps"])
        for name, d, _, _ in plats:
            for k in sorted(d):
                v = d[k]
                wr.writerow([name, *k, f"{statistics.median(v):.3f}", f"{min(v):.3f}", f"{max(v):.3f}", len(v)])
    print("saved fig1_latency.png, fig2_slowdown.png, summary_both.csv")


if __name__ == "__main__":
    main()
