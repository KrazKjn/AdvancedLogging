using AdvancedLogging.Interfaces;
using AdvancedLogging.Logging;
using Microsoft.Win32;
using System;
using System.Security.AccessControl;

namespace AdvancedLogging.TestExtensions
{
    public class CurrentRegistryKeyTest : IRegistryKey
    {
        public CurrentRegistryKeyTest() { }

        public string Name { get; set; }
        public string Value { get; set; }

        public IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck }))
            {
                try
                {
                    return new CurrentRegistryKeyTest();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public IRegistryKey CreateSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity registrySecurity)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck }))
            {
                try
                {
                    return new CurrentRegistryKeyTest();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public IRegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, permissionCheck }))
            {
                try
                {
                    return new CurrentRegistryKeyTest();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, permissionCheck }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void SetValue(string name, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    Name = name;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void SetValue(string name, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    Name = name;
                    Value = value.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void SetValue(string name, double value, RegistryValueKind registryValueKind)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    Name = name;
                    Value = value.ToString();
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public string GetValue(string name, string value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    return value;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public double GetValue(string name, double value)
        {
            using (var vAutoLogFunction = new AutoLogFunction(new { name, value }))
            {
                try
                {
                    return value;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(new { name, value }, System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
        }

        public RegistrySecurity GetAccessControl()
        {
            using (var vAutoLogFunction = new AutoLogFunction(System.Reflection.MethodBase.GetCurrentMethod()))
            {
                try
                {
                    return null;
                }
                catch (Exception exOuter)
                {
                    vAutoLogFunction.LogFunction(System.Reflection.MethodBase.GetCurrentMethod(), true, exOuter);
                    throw;
                }
            }
        }

        public void Close()
        {
        }
    }
}