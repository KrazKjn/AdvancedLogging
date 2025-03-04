namespace AdvancedLogging.Constants
{
    public class ConfigurationSetting
    {
        #region DefinedSettingsValues
        // When changing these values, make sure to update ApplicationSettings.SettingsLookup Get Property with new or updated value pairs.
        // Logging Level Settings
        public const string Log_FunctionHeaderMethod = "AutoLog:FunctionHeaderMethod";
        public const string Log_FunctionHeaderConstructor = "AutoLog:FunctionHeaderConstructor";
        public const string Log_ComplexParameterValues = "AutoLog:ComplexParameterValues";
        public const string Log_SqlCommand = "AutoLog:SqlCommand";
        public const string Log_SqlParameters = "AutoLog:SqlParameters";
        public const string Log_SqlCommandResults = "AutoLog:SqlCommandResults";
        public const string Log_MemberTypeInformation = "AutoLog:MemberTypeInformation";
        public const string Log_DynamicLoggingNotice = "AutoLog:DynamicLoggingNotice";
        public const string Log_DumpComplexParameterValues = "AutoLog:DumpComplexParameterValues";
        public const string Log_DebugDumpSQL = "AutoLog:DebugDumpSQL";
        public const string Log_ToConsole = "AutoLog:LogToConsole";
        public const string Log_ToDebugWindow = "AutoLog:LogToDebugWindow";

        public const string Log_FunctionHeaderMethod_Lower = "autolog:functionheadermethod";
        public const string Log_FunctionHeaderConstructor_Lower = "autolog:functionheaderconstructor";
        public const string Log_ComplexParameterValues_Lower = "autolog:complexparametervalues";
        public const string Log_SqlCommand_Lower = "autolog:sqlcommand";
        public const string Log_SqlParameters_Lower = "autolog:sqlparameters";
        public const string Log_SqlCommandResults_Lower = "autolog:sqlcommandresults";
        public const string Log_MemberTypeInformation_Lower = "autolog:membertypeinformation";
        public const string Log_DynamicLoggingNotice_Lower = "autolog:dynamicloggingnotice";
        public const string Log_DumpComplexParameterValues_Lower = "autolog:dumpcomplexparametervalues";
        public const string Log_DebugDumpSQL_Lower = "autolog:debugdumpsql";
        public const string Log_ToConsole_Lower = "autolog:logtoconsole";
        public const string Log_ToDebugWindow_Lower = "autolog:logtodebugwindow";

        // Settings
        public const string Common_LogFile = "AutoLog:LogFile";
        public const string Common_LoggingThreshold = "AutoLog:LoggingThreshold";
        public const string Common_AutoLogSQLThreshold = "AutoLog:AutoLogSQLThreshold";
        public const string Common_MaxFunctionTimeThreshold = "AutoLog:MaxFunctionTimeThreshold";
        public const string Common_DebugLevels = "AutoLog:DebugLevels";
        public const string Common_IgnoreFunctions = "AutoLog:IgnoreFunctions";
        public const string Common_LogLevel = "AutoLog:LogLevel";
        public const string Common_AllowLoging = "AutoLog:AllowLoging";
        public const string Common_EnableDebugCode = "AutoLog:EnableDebugCode";
        public const string Common_IISSoapLogging = "AutoLog:IISSoapLogging";
        public const string Common_MaxAutoRetriesSql = "AutoLog:MaxAutoRetriesSql";
        public const string Common_AutoRetrySleepMsSql = "AutoLog:AutoRetrySleepMsSql";
        public const string Common_AutoTimeoutIncrementSecondsSql = "AutoLog:AutoTimeoutIncrementSecondsSql";
        public const string Common_MaxAutoRetriesHttp = "AutoLog:MaxAutoRetriesHttp";
        public const string Common_AutoRetrySleepMsHttp = "AutoLog:AutoRetrySleepMsHttp";
        public const string Common_AutoTimeoutIncrementMsHttp = "AutoLog:AutoTimeoutIncrementMsHttp";

        public const string Common_LogFile_Lower = "autolog:logfile";
        public const string Common_LoggingThreshold_Lower = "autolog:loggingthreshold";
        public const string Common_AutoLogSQLThreshold_Lower = "autolog:autologsqlthreshold";
        public const string Common_MaxFunctionTimeThreshold_Lower = "autolog:maxfunctiontimethreshold";
        public const string Common_DebugLevels_Lower = "autolog:debuglevels";
        public const string Common_IgnoreFunctions_Lower = "autolog:ignorefunctions";
        public const string Common_LogLevel_Lower = "autolog:loglevel";
        public const string Common_AllowLoging_Lower = "autolog:allowloging";
        public const string Common_EnableDebugCode_Lower = "autolog:enabledebugcode";
        public const string Common_IISSoapLogging_Lower = "autolog:iissoaplogging";
        public const string Common_MaxAutoRetriesSql_Lower = "autolog:maxautoretriessql";
        public const string Common_AutoRetrySleepMsSql_Lower = "autolog:autoretrysleepmssql";
        public const string Common_AutoTimeoutIncrementSecondsSql_Lower = "autolog:autotimeoutincrementsecondssql";
        public const string Common_MaxAutoRetriesHttp_Lower = "autolog:maxautoretrieshttp";
        public const string Common_AutoRetrySleepMsHttp_Lower = "autolog:autoretrysleepmshttp";
        public const string Common_AutoTimeoutIncrementMsHttp_Lower = "autolog:autotimeoutincrementmshttp";
        #endregion // DefinedSettingsValues
    }
}
