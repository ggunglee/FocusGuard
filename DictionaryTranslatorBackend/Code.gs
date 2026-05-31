// 설정: Apps Script의 스크립트 속성(Settings -> Script Properties)에 아래 변수들을 저장하거나 이 코드에 직접 입력하세요.
const NAVER_CLIENT_ID = PropertiesService.getScriptProperties().getProperty("NAVER_CLIENT_ID") || "YOUR_NAVER_CLIENT_ID";
const NAVER_CLIENT_SECRET = PropertiesService.getScriptProperties().getProperty("NAVER_CLIENT_SECRET") || "YOUR_NAVER_CLIENT_SECRET";
const SHEET_ID = PropertiesService.getScriptProperties().getProperty("SHEET_ID") || "YOUR_GOOGLE_SHEET_ID"; // 기록할 스프레드시트 ID
const SHEET_NAME = "DictTransLog"; // 기록할 시트 이름 (없으면 자동 생성됨)

function doGet(e) {
  return HtmlService.createHtmlOutputFromFile('Index')
    .setTitle('사전/번역 기록 관리자')
    .addMetaTag('viewport', 'width=device-width, initial-scale=1');
}

function doPost(e) {
  try {
    const postData = JSON.parse(e.postData.contents);
    const action = postData.action;
    
    if (action === "get_logs") {
      return ContentService.createTextOutput(JSON.stringify({ result: getLogsFromSheet() })).setMimeType(ContentService.MimeType.JSON);
    } 
    else if (action === "delete_log") {
      const rowIndex = postData.rowIndex;
      const success = deleteLogFromSheet(rowIndex);
      return ContentService.createTextOutput(JSON.stringify({ result: success ? "Deleted" : "Failed" })).setMimeType(ContentService.MimeType.JSON);
    }
    
    const text = postData.text;
    let resultText = "";
    
    if (action === "dictionary") {
      resultText = searchNaverDictionary(text);
      logToSheet("사전", text, resultText);
    } else if (action === "translate") {
      // 구글 내장 번역기 사용 (무료)
      resultText = LanguageApp.translate(text, '', 'ko');
      logToSheet("번역", text, resultText);
    } else {
      return ContentService.createTextOutput(JSON.stringify({ error: "Unknown action" })).setMimeType(ContentService.MimeType.JSON);
    }
    
    return ContentService.createTextOutput(JSON.stringify({ result: resultText })).setMimeType(ContentService.MimeType.JSON);
    
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ error: err.toString() })).setMimeType(ContentService.MimeType.JSON);
  }
}

function searchNaverDictionary(query) {
  // 입력된 단어에 영어 알파벳이 포함되어 있는지 검사 (영단어 여부)
  if (/[a-zA-Z]/.test(query)) {
    try {
      // 구글 번역의 내부 사전(Dictionary) API 활용
      const url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=ko&dt=t&dt=bd&q=" + encodeURIComponent(query);
      const res = UrlFetchApp.fetch(url, { muteHttpExceptions: true });
      const json = JSON.parse(res.getContentText());
      
      let output = "🌐 [영한 사전]\n\n";
      
      // json[1]에 상세 품사와 뜻 배열이 들어있습니다.
      if (json[1] && json[1].length > 0) {
        for(let i=0; i<json[1].length; i++) {
          let pos = json[1][i][0]; // 품사 (noun, adjective 등)
          let meanings = json[1][i][1].join(", "); // 뜻 목록
          output += "▪ [" + pos + "]: " + meanings + "\n";
        }
        return output;
      } else if (json[0] && json[0][0] && json[0][0][0]) {
        // 사전 결과는 없지만 일반 번역 텍스트는 있는 경우
        return output + "기본 의미: " + json[0][0][0];
      }
    } catch(e) {}
    
    // 만약 파싱에 실패하면 일반 번역으로 폴백
    return "🌐 [영한 번역]\n결과: " + LanguageApp.translate(query, '', 'ko');
  }

  if (NAVER_CLIENT_ID === "YOUR_NAVER_CLIENT_ID") {
    // API 키가 설정 안된 경우 대체 수단으로 구글 번역 결과라도 반환
    return "⚠️ 네이버 API 키가 설정되지 않았습니다.\n\n[구글 대체 번역 뜻]:\n" + LanguageApp.translate(query, '', 'ko');
  }
  
  // 네이버 검색 API (백과사전/사전 - encyc)
  const url = "https://openapi.naver.com/v1/search/encyc.json?query=" + encodeURIComponent(query);
  const options = {
    method: "get",
    headers: {
      "X-Naver-Client-Id": NAVER_CLIENT_ID,
      "X-Naver-Client-Secret": NAVER_CLIENT_SECRET
    },
    muteHttpExceptions: true
  };
  
  const response = UrlFetchApp.fetch(url, options);
  const json = JSON.parse(response.getContentText());
  
  if (json.items && json.items.length > 0) {
    let output = "📖 [" + query + "] 국어/백과사전 검색 결과:\n\n";
    for(let i=0; i<Math.min(json.items.length, 3); i++) { // 상위 3개 뜻
      let title = json.items[i].title.replace(/<[^>]+>/g, '');
      let desc = json.items[i].description.replace(/<[^>]+>/g, '');
      output += (i+1) + ". " + title + " : " + desc + "\n";
    }
    return output;
  } else {
    return "검색 결과가 없습니다.\n\n[구글 번역 결과]:\n" + LanguageApp.translate(query, '', 'ko');
  }
}

function logToSheet(type, input, output) {
  if (SHEET_ID === "YOUR_GOOGLE_SHEET_ID") return; 
  
  try {
    const ss = SpreadsheetApp.openById(SHEET_ID);
    let sheet = ss.getSheetByName(SHEET_NAME);
    if (!sheet) {
      sheet = ss.insertSheet(SHEET_NAME);
      sheet.appendRow(["시간", "유형", "입력/단어", "결과/뜻"]);
    }
    
    const timeStr = Utilities.formatDate(new Date(), "Asia/Seoul", "yyyy-MM-dd HH:mm:ss");
    sheet.appendRow([timeStr, type, input, output]);
  } catch(e) {}
}

// 기록 조회 (웹페이지 및 데스크톱 앱 공용)
function getLogsFromSheet() {
  if (SHEET_ID === "YOUR_GOOGLE_SHEET_ID") return [];
  try {
    const ss = SpreadsheetApp.openById(SHEET_ID);
    let sheet = ss.getSheetByName(SHEET_NAME);
    if (!sheet) return [];
    
    const data = sheet.getDataRange().getValues();
    if (data.length <= 1) return []; 
    
    const logs = [];
    for(let i = 1; i < data.length; i++) {
      logs.push({
        rowIndex: i + 1, 
        time: data[i][0].toString(),
        type: data[i][1].toString(),
        input: data[i][2].toString(),
        output: data[i][3].toString()
      });
    }
    return logs.reverse(); // 최신순
  } catch(e) {
    return [];
  }
}

// 기록 삭제
function deleteLogFromSheet(rowIndex) {
  if (SHEET_ID === "YOUR_GOOGLE_SHEET_ID") return false;
  try {
    const ss = SpreadsheetApp.openById(SHEET_ID);
    let sheet = ss.getSheetByName(SHEET_NAME);
    if (!sheet) return false;
    
    sheet.deleteRow(rowIndex);
    return true;
  } catch(e) {
    return false;
  }
}
