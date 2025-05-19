using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MDataCollector
{
    [RunInstaller(true)]
    public class MDataCollectorInstaller : Installer
    {
        public MDataCollectorInstaller()
        {
            var spi = new ServiceProcessInstaller();
            ServiceInstaller si = new ServiceInstaller();

            spi.Account = ServiceAccount.LocalSystem;
            spi.Username = null;
            spi.Password = null;

            si.DisplayName = Program.ServiceName;
            si.ServiceName = Program.ServiceName;
            si.Description = "Montronix IBU-NG filtered data packet collector from 108";
            si.StartType = ServiceStartMode.Automatic;

            // SetServiceName(si);
           
            Installers.Add(spi);
            Installers.Add(si);
        }

    }
}