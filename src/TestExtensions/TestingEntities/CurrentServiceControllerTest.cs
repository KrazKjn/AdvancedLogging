using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using System;
using System.Collections.Generic;
using System.ServiceProcess;

namespace AdvancedLogging.TestExtensions
{
    public class CurrentServiceControllerTest : ICurrentServiceController
    {

        public CurrentServiceControllerTest() { }

        public int Command { get; set; }
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public ServiceStartMode StartType { get; set; }
        public ServiceControllerStatus Status { get; set; }
        public string ExecutableName { get; set; }
        public string LogOnAccount { get; set; }
        public ICurrentServiceController[] GetCurrentServices()
        {
            using (var vAutoLogFunction = new AutoLogFunction(System.Reflection.MethodBase.GetCurrentMethod()))
            {
                try
                {
                    List<ICurrentServiceController> serviceControllers = new List<ICurrentServiceController>();
                    return serviceControllers.ToArray();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void ExecuteCommand(int command)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { command }))
            {
                try
                {
                    Command = command;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { command }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }
    }

}