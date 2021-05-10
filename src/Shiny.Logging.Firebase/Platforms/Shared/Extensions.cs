﻿using System;
using Microsoft.Extensions.Logging;


namespace Shiny
{
    //https://www.thewissen.io/using-firebase-analytics-in-your-xamarin-forms-app/
    //https://medium.com/@hakimgulamali88/firebase-crashlytics-with-xamarin-5421089bb561
    public static class Extensions
    {
        public static void AddFirebase(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Warning)
        {
#if MONOANDROID || XAMARIN_IOS
            builder.AddProvider(new Shiny.Logging.Firebase.FirebaseLoggerProvider(logLevel));
#endif
#if XAMARIN_IOS
            // ensure GoogleService-Info.plist
            Firebase.Core.App.Configure();
#elif MONOANDROID
            // string resource com.crashlytics.android.build_id
#endif
        }
    }
}
