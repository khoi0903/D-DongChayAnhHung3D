# Dong Chay Anh Hung 3D

Prototype Unity 6 / URP cho game 3D third-person historical fantasy adventure **Dong Chay Anh Hung**.

## Tong quan

Game theo chan nhom hoc sinh hien dai bi cuon vao mot bien co lich su - ky ao. Prototype hien tai tap trung vao hai canh dau:

- **S01_CityPrototype**: canh mo dau, tutorial chay tron trong thanh pho.
- **S02_UndergroundCave**: canh sinh ton - bi an duoi long dat, dan toi TimeRift.

Nhan vat chinh o S01 la hoc sinh binh thuong, nen khong co chien dau trong canh nay. Combat chi bat dau tam thoi trong S02 sau khi TimeRift cong huong voi Van An.

## S01_CityPrototype

### Muc tieu canh

Nguoi choi hoc cac thao tac co ban:

- Di chuyen bang WASD.
- Chay nhanh bang Shift.
- Tranh chuong ngai vat.
- Tuong tac QTE voi vat can hop ly.
- Chay tron khoi Hac Tinh.
- Den khu sap mat dat de chuyen sang S02.

### Flow hien tai

1. **Khu thanh pho / bao tang**
   - Van An bat dau o khu duong hien dai.
   - UI huong dan cach di chuyen va chay.
   - Tin hieu bat thuong xuat hien qua story text.

2. **Duong bi chan**
   - Main road bi chan bang vat can cong trinh va do do nat.
   - Nguoi choi bi dan vao side route thay vi di thang tren duong lon.

3. **Duong dat / cong trinh**
   - Route rong va ro hon so voi ban cu.
   - Co bien chi dan, den, mui ten vang va cac marker de nguoi choi biet di dau.

4. **QTE blocker**
   - Khong dung metal gate giua duong nua.
   - Dung cac vat can hop ly hon nhu construction fence / fallen tree / debris.
   - Text QTE: `Nhan E lien tuc de vuot chuong ngai: X/Y`.

5. **Slow zone va collapse**
   - SlowZone dung bun / debris thay vi san dien.
   - Cuoi canh co crack/collapse zone va exit trigger chuan bi load S02.

### He thong chase S01

- `S01ChaseThreat` la Hac Tinh khong co HP, khong the giet.
- Uu tien chase Player truc tiep neu thay duong.
- Neu bi tuong/goc chan, dung waypoint lam navigation helper.
- Cham Player hoac vao catch distance thi Player chet ngay.
- Khong dung NavMesh.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S01 City Escape Zigzag`
- `Tools > Dong Chay Anh Hung > Create S01 Chase Threat`

## S02_UndergroundCave

### Muc tieu canh

S02 duoc thiet ke lai thanh canh survival mystery:

- Van An tinh day mot minh duoi long dat.
- Kham pha hang co, ky hieu Dong Son / Co Loa.
- Nghe tieng ban goi tu trong bong toi.
- Hac Tinh xuat hien tu ho sup phia tren, khong phai tu TimeRift.
- TimeRift cong huong va mo khoa phan kich tam thoi.
- Nguoi choi on dinh TimeRift trong 30 giay.
- TimeRift qua tai va keo nhom sang S03_CoLoaArrival.

### Flow hien tai

1. **Wake Area**
   - Player spawn an toan o dau hang.
   - Combat bi tat.
   - Co intro text va camera cutscene ngan.

2. **Ancient Signs Path**
   - Hang co co cac dau hieu Dong Son / Co Loa.
   - Co interactable symbol de tao cam giac bi an.
   - Anh sang xanh dan duong.

3. **Voices Path**
   - UI story text goi y ban be dang o gan do.
   - Route tiep tuc dan den vung ap luc.

4. **Hac Tinh Descent**
   - Hac Tinh roi/xuat hien tu `HacTinh_Descent_Hole`.
   - Camera shake va warning text bao nguoi choi chay.
   - Enemy spawn phia sau player, khong spawn truoc mat.

5. **TimeRift Chamber**
   - Chamber co san di lai an toan, khong con bi ket o vong TimeRift.
   - Vong/core TimeRift la visual, collider chan duong bi tat.
   - Co prompt `Nhan E de cong huong voi khe nut thoi gian`.

6. **Stabilization Event**
   - Sau khi nhan E, PlayerCombat3D duoc bat tam thoi.
   - UI hien tien do: `On dinh khe nut: X%`.
   - Hac Tinh pressure enemies spawn tu cave spawn points.
   - Sau 30 giay, TimeRift qua tai va load S03 neu scene da co trong Build Settings.

### Cutscene S02

Da them `S02CutsceneController`:

- Intro fade/camera move.
- Hac Tinh descent camera shake.
- TimeRift resonance orbit shot.
- Ending fade out sang S03.

Cutscene duoc gan tu `S02CaveBuilder` va co fallback tu runtime neu scene cu chua rebuild.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S02 Underground Cave`

## S03_CoLoa Combat Arena

### Muc tieu canh

S03 la arena chien dau single-player tren map Co Loa:

- Giu nguyen map `coloa_map_stage03_unity_colored` lam nen chinh.
- Them player Anh Thu voi movement, dash, light attack va heavy attack.
- Spawn Hac Tinh theo wave tu cac spawn point quanh khu chien dau.
- Hien thanh mau player va thanh mau tung Hac Tinh.
- Sau moi wave, hien 3 Blessings de nguoi choi tao build rieng.
- Co 20 BlessingDefinition cho 5 hero types: Trung Trac, Trung Nhi, Quang Trung, An Duong Vuong, Thanh Giong.

### Menu builder

- `Tools > Dong Chay Anh Hung > Rebuild S03 Co Loa Combat Map`
- `Tools > Dong Chay Anh Hung > Verify S03 Co Loa Combat Map`

Builder se tao root `S03_CoLoaCombatIntegration`, player, camera, HUD, `S03_BlessingManager`, `S03_ArenaDirector`, invisible combat floor/boundary va 8 spawn point tren map Co Loa.

`S03CoLoaAutoBuildOnOpen` se tu dong chay builder mot lan khi Unity mo project neu `Assets/Scenes/S03.unity` chua co lop combat S03 day du.

## Scripts chinh

### Runtime

- `Assets/Scripts/Player/PlayerController3D.cs`
- `Assets/Scripts/Player/PlayerHealth3D.cs`
- `Assets/Scripts/Player/PlayerCombat3D.cs`
- `Assets/Scripts/Enemy/S01ChaseThreat.cs`
- `Assets/Scripts/Scene/S02CaveEventController.cs`
- `Assets/Scripts/Scene/S02CutsceneController.cs`
- `Assets/Scripts/Scene/S02TimeRiftTrigger.cs`
- `Assets/Scripts/Scene/S03ArenaDirector.cs`
- `Assets/Scripts/Blessing/BlessingManager.cs`
- `Assets/Scripts/Blessing/BlessingRuntimeController.cs`
- `Assets/Scripts/Scene/EscapeDoorQTE.cs`
- `Assets/Scripts/Player/SlowZone.cs`

### Editor builders

- `Assets/Scripts/Editor/S01CityEscapeBuilder.cs`
- `Assets/Scripts/Editor/S01ChaseSetupBuilder.cs`
- `Assets/Scripts/Editor/S02CaveBuilder.cs`
- `Assets/Scripts/Editor/S03CoLoaCombatArenaBuilder.cs`
- `Assets/Scripts/Editor/S03CoLoaAutoBuildOnOpen.cs`
- `Assets/Scripts/Editor/PlayerVisualBuilder.cs`

## Cach test nhanh

### Test S01

1. Mo scene `Assets/Scenes/S01_CityPrototype.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S01 City Escape Zigzag`.
3. Chay `Tools > Dong Chay Anh Hung > Create S01 Chase Threat`.
4. Bam Play.
5. Test route: tutorial start -> side route -> QTE blocker -> slow zone -> collapse zone.

### Test S02

1. Mo scene `Assets/Scenes/S02_UndergroundCave.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S02 Underground Cave`.
3. Bam Play.
4. Test flow: intro -> ancient signs -> voices -> Hac Tinh descent -> TimeRift resonance -> 30s stabilization -> ending cutscene.

### Test S03

1. Mo scene `Assets/Scenes/S03.unity`.
2. Chay `Tools > Dong Chay Anh Hung > Rebuild S03 Co Loa Combat Map`.
3. Chay `Tools > Dong Chay Anh Hung > Verify S03 Co Loa Combat Map`.
4. Bam Play.
5. Test flow: spawn tren map Co Loa -> Hac Tinh wave -> giet het wave -> chon Blessing -> wave tiep theo -> game over/victory state.

## Ghi chu hien tai

- Project da co `.gitignore` Unity de khong commit `Library`, `Temp`, `obj`, `.vs`.
- Hinh anh/animation player hien van la prototype va can refactor sau neu muon chat luong nhan vat tot hon.
- Mot so warning `System.Net.Http` / `System.IO.Compression` den tu Unity AI / package editor, khong phai loi gameplay script.
