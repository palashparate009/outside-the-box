package com.outsidethebox.notifications;

import java.util.Calendar;

/** Pure scheduling rules shared by initial scheduling and subsequent deliveries. */
public final class ReminderPolicy {
    public static final long DAY = 24L * 60L * 60L * 1000L;

    private ReminderPolicy() { }

    public static long nextAllowedTime(long candidate) {
        Calendar time = Calendar.getInstance();
        time.setTimeInMillis(candidate);
        int hour = time.get(Calendar.HOUR_OF_DAY);
        if (hour >= 9 && hour < 21) return candidate;
        if (hour >= 21) time.add(Calendar.DAY_OF_MONTH, 1);
        time.set(Calendar.HOUR_OF_DAY, 10);
        time.set(Calendar.MINUTE, 0);
        time.set(Calendar.SECOND, 0);
        time.set(Calendar.MILLISECOND, 0);
        return time.getTimeInMillis();
    }

    public static long nextDelivery(long candidate, long lastDelivered) {
        return nextAllowedTime(Math.max(candidate, lastDelivered > 0 ? lastDelivered + DAY : candidate));
    }

    public static int quoteIndex(long cursor, int count) {
        return count > 0 ? (int)Math.floorMod(cursor, (long)count) : 0;
    }
}
