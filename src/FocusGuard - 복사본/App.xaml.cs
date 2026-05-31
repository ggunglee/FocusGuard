using System.Windows;
using FocusGuard.Data;

namespace FocusGuard
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 앱이 켜질 때 DB 파일(focusguard.db)과 테이블을 자동 생성합니다.
            var dbHelper = new DatabaseHelper();
            dbHelper.InitializeDatabase();
        }
    }
}
