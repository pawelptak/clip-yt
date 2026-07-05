# Upgrade Options — ClipYT

Assessment: 2 projects, all on net8.0, upgrading to net10.0; SDK-style, 2 minor package issues, 99 API issues (mostly behavioral changes)

## Strategy

### Upgrade Strategy
Small solution (2 projects), shallow dependency graph (2 tiers), no complex migrations — all projects can be upgraded together in a single atomic pass.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all projects simultaneously in a single atomic pass |
| Top-Down | Upgrade entry-point applications first, temporarily multi-targeting shared libraries |
