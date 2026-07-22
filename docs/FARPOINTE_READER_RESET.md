# Resetting Farpointe Readers over OSDP

Farpointe Data readers (vendor code `DA-0D-38`) are reset to factory defaults with a
manufacturer-specific OSDP command, not with any command defined in the OSDP spec itself.
This document describes the exact sequence OSDP-Bench uses.

## Summary

| Item | Value |
| --- | --- |
| Reader vendor code (osdp_PDID) | `DA-0D-38` — "Farpointe Data, Inc." |
| Reset command | `osdp_MFG` (0x80) |
| Vendor code in the MFG payload | `CA-44-6C` (Cypress Computer Systems) |
| Command data | `0x05` |
| Expected reply | `osdp_ACK` |
| Required successful ACKs | 10 |
| Timing | Must begin immediately after the reader powers on |

Note the asymmetry: the reader *identifies* itself as Farpointe (`DA-0D-38`), but the
manufacturer-specific reset payload carries the **Cypress** vendor code `CA-44-6C`.
Farpointe readers run Cypress OSDP firmware, so the Cypress reset command is the correct
one to send. Do not substitute `DA-0D-38` into the MFG payload.

## Command sequence

1. **Power down the reader.** The reset command is only accepted during the boot window.
2. **Open the serial connection** at the reader's current baud rate and shut down any
   existing polling session first, so nothing else is on the bus.
3. **Start a connection with a zero poll interval** and add the device with secure channel
   and CRC negotiation disabled (`AddDevice(connectionId, address, useCrc: false, useSecureChannel: false)`).
   Secure channel must be off — the reader will not accept the reset inside a secure session.
4. **Power up the reader**, then immediately begin sending:

   ```
   osdp_MFG  vendor = CA 44 6C   data = 05
   ```

5. **Repeat until 10 ACKs are received.** Send the command in a tight loop; the reader
   ignores it until its OSDP stack is up, so early attempts will time out or NAK. Allow up
   to 3 failed attempts beyond the number of ACKs already collected before giving up, with
   a 1-second delay after each failure.
6. **Reader resets** once the 10th ACK is returned. It comes back at its factory defaults:
   address 0, 9600 baud, secure channel keyed to the default install key (SCBK-D).

## Why 10 ACKs

A single ACK only proves the reader parsed the packet. The firmware requires the command to
be repeated during the boot window before it commits the reset, which guards against an
accidental single MFG command wiping a configured reader. Ten consecutive accepted commands
is the threshold OSDP-Bench uses.

## After the reset

The reader is back on its defaults, so the existing connection parameters no longer apply.
Reconnect using:

- Address `0`
- Baud rate `9600`
- Secure channel with the default install key, if secure channel is desired

## Implementation reference

- `src/Core/Actions/ResetCypressDeviceAction.cs` — the command loop described above
- `src/Core/Models/IdentityLookup.cs` — vendor lookup table; the Farpointe entry sets
  `CanSendResetCommand = true` and carries the "send right after power-on" instruction
- `src/Core/ViewModels/Pages/ManageViewModel.cs` — `HandleResetCypressDeviceAction`, which
  shuts down the panel, confirms with the user, then runs the action

## Readers that cannot be reset this way

For contrast, other vendors in the lookup table require out-of-band resets and reject any
OSDP reset command: LenelS2 (pairing card), HID Signo (HID Reader Manager app), INID
(RF-DISTIFLEX app), and WaveLynx Ethos (tamper-tilt plus power cycle).
