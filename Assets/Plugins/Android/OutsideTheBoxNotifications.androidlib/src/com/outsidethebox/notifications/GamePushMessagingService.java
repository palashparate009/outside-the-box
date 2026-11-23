package com.outsidethebox.notifications;

import android.Manifest;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.os.Build;
import android.util.Log;
import com.google.firebase.messaging.FirebaseMessaging;
import com.google.firebase.messaging.FirebaseMessagingService;
import com.google.firebase.messaging.RemoteMessage;

/** FCM recipient. Sending credentials belong in Firebase/servers, never in the game. */
public final class GamePushMessagingService extends FirebaseMessagingService {
    private static final String PREFS = "otb_push_device";
    private static final String CHANNEL = "otb_push_updates";
    private static final String TAG = "OutsideTheBoxPush";
    private static final int NOTIFICATION_ID = 45002;
    private static volatile boolean configuredThisProcess;

    private static SharedPreferences preferences(Context context) {
        return context.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    private static void createChannel(Context context) {
        if (Build.VERSION.SDK_INT < 26) return;
        NotificationManager manager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);
        NotificationChannel channel = new NotificationChannel(CHANNEL, "Game updates", NotificationManager.IMPORTANCE_DEFAULT);
        channel.setDescription("Optional Outside the Box announcements and events");
        manager.createNotificationChannel(channel);
    }

    // Opt-in is device state; no password or game progress is stored here.
    public static synchronized void configure(Context context, boolean enabled, String topic, boolean logTestToken) {
        Context application = context.getApplicationContext();
        SharedPreferences prefs = preferences(application);
        boolean wasEnabled = prefs.getBoolean("enabled", false);
        String oldTopic = prefs.getString("topic", "");
        boolean unchanged = configuredThisProcess && wasEnabled == enabled && oldTopic.equals(topic)
                && prefs.getBoolean("logTestToken", false) == logTestToken;
        prefs.edit().putBoolean("enabled", enabled).putString("topic", topic)
                .putBoolean("logTestToken", logTestToken).apply();
        if (unchanged) return;
        configuredThisProcess = true;
        FirebaseMessaging messaging = FirebaseMessaging.getInstance();
        messaging.setAutoInitEnabled(enabled);
        if (!oldTopic.isEmpty() && (!enabled || !oldTopic.equals(topic))) messaging.unsubscribeFromTopic(oldTopic);
        if (!enabled) {
            NotificationManager manager = (NotificationManager) application.getSystemService(Context.NOTIFICATION_SERVICE);
            manager.cancel(NOTIFICATION_ID);
            prefs.edit().remove("token").apply();
            revokeToken(application);
            return;
        }
        createChannel(application);
        register(application);
    }

    private static void revokeToken(Context context) {
        FirebaseMessaging.getInstance().deleteToken().addOnCompleteListener(task -> {
            if (!task.isSuccessful()) {
                Log.w(TAG, "FCM token removal failed; retry on the next menu/resume", task.getException());
                configuredThisProcess = false;
            }
            // A quick OFF -> ON toggle may complete deletion after re-registration.
            if (preferences(context).getBoolean("enabled", false)) register(context);
        });
    }

    private static void register(Context context) {
        FirebaseMessaging messaging = FirebaseMessaging.getInstance();
        messaging.getToken().addOnCompleteListener(task -> {
            if (!task.isSuccessful()) {
                Log.w(TAG, "FCM registration failed; retry when the game reconnects", task.getException());
                configuredThisProcess = false;
                return;
            }
            SharedPreferences prefs = preferences(context);
            if (!prefs.getBoolean("enabled", false)) { revokeToken(context); return; }
            rememberToken(context, task.getResult());
            String topic = prefs.getString("topic", "");
            if (!topic.isEmpty()) messaging.subscribeToTopic(topic).addOnCompleteListener(subscription -> {
                if (!subscription.isSuccessful()) {
                    Log.w(TAG, "FCM topic subscription failed", subscription.getException());
                    configuredThisProcess = false;
                }
            });
        });
    }

    private static void rememberToken(Context context, String token) {
        SharedPreferences prefs = preferences(context);
        prefs.edit().putString("token", token).apply();
        // C# passes true only for development builds. Release builds do not log device tokens.
        if (prefs.getBoolean("logTestToken", false)) Log.i(TAG, "FCM test token: " + token);
    }

    public static String getRegistrationToken(Context context) {
        return preferences(context).getString("token", "");
    }

    @Override public void onNewToken(String token) {
        if (preferences(this).getBoolean("enabled", false)) rememberToken(this, token);
    }

    @Override public void onMessageReceived(RemoteMessage message) {
        if (!preferences(this).getBoolean("enabled", false)) return;
        if (Build.VERSION.SDK_INT >= 33 && checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)
                != PackageManager.PERMISSION_GRANTED) return;
        NotificationManager manager = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        if (Build.VERSION.SDK_INT >= 24 && !manager.areNotificationsEnabled()) return;
        RemoteMessage.Notification notification = message.getNotification();
        String title = notification != null ? notification.getTitle() : message.getData().get("title");
        String body = notification != null ? notification.getBody() : message.getData().get("body");
        if (body == null || body.isEmpty()) return;
        if (title == null || title.isEmpty()) title = "Outside the Box";
        Intent launch = getPackageManager().getLaunchIntentForPackage(getPackageName());
        if (launch == null) return;
        launch.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP);
        int flags = PendingIntent.FLAG_UPDATE_CURRENT | (Build.VERSION.SDK_INT >= 23 ? PendingIntent.FLAG_IMMUTABLE : 0);
        PendingIntent openGame = PendingIntent.getActivity(this, NOTIFICATION_ID, launch, flags);
        createChannel(this);
        int icon = getResources().getIdentifier("otb_notification", "drawable", getPackageName());
        if (icon == 0) icon = android.R.drawable.ic_dialog_info;
        Notification.Builder builder = Build.VERSION.SDK_INT >= 26
                ? new Notification.Builder(this, CHANNEL) : new Notification.Builder(this);
        builder.setSmallIcon(icon).setContentTitle(title).setContentText(body)
                .setStyle(new Notification.BigTextStyle().bigText(body)).setContentIntent(openGame)
                .setAutoCancel(true).setOnlyAlertOnce(true);
        manager.notify(NOTIFICATION_ID, builder.build());
    }
}
