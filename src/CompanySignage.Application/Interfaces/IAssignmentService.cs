using CompanySignage.Application.DTOs.Assignment;

namespace CompanySignage.Application.Interfaces;

public interface IAssignmentService
{
    Task<List<AssignmentDto>> GetAllAsync();
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
    Task<AssignmentDto?> UpdateAsync(int id, CreateAssignmentDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> SendNowAsync(SendNowDto dto);
}
