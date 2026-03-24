# VR Hazard Highlighting Study

A Unity VR application that experimentally compares four hazard highlighting techniques in a within-subjects design. Participants drive forward in a first-person perspective and must identify a highlighted hazard vehicle as quickly as possible. Response times are logged automatically to a CSV file.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Scenarios](#scenarios)
- [Highlighting Conditions](#highlighting-conditions)
- [Running in Development Mode (Desktop)](#running-in-development-mode-desktop)
- [Deploying to Meta Quest (VR Mode)](#deploying-to-meta-quest-vr-mode)
- [Switching Between Development and VR Mode](#switching-between-development-and-vr-mode)
- [Data Output](#data-output)
- [Study Configuration](#study-configuration)
- [Known Issues](#known-issues)

---

## Project Overview

The study presents participants with a driving scene in which five vehicles appear per trial. One vehicle is designated as the hazard and highlighted using one of four visual techniques. The study uses a within-subjects design with counterbalanced condition ordering via Latin square rotation.

Two scenarios are implemented:
- **Scenario 1** — Stationary vehicles ahead of the player
- **Scenario 2** — Oncoming vehicles, one of which drifts into the player's lane

---

## Requirements

- **Unity** 2021.3 LTS or later (URP — Universal Render Pipeline)
- **Unity Input System** package (new input system)
- **Universal Render Pipeline** package
- **XR Plugin Management** package (for VR deployment only)
- **Oculus XR Plugin** package (for Meta Quest deployment only)
- A **Meta Quest** headset and USB-C cable (for VR deployment only)
- **Meta Quest Developer Hub** or **ADB** installed on your machine (for VR deployment only)

---

## Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/YourUsername/VRHazardStudy.git
   ```

2. **Open in Unity:**
   - Open Unity Hub
   - Click **Add → Add project from disk**
   - Navigate to the cloned folder and select it
   - Open the project — Unity will import all assets automatically

3. **Open the scene:**
   - In the Project panel navigate to `Assets/Scenes/`
   - Double-click **SampleScene**

4. **Install required packages** (if not already installed):
   - Go to **Window → Package Manager**
   - Install **Input System** (Unity Registry)
   - Install **Universal RP** (Unity Registry)
   - When prompted to enable the new Input System, click **Yes** and let Unity restart

5. **Add the outline shader to Always Included Shaders:**
   - Go to **Edit → Project Settings → Graphics → Always Included Shaders**
   - Click **+** and select `Hidden/HazardOutline` from the picker

6. **Press Play** to run in development mode

---

## Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity          — main study scene
├── Scripts/
│   ├── Hazards/
│   │   ├── ObjectOutlineHighlight.cs
│   │   ├── PeripheralHaloHighlight.cs
│   │   ├── DepthColourHighlight.cs
│   │   └── DirectionalBeamHighlight.cs
│   ├── Management/
│   │   ├── HazardHighlightManager.cs
│   │   ├── TrialController.cs
│   │   └── TrialControllerScenario2.cs
│   ├── Vehicles/
│   │   ├── VehicleSpawner.cs
│   │   ├── OncomingVehicleSpawner.cs
│   │   └── OncomingVehicle.cs
│   ├── Player/
│   │   ├── PlayerForwardMovement.cs
│   │   ├── VRInputDetector.cs
│   │   └── CameraMove.cs
│   ├── Environment/
│   │   └── RoadEnvironment.cs
│   └── Shaders/
│       └── HazardOutline.shader
```

---

## Scenarios

### Scenario 1 — Stationary Hazard
- Five vehicles spawn ahead of the player in randomised lanes
- Player drives toward them at constant speed
- One vehicle is highlighted as the hazard
- Managed by **TrialController** and **VehicleSpawner**

### Scenario 2 — Oncoming Hazard
- Five vehicles travel toward the player in the opposite lane
- After a random delay (1.5–4 seconds), the hazard vehicle gradually drifts into the player's lane
- Highlight activates at the moment drifting begins
- Response timer starts from drift onset
- Managed by **TrialControllerScenario2** and **OncomingVehicleSpawner**

**To switch scenarios:**
1. Select **Study Manager** in the Hierarchy
2. In the Inspector, **disable** the trial controller you are not using by unchecking its component checkbox
3. **Enable** the trial controller for the scenario you want to run
4. Never run both simultaneously

---

## Highlighting Conditions

| # | Condition | Description |
|---|---|---|
| 1 | Object Outline | A bright red outline surrounds the hazard vehicle directly |
| 2 | Peripheral Halo | A pulsing ring appears on-screen over the hazard vehicle |
| 3 | Depth-Based Colour | Vehicle colour shifts from grey (far) to red (near) as player approaches |
| 4 | Directional Beam | A single solid beam points from the hazard toward the player |

---

## Running in Development Mode (Desktop)

Development mode uses keyboard and mouse input and runs directly in the Unity editor or as a standalone desktop build. This is the current default configuration.

**Controls:**
- **W / S** — move forward / backward
- **A / D** — strafe left / right
- **Right-click + drag** — look around
- **Spacebar or Enter** — indicate hazard detection (replaces VR controller button)

**To run:**
1. Open **SampleScene**
2. Confirm **CameraMove.cs** is enabled on **Main Camera**
3. Confirm **XR Plugin Management** is not set to use an XR loader (or is disabled)
4. Press **Play** in the Unity editor

---

## Deploying to Meta Quest (VR Mode)

### One-time setup

**1. Install required packages:**
- Go to **Window → Package Manager**
- Install **XR Plugin Management** (Unity Registry)
- Install **Oculus XR Plugin** (Unity Registry)

**2. Enable XR Plugin:**
- Go to **Edit → Project Settings → XR Plugin Management**
- Under the **Android** tab, check **Oculus**

**3. Switch build platform:**
- Go to **File → Build Settings**
- Select **Android** and click **Switch Platform**
- Wait for Unity to reimport assets

**4. Set Android/Quest settings:**
- In **Build Settings**, click **Player Settings**
- Under **Other Settings**:
  - Set **Minimum API Level** to **Android 10.0 (API 29)**
  - Set **Target API Level** to **Automatic (highest installed)**
  - Set **Scripting Backend** to **IL2CPP**
  - Check **ARM64** under **Target Architectures**
- Under **XR Plug-in Management → Oculus**:
  - Check **Quest** and **Quest 2/3** as target devices

**5. Enable Developer Mode on your headset:**
- On your phone, open the **Meta Quest** app
- Go to **Menu → Devices** and select your headset
- Go to **Developer Mode** and toggle it **on**
- Connect the headset to your PC via USB-C
- Put on the headset and **Allow** the USB debugging prompt

### Scene changes for VR mode

**1. Disable CameraMove.cs:**
- Select **Main Camera** in the Hierarchy
- In the Inspector, **uncheck** the **CameraMove** component checkbox
- The VR SDK will handle all camera movement via the headset

**2. Configure VR controller input:**
- Select **Study Manager** in the Hierarchy
- Find the **VRInputDetector** component
- Click the circle picker on the **Detection Button Action** field
- Select your Input Action Asset trigger binding (e.g. `XRI RightHand/Select` for the right trigger)
- If you don't have an Input Action Asset yet:
  - Go to **Assets → Create → Input Actions**
  - Name it `VRInputActions`
  - Add an Action Map called `Study`
  - Add an Action called `Detect` bound to `<XRController>{RightHand}/triggerPressed`
  - Save and assign the action to the **Detection Button Action** field

**3. Add XR Rig (if not already present):**
- The **Main Camera** needs to be part of an XR Rig for proper VR tracking
- Go to **GameObject → XR → XR Origin (Action-based)**
- Delete or disable the existing **Main Camera**
- The XR Origin includes its own camera — update all Inspector references from **Main Camera** to the **XR Origin's Camera** GameObject

### Build and deploy

1. Put your Quest headset in developer mode and connect via USB-C
2. Go to **File → Build Settings**
3. Click **Refresh** next to Run Device — your headset should appear
4. Click **Build and Run**
5. Unity will compile and push the app to the headset automatically

---

## Switching Between Development and VR Mode

| Setting | Development Mode | VR Mode |
|---|---|---|
| **CameraMove.cs** | Enabled | Disabled |
| **Input** | Spacebar / Enter | VR controller trigger |
| **XR Plugin** | Disabled | Oculus enabled |
| **Build Platform** | PC / Mac / Linux | Android |
| **VRInputDetector — Detection Button Action** | Unassigned | Assigned to trigger action |
| **Detection input fallback** | Spacebar works automatically | Not needed |

> **Quick toggle checklist when switching to VR:**
> 1. Disable CameraMove on Main Camera
> 2. Switch build platform to Android
> 3. Enable Oculus in XR Plugin Management
> 4. Assign VR controller action to VRInputDetector
> 5. Ensure XR Origin is in the scene

> **Quick toggle checklist when switching back to development:**
> 1. Enable CameraMove on Main Camera
> 2. Switch build platform to PC
> 3. Input System spacebar fallback works automatically

---

## Data Output

Results are saved automatically when the study ends.

**File location:** `Application.persistentDataPath` on the device
- **Windows (development):** `C:\Users\[Username]\AppData\LocalLow\[Company]\[Project]\`
- **Meta Quest (VR):** Internal headset storage — retrieve via ADB:
  ```bash
  adb pull /sdcard/Android/data/[package.name]/files/
  ```

**To set a custom save path**, edit `SaveResults()` in `TrialController.cs`:
```csharp
string folder = @"C:\Users\YourName\Desktop\StudyResults";
Directory.CreateDirectory(folder);
```

**Filename format:**
- Scenario 1: `HazardStudy_P{ID}_{yyyyMMdd_HHmmss}.csv`
- Scenario 2: `HazardStudy_S2_P{ID}_{yyyyMMdd_HHmmss}.csv`

**CSV columns:**
```
ParticipantID, TrialNumber, Condition, TrialWithinCondition, ResponseTime_s, Responded
```

---

## Study Configuration

All values below are adjustable in the Unity Inspector without editing code.

| Parameter | Location | Default | Description |
|---|---|---|---|
| Participant ID | TrialController | 1 | Increment per participant |
| Trials Per Condition | TrialController | 5 | Set to 1 for quick testing |
| Approach Duration | TrialController | 0.5s | Delay before highlight activates |
| Response Time Limit | TrialController | 10s | Max time per trial |
| Inter Trial Interval | TrialController | 2s | Pause between trials |
| Player Speed | PlayerForwardMovement | 2 | Forward movement speed |
| Spawn Distance | VehicleSpawner | 30 | How far ahead vehicles spawn |
| Lane Positions | VehicleSpawner | -6,-3,0,3,6 | X positions of each lane |
| Drift Delay | OncomingVehicle | 1.5–4s | Random delay before lane drift |
| Drift Duration | OncomingVehicle | 2.5s | Time to complete lane change |

---

## Known Issues

1. **PeripheralHaloHighlight on Cube** — the component on the original Cube (child of Hazard) must remain **disabled** in the Inspector at all times. If Unity recompiles scripts it may re-enable — check before each session.

2. **VehicleSpawner AddComponent lines** — if testing a single condition, three unused highlight `AddComponent` calls may be commented out in the spawner scripts. Uncomment all four before running the full study.

3. **Road length** — the road is a fixed procedural length. Long sessions or high player speed may cause the player to reach the end. Increase `roadLength` on **RoadEnvironment** if needed.

4. **Car prefab materials** — if imported car prefabs appear purple, go to **Edit → Rendering → Render Pipeline Converter** and run **Material Upgrade** to convert Built-in materials to URP.

5. **Highlight scripts on prefab children** — if using a car prefab whose mesh is on a child object, highlight scripts must be added to the correct child. See `CreateVehicle()` in both spawner scripts.
