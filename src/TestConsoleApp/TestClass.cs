using AdvancedLogging.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdvancedLogging.TestConsoleApp
{
    class TestClass
    {
        private int myIntVar;

        public int MyIntProperty
        {
            get { return myIntVar; }
            set { myIntVar = value; }
        }

        public TestClass()
        {
            using (var vAutoLogFunction = new AutoLogFunction())
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
            using (var vAutoLogFunction = new AutoLogFunction())
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
