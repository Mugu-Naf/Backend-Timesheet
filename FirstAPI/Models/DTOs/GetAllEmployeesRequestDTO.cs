using System.ComponentModel.DataAnnotations;

namespace TimeSheetApp.Models.DTOs
{
    public class GetAllEmployeesRequestDTO
    {
        [Required]
        public int Limit {  get; set; }
        [Required]
        public int PageSize { get; set; }
    }
}
