using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yssystem_solution.Entities
{
    [Table("TB_EQUIP_DATA")]
    public class EquipData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("DATA_ID")]
        public long DataId { get; set; }

        [Required]
        [Column("EQUIP_ID")]
        [StringLength(200)]
        public string EquipId { get; set; } = string.Empty;

        [Required]
        [Column("TEMP_VAL")]
        public double TempVal { get; set; }

        [Required]
        [Column("PRESS_VAL")]
        public double PressVal { get; set; }

        [Column("IS_FAULT")]
        [StringLength(200)]
        public string? IsFault { get; set; } = "NORMAL";

        // default = DateTime.Now
        [Column("COLLECT_DT")]
        public DateTime? CollectDt { get; set; } = DateTime.Now;
    }
}