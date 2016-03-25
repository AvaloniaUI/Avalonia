// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Perspex.Logging
{
    public static class Logger
    {
        public static ILogSink Sink { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(
            LogEventLevel level, 
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Sink?.Log(level, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Verbose(
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Log(LogEventLevel.Verbose, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Debug, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Information(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Information, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Warning, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(
            string area,
            object source,
            string messageTemplate, 
            params object[] propertyValues)
        {
            Log(LogEventLevel.Error, area, source, messageTemplate, propertyValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fatal(
            string area,
            object source,
            string messageTemplate,
            params object[] propertyValues)
        {
            Log(LogEventLevel.Fatal, area, source, messageTemplate, propertyValues);
        }
    }
}
