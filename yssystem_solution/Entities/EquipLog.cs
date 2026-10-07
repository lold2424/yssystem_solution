using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yssystem_solution.Entities
{
    [Table("TB_EQUIP_LOG")]
    public class EquipLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("LOG_ID")]
        public long LogId { get; set; }

        [Column("EQUIP_ID")]
        [StringLength(200)]
        public string? EquipId { get; set; }

        [Column("LOG_TYPE")]
        [StringLength(200)]
        public string? LogType { get; set; }

        [Column("LOG_MSG")]
        [StringLength(200)]
        public string? LogMsg { get; set; }

        // default = DateTime.Now
        [Column("OCCUR_DT")]
        public DateTime? OccurDt { get; set; } = DateTime.Now;
    }
}