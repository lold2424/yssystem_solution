using Microsoft.EntityFrameworkCore;
using yssystem_solution.Data;
using yssystem_solution.Entities;

namespace yssystem_solution.Services
{
    public class EquipmentService
    {
        // 전체 설비 목록 조회
        public List<Equipment> GetEquipmentList()
        {
            using var db = new AppDbContext();

            return db.Equipments
                .AsNoTracking()
                .OrderBy(e => e.EquipId)
                .ToList();
        }

        // 중복 ID 체크 후 신규 설비 저장
        public void AddEquipment(Equipment item)
        {
            if (string.IsNullOrWhiteSpace(item.EquipId))
            {
                throw new ArgumentException("설비 ID를 입력하세요.");
            }

            using var db = new AppDbContext();

            if (db.Equipments.Any(e => e.EquipId == item.EquipId))
            {
                throw new InvalidOperationException($"이미 존재하는 설비 ID입니다: {item.EquipId}");
            }

            db.Equipments.Add(item);
            db.SaveChanges();
        }

        // 선택 설비의 이력을 발생일시 역순(최신순)으로 조회
        public List<EquipLog> GetLogsByEquipId(string equipId)
        {
            using var db = new AppDbContext();

            return db.EquipLogs
                .AsNoTracking()
                .Where(l => l.EquipId == equipId)
                .OrderByDescending(l => l.OccurDt)
                .ThenByDescending(l => l.LogId)
                .ToList();
        }

        // 설비 상태 UPDATE + 이력 INSERT (트랜잭션 처리)
        public void UpdateEquipmentStatus(string id, string newStatus)
        {
            using var db = new AppDbContext();

            Equipment? equipment = db.Equipments.Find(id);
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
            db.SaveChanges();
        }
    }
}