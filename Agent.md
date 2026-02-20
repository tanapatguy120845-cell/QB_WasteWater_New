# Agent.MD - QB_WasteWater_New Project Documentation

> **Last Updated:** 2026-02-20
> **Unity Project:** UnityProject-QB_WasteWater_New
> **Solution:** QB-WasteWater.sln
> **Platform Targets:** WebGL, Android, iOS

---

## 1. Project Overview

โปรเจค **QB_WasteWater_New** เป็นแอปพลิเคชัน Unity 2D สำหรับ **ระบบจัดการน้ำเสีย (Wastewater Management)** ของ Quality Brain / WMA ใช้สำหรับ:

- **แสดงผล Layout โรงบำบัดน้ำเสีย** แบบ 2D (ถัง, อุปกรณ์, ท่อ, เซ็นเซอร์)
- **ควบคุมอุปกรณ์** (Start/Stop) ผ่าน REST API
- **รับสถานะ Real-time** ผ่าน SSE (Server-Sent Events)
- **แก้ไข Layout** (วาง/ย้าย/ลบ ถัง, อุปกรณ์, ท่อ) แล้ว Save/Load จาก Server
- **เปิด WebView** (SCADA Dashboard) สำหรับดูรายละเอียดเครื่องจักร

---

## 2. Scenes

| Scene | Path | Description |
|-------|------|-------------|
| **Main** | `Assets/PGroup/Main.unity` | Scene หลักของ PGroup (ระบบใหม่) |
| **BuildGame** | `Assets/Scenes/BuildGame.unity` | Scene หลักสำหรับ Build (ระบบเดิม) |
| **Lobby** | `Assets/Scenes/Lobby.unity` | หน้า Lobby |
| **SampleScene** | `Assets/Scenes/SampleScene.unity` | Scene ตัวอย่าง |

---

## 3. Folder Structure

```
Assets/
  PGroup/                    -- ระบบใหม่ (PGroup Team)
    2D Asset/                -- Sprite assets สำหรับระบบใหม่
    Fonts/                   -- ฟอนต์
    Materials/               -- Materials
    Packages/                -- Package assets
    Prefabs/                 -- Prefabs ระบบใหม่ (Tank, Pipe, Pump, etc.)
    Scripts/                 -- Scripts ระบบใหม่ (namespace PGroup)
    Shaders/                 -- Custom shaders (Water shader)
    Main.unity               -- Scene หลัก PGroup

  Script/                    -- Scripts ระบบเดิม (ไม่มี namespace)
    API/                     -- API Service, SSE Client, DTOs
    Data/                    -- Data Models, Managers, Components
    Popup/                   -- UI Popup scripts
    WaterPipe/               -- Pipe building system

  Prefabs/                   -- Prefabs ระบบเดิม
    Float/                   -- Float sensor prefabs
    Pipe/                    -- Pipe prefabs
    PipeJoint/               -- Pipe joint prefabs (Corner, T, Cross)
    Prefabs/                 -- Machine/Device prefabs
    Pump/                    -- Pump prefabs
    Resources/               -- Runtime-loadable prefabs
    Sprites/                 -- Sprite assets
    Tank/                    -- Tank prefabs
    Valve/                   -- Valve prefabs

  Picture/                   -- รูปภาพ (Device, Pipe, Tank, Valve, etc.)
  Plugins/                   -- Native plugins (Android, iOS, WebGL)
  Scenes/                    -- Unity scenes
  Settings/                  -- URP and project settings
  TextMesh Pro/              -- TMP assets
```

---

## 4. Architecture Overview

### 4.1 Two Parallel Systems

โปรเจคมี **2 ระบบที่ทำงานคู่กัน**:

| | ระบบเดิม (Script/) | ระบบใหม่ (PGroup/Scripts/) |
|---|---|---|
| **Namespace** | Global (ไม่มี namespace) | `PGroup` |
| **Scene** | BuildGame.unity | Main.unity |
| **Object Placement** | `ObjectPlacement.cs` | `NewObjectPlacement.cs` |
| **Pipe Builder** | `PipeBuilder.cs` | `NewPipeBuilder.cs` |
| **Gameplay Controller** | ไม่มี (กระจายตาม script) | `GameplayController.cs` |
| **Water Level** | `WaterLevel2D.cs` (Sprite scale) | `WaterLevelController.cs` + `InteractableWater.cs` (Mesh-based) |

### 4.2 Shared Systems (ใช้ร่วมกันทั้ง 2 ระบบ)

- **API Layer** -- `ApiService`, `APIUseCase`, `SSEClient`, `SSEManager`
- **Auth** -- `AuthManager`
- **Data Models** -- `TankData`, `DeviceComponent`, `SensorComponent`, `SaveDataModel`
- **Save/Load** -- `SaveManager`
- **Popup System** -- `PopupManager`, `ManagerPopup`, `SelectMachine`
- **WebView** -- `SimpleWebView`, `WebViewTester`

---

## 5. Script Reference

### 5.1 API Layer (Assets/Script/API/)

| Script | Type | Description |
|--------|------|-------------|
| **ApiService.cs** | MonoBehaviour | HTTP client (GET/POST) พร้อม Bearer token auth |
| **APIUseCase.cs** | Static class | High-level API calls: `StartDevice()`, `StopDevice()`, `SetAutoManual()` |
| **DTOs.cs** | Data classes | Response models: `CommandResponse`, `LoginResponse`, `SSEMessage`, `SSEDeviceData`, `SSEPayload` |
| **SSEClient.cs** | MonoBehaviour (Singleton) | SSE connection handler พร้อม `StreamingDownloadHandler` |
| **StoreData.cs** | Static class | PlayerPrefs wrapper สำหรับ token, org, password |

### 5.2 Data and Managers (Assets/Script/Data/)

| Script | Type | Description |
|--------|------|-------------|
| **AuthManager.cs** | MonoBehaviour (Singleton) | Authentication -- รับ token จาก React (WebGL) หรือ login เอง |
| **SaveManager.cs** | MonoBehaviour (Singleton) | Save/Load layout (JSON) -- local file + remote server upload |
| **SaveDataModel.cs** | Data classes | `GameSaveData`, `ObjectSaveData`, `ChildSaveData`, `ChildProperties` |
| **SSEManager.cs** | MonoBehaviour (Singleton) | จัดการ SSE data -> อัปเดต `DeviceComponent` ตาม topic |
| **ControlManager.cs** | MonoBehaviour (Singleton) | ส่งคำสั่ง start/stop ผ่าน REST API |
| **DeviceComponent.cs** | MonoBehaviour | Component ติดกับ Device -- เก็บ ID, topic, status, visual feedback (สีเขียว=start) |
| **SensorComponent.cs** | MonoBehaviour | Component ติดกับ Sensor -- เก็บ ID, dataKey, type |
| **TankData.cs** | MonoBehaviour | Component ติดกับ Tank -- เก็บ ID, displayName, capacity, water level |
| **DeviceData.cs** | Data class | Serializable data สำหรับ Device |
| **SensorData.cs** | Data class | Serializable data สำหรับ Sensor |
| **MachineImageLibrary.cs** | MonoBehaviour | Library สำหรับดึงรูปภาพตาม deviceType |
| **DeviceRename.cs** | MonoBehaviour | UI สำหรับเปลี่ยนชื่อ Device |
| **SensorRename.cs** | MonoBehaviour | UI สำหรับเปลี่ยนชื่อ Sensor |
| **TankRename.cs** | MonoBehaviour | UI สำหรับเปลี่ยนชื่อ Tank |
| **WebViewInitData.cs** | Data classes | DTOs สำหรับส่งข้อมูลไป WebView |

### 5.3 Core Gameplay (Assets/Script/)

| Script | Type | Description |
|--------|------|-------------|
| **ObjectPlacement.cs** | MonoBehaviour | ระบบวาง/ย้าย/ลบ วัตถุ (Tank, Device, Sensor, Pipe) พร้อม Snap |
| **CameraController.cs** | MonoBehaviour (Singleton) | Pan (คลิกขวา), Zoom (scroll), FitAllObjects |
| **WaterLevel2D.cs** | MonoBehaviour | ระบบระดับน้ำ 2D -- ตาม Float sensor หรือปุ่ม UI |
| **FloatSensor.cs** | MonoBehaviour | ลูกลอยวัดระดับน้ำ |
| **EditButton.cs** | MonoBehaviour | สลับ UI ระหว่าง Lobby/Edit mode |
| **UICategoryManager.cs** | MonoBehaviour | จัดการ Category panel (Tank, Device, Decorator) |
| **TankManager.cs** | MonoBehaviour (Singleton) | จัดการ Tank selection, rename panels |
| **SimpleWebView.cs** | MonoBehaviour (Singleton) | Cross-platform WebView wrapper (Android/iOS/WebGL) |
| **WebViewTester.cs** | MonoBehaviour (Singleton) | เปิด WebView + ส่ง INIT_CONTROL data ไป SCADA Dashboard |
| **ColorController.cs** | MonoBehaviour | ควบคุมสี |
| **PipeColorController.cs** | MonoBehaviour | ควบคุมสีท่อ |
| **PipeConnector.cs** | MonoBehaviour | Pipe connection points |
| **IPipeConnector.cs** | Interface | Interface สำหรับ Pipe snap system |
| **WaterSwitch.cs** | MonoBehaviour | สวิตช์น้ำ |
| **WaterSystemManager.cs** | MonoBehaviour | จัดการระบบน้ำ |
| **InputHelper.cs** | MonoBehaviour | ช่วยจัดการ Input |
| **ChangeScenes.cs** | MonoBehaviour | เปลี่ยน Scene |

### 5.4 Popup System (Assets/Script/Popup/)

| Script | Type | Description |
|--------|------|-------------|
| **PopupManager.cs** | MonoBehaviour (Singleton) | จัดการ popup ทั้งหมด (SelectMachine, ManagerPopup) |
| **SelectMachine.cs** | MonoBehaviour | Popup เล็กเหนืออุปกรณ์ -- กด "เลือก" เปิด WebView |
| **ManagerPopup.cs** | MonoBehaviour | Modal popup แสดงรายละเอียดเครื่อง + Toggle START/STOP + Remote/Local |
| **DeletePopup.cs** | MonoBehaviour | Popup ลบ/ย้ายวัตถุ (Edit Mode) |

### 5.5 Pipe System (Assets/Script/WaterPipe/)

| Script | Type | Description |
|--------|------|-------------|
| **PipeBuilder.cs** | MonoBehaviour | สร้างท่อจาก path points -- Grid-based bitmask system |
| **PipeGroupPath.cs** | MonoBehaviour | เก็บ path data ของกลุ่มท่อ + Water flow animation |
| **PipeInputManager.cs** | MonoBehaviour | Input handler สำหรับวาดท่อ (click-to-draw) |
| **WaterWalker.cs** | MonoBehaviour | Animation น้ำไหลตามท่อ |

### 5.6 PGroup Scripts (Assets/PGroup/Scripts/)

| Script | Namespace | Description |
|--------|-----------|-------------|
| **GameplayController.cs** | PGroup | Controller หลัก -- Edit mode, object selection, water settings, pipe control |
| **NewObjectPlacement.cs** | PGroup | ระบบวางวัตถุใหม่ -- รองรับ PlacementChecker, Highlight |
| **NewPipeBuilder.cs** | PGroup | ระบบสร้างท่อใหม่ |
| **PipeController.cs** | PGroup | ควบคุม Pipe individual |
| **InteractableWater.cs** | PGroup | ระบบน้ำแบบ Mesh-based (สร้าง mesh จริง) |
| **WaterLevelController.cs** | PGroup | ควบคุมระดับน้ำ 3 ชั้น (Base, Dirty, Green) |
| **HighlightController.cs** | PGroup | ควบคุม Highlight เมื่อเลือกวัตถุ |
| **DeviceDataController.cs** | PGroup | ควบคุมข้อมูล Device ในระบบใหม่ |
| **PlacementChecker.cs** | PGroup | ตรวจสอบตำแหน่งวาง |
| **SettingPopup.cs** | PGroup | Popup ตั้งค่า (Delete, Move, Water Level, Pipe Water) |

---

## 6. API Endpoints and Services

### 6.1 Base URLs (UAT)

| Service | URL |
|---------|-----|
| **Control** | `https://limbic-control-service-uat.qualitybrain.tech` |
| **Auth** | `https://limbic-authenticate-service-uat.qualitybrain.tech` |
| **Report** | `https://wma-report.qualitybrain.tech` |
| **Integration** | `https://scada-dashboard.qualitybrain.tech` |
| **Maker (Layout)** | `https://limbic-maker-service-uat.qualitybrain.tech` |

### 6.2 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login (org_name, username, password) -> access_token |
| POST | `/api/control/commands/start` | Start device (topic, password) |
| POST | `/api/control/commands/stop` | Stop device (topic, password) |
| POST | `/api/control/commands/automanual` | Set auto/manual (topic, automanual) |
| GET | `/api/control/sse/{org}/main-status` | SSE stream สำหรับสถานะอุปกรณ์ |
| GET | `/api/layouts/{org}` | ดึง layout JSON จาก server |
| POST | `/api/layouts/{org}` | อัปโหลด layout JSON ไป server |

### 6.3 Authentication Flow

1. **WebGL Mode (Default):** React app ส่ง token ผ่าน `sendMessage("AuthManager", "ReceiveToken", token)` หรืออ่านจาก `localStorage("unity_auth_token")`
2. **Standalone Mode (Disabled):** Login ด้วย org_name/username/password -> ได้ access_token
3. Token ถูกเก็บใน `PlayerPrefs("AUTH_TOKEN")` และใช้เป็น Bearer token ในทุก API call

### 6.4 SSE Data Flow

```
SSEClient (connect) -> StreamingDownloadHandler (parse SSE blocks)
    -> SSEClient.OnMessageReceived (raw JSON)
    -> SSEManager.ProcessSSEData (parse SSEMessage)
    -> SSEManager.UpdateDevicesFromSSE (match by topic)
    -> DeviceComponent.UpdateFromSSE (update status + visual)
    -> DeviceComponent.OnStatusChanged (notify ManagerPopup)
```

---

## 7. Data Models

### 7.1 Save/Load JSON Format (GameSaveData)

```json
{
  "name": "New Layout",
  "plant_image": " ",
  "objects": [
    {
      "id": "Tank2D_123",
      "category": "group",
      "type": "Tank01",
      "name": "ถังเติมอากาศ",
      "position": { "x": 0, "y": 0 },
      "children": [
        {
          "id": "DEV_1234",
          "category": "device",
          "type": "pump",
          "name": "เครื่องสูบน้ำ SP-1",
          "position": { "x": 0.5, "y": -0.2 },
          "topic": "org/device/pump-1"
        },
        {
          "id": "SEN_5678",
          "category": "sensor",
          "type": "floating_ball",
          "name": "ลูกลอย L1",
          "position": { "x": -0.3, "y": 0.1 },
          "properties": { "data_key": "level_1" }
        }
      ]
    }
  ]
}
```

**Note:** เมื่อ upload ไป server จะตัด `topic` ออก (เก็บเฉพาะ local)

### 7.2 SSE Message Format

```json
{
  "status": "ok",
  "data": [
    {
      "_id": "abc123",
      "topic": "org/device/pump-1",
      "status": "start",
      "payload": {
        "RemLoc": "1",
        "automanual": "auto",
        "submode": "",
        "status": "start",
        "OVL": "0",
        "runtime": "1234",
        "DT": "2024-01-01",
        "disable": "0",
        "EMR": "0"
      }
    }
  ],
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 7.3 WebView INIT_CONTROL Message

```json
{
  "type": "INIT_CONTROL",
  "payload": {
    "token": "eyJhbGci...",
    "device": {
      "_id": "DEV_1234",
      "id": "DEV_1234",
      "name": "เครื่องสูบน้ำ SP-1",
      "topic_name": "org/device/pump-1",
      "mainStatus": "start",
      "device_code": "pump"
    },
    "plantStatus": true,
    "plantData": [...],
    "screenWidth": 900,
    "screenHeight": 640
  }
}
```

---

## 8. Key Singletons

| Singleton | Script | DontDestroyOnLoad | Description |
|-----------|--------|-------------------|-------------|
| `AuthManager.Instance` | AuthManager.cs | No | Authentication |
| `SaveManager.Instance` | SaveManager.cs | No | Save/Load layout |
| `SSEManager.Instance` | SSEManager.cs | Yes | SSE data processing |
| `SSEClient.Instance` | SSEClient.cs | Yes | SSE connection |
| `CameraController.Instance` | CameraController.cs | No | Camera control |
| `TankManager.Instance` | TankManager.cs | No | Tank selection |
| `PopupManager.Instance` | PopupManager.cs | Yes | Popup management |
| `ControlManager.Instance` | ControlManager.cs | Yes | Device control commands |
| `SimpleWebView.Instance` | SimpleWebView.cs | Yes | WebView wrapper |
| `WebViewTester.Instance` | WebViewTester.cs | No | WebView + INIT_CONTROL |
| `GameplayController.instance` | GameplayController.cs | Yes | PGroup main controller |

---

## 9. User Interaction Flows

### 9.1 View Mode (Default)

1. App starts -> `AuthManager` รับ token จาก React
2. `SaveManager.LoadGame()` -> ดึง layout จาก server (fallback local)
3. `ApplySaveData()` -> สร้าง Tank/Device/Sensor จาก JSON
4. `SSEManager.StartSSEConnection()` -> เริ่มรับสถานะ real-time
5. คลิกที่ Device -> `SelectMachine` popup -> กด "เลือก" -> เปิด WebView (SCADA Dashboard)

### 9.2 Edit Mode

1. กดปุ่ม Edit -> `SaveManager.ToggleEditMode()`
2. เลือก Category (Tank/Device/Sensor) -> `UICategoryManager`
3. คลิกวาง -> `ObjectPlacement.StartPlacement()` -> Snap to grid/tank
4. Single click object -> `DeletePopup` (Delete/Move)
5. Double click object -> Rename panel
6. กด Save -> `SaveManager.SaveAllData()` -> upload to server
7. กด Back -> `SaveManager.ExitEditMode()`

### 9.3 Pipe Drawing (PGroup System)

1. กดปุ่ม Pipe -> `PipeInputManager.ToggleBuildingMode()`
2. คลิกจุดแรก -> เริ่มวาด
3. คลิกจุดที่สอง -> Auto-lift (สร้าง segment)
4. คลิกขวา -> `BuildAll()` -> `NewPipeBuilder.BuildPipeBatch()`
5. เลือก PipeGroup -> Spacebar = Toggle water flow, Delete = ลบกลุ่มท่อ

---

## 10. Prefab Library System

`SaveManager` ใช้ระบบ **PrefabMapping** สำหรับ Save/Load:

- **tankLibrary** -- Map typeID (เช่น "Tank01") -> Tank Prefab
- **deviceLibrary** -- Map typeID (เช่น "pump", "valve") -> Device Prefab
- **sensorLibrary** -- Map typeID (เช่น "floating_ball") -> Sensor Prefab

เมื่อ Save: Auto-detect type จาก Library หรือใช้ชื่อ Prefab เป็น fallback
เมื่อ Load: ค้นหา Prefab จาก type -> Instantiate -> ตั้งค่า ID/Name/Topic

---

## 11. Platform-Specific Notes

### WebGL
- Save/Load ใช้ `PlayerPrefs` แทน File I/O
- Auth token มาจาก React ผ่าน `sendMessage` หรือ `localStorage`
- WebView ใช้ jslib plugin (`WebView_Show`, `WebView_Hide`, etc.)
- `PostMessage` ส่ง pure JSON (jslib จะ `JSON.parse` แล้ว `postMessage`)

### Android
- WebView ใช้ Java plugin `com.qualitybrain.webviewplugin.WebViewPlugin`
- Save/Load ใช้ `Application.persistentDataPath`

### iOS
- WebView ใช้ native DllImport (`__Internal`)
- Save/Load ใช้ `Application.persistentDataPath`

---

## 12. Important Tags and Layers

### Tags Used
- `Device` -- อุปกรณ์ (Pump, Valve, etc.)
- `Float` -- ลูกลอยวัดระดับน้ำ
- `Sensor` -- เซ็นเซอร์
- `Intersection` -- จุดตัดท่อ

### Key Layers
- `Default` -- วัตถุทั่วไป
- Tank Layer -- ใช้สำหรับ Raycast snap (กำหนดใน Inspector)
- Pipe Layer -- ท่อ (กำหนดใน PipeBuilder)
- WaterBounds -- ขอบเขตน้ำ (ถูก ignore ใน Raycast)

---

## 13. Dependencies

- **Unity Input System** (New Input System)
- **TextMeshPro** (UI Text)
- **Universal Render Pipeline (URP)**
- **Unity AI Navigation**
- **Adaptive Performance**
- **Visual Scripting**
- **Timeline**

---

## 14. Known Patterns and Conventions

1. **Singleton Pattern** -- ใช้ทั่วโปรเจค (`Instance` property + `DontDestroyOnLoad`)
2. **Event-based Communication** -- `DeviceComponent.OnStatusChanged`, `OnPayloadChanged`
3. **Async/Await** -- ใช้ใน `APIUseCase` และ `ManagerPopup` (async void)
4. **Coroutine** -- ใช้ใน `AuthManager`, `SaveManager`, `SSEClient`, `ControlManager`
5. **PlayerPrefs** -- ใช้เก็บ token, org, password (keys: `AUTH_TOKEN`, `AUTH_ORG`, `AUTH_PASSWORD`)
6. **JsonUtility** -- ใช้สำหรับ serialize/deserialize ทุก JSON (ต้องมี `[Serializable]` attribute)
7. **Status Values** -- `"start"` = กำลังทำงาน, `"stop"/"stopped"` = หยุด, `"unknown"/"inprogress"` = ignore
8. **RemLoc** -- `"1"` = Remote, `"0"` = Local
