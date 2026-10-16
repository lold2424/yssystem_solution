using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yssystem_solution.Entities
{
    [Table("TB_PARAM_LIMIT")]
    public class ParamLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("LIMIT_ID")]
        public long LimitId { get; set; }

        [Required]
        [Column("EQUIP_ID")]
        [StringLength(200)]
        public string EquipId { get; set; } = string.Empty;

        // TEMP, PRESS
        [Required]
        [Column("PARAM_NAME")]
        [StringLength(200)]
        public string ParamName { get; set; } = string.Empty;

        [Required]
        [Column("LOWER_LIMIT")]
        public double LowerLimit { get; set; }

        [Required]
        [Column("UPPER_LIMIT")]
        public double UpperLimit { get; set; }

        // default = DateTime.Now
        [Column("CREATE_DT")]
        public DateTime? CreateDt { get; set; } = DateTime.Now;
    }
}