# Testing

## Automated/Manual Station Switch Stress Test

Due to the sensitive nature of the P/Invoke boundary with `rtlsdr.dll`, rapid station switching was prone to deadlocks.

To manually verify the SDR state machine:
1. Launch the application from PowerShell using `dotnet run` to attach the diagnostic console.
2. Ensure you are connected to the NESDR.
3. Rapidly tap the following stations in sequence:
   `88.9 → 92.7 → 93.5 → 102.3 → 105.5 → 93.5`
4. Repeat at least 20 times.
5. **Acceptance:**
   - The UI correctly displays "Tuning..." and ignores stale tune commands.
   - The console reports `Read loop stopped`, `RTL buffer reset: PASS`, and `First audio samples queued` for the winning command.
   - You do not encounter the "silence" bug where the UI shows "Now Playing" but no audio is produced.
