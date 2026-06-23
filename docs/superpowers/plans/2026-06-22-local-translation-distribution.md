# FocusGuard Local Translation Distribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a separate FocusGuard executable that inherits the current UI/themes, calls Google Translate directly, stores all user data under `%LocalAppData%\FocusGuard`, and is previewed before producing the final two-file distribution.

**Architecture:** `FocusGuard.Local` is a separate WPF project that links the current project's UI and unchanged code while replacing only files that need local persistence or Apps Script removal. Translation and dictionary parsing live behind a focused service, and dictionary history lives in its own SQLite repository. Release publishing is gated behind a user-approved development run.

**Tech Stack:** .NET 8 WPF, C# 12, `HttpClient`, `System.Text.Json`, Microsoft.Data.Sqlite, Dapper, Microsoft WebView2, xUnit

---

## File map

- Create `src/FocusGuard.Local/FocusGuard.Local.csproj`: linked-source WPF project and single-file publish settings.
- Create `src/FocusGuard.Local/Infrastructure/UserDataPaths.cs`: canonical `%LocalAppData%\FocusGuard` paths.
- Create `src/FocusGuard.Local/Services/GoogleTranslationService.cs`: direct HTTP request and Google response parsing.
- Create `src/FocusGuard.Local/Data/DictionaryHistoryRepository.cs`: local translation-history schema and CRUD.
- Create `src/FocusGuard.Local/DictTranslatorWindow.xaml`: current themed UI with local-storage wording.
- Create `src/FocusGuard.Local/DictTranslatorWindow.xaml.cs`: local service/repository orchestration.
- Create `src/FocusGuard.Local/Data/DatabaseHelper.cs`: linked project replacement using the local DB path.
- Create `src/FocusGuard.Local/MemoWindow.xaml.cs`: linked project replacement using the local memo path.
- Create `src/FocusGuard.Local/StudyWindow.xaml.cs`: linked project replacement using the local memo path.
- Create `tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj`: non-UI unit-test project.
- Create `tests/FocusGuard.Local.Tests/UserDataPathsTests.cs`: path invariants.
- Create `tests/FocusGuard.Local.Tests/GoogleTranslationServiceTests.cs`: HTTP/parsing behavior.
- Create `tests/FocusGuard.Local.Tests/DictionaryHistoryRepositoryTests.cs`: SQLite CRUD behavior.
- Create `packaging/FocusGuard-Setup.bat`: WebView2 detection/download/install.
- Create `scripts/Publish-Local.ps1`: clean two-file release publication.

### Task 1: Scaffold the isolated linked-source project

**Files:**
- Create: `src/FocusGuard.Local/FocusGuard.Local.csproj`
- Create: `tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj`

- [ ] **Step 1: Create the WPF project with default item discovery disabled**

Use this project skeleton so source files remain untouched and current theme XAML is linked at every build:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <AssemblyName>FocusGuard</AssemblyName>
    <RootNamespace>FocusGuard</RootNamespace>
    <EnableDefaultApplicationDefinition>false</EnableDefaultApplicationDefinition>
    <EnableDefaultPageItems>false</EnableDefaultPageItems>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
  <ItemGroup>
    <ApplicationDefinition Include="..\FocusGuard\App.xaml" Link="App.xaml" />
    <Page Include="..\FocusGuard\**\*.xaml"
          Exclude="..\FocusGuard\App.xaml;..\FocusGuard\DictTranslatorWindow.xaml;..\FocusGuard\bin\**;..\FocusGuard\obj\**">
      <Link>%(RecursiveDir)%(Filename)%(Extension)</Link>
    </Page>
    <Page Include="DictTranslatorWindow.xaml" />
    <Compile Include="..\FocusGuard\**\*.cs"
             Exclude="..\FocusGuard\DictTranslatorWindow.xaml.cs;..\FocusGuard\Data\DatabaseHelper.cs;..\FocusGuard\MemoWindow.xaml.cs;..\FocusGuard\StudyWindow.xaml.cs;..\FocusGuard\bin\**;..\FocusGuard\obj\**">
      <Link>%(RecursiveDir)%(Filename)%(Extension)</Link>
    </Compile>
    <Compile Include="**\*.cs" Exclude="bin\**;obj\**" />
    <PackageReference Include="Dapper" Version="2.1.79" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.8" />
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.3967.48" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create the xUnit test project**

Reference `FocusGuard.Local.csproj`; use `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`, with `net8.0-windows` as the target framework.

- [ ] **Step 3: Verify the empty replacement project fails for known missing files**

Run: `dotnet build src/FocusGuard.Local/FocusGuard.Local.csproj`

Expected: FAIL because excluded local replacements do not exist yet; no source file under `src/FocusGuard` changes.

- [ ] **Step 4: Commit the scaffold**

Run:

```powershell
git add src/FocusGuard.Local/FocusGuard.Local.csproj tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj
git commit -m "build: scaffold isolated local distribution"
```

### Task 2: Centralize local user-data paths

**Files:**
- Create: `src/FocusGuard.Local/Infrastructure/UserDataPaths.cs`
- Create: `tests/FocusGuard.Local.Tests/UserDataPathsTests.cs`
- Create: `src/FocusGuard.Local/Data/DatabaseHelper.cs`
- Create: `src/FocusGuard.Local/MemoWindow.xaml.cs`
- Create: `src/FocusGuard.Local/StudyWindow.xaml.cs`

- [ ] **Step 1: Write failing path tests**

Test that `RootDirectory` equals `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusGuard")`, `DatabasePath` ends in `focusguard.db`, and `MemoPath` ends in `memo.txt`.

- [ ] **Step 2: Run the focused tests and confirm failure**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter UserDataPathsTests`

Expected: FAIL because `UserDataPaths` does not exist.

- [ ] **Step 3: Implement canonical paths**

```csharp
namespace FocusGuard.Infrastructure;

public static class UserDataPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusGuard");

    public static string DatabasePath { get; } = Path.Combine(RootDirectory, "focusguard.db");
    public static string MemoPath { get; } = Path.Combine(RootDirectory, "memo.txt");

    public static void EnsureDirectory() => Directory.CreateDirectory(RootDirectory);
}
```

- [ ] **Step 4: Create local replacements for existing path consumers**

Copy the three corresponding source files into `FocusGuard.Local`. In `DatabaseHelper`, call `UserDataPaths.EnsureDirectory()` and build the connection string from `UserDataPaths.DatabasePath`. In both memo-related files replace only `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memo.txt")` with `UserDataPaths.MemoPath` and add the infrastructure namespace import.

- [ ] **Step 5: Run tests and compile the replacement files**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter UserDataPathsTests`

Expected: PASS.

- [ ] **Step 6: Commit local persistence paths**

```powershell
git add src/FocusGuard.Local/Infrastructure src/FocusGuard.Local/Data/DatabaseHelper.cs src/FocusGuard.Local/MemoWindow.xaml.cs src/FocusGuard.Local/StudyWindow.xaml.cs tests/FocusGuard.Local.Tests/UserDataPathsTests.cs
git commit -m "feat: store user data under local app data"
```

### Task 3: Implement local dictionary-history persistence

**Files:**
- Create: `src/FocusGuard.Local/Data/DictionaryHistoryRepository.cs`
- Create: `tests/FocusGuard.Local.Tests/DictionaryHistoryRepositoryTests.cs`

- [ ] **Step 1: Write failing CRUD tests against a temporary SQLite file**

Cover schema initialization, insertion of `사전` and `번역` records, newest-first ordering, stable integer IDs, deletion by ID, and an empty result after deletion. Delete the temporary directory in test teardown.

- [ ] **Step 2: Verify the repository tests fail**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter DictionaryHistoryRepositoryTests`

Expected: FAIL because the repository does not exist.

- [ ] **Step 3: Implement the schema and CRUD API**

Use this schema:

```sql
CREATE TABLE IF NOT EXISTS dictionary_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    searched_at TEXT NOT NULL,
    type TEXT NOT NULL,
    input TEXT NOT NULL,
    output TEXT NOT NULL
);
```

Expose `Initialize()`, `Add(string type, string input, string output)`, `IReadOnlyList<DictionaryHistoryRecord> GetAll()`, and `Delete(int id)`. The default constructor uses `UserDataPaths.DatabasePath`; an internal/test constructor accepts an explicit DB path. Map `id` to the existing view-model property `rowIndex` and format display time as `yyyy-MM-dd HH:mm:ss`.

- [ ] **Step 4: Verify CRUD tests pass**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter DictionaryHistoryRepositoryTests`

Expected: PASS.

- [ ] **Step 5: Commit the repository**

```powershell
git add src/FocusGuard.Local/Data/DictionaryHistoryRepository.cs tests/FocusGuard.Local.Tests/DictionaryHistoryRepositoryTests.cs
git commit -m "feat: persist dictionary history locally"
```

### Task 4: Implement direct Google translation

**Files:**
- Create: `src/FocusGuard.Local/Services/GoogleTranslationService.cs`
- Create: `tests/FocusGuard.Local.Tests/GoogleTranslationServiceTests.cs`

- [ ] **Step 1: Write failing HTTP and parser tests**

Use a fake `HttpMessageHandler`. Assert that the request targets `https://translate.googleapis.com/translate_a/single`, includes `client=gtx`, `sl=auto`, `tl=ko`, `dt=t`, `dt=bd`, and URL-encodes the query. Test concatenated sentence segments, English dictionary parts of speech, fallback to basic meaning, non-success HTTP status, and malformed JSON.

- [ ] **Step 2: Verify focused tests fail**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter GoogleTranslationServiceTests`

Expected: FAIL because `GoogleTranslationService` does not exist.

- [ ] **Step 3: Implement the service**

Expose:

```csharp
public sealed record TranslationResult(string DisplayText, string PlainTranslation);

public sealed class GoogleTranslationService
{
    public GoogleTranslationService(HttpClient httpClient);
    public Task<TranslationResult> TranslateAsync(string query, bool dictionaryMode, CancellationToken cancellationToken = default);
}
```

Build the URL with `Uri.EscapeDataString(query)`. Parse sentence text from the first element array. In dictionary mode, format `root[1]` entries as `▪ [part of speech]: meaning1, meaning2`; when dictionary detail is absent return `🌐 [영한 번역]\n결과: {translation}`. Throw a typed `TranslationServiceException` for HTTP, empty-result, and malformed-response failures; do not silently return the raw response.

- [ ] **Step 4: Run parser/service tests**

Run: `dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj --filter GoogleTranslationServiceTests`

Expected: PASS.

- [ ] **Step 5: Commit direct translation**

```powershell
git add src/FocusGuard.Local/Services/GoogleTranslationService.cs tests/FocusGuard.Local.Tests/GoogleTranslationServiceTests.cs
git commit -m "feat: call Google translation directly"
```

### Task 5: Wire the local translator window

**Files:**
- Create: `src/FocusGuard.Local/DictTranslatorWindow.xaml`
- Create: `src/FocusGuard.Local/DictTranslatorWindow.xaml.cs`

- [ ] **Step 1: Copy the current themed translator XAML**

Copy the current `src/FocusGuard/DictTranslatorWindow.xaml` so all of Antigravity's current dynamic resources and layout are preserved. Change only user-facing storage wording: `시트 자동 저장` to `로컬 자동 저장`, and `구글 번역 실행 (시트 저장)` to `번역 실행 (로컬 저장)`.

- [ ] **Step 2: Replace Apps Script orchestration**

Construct one `HttpClient`, `GoogleTranslationService`, and `DictionaryHistoryRepository` per window. Initialize the repository on load. Search and translation handlers must trim input, disable their button, await the service, update the result box, persist only successful results, invalidate `_logsLoaded`, and restore the button in `finally`.

- [ ] **Step 3: Replace remote history operations**

`LoadLogsAsync` reads `repository.GetAll()` and assigns `ListLogs.ItemsSource`. `DeleteRow` deletes the stable local ID after confirmation and reloads the list. Keep existing event-handler names so the copied XAML remains compatible.

- [ ] **Step 4: Build and run all unit tests**

Run:

```powershell
dotnet build src/FocusGuard.Local/FocusGuard.Local.csproj
dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj
```

Expected: build succeeds; all tests pass.

- [ ] **Step 5: Confirm the original source tree was not modified by this implementation**

Run: `git diff -- src/FocusGuard`

Expected: only the user's pre-existing Antigravity changes appear; no implementation edits from this work.

- [ ] **Step 6: Commit the window integration**

```powershell
git add src/FocusGuard.Local/DictTranslatorWindow.xaml src/FocusGuard.Local/DictTranslatorWindow.xaml.cs
git commit -m "feat: use local translation workflow"
```

### Task 6: Development-run checkpoint

**Files:** None

- [ ] **Step 1: Build Debug without publishing**

Run: `dotnet build src/FocusGuard.Local/FocusGuard.Local.csproj -c Debug`

Expected: Build succeeded with zero errors.

- [ ] **Step 2: Launch the development executable**

Run: `Start-Process -FilePath (Resolve-Path 'src/FocusGuard.Local/bin/Debug/net8.0-windows/win-x64/FocusGuard.exe')`

Expected: the themed main window opens.

- [ ] **Step 3: Perform hands-on checks**

Check theme switching, English dictionary search, sentence translation, persisted history, history deletion, restart persistence, study-session DB, memo persistence, and WebView screens. Verify files are created under `%LocalAppData%\FocusGuard` and not beside the executable.

- [ ] **Step 4: Stop before release publication**

Report the running build path and wait for the user to inspect it. Do not execute Task 7 until the user explicitly approves the preview.

### Task 7: Create the two-file release after approval

**Files:**
- Create: `packaging/FocusGuard-Setup.bat`
- Create: `scripts/Publish-Local.ps1`

- [ ] **Step 1: Write the WebView2 setup script**

The BAT checks both 32-bit and 64-bit EdgeUpdate registry client keys for WebView2 Runtime. If absent, download the Microsoft Evergreen Bootstrapper from `https://go.microsoft.com/fwlink/p/?LinkId=2124703` to `%TEMP%\MicrosoftEdgeWebview2Setup.exe`, run `/silent /install`, check the exit code, delete the temporary file, and display a Korean success/failure message. It must not download anything when WebView2 is already installed.

- [ ] **Step 2: Write a deterministic publish script**

The PowerShell script removes only the verified `dist/FocusGuard-local-win-x64` directory, runs `dotnet publish src/FocusGuard.Local/FocusGuard.Local.csproj -c Release -r win-x64 --self-contained true`, copies only the produced `FocusGuard.exe` and `packaging/FocusGuard-Setup.bat`, then fails unless the output contains exactly those two files.

- [ ] **Step 3: Publish and inspect output**

Run: `powershell -ExecutionPolicy Bypass -File scripts/Publish-Local.ps1`

Expected: `dist/FocusGuard-local-win-x64` contains exactly `FocusGuard.exe` and `FocusGuard-Setup.bat`.

- [ ] **Step 4: Smoke-test the published EXE**

Launch `dist/FocusGuard-local-win-x64/FocusGuard.exe`, perform one translation, close, relaunch, and verify the history remains under `%LocalAppData%\FocusGuard`.

- [ ] **Step 5: Commit packaging assets**

```powershell
git add packaging/FocusGuard-Setup.bat scripts/Publish-Local.ps1
git commit -m "build: add two-file Windows distribution"
```

## Final verification

Run:

```powershell
dotnet test tests/FocusGuard.Local.Tests/FocusGuard.Local.Tests.csproj
git diff --check
Get-ChildItem dist/FocusGuard-local-win-x64 | Select-Object Name
```

Expected: all tests pass, `git diff --check` reports no whitespace errors, and the final listing contains exactly the BAT and EXE.
