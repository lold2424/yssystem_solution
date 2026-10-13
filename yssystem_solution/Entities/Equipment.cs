using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yssystem_solution.Entities
{
    [Table("TB_EQUIPMENT")]
    public class Equipment
    {
        [Key]
        [Column("EQUIP_ID")]
        [StringLength(200)]
        public string EquipId { get; set; } = string.Empty;

        [Required]
        [Column("EQUIP_NAME")]
        [StringLength(200)]
        public string EquipName { get; set; } = string.Empty;

        [Required]
        [Column("LINE_NAME")]
        [StringLength(200)]
        public string LineName { get; set; } = string.Empty;

        // default = IDLE
        [Column("STATUS")]
        [StringLength(200)]
        public string? Status { get; set; } = "IDLE";

        [Column("CREATE_DT")]
        public DateTime? CreateDt { get; set; } = DateTime.Now;
    }
}