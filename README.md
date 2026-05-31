# Focus Guard (포커스 가드)

Focus Guard는 사용자의 집중력 향상을 돕고 공부 시간을 추적하는 Windows 데스크톱 애플리케이션입니다. 
악성코드(Malware)와 같은 강압적인 시스템 제어를 지양하고, 사용자의 자율성과 '안전 경계'를 존중하는 스마트 락 기능을 제공합니다.

## 🎯 주요 기능
- **웹사이트 잠금 모드 (Website Lock Mode):** 내장 브라우저(WebView2)를 사용하여 지정된 URL 외의 탐색, 팝업, 다운로드를 차단합니다.
- **프로그램 잠금 모드 (Program Lock Mode):** 지정된 실행 파일이 최상단 활성 창인지 감지하고, 이탈 시 경고 및 실제 집중 시간을 기록합니다.
- **자동/수동 모드 (Auto/Manual Mode):** 뽀모도로(Pomodoro) 기법과 유사하게 공부-휴식 사이클을 자동화합니다.
- **비상 해제 (Emergency Unlock):** 특정 문장을 입력하고 10초를 대기하는 의도적인 허들을 두어 충동적인 세션 종료를 방지합니다.
- **대시보드 (Dashboard):** SQLite 기반의 로컬 데이터를 바탕으로 일간/주간/누적 집중 시간 및 스트릭(Streak)을 확인합니다.

## 🛠️ 기술 스택
- **플랫폼:** Windows (Windows 10/11)
- **프레임워크:** .NET 8 (C#) / WPF (Windows Presentation Foundation)
- **데이터베이스:** SQLite (Microsoft.Data.Sqlite)
- **웹 컴포넌트:** WebView2 (Microsoft.Web.WebView2)
- **언어:** 한국어 (ko-KR) 기본 지원 (리소스 파일 분리)

## 🛡️ 안전 경계 (Safety Boundaries)
이 앱은 자기 통제 및 학습 추적을 위한 도구이며 스파이웨어가 아닙니다.
- 작업 관리자(Task Manager)에서 숨지 않습니다.
- 프로그램 삭제를 방해하지 않습니다.
- 시스템 보안 기능을 비활성화하거나 Ctrl+Alt+Delete를 막지 않습니다.
- 백그라운드에 비밀스럽게 상주하거나 루트킷 기술을 사용하지 않습니다.
