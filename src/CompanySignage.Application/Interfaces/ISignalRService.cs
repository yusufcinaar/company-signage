using CompanySignage.Domain.Enums;

namespace CompanySignage.Application.Interfaces;

public interface ISignalRService
{
    Task SendToScreenAsync(string screenCode, string method, object payload);
    Task SendToAllScreensAsync(string method, object payload);
    Task SendToScreensAsync(List<string> screenCodes, string method, object payload);
}
