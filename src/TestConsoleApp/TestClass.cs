using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using AdvancedLogging.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdvancedLogging.TestConsoleApp
{
    class TestClass
    {
        private int myIntVar;
        private readonly ICommonLogger _logger;
        private readonly ILoggingContext _loggingContext;

        public int MyIntProperty
        {
            get { return myIntVar; }
            set { myIntVar = value; }
        }

        public TestClass(ICommonLogger logger, ILoggingContext loggingContext)
        {
            _logger = logger;
            _loggingContext = loggingContext;

            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { }))
            {
                try
                {
                    MyIntProperty = 10;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public string Test(bool bThrowException = false)
        {
            using (var vAutoLogFunction = new AutoLogFunction(_logger, _loggingContext, new { bThrowException }))
            {
                try
                {
                    if (bThrowException)
                        throw new Exception("Test Exception!");
                    return MyIntProperty.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    if (bThrowException)
                        return "Test Exception!";
                    else
                        throw;
                }
            }
        }
    }
}
