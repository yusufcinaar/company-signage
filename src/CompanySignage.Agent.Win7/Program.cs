using System.ServiceProcess;

namespace CompanySignage.Agent.Win7;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (Environment.UserInteractive || args.Any(a => a.Equals("--console", StringComparison.OrdinalIgnoreCase)))
        {
            using var service = new SignageAgentService();
            service.StartConsole();
            Console.WriteLine("CompanySignage Agent çalışıyor. Çıkmak için Enter.");
            Console.ReadLine();
            service.StopConsole();
            return;
        }

        ServiceBase.Run(new SignageAgentService());
    }
}
