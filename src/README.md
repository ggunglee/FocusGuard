# FocusGuard source variants

이 저장소의 Windows 앱 소스는 두 갈래로 관리합니다.

- `FocusGuard/`: 기존 Apps Script 연동 버전입니다. 공통 UI와 앱스크립트 기반 단어 기록 로직을 포함합니다.
- `FocusGuard.Local/`: 배포용 로컬 저장 버전입니다. `FocusGuard/`의 공통 UI를 링크해 쓰고, 단어 검색 기록·메모 저장·번역 처리처럼 구글 계정/App Script 의존성을 없애야 하는 부분만 별도 파일로 대체합니다.

배포 폴더도 같은 기준을 따릅니다.

- `dist/FocusGuard-appscript-win-x64/`
- `dist/FocusGuard-local-win-x64/`

macOS 변환물, 오래된 zip, publish 중간 산출물, 복사본 폴더는 정식 유지 대상이 아닙니다.
