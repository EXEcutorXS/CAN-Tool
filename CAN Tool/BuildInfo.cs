using System;
using System.Reflection;

namespace CAN_Tool
{
    public static class BuildInfo
    {
        public static string BuildDate
        {
            get
            {
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyMetadataAttribute>();

                if (attribute != null && attribute.Key == "BuildDate")
                    return attribute.Value;

                // Fallback: дата изменения файла сборки
                return System.IO.File.GetLastWriteTime(
                    Assembly.GetExecutingAssembly().Location)
                    .ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}