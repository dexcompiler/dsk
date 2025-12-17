# dsk

Disk usage, visualized beautifully. dsk gives you instant insights into your storage with rich terminal tables, smart grouping, and powerful filters. Native AOT compiled for speed, cross-platform by design. Supports JSON output, multiple themes, and wildcard filtering.

## Features

- **Cross-platform**: Windows, Linux, and macOS support
- **Rich output**: Beautiful tables with color themes via Spectre.Console
- **Fast**: Native AOT compiled for instant startup and minimal memory
- **Flexible filtering**: Filter by device type, filesystem, or mount point patterns
- **Multiple output formats**: Table (Unicode/ASCII) or JSON
- **Smart grouping**: Automatically groups devices by type (local, network, fuse, special)

## Installation

### From source

```bash
# Build
dotnet build

# Run
dotnet run

# Publish native binary
dotnet publish -c Release -r win-x64    # Windows
dotnet publish -c Release -r linux-x64  # Linux
dotnet publish -c Release -r osx-x64    # macOS Intel
dotnet publish -c Release -r osx-arm64  # macOS Apple Silicon
```

## Usage

```bash
# Show all mounted filesystems
dsk

# Show specific paths
dsk /home /var

# Filter by device type
dsk --only local,network
dsk --hide loops,binds

# Filter by filesystem
dsk --only-fs ext4,btrfs
dsk --hide-fs tmpfs,squashfs

# Filter by mount point (supports wildcards)
dsk --only-mp /home/*
dsk --hide-mp /snap/*

# Include all (pseudo, duplicate, inaccessible filesystems)
dsk --all

# Output options
dsk --json                    # JSON output
dsk --output size,used,avail  # Select columns
dsk --output usage,trend      # Usage bars with sparkline trends
dsk --sort size               # Sort by column
dsk --inodes                  # Show inode information

# Appearance
dsk --theme dark              # dark, light, or ansi
dsk --style unicode           # unicode or ascii
dsk --width 120               # Fixed width (0 = auto)

# History
dsk --no-save                 # Don't save usage to history
```

## Options

| Option | Description |
|--------|-------------|
| `-a, --all` | Include pseudo, duplicate, inaccessible filesystems |
| `--hide <types>` | Hide device types: local, network, fuse, special, loops, binds |
| `--only <types>` | Show only specific device types |
| `--hide-fs <fs>` | Hide specific filesystems |
| `--only-fs <fs>` | Show only specific filesystems |
| `--hide-mp <pattern>` | Hide mount points matching pattern |
| `--only-mp <pattern>` | Show only mount points matching pattern |
| `-o, --output <cols>` | Columns: mountpoint, size, used, avail, usage, inodes, inodes_used, inodes_avail, inodes_usage, type, filesystem, trend |
| `-s, --sort <col>` | Sort by column (prefix with ~ for descending) |
| `-w, --width <n>` | Terminal width (0 = auto-detect) |
| `--theme <name>` | Color theme: dark, light, ansi |
| `--style <name>` | Table style: unicode, ascii |
| `-i, --inodes` | Show inode information |
| `-j, --json` | Output as JSON |
| `--no-save` | Don't save usage data to history |
| `--warnings` | Show warnings |

## Output Columns

| Column | Description |
|--------|-------------|
| `mountpoint` | Where the filesystem is mounted |
| `size` | Total size |
| `used` | Space used |
| `avail` | Space available |
| `usage` | Usage percentage with visual bar |
| `trend` | Sparkline showing usage history (▁▂▃▄▅▆▇█) |
| `inodes` | Total inodes |
| `inodes_used` | Inodes used |
| `inodes_avail` | Inodes available |
| `inodes_usage` | Inode usage percentage |
| `type` | Filesystem type (ext4, ntfs, apfs, etc.) |
| `filesystem` | Device or volume name |

## Device Types

| Type | Description |
|------|-------------|
| `local` | Local block devices |
| `network` | Network filesystems (NFS, SMB, CIFS) |
| `fuse` | FUSE filesystems |
| `special` | Special filesystems (tmpfs, devfs, etc.) |
| `loops` | Loop devices |
| `binds` | Bind mounts |

## Requirements

- .NET 10 SDK (for building)
- No runtime dependencies when published as Native AOT

## Built With

- [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework) — Zero-dependency, high-performance CLI parsing with source generation
- [Spectre.Console](https://github.com/spectreconsole/spectre.console) — Beautiful terminal UI, tables, and colors
- Inspired by [duf](https://github.com/muesli/duf)

## License

MIT

