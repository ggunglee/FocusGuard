# FocusGuard Handoff

## 1. 프로젝트 개요

FocusGuard는 Windows WPF 기반 집중 보조 앱이다.

사용자가 공부나 작업을 할 때 지정한 웹사이트, 앱, 파일만 사용하도록 유도하고, 집중 시간과 딴짓 시간을 기록한다.

핵심 흐름:

1. 사용자가 과제 이름, 목표 시간, URL 또는 파일, 허용 앱을 등록한다.
2. 과제를 시작하면 StudyWindow 타이머 위젯이 열린다.
3. 웹 리소스가 있으면 WebLockWindow 기반 WebView2 창이 열린다.
4. 지정된 앱 또는 웹뷰가 활성 상태이면 집중으로 기록한다.
5. 허용되지 않은 창을 보면 딴짓으로 기록하고 허용 창을 앞으로 가져온다.
6. 세션 종료 시 SQLite DB에 집중 기록을 저장한다.
7. 대시보드에서 오늘/최근 기록, 집중률, CSV 내보내기를 확인할 수 있다.

---

## 2. 프로젝트 위치

프로젝트 루트:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard
```

csproj 위치:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\src\FocusGuard
```

배포 폴더:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64
```

배포 EXE:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64\FocusGuard.exe
```

ZIP 배포 파일:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64.zip
```

---

## 3. 주요 파일 설명

### App.xaml.cs

앱 시작 시 설정과 DB를 초기화한다.

현재 주요 흐름:

```csharp
AppSettings.Load();
EnsureWebView2Runtime();
var dbHelper = new DatabaseHelper();
dbHelper.InitializeDatabase();
```

최근 WebView2 Runtime 자동 확인/설치 로직이 추가되었다.

앱 실행 시 WebView2 Runtime이 없으면 배포 폴더에 포함된 설치 파일을 찾아 자동 설치를 시도한다.

후보 설치 파일명:

```text
MicrosoftEdgeWebView2RuntimeInstallerX64.exe
MicrosoftEdgeWebView2Setup.exe
```

---

### AppSettings.cs

앱 설정을 관리한다.

현재 설정값:

- FocusedOpacity
- IsRealRestMode
- ReadingLogUrl

기록용 사이트 URL 저장 위치:

```text
%LOCALAPPDATA%\FocusGuard\focusguard.settings.json
```

예시:

```text
C:\Users\MBC\AppData\Local\FocusGuard\focusguard.settings.json
```

이전 버전이 EXE 폴더에 저장한 설정이 있으면 새 위치로 복사하는 마이그레이션 코드도 포함되어 있다.

---

### DatabaseHelper.cs

SQLite 기반 저장소를 담당한다.

테이블:

- sessions
- missions

주요 기능:

- InitializeDatabase()
- SaveSession()
- GetTodaySessions()
- GetWeeklySessions()
- GetStudyDates()
- GetMissions()
- AddMission()
- DeleteMission()

현재 DB 연결 문자열:

```csharp
Data Source=focusguard.db
```

주의:

현재 DB는 상대 경로 기반이다. 실행 위치에 따라 DB 생성 위치가 달라질 수 있다. 추후에는 AppData 또는 실행 폴더 기준 절대 경로로 고정하는 것이 좋다.

---

### MainWindow.xaml / MainWindow.xaml.cs

과제 등록, 과제 시작, 대시보드, 환경 설정 진입을 담당한다.

최근 변경사항:

- 메인 창 크기 조절 가능
- 타겟 앱 직접 입력칸 숨김
- 앱찾기 버튼 유지
- URL 붙여넣기 버튼 복구
- 파일 선택 폴더 버튼 유지
- 기록 사이트 체크박스 추가
- 선택된 앱 목록 표시

UI 변경:

- InputApp은 남겨두되 Visibility="Collapsed" 처리
- TxtSelectedApps로 선택된 앱 표시
- ChkUseReadingLog 체크박스 추가
- BtnPasteUrl_Click 추가
- BtnPickApp_Click 추가

앱찾기 동작:

1. 현재 열린 창 목록을 가져온다.
2. 사용자가 창을 선택한다.
3. 해당 창의 ProcessName을 InputApp.Text에 저장한다.
4. 과제 저장 시 기존 missions.target_app 필드에 저장한다.

DB 구조 변경 없이 기능을 추가하기 위해 기존 target_app, resource_path 컬럼을 계속 사용한다.

---

### SettingsWindow.xaml / SettingsWindow.xaml.cs

환경 설정 창이다.

현재 설정 가능 항목:

- 집중 중 위젯 투명도
- 찐휴식 모드
- 기록용 사이트 URL

기록용 사이트 URL은 AppSettings.ReadingLogUrl에 저장된다.

저장 버튼 클릭 시 현재 저장 위치도 메시지로 보여주도록 수정했다.

---

### StudyWindow.xaml.cs

세션 타이머와 감시 로직을 담당한다.

주요 역할:

- 전체 목표 시간 관리
- 집중/휴식 페이즈 전환
- 집중 시간 기록
- 딴짓 시간 기록
- 허용되지 않은 창 사용 시 경고
- 허용 웹/앱을 다시 앞으로 가져오기
- 세션 종료 시 DB 저장

최근 변경사항:

기존에는 WebLockWindow 하나만 받는 구조였으나, 여러 WebLockWindow를 받을 수 있도록 확장했다.

생성자:

```csharp
public StudyWindow(..., WebLockWindow webWin = null)
public StudyWindow(..., IEnumerable<WebLockWindow> webWins)
```

감시 기준:

- StudyWindow 자체가 활성 상태이면 집중으로 인정
- 연결된 WebLockWindow 중 하나가 활성 상태이면 집중으로 인정
- target_app에 등록된 프로세스 중 하나가 활성 창이면 집중으로 인정
- 그 외에는 딴짓으로 기록

기록 사이트 compactTop 창도 WebLockWindow 목록에 포함된다.

---

### WebLockWindow.xaml.cs

WebView2 기반 웹 집중 창이다.

현재 세 가지 모드가 있다.

#### 1. strictLock 모드

단일 공부 URL용.

특징:

- 전체화면
- Topmost
- WindowStyle None
- ResizeMode NoResize
- ShowInTaskbar false
- KeyboardBlocker 실행
- 최소화 시 다시 최대화
- 허용 도메인 밖으로 이동 차단

#### 2. soft web 모드

여러 웹/앱을 함께 보는 멀티 감시용.

특징:

- 일반 창
- 크기 조절 가능
- 여러 개 띄울 수 있음
- Topmost 아님
- 단축키 차단 없음

#### 3. compactTop 모드

기록용 사이트용.

특징:

- 오른쪽 위 작은 창
- 기본 크기 520 x 260
- ToolWindow
- ResizeMode CanResize
- Topmost true
- ShowInTaskbar true
- 포커스를 빼앗지 않고 계속 맨 위 유지

기록 사이트가 전체화면 웹뷰 뒤에 묻히는 문제가 있어 다음 로직을 추가했다.

- DispatcherTimer 사용
- 0.7초마다 Topmost 순서 재갱신
- SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE) 사용

핵심 메서드:

```csharp
KeepCompactTopMost()
```

---

### WindowManager.cs

Windows API를 통해 현재 활성 창과 열린 창 목록을 다룬다.

주요 기능:

- GetOpenWindows()
- IsTargetWindowActive()
- BringTargetToForeground()

최근 추가:

- OpenWindowInfo 클래스
- EnumWindows 기반 열린 창 목록 조회

앱찾기 버튼에서 이 기능을 사용한다.

주의:

현재 감시는 프로세스명 기반이다. 예를 들어 Chrome을 허용하면 특정 탭만 허용되는 게 아니라 Chrome 프로세스 전체가 허용될 수 있다.

---

### KeyboardBlocker.cs

strictLock 웹 집중 모드에서 탈출 단축키를 차단한다.

차단 대상:

- Windows 키
- Alt+Tab
- Alt+Esc

주의:

앱이 예외 종료될 경우 키보드 훅 해제가 정상적으로 되는지 테스트가 필요하다.

---

## 4. 반영된 요구사항

### 멀티 웹/앱 감시

웹뷰 하나만 강제로 보게 하지 않고, 웹뷰와 메모장/워드/다른 웹을 같이 볼 수 있게 변경했다.

반영:

- StudyWindow가 여러 WebLockWindow와 여러 앱 프로세스를 동시에 감시
- resource_path에는 여러 URL/파일을 콤마, 세미콜론, 줄바꿈으로 입력 가능
- target_app은 기존 DB 구조를 유지하되 내부적으로 콤마 분리

---

### 앱찾기

사용자가 notepad, WINWORD 같은 프로세스명을 몰라도 되도록 앱찾기 버튼을 추가했다.

반영:

- 현재 열린 창 목록에서 선택
- 선택한 창의 ProcessName 자동 저장
- 직접 입력칸은 숨김

---

### 기존 기능 최소 변경

DB 스키마는 변경하지 않았다.

반영:

- missions.target_app 유지
- missions.resource_path 유지
- 기록 사이트는 resource_path에 [READING_LOG] 토큰을 저장하는 방식으로 구현

---

### 기록용 사이트

기록용 자동화 사이트는 작은 창으로 맨 위에 띄운다.

반영:

- AppSettings.ReadingLogUrl 추가
- SettingsWindow에 기록용 사이트 URL 입력칸 추가
- MainWindow에 📚 기록 사이트 체크박스 추가
- 과제 시작 시 [READING_LOG] 토큰이 있으면 ReadingLogUrl을 compactTop WebLockWindow로 실행

---

### 일반 공부 URL

공부 URL은 기존처럼 전체화면 웹뷰로 실행된다.

조건:

- 일반 공부 URL이 하나이고
- 별도 앱 감시가 없으면
- strictLock 모드로 실행

기록 사이트는 별도 compactTop 창으로 실행된다.

---

### 기록 사이트 맨 위 유지

기록 사이트가 전체화면 웹뷰 뒤에 묻히는 문제가 있어 compactTop 모드에서 0.7초마다 Topmost 순서를 재갱신한다.

SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE)를 사용해 포커스를 빼앗지 않도록 했다.

---

### 기록용 사이트 저장 문제

기록용 사이트 URL이 저장되지 않는 문제가 있었다.

원인:

- 초기 구현은 실행 폴더 기준 focusguard.settings.json 저장
- 배포 EXE 환경에서는 실행 폴더 쓰기 권한/위치 문제가 생길 수 있음

해결:

- 저장 위치를 AppData Local로 변경

현재 저장 위치:

```text
%LOCALAPPDATA%\FocusGuard\focusguard.settings.json
```

---

### 실행에 필요한 구성요소 자동 설치

.NET은 self-contained publish로 포함한다.

WebView2 Runtime은 앱 실행 시 확인하고, 없으면 배포 폴더에 있는 설치 파일을 실행한다.

배포 폴더에 포함해야 하는 파일:

```text
MicrosoftEdgeWebView2RuntimeInstallerX64.exe
```

주의:

- WebView2 설치에는 인터넷 연결이 필요할 수 있다.
- 회사/학교 PC 정책에 따라 설치가 차단될 수 있다.
- 설치 후 앱을 다시 실행해야 할 수 있다.

---

## 5. 현재 사용 방법

### 기록용 사이트 설정

1. FocusGuard 실행
2. 환경 설정 클릭
3. 기록용 사이트 URL 입력
4. 저장

저장 파일 확인:

```powershell
notepad "$env:LOCALAPPDATA\FocusGuard\focusguard.settings.json"
```

---

### 기록 사이트 + 공부 URL

1. 새 목표 설정
2. 과제명 입력
3. 목표 시간 입력
4. 📚 기록 사이트 체크
5. 공부 URL 입력
6. 추가
7. START

결과:

- 공부 URL은 전체화면 웹뷰
- 기록 사이트는 오른쪽 위 작은 창
- 기록 사이트는 계속 맨 위 유지

---

### 메모장/워드 허용

1. 메모장 또는 Word를 먼저 실행
2. FocusGuard에서 앱찾기 클릭
3. 열린 창 목록에서 해당 앱 선택
4. 과제 추가
5. START

결과:

- 선택 앱이 활성 상태이면 집중으로 인정
- 다른 창을 보면 딴짓으로 기록

---

## 6. 배포 빌드 방법

PowerShell에서 실행:

```powershell
$ErrorActionPreference = "Stop"

$projectDir = "C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\src\FocusGuard"
$repoRoot = "C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard"

$proj = Get-ChildItem -Path $projectDir -Filter *.csproj -File | Select-Object -First 1
if (-not $proj) {
    throw "csproj 파일을 찾지 못했습니다: $projectDir"
}

$distRoot = Join-Path $repoRoot "dist"
$publishDir = Join-Path $distRoot "FocusGuard-win-x64"
$zipPath = Join-Path $distRoot "FocusGuard-win-x64.zip"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

if (-not (Test-Path $distRoot)) {
    New-Item -ItemType Directory -Path $distRoot | Out-Null
}

dotnet publish "$($proj.FullName)" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    -o "$publishDir"

$webviewInstaller = Join-Path $publishDir "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"

Invoke-WebRequest `
    -Uri "https://go.microsoft.com/fwlink/p/?LinkId=2124703" `
    -OutFile $webviewInstaller

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "배포 빌드 완료!"
Write-Host "EXE 폴더: $publishDir"
Write-Host "ZIP 파일: $zipPath"
```

---

## 7. 배포 시 전달해야 하는 것

권장 전달물:

```text
FocusGuard-win-x64.zip
```

FocusGuard.exe만 단독으로 보내지 말 것.

이유:

- WebView2 Runtime 자동 설치 파일이 함께 있어야 함
- README 등 보조 파일도 함께 전달하는 것이 좋음

---

## 8. 배포 전 테스트 체크리스트

- [ ] 앱 실행 여부 확인
- [ ] 환경 설정에서 기록용 사이트 URL 저장 확인
- [ ] 앱 재시작 후 기록용 사이트 URL 유지 확인
- [ ] 단일 공부 URL 전체화면 실행 확인
- [ ] 기록 사이트 체크 시 오른쪽 위 작은 창 실행 확인
- [ ] 기록 사이트가 전체화면 웹뷰 위에 계속 남는지 확인
- [ ] 기록 사이트가 키보드 포커스를 계속 빼앗지 않는지 확인
- [ ] 앱찾기로 메모장 추가 가능 여부 확인
- [ ] 메모장 활성 시 집중으로 기록되는지 확인
- [ ] 다른 앱 활성 시 딴짓 감지되는지 확인
- [ ] 휴식 전환 확인
- [ ] 찐휴식 모드 확인
- [ ] 비상 해제 확인
- [ ] 세션 종료 후 대시보드 저장 확인
- [ ] CSV 내보내기 확인
- [ ] WebView2 Runtime 없는 PC에서 자동 설치 확인

---

## 9. 알려진 한계

1. 앱 감시는 프로세스명 기반이다.  
   예를 들어 Chrome을 허용하면 특정 탭만이 아니라 Chrome 프로세스 전체가 허용될 수 있다.

2. 기록 사이트 compactTop 창은 강제로 맨 위 유지된다.  
   사용자가 다른 작업을 할 때 약간 거슬릴 수 있다.

3. focusguard.db는 현재 상대 경로 기반이다.  
   실행 위치에 따라 DB 생성 위치가 달라질 수 있다.

4. WebView2 Runtime 설치는 회사/학교 PC 정책에 따라 차단될 수 있다.

5. strictLock 모드에서 KeyboardBlocker가 사용되므로, 예외 종료 시 단축키 훅이 남지 않는지 테스트가 필요하다.

6. WebView2 설치 후 즉시 앱이 정상 실행되지 않으면 앱을 다시 실행해야 할 수 있다.

---

## 10. 추후 개선 아이디어

1. DB 경로를 %LOCALAPPDATA%\FocusGuard\focusguard.db로 고정.
2. target_app 문자열 대신 별도 allowed_targets 테이블 도입.
3. 웹 도메인별 허용 목록을 mission 단위로 관리.
4. Chrome/Edge 외부 브라우저 탭 제목 기반 감시 추가.
5. 기록 사이트 창 크기와 위치를 설정에서 저장.
6. compactTop 유지 간격을 설정 가능하게 변경.
7. 앱찾기 목록에서 이미 선택된 앱 표시.
8. 과제별 기록 사이트 사용 여부를 UI에서 더 명확히 표시.
9. 배포용 설치 프로그램 생성.
10. 앱 버전 표시 추가.
11. 자동 업데이트 도입.
12. 로그 파일 생성 기능 추가.
13. 예외 발생 시 사용자에게 오류 파일 열기 버튼 제공.

---

## 11. 현재 산출물 위치

handoff 문서:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\handoff.md
```

배포 폴더:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64
```

실행 파일:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64\FocusGuard.exe
```

ZIP 파일:

```text
C:\Users\MBC\Desktop\coding\Focus Man\FocusGuard\dist\FocusGuard-win-x64.zip
```

---

## 12. 다음 작업자에게 남기는 메모

이 프로젝트는 단순 타이머 앱이 아니라, 웹뷰와 Windows 창 감시를 섞은 집중 보조 앱이다.

가장 민감한 부분은 다음 세 가지다.

1. WebLockWindow의 strictLock / compactTop 동작
2. KeyboardBlocker의 정상 해제
3. 설정과 DB의 저장 위치

기능을 확장할 때 DB 스키마를 바로 바꾸기보다는, 현재는 기존 구조를 최대한 유지하며 target_app, resource_path에 정보를 저장하는 방식으로 패치되어 있다.

장기적으로는 mission_allowed_targets 같은 별도 테이블을 도입하는 것이 바람직하다.
