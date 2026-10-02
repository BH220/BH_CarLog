using BH_CarLog.Core.Common;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace BH_CarLog.Core.Helper
{
    /// <summary>
    /// 파일 로거. 텍스트/JSON 두 파일에 비동기로 기록한다. 종료 전에 <see cref="Flush"/> 를 불러야 마지막 기록이 남는다.
    /// </summary>
    public static class Log
    {
        private static ILoggerFactory? _loggerFactory;
        private static ILogger? _logger;

        public static void Configure(string projectName)
        {
            if (_loggerFactory != null)
                return;

            NLog.LogManager.Configuration = CreateFallbackConfiguration();

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });
            _logger = _loggerFactory.CreateLogger(projectName);
        }

        private static LoggingConfiguration CreateFallbackConfiguration()
        {
            var config = new LoggingConfiguration();

            var fileTarget = new FileTarget("fileTarget")
            {
                FileName = $"{FilePathHelper.LogRoot}/${{logger}}/log_${{logger}}_${{shortdate}}_00001.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message} ${exception:format=tostring}",
                ArchiveAboveSize = 104857600,
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveSuffixFormat = "_{0:00000}",
                KeepFileOpen = false
            };
            var fileAsync = new AsyncTargetWrapper("fileAsync", fileTarget);

            var jsonTarget = new FileTarget("jsonFile")
            {
                FileName = $"{FilePathHelper.LogRoot}/${{logger}}/log_${{logger}}_${{shortdate}}_00001.json",
                ArchiveAboveSize = 104857600,
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveSuffixFormat = "_{0:00000}",
                KeepFileOpen = false,
                Layout = new JsonLayout { IncludeEventProperties = true }
            };
            var jsonAsync = new AsyncTargetWrapper("jsonAsync", jsonTarget);

            config.AddTarget(fileAsync);
            config.AddTarget(jsonAsync);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, fileAsync, "*");
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, jsonAsync, "*");

            return config;
        }

        public static void Debug(string message)
        {
            Console.WriteLine("Debug:" + message);
            _logger?.LogDebug("{Message}", message);
        }

        public static void Info(string message)
        {
            Console.WriteLine("Info:" + message);
            _logger?.LogInformation("{Message}", message);
        }

        public static void Warn(string message)
        {
            Console.WriteLine("Warn:" + message);
            _logger?.LogWarning("{Message}", message);
        }

        public static void ErrorLog(string message, Exception? ex = null)
        {
            string msg = ex == null ? message : $"{message}, Exception: {ex.Message}";
            Console.WriteLine("Error:" + msg);
            _logger?.LogError(ex, "{Message}", msg);
        }

        public static void Exception(Exception ex, string message) => ErrorLog(message, ex);

        /// <summary>비동기 래퍼에 쌓인 기록을 파일에 쓴다. 프로그램 종료 직전에 호출한다.</summary>
        public static void Flush() => NLog.LogManager.Flush(TimeSpan.FromSeconds(1));
    }
}
