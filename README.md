# PaperTrackerPlugin

PaperTrackerPlugin is a Virt-A-Mate plugin that listens for face and eye tracking data sent through the VRCFT OSC interface.  The plugin opens UDP port **8888** and expects messages from `paper_face_tracker` and `paper_eye_tracker`.

## Features

- Receives OSC messages on port 8888
- `/paper_face_tracker/<name>` values drive dozens of morphs for eyes, brows, mouth and tongue
- `/paper_eye_tracker/leftYaw`, `/leftPitch`, `/rightYaw`, `/rightPitch` rotate the corresponding eye controllers

### Supported face parameters

The plugin ships with a comprehensive default map.  Examples include:

| OSC Address | VAM Morph |
|-------------|-----------|
| `/paper_face_tracker/EyeBlinkLeft` | Blink Left |
| `/paper_face_tracker/BrowInnerUp` | Brow Inner Up |
| `/paper_face_tracker/MouthFrownRight` | Mouth Frown Right |
| `/paper_face_tracker/TongueOut` | Tongue Out |

Add or adjust mappings in `PaperTrackerPlugin.cs` to suit your model.

## Building

Run `dotnet build` inside the plugin directory or compile directly in Virt-A-Mate.
