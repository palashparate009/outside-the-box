# Unity calls these methods through JNI, so retain their names in release builds.
-keep class com.outsidethebox.notifications.GameReminderReceiver { *; }
-keep class com.outsidethebox.notifications.GamePushMessagingService { *; }
