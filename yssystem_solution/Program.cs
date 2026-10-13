using yssystem_solution.Data;

namespace yssystem_solution
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // 처리되지 않은 예외 처리 방식은 창(컨트롤)을 만들기 "전"에 정해야 함
            // (창이 만들어진 뒤에 부르면 InvalidOperationException이 날 수 있음)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            var mainForm = new EquipmentMonitorForm();

            // UI 스레드에서 처리되지 않은 예외는 모두 여기서 처리 (프로그램이 종료되지 않게)
            // ※ Task.Run으로 돌리는 Worker 스레드의 예외는 여기로 오지 않으므로 루프 안에서 직접 처리
            // ※ 사용자가 직접 한 행동(버튼 등)에서 난 오류가 여기로 오므로, 메시지 창으로 확실히 알림
            // ※ 처리기 안에서 mainForm을 쓰므로 창을 만든 뒤에 등록
            Application.ThreadException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[처리되지 않은 예외] {e.Exception}");

                if (DbErrorHelper.IsDbError(e.Exception))
                {
                    // DB 문제: 알기 쉬운 문구로 안내하고 화면의 DB 상태 표시도 갱신
                    mainForm.SetDbStatus(false);
                    MessageBox.Show(DbErrorHelper.UserMessage, "DB 오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    // 그 외(예상하지 못한 버그 등)
                    // 지금은 DB에 따로 저장하지 않지만 이런것도 DB에 기록해야할 것 같음.
                    MessageBox.Show($"예상치 못한 오류가 발생했습니다.\n{e.Exception.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Application.Run(mainForm);
        }
    }
}
