# model_house 협업 컨벤션

> Unity 6 (6000.6.0f1) · URP · GitHub
> 커밋 메시지 규칙은 [COMMIT_RULES.md](COMMIT_RULES.md) 참고

---

## 0. 최초 1회 설정 (clone 직후)

```bash
git clone git@github.com:<계정>/model_house.git
cd model_house
git config core.hooksPath .githooks   # 커밋 규칙 자동 검사 활성화
git lfs install                       # 모델·텍스처 LFS
git checkout develop
```

- Unity 에디터 버전은 **팀 전원 동일 버전**(6000.6.0f1)을 사용한다. 버전 업은 `chore:` 커밋으로 한 사람이 올리고 공지한다.
- `Edit → Project Settings → Editor` 에서 아래 두 값이 바뀌어 있지 않은지 확인한다.
  - Version Control Mode: **Visible Meta Files**
  - Asset Serialization Mode: **Force Text**

---

## 1. 브랜치 구조

| 브랜치 | 역할 | 직접 커밋 |
|---|---|---|
| `main` | 항상 빌드 가능한 배포본 ("완성된 버전 보관소") | ❌ 금지 — develop에서만 머지 |
| `develop` | 팀 통합 브랜치, 매일 작업의 기준 | ❌ 금지 — PR로만 머지 |
| `feature/이름-기능` | 1인 1기능 작업 공간 | ✅ |

### feature 브랜치 이름 규칙

```
feature/<이름>-<기능>
```

- 영문 소문자, 숫자, 하이픈(`-`)만 사용한다.
- 이름만 쓰지 않고 **기능까지** 적는다. 한 사람이 여러 기능을 작업할 수 있기 때문이다.

| ✅ 좋은 예 | ❌ 나쁜 예 |
|---|---|
| `feature/jake-inventory` | `feature/jake` (기능 없음) |
| `feature/sam-dialogue` | `feature/Sam_Dialogue` (대문자·언더스코어) |
| `feature/mina-main-ui` | `mina-ui` (`feature/` 접두어 없음) |

---

## 2. 작업 흐름

```bash
# 1. develop 최신화 후 브랜치 생성
git checkout develop
git pull origin develop
git checkout -b feature/jake-inventory

# 2. 작업 · 짧게 끊어서 자주 커밋
git add .
git commit -m "feat: 인벤토리 슬롯 UI 프리팹 추가"

# 3. PR 올리기 전 develop 최신 변경 반영
git pull origin develop
#   (충돌 시 해결 → Unity에서 열어 정상 동작 확인 → 커밋)

# 4. 푸시 후 GitHub에서 develop 대상으로 PR 생성
git push -u origin feature/jake-inventory
```

5. (가능하면) 팀원 1명 이상 리뷰 후 머지한다.
6. 머지된 feature 브랜치는 삭제한다. 기록은 develop 히스토리에 남는다.

```bash
git checkout develop
git pull origin develop
git branch -d feature/jake-inventory
```

### develop → main 릴리스

- develop이 안정화되고 **빌드가 실제로 성공하는 것을 확인한 뒤** develop → main PR을 올린다.
- 릴리스 시점에 태그를 단다: `git tag v0.1.0 && git push origin v0.1.0`

---

## 3. 씬(Scene) 충돌 방지 규칙

씬 파일(`.unity`)은 Git이 자동 병합하지 못하는 경우가 많다. 아래 우선순위대로 적용한다.

### 3-1. 기본: 프리팹 단위로 쪼개기 ★

- **씬 담당자는 1명**으로 정한다. (`MainScene` 담당: ________)
- 나머지 팀원은 자기 기능을 **프리팹**으로 만들고, 씬에는 인스턴스만 배치한다.

```
MainScene  (씬 담당자만 수정)
├─ Inventory.prefab    ← Jake
├─ DialogueUI.prefab   ← Sam
└─ MainUI.prefab       ← Mina
```

### 3-2. 기능별로 독립적이면: 씬 분리 (Additive)

```
MainScene (베이스, 거의 안 바뀜)
├─ Scene_Inventory   ← Jake만 수정
├─ Scene_Dialogue    ← Sam만 수정
└─ Scene_UI          ← Mina만 수정
```

```csharp
SceneManager.LoadSceneAsync("Scene_Inventory", LoadSceneMode.Additive);
```

> 지형/월드가 베이스 씬 하나에 강하게 묶여 있으면 3-1 방식을 쓴다.

### 3-3. 어쩔 수 없이 같은 씬을 같이 써야 할 때

- 작업 시작 전 팀 채널에 공유: **"지금 MainScene 작업 중!"** → 끝나면 **"MainScene 작업 끝, 푸시함"**
- 짧게 작업하고 바로 커밋·머지한다. 오래 들고 있을수록 충돌이 커진다.
- 씬 변경은 **다른 변경과 섞지 않고 단독 커밋**으로 만든다. (충돌 시 되돌리기 쉬움)
- 충돌 나면 UnityYAMLMerge(Smart Merge)로 해결한다. → [5. Smart Merge 설정](#5-smart-merge-설정-선택)

---

## 4. 프리팹 수정 규칙 (필수)

씬에 배치된 프리팹 인스턴스를 씬에서 그냥 고치면, 변경 내용이 `.prefab`이 아니라 **`.unity`에 기록**된다. 그러면 프리팹으로 쪼갠 의미가 사라진다.

프리팹을 고칠 때는 아래 둘 중 하나를 **반드시** 따른다.

| 방법 | 방법 설명 | 비고 |
|---|---|---|
| **A. Prefab Mode** ★ | 프리팹을 더블클릭 → 회색 배경 Prefab Mode에서 수정 | 변경이 `.prefab`에 바로 저장됨. 권장 |
| **B. Apply All** | 씬에서 수정 후 Inspector 상단 **Overrides → Apply All** | Apply 안 하면 씬 파일에 오버라이드로 남음 |

> 인스턴스마다 달라야 하는 값(위치·회전 등)만 씬에 오버라이드로 남긴다. 그 외는 모두 프리팹에 반영한다.

---

## 5. Smart Merge 설정 (선택)

로컬 `.git/config`에만 적용되는 설정이다. 경로는 각자 Unity 설치 경로에 맞게 바꾼다.

```bash
git config merge.tool unityyamlmerge
git config mergetool.unityyamlmerge.trustExitCode false
git config mergetool.unityyamlmerge.cmd "'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p \"\$BASE\" \"\$REMOTE\" \"\$LOCAL\" \"\$MERGED\""
```

충돌 시: `git mergetool`

---

## 6. Unity × Git 기본 규칙

- **`.meta` 파일은 반드시 원본 에셋과 같이 커밋한다.** (추가·삭제·이름 변경 모두)
  - 에셋 이동·이름 변경은 **Unity Project 창 안에서** 한다. 탐색기에서 옮기면 `.meta`가 끊겨 참조가 깨진다.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`은 커밋하지 않는다. (`.gitignore` 처리됨)
- 모델·텍스처·오디오 등 대용량 바이너리는 Git LFS로 관리된다. (`.gitattributes` 처리됨)
- 커밋 전 Unity에서 **File → Save (Ctrl+S)** 로 씬·프리팹을 저장했는지 확인한다. 저장 안 한 변경은 커밋되지 않는다.
- Console에 **에러가 있는 상태로 PR을 올리지 않는다.**

---

## 7. 체크리스트

작업 시작 전, 커밋 전에 한 번씩 확인한다.

- [ ] `main` / `develop`에 직접 커밋하지 않았는가?
- [ ] `develop`에서 `feature/이름-기능` 브랜치를 새로 따서 작업했는가?
- [ ] UI·오브젝트는 가능하면 프리팹으로 만들었는가?
- [ ] 프리팹은 Prefab Mode에서 수정했는가? (또는 수정 후 Overrides → Apply All을 눌렀는가?)
- [ ] 같은 씬을 건드렸다면 팀 채널에 공유했는가?
- [ ] 작업을 짧게 끊어서 커밋했는가?
- [ ] `.meta` 파일이 같이 커밋되었는가?
- [ ] PR 올리기 전 `git pull origin develop`으로 최신 변경을 받아왔는가?
