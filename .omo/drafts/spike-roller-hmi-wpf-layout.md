# Draft: Spike Roller HMI WPF Layout Reproduction

## Requirements (confirmed)
- Existing HTML design files are under `spike-roller-hmi`.
- There are three HTML pages whose layouts must be reproduced in the `CX102PrickHMI` WPF application.
- The WPF pages should match the HTML page size/layout.
- The prick roller monitoring page contains an image stored under the HTML project's `assets` directory and should reuse that image.
- WPF implementation must follow the repository guidance in `CLAUDE.md`, including MVVM and XAML resource conventions.

## Technical Decisions
- Pending repository exploration: determine the three HTML entry files, viewport/canvas dimensions, asset path, and existing WPF navigation/resource patterns.
- User decision: visual-only reproduction with simulated/mock data; do not integrate backend/device data in this milestone.

## Research Findings
- HTML design location confirmed by user: `spike-roller-hmi`.
- Project stack from repository guidance: .NET Framework 4.7.2, WPF, MVVM, CommunityToolkit.Mvvm.

## Open Questions
- What exact display target/resolution should be treated as the acceptance viewport if the HTML pages are responsive or have multiple breakpoints?

## Scope Boundaries
- INCLUDE: three WPF page layouts matching the three HTML designs, shared styling/resources, navigation wiring, and reuse of the monitoring image asset.
- EXCLUDE: real device protocol/data behavior, backend service integration, persistence, live-data alarms, and production monitoring state; use deterministic simulated values only.
