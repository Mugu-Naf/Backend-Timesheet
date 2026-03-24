using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstAPI.Models
{
    public class LeaveBalance
    {
        [Key]
        public int LeaveBalanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int Year { get; set; }

        public int CasualTotal     { get; set; } = 10;
        public int CasualUsed      { get; set; } = 0;

        public int SickTotal       { get; set; } = 10;
        public int SickUsed        { get; set; } = 0;

        public int EarnedTotal     { get; set; } = 15;
        public int EarnedUsed      { get; set; } = 0;

        public int MaternityTotal  { get; set; } = 180; // ~26 weeks in days
        public int MaternityUsed   { get; set; } = 0;

        public int PaternityTotal  { get; set; } = 15;
        public int PaternityUsed   { get; set; } = 0;

        // Unpaid is unlimited — no tracking needed
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}
