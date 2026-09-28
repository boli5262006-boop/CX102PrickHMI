# Learnings - bind-monitor-plc-points

## Environment Facts
- F:\CX102PrickHMI is NOT a git repository - commit steps must be skipped or require user-run git init.
- openspec CLI not on PATH; use "npx --yes @fission-ai/openspec" from repo root.
- PowerShell 5.1 console is GBK: UTF-8 (no BOM) files show mojibake in Select-String output - display-only artifact; verify content via [System.IO.File]::ReadAllLines(path, [Text.Encoding]::UTF8).
- Concurrent dotnet build from parallel subagents collides in obj/ (RG1000 same-key .baml); transient, retry when the other build finishes.
- OPCUADevice/OPCUAGroup/OPCUAVariable live in thinger.ConfigLib (NOT thinger.CommunicationLib); OPCUA read/write client (WriteNodeAsync<T> returning Task<bool>) is in thinger.CommunicationLib. Use float for PLC REAL, bool for commands.

## Codebase Facts
- Old-style csproj: every new file needs explicit Compile/Page Include entries.
- net472 + packages.config: CommunityToolkit source generators unreliable - manual SetProperty only, never [ObservableProperty].
- Runtime DataContext of ALL pages = MainViewModel (DI singletons in ConfigureService); the four per-page ViewModels are design-time ghosts - never edit them.
- GetOPCUAValue poll loop ships COMMENTED OUT; uncomment verbatim or CurrentValue stays empty.
- 500ms updateTimer.Tick is the natural hook for a read-only CurrentValue-to-VM snapshot pump.
- Momentary buttons: ButtonBase captures the mouse itself; PreviewMouseLeftButtonDown + Up + LostMouseCapture with a per-button pressed flag gives exactly one true/false per press; touch works via WPF mouse promotion.

## Process Notes
- Subagent load_skills must use names from the subagent available list (wpf-dev-pack:* unavailable there) - prompts must be self-contained.
- 4-reviewer final wave (oracle compliance / quality / QA / scope) all APPROVE; minor-only findings: CurrentValue dict thread-safety, dead Servo1/2Actual props, dead alarm-pulse storyboard, DateSummary stub.

## PowerShell encoding hazard
- NEVER use `Set-Content -Encoding UTF8` on a UTF-8 .cs file with Chinese characters on PS5.1 — it reads as GBK and writes as UTF-8, garbling the CJK bytes plus inserting CRLF.
- For C# source files with Chinese: use python (explicit UTF-8 LF no-BOM) or `[System.IO.File]::WriteAllText($p, $content, (New-Object System.Text.UTF8Encoding $false))`.
