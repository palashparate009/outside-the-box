package com.outsidethebox.notifications;

import android.Manifest;
import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.os.Build;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/** Optional daily reminders. Only the next inexact alarm is pending; Unity need not run. */
public final class GameReminderReceiver extends BroadcastReceiver {
    private static final int ID = 45001;
    private static final String CHANNEL = "otb_game_reminders";

    private static SharedPreferences preferences(Context context) {
        return context.getSharedPreferences("otb_reminder_schedule", Context.MODE_PRIVATE);
    }

    private static boolean allowed(Context context) {
        if (Build.VERSION.SDK_INT >= 33 && context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)
                != PackageManager.PERMISSION_GRANTED) return false;
        NotificationManager manager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        return manager != null && (Build.VERSION.SDK_INT < 24 || manager.areNotificationsEnabled());
    }

    private static int immutableFlag() {
        return Build.VERSION.SDK_INT >= 23 ? PendingIntent.FLAG_IMMUTABLE : 0;
    }

    private static Intent reminderIntent(Context context) {
        return new Intent(context, GameReminderReceiver.class).setAction("com.outsidethebox.REMINDER");
    }

    private static void createChannel(Context context) {
        if (Build.VERSION.SDK_INT < 26) return;
        NotificationManager manager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        NotificationChannel channel = new NotificationChannel(CHANNEL, "Game reminders", NotificationManager.IMPORTANCE_DEFAULT);
        channel.setDescription("Optional chapter gifts and puzzle reminders");
        manager.createNotificationChannel(channel);
    }

    // Called from Unity through JNI. At most one reminder is pending per installation.
    public static synchronized void scheduleDaily(Context context, long timestamp, String title,
            String firstBody, String fallbackBody, String messagesJson, long interval) {
        cancel(context);
        if (!allowed(context)) return;
        createChannel(context);
        SharedPreferences prefs = preferences(context);
        long generation = prefs.getLong("generation", 0);
        prefs.edit().putBoolean("active", true).putString("title", title)
                .putString("firstBody", firstBody).putString("fallbackBody", fallbackBody)
                .putString("messages", messagesJson).putLong("interval", Math.max(ReminderPolicy.DAY, interval)).apply();
        enqueue(context, ReminderPolicy.nextDelivery(timestamp, prefs.getLong("lastDelivered", 0)), generation);
    }

    private static void enqueue(Context context, long timestamp, long generation) {
        preferences(context).edit().putLong("nextAt", timestamp).apply();
        Intent intent = reminderIntent(context).putExtra("generation", generation);
        PendingIntent pending = PendingIntent.getBroadcast(context, ID, intent, PendingIntent.FLAG_UPDATE_CURRENT | immutableFlag());
        AlarmManager manager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
        if (manager != null) manager.set(AlarmManager.RTC_WAKEUP, timestamp, pending);
    }

    public static synchronized void cancel(Context context) {
        SharedPreferences prefs = preferences(context);
        // Preserve quote rotation across visits, while invalidating any already queued broadcast.
        prefs.edit().putBoolean("active", false).putLong("generation", prefs.getLong("generation", 0) + 1).apply();
        PendingIntent pending = PendingIntent.getBroadcast(context, ID, reminderIntent(context), PendingIntent.FLAG_NO_CREATE | immutableFlag());
        if (pending != null) {
            AlarmManager manager = (AlarmManager) context.getSystemService(Context.ALARM_SERVICE);
            if (manager != null) manager.cancel(pending);
            pending.cancel();
        }
        NotificationManager notifications = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        if (notifications != null) notifications.cancel(ID);
    }

    @Override public void onReceive(Context context, Intent intent) {
        if (Intent.ACTION_BOOT_COMPLETED.equals(intent.getAction()) || Intent.ACTION_MY_PACKAGE_REPLACED.equals(intent.getAction())) {
            restore(context);
            return;
        }
        deliver(context, intent);
    }

    private static synchronized void restore(Context context) {
        SharedPreferences prefs = preferences(context);
        if (!prefs.getBoolean("active", false)) return;
        if (!allowed(context)) { cancel(context); return; }
        long candidate = Math.max(System.currentTimeMillis(), prefs.getLong("nextAt", 0));
        enqueue(context, ReminderPolicy.nextDelivery(candidate, prefs.getLong("lastDelivered", 0)), prefs.getLong("generation", 0));
    }

    private static synchronized void deliver(Context context, Intent intent) {
        SharedPreferences prefs = preferences(context);
        long generation = intent.getLongExtra("generation", -1);
        if (!prefs.getBoolean("active", false) || generation != prefs.getLong("generation", 0)) return;
        if (!allowed(context)) { cancel(context); return; }
        long now = System.currentTimeMillis();
        long allowedTime = ReminderPolicy.nextDelivery(now, prefs.getLong("lastDelivered", 0));
        // An inexact alarm can arrive during quiet hours. Defer rather than notify overnight.
        if (allowedTime > now) { enqueue(context, allowedTime, generation); return; }
        Intent launch = context.getPackageManager().getLaunchIntentForPackage(context.getPackageName());
        if (launch == null) return;
        launch.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        PendingIntent openGame = PendingIntent.getActivity(context, ID, launch, PendingIntent.FLAG_UPDATE_CURRENT | immutableFlag());
        createChannel(context);
        int icon = context.getResources().getIdentifier("otb_notification", "drawable", context.getPackageName());
        if (icon == 0) icon = android.R.drawable.ic_dialog_info;
        Notification.Builder builder = Build.VERSION.SDK_INT >= 26
                ? new Notification.Builder(context, CHANNEL) : new Notification.Builder(context);
        String body = prefs.getString("firstBody", "");
        boolean gift = !body.isEmpty();
        long cursor = prefs.getLong("quoteCursor", 0);
        if (!gift) {
            body = prefs.getString("fallbackBody", "Your next magnetic puzzle is waiting.");
            try {
                JSONArray messages = new JSONObject(prefs.getString("messages", "{}")).optJSONArray("messages");
                if (messages != null && messages.length() > 0) {
                    String quote = messages.optString(ReminderPolicy.quoteIndex(cursor, messages.length()), "");
                    if (!quote.trim().isEmpty()) body = quote;
                }
            } catch (JSONException ignored) { /* Invalid remote copy uses the single-message fallback. */ }
        }
        NotificationArtwork artwork = NotificationArtwork.load(context, cursor, body, gift);
        String title = artwork != null && !gift ? artwork.title : prefs.getString("title", "Outside the Box");
        builder.setSmallIcon(icon).setContentTitle(title).setContentText(body)
                .setStyle(new Notification.BigTextStyle().bigText(body))
                .setContentIntent(openGame).setAutoCancel(true);
        if (artwork != null) {
            Notification.BigPictureStyle style = new Notification.BigPictureStyle()
                    .bigPicture(artwork.picture).setBigContentTitle(title).setSummaryText(body);
            if (Build.VERSION.SDK_INT >= 31) style.showBigPictureWhenCollapsed(true).setContentDescription(title);
            builder.setLargeIcon(artwork.picture).setStyle(style);
        }
        NotificationManager manager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        manager.notify(ID, builder.build());
        prefs.edit().putLong("lastDelivered", now).putString("firstBody", "")
                .putLong("quoteCursor", gift ? cursor : cursor + 1).apply();
        // No catch-up burst after power saving: schedule one future message from actual delivery.
        enqueue(context, ReminderPolicy.nextDelivery(now + prefs.getLong("interval", ReminderPolicy.DAY), now), generation);
    }
}
