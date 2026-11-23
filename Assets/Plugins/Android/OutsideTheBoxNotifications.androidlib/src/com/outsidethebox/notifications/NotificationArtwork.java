package com.outsidethebox.notifications;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.util.Log;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/** Packaged notification artwork: no downloads, hosting or running Unity instance. */
public final class NotificationArtwork {
    public final String title;
    public final Bitmap picture;

    private NotificationArtwork(String title, Bitmap picture) {
        this.title = title;
        this.picture = picture;
    }

    public static NotificationArtwork load(Context context, long cursor, String body, boolean gift) {
        try {
            JSONArray items;
            try (InputStream input = context.getAssets().open("otb_notifications/catalog.json");
                    ByteArrayOutputStream output = new ByteArrayOutputStream()) {
                byte[] buffer = new byte[4096];
                int count;
                while ((count = input.read(buffer)) != -1) output.write(buffer, 0, count);
                items = new JSONObject(new String(output.toByteArray(), StandardCharsets.UTF_8)).getJSONArray("items");
            }
            if (items.length() == 0) return null;
            int index = gift ? 0 : ReminderPolicy.quoteIndex(cursor, items.length());
            if (!gift) {
                // Keep stock image/title pairs correct even if remote message order changes.
                for (int i = 0; i < items.length(); i++) {
                    if (body.equals(items.getJSONObject(i).optString("body", ""))) { index = i; break; }
                }
            }
            JSONObject item = items.getJSONObject(index);
            Bitmap picture;
            try (InputStream image = context.getAssets().open(item.getString("image"))) {
                BitmapFactory.Options options = new BitmapFactory.Options();
                // 1280x720 source becomes 640x360, keeping expanded notification memory modest.
                options.inSampleSize = 2;
                picture = BitmapFactory.decodeStream(image, null, options);
            }
            return picture == null ? null : new NotificationArtwork(item.optString("title", "Outside the Box"), picture);
        } catch (IOException | JSONException | IllegalArgumentException exception) {
            Log.w("OutsideTheBoxPush", "Packaged reminder image unavailable; showing text instead.", exception);
            return null;
        }
    }
}
