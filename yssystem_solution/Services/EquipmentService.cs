using Microsoft.EntityFrameworkCore;
using yssystem_solution.Data;
using yssystem_solution.Entities;

namespace yssystem_solution.Services
{
    public class EquipmentService
    {
        // 전체 설비 목록 조회 (void -> task로 변경)
        // 비동기의 경우 결과를 바로 반환하지 않기 때문에 Task<T>를 사용해 어떤 형식이 반환될지 명시
        public async Task<List<Equipment>> GetEquipmentListAsync()
        {
            await using var db = new AppDbContext();

            return await db.Equipments
                .AsNoTracking()
                .OrderBy(e => e.EquipId)
                .ToListAsync();
        }

        // 입력값 검사 + 중복 ID 체크 후 신규 설비 저장 (입력값 검사는 이곳에서만 함)
        public async Task AddEquipmentAsync(Equipment item)
        {
            // 앞뒤 공백 제거
            item.EquipId = item.EquipId?.Trim() ?? string.Empty;
            item.EquipName = item.EquipName?.Trim() ?? string.Empty;
            item.LineName = item.LineName?.Trim() ?? string.Empty;

            // null / 빈 값 / 공백만 입력 검사
            if (string.IsNullOrWhiteSpace(item.EquipId) ||
                string.IsNullOrWhiteSpace(item.EquipName) ||
                string.IsNullOrWhiteSpace(item.LineName))
            {
                throw new ArgumentException("설비ID, 설비명, 라인을 모두 입력하세요.");
            }

            await using var db = new AppDbContext();

            if (await db.Equipments.AnyAsync(e => e.EquipId == item.EquipId))
            {
                throw new InvalidOperationException($"이미 존재하는 설비 ID입니다: {item.EquipId}");
            }

            // Add의 경우 DB에 바로 INSERT 하지 않음
            // SaveChangesAsync 호출 시점에 INSERT 실행됨
            db.Equipments.Add(item);
            await db.SaveChangesAsync();
        }

        // 화면 갱신이 필요한지 확인하기 위한 가벼운 조회 (폴링 때 비교, 목록을 그릴 때 기준값으로 기록)
        // EquipChecksum: 설비 목록(ID, 이름, 라인, 상태)이 바뀌면 달라지는 값
        // LastLogId: 로그가 새로 쌓이면 커지는 값
        public async Task<(int EquipChecksum, long LastLogId)> GetDataVersionAsync()
        {
            await using var db = new AppDbContext();

            int equipChecksum = await db.Database
                .SqlQuery<int>($"SELECT ISNULL(CHECKSUM_AGG(CHECKSUM(EQUIP_ID, EQUIP_NAME, LINE_NAME, STATUS)), 0) AS [Value] FROM TB_EQUIPMENT")
                .SingleAsync();

            // long? 타입으로 MaxAsync를 호출하고, null이면 0으로 처리 (순서가 중요)
            long lastLogId = await db.EquipLogs.MaxAsync(l => (long?)l.LogId) ?? 0;

            return (equipChecksum, lastLogId);
        }

        // 선택 설비의 이력을 발생일시 역순(최신순)으로 조회
        public async Task<List<EquipLog>> GetLogsByEquipIdAsync(string equipId)
        {
            await using var db = new AppDbContext();

            return await db.EquipLogs
                .AsNoTracking()
                .Where(l => l.EquipId == equipId)
                .OrderByDescending(l => l.OccurDt)
                .ThenByDescending(l => l.LogId)
                .ToListAsync();
        }

        // 설비 상태 UPDATE + 이력 INSERT (트랜잭션 처리)
        public async Task UpdateEquipmentStatusAsync(string id, string newStatus)
        {
            await using var db = new AppDbContext();

            Equipment? equipment = await db.Equipments.FindAsync(id);
            if (equipment == null)
            {
                throw new InvalidOperationException($"존재하지 않는 설비 ID입니다: {id}");
            }

            string oldStatus = equipment.Status ?? "";
            equipment.Status = newStatus;

            db.EquipLogs.Add(new EquipLog
            {
                EquipId = id,
                LogType = "STATUS_CHG",
                LogMsg = $"상태 변경 [{oldStatus} ➔ {newStatus}]",
                OccurDt = DateTime.Now
            });

            // UPDATE + INSERT가 하나의 트랜잭션으로 실행됨
            /*
             * 현재와는 다르게 UPDATE는 바로 실행되고, INSERT로 LOG를 대응하는 방식으로 진행할 수 있음
             * 이렇게 하려면 INSERT의 예외 처리를 잘 해야 함
             */
            await db.SaveChangesAsync();
        }

        // FDC 판정: 이상이면 설비 DOWN + ALARM 로그를 한 트랜잭션으로 저장, 알람이면 true 반환
        /*
         * FDC의 기능은 눈에 잘 띄지않는 drft, 값 조합 이상, 품질 불량 징후 등 설비에 critical한 문제가 아닌 제품 품질에 영향을 주는 문제를 감지하는 것
         * 때문에, DOWN + ALARM을 묶었을 때 실패가 지속적으로 뜬다면 담당자 호출과 같은 다른 조치가 필요할 수 있음
         */
        public async Task<bool> CheckParameterAlarmAsync(string equipId, string paramName, double val)
        {
            await using var db = new AppDbContext();

            // 수동 판정은 판정을 요청한 순간이 곧 발생 시각
            bool isAlarm = await ApplyParameterAlarmAsync(db, equipId, paramName, val, DateTime.Now);

            // 이상이면 UPDATE + INSERT 한 트랜잭션으로 실행 (정상이면 저장할 변경이 없어 DB에 아무것도 보내지 않음)
            await db.SaveChangesAsync();

            return isAlarm;
        }

        // 센서 데이터 저장 + FDC 판정: 수집 데이터 INSERT, 이상 시 설비 DOWN + ALARM 로그까지 한 트랜잭션으로 저장
        // collectDt: 센서값을 측정한 시각 (수집 데이터와 알람 로그에 같은 시각을 기록)
        public async Task<bool> SaveSensorDataAsync(string equipId, double temp, double press, DateTime collectDt)
        {
            await using var db = new AppDbContext();

            // 온도, 압력 각각 판정
            bool tempAlarm = await ApplyParameterAlarmAsync(db, equipId, "TEMP", temp, collectDt);
            bool pressAlarm = await ApplyParameterAlarmAsync(db, equipId, "PRESS", press, collectDt);
            bool isFault = tempAlarm || pressAlarm;

            db.EquipDatas.Add(new EquipData
            {
                EquipId = equipId,
                TempVal = temp,
                PressVal = press,
                IsFault = isFault ? "ALARM" : "NORMAL",
                CollectDt = collectDt
            });

            // 데이터 INSERT + (알람 시) 상태 UPDATE + 로그 INSERT 한 트랜잭션으로 실행
            await db.SaveChangesAsync();

            return isFault;
        }

        // 판정 로직 공통 부분: 변경 내용을 db에 예약만 하고 저장은 호출한 쪽에서 함
        // 호출하는 쪽에서 같이 저장할 데이터가 다르기 때문에 판정 메서드에서 저장을 위임
        private async Task<bool> ApplyParameterAlarmAsync(AppDbContext db, string equipId, string paramName, double val, DateTime occurDt)
        {
            ParamLimit? limit = await db.ParamLimits
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.EquipId == equipId && l.ParamName == paramName);
            if (limit == null)
            {
                throw new InvalidOperationException($"설비 ID '{equipId}'와 파라미터 '{paramName}'에 대한 제한값이 존재하지 않습니다.");
            }

            // 정상 판정: LowerLimit <= val <= UpperLimit
            if (val >= limit.LowerLimit && val <= limit.UpperLimit)
            {
                return false;
            }

            // 이상 판정: 설비 상태 DOWN 변경 (이미 DOWN이면 유지)
            Equipment? equipment = await db.Equipments.FindAsync(equipId);
            if (equipment == null)
            {
                throw new InvalidOperationException($"존재하지 않는 설비 ID입니다: {equipId}");
            }

            if (equipment.Status != "DOWN")
            {
                equipment.Status = "DOWN";
            }

            // ALARM 로그
            string paramText = paramName == "TEMP" ? "온도" : paramName == "PRESS" ? "압력" : paramName;
            string unit = paramName == "TEMP" ? "℃" : paramName == "PRESS" ? "bar" : "";
            string alarmMsg = val > limit.UpperLimit
                ? $"{paramText} 상한({limit.UpperLimit}{unit}) 초과 발생: {val}{unit}"
                : $"{paramText} 하한({limit.LowerLimit}{unit}) 미달 발생: {val}{unit}";

            db.EquipLogs.Add(new EquipLog
            {
                EquipId = equipId,
                LogType = "ALARM",
                LogMsg = alarmMsg,
                OccurDt = occurDt
            });

            return true;
        }
    }
}