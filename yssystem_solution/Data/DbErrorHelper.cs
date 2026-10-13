using System.ComponentModel;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace yssystem_solution.Data
{
    // DB 오류 판별을 한 곳에서 처리 (폴링, 시뮬레이터, 전역 처리기가 모두 같은 기준을 쓰도록)
    public static class DbErrorHelper
    {
        // 사용자에게 보여줄 DB 오류 안내 문구
        public const string UserMessage = "DB에 연결할 수 없거나 응답이 없습니다.\n잠시 후 다시 시도하세요.";

        // DB 연결 끊김, 응답 시간 초과 등 "DB 쪽 문제"로 난 예외인지 판단
        // - EF Core는 SqlException을 다른 예외 안에 감싸서 던지기도 하므로 안쪽 예외까지 확인
        // - SqlException의 연결 실패/시간 초과는 안쪽에 Win32Exception(또는 SocketException)을 담고 있는데,
        //   WinForms 전역 처리기(Application.ThreadException)는 겉을 벗기고 "가장 안쪽 예외"만 넘겨주므로 이것도 DB 오류로 봄
        public static bool IsDbError(Exception ex)
        {
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is SqlException ||
                    current is TimeoutException ||
                    current is Win32Exception ||
                    current is SocketException)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
