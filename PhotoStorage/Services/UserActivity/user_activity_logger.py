import os
import sqlite3
import json
from datetime import datetime

# Path to the SQLite database
db_path = "photostorage.db"

# Function to collect user activity data from the database
def collect_user_activity_from_db():
    user_data = []
    media_stats = {
        "videos": {
            "total": 0,
            "types": {},
            "uploads_by_date": {},
            "uploaded_by_user": {}
        },
        "photos": {
            "total": 0,
            "types": {},
            "uploads_by_date": {},
            "uploaded_by_user": {}
        },
        "audios": {
            "total": 0,
            "types": {},
            "uploads_by_date": {},
            "uploaded_by_user": {}
        }
    }

    # Connect to the SQLite database
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Updated query to use the Title column instead of OriginalFileName
    query = """
    SELECT u.Id AS user_id, u.UserName AS username, p.Id AS photo_id, p.FileName AS file_name, p.Title AS title, p.UploadDate AS upload_date
    FROM AspNetUsers u
    JOIN Photos p ON u.Id = p.UserId
    """

    cursor.execute(query)
    rows = cursor.fetchall()

    # Updated processing to include the title
    for row in rows:
        user_id, username, photo_id, file_name, title, upload_date = row
        user_info = next((u for u in user_data if u["user_id"] == user_id), None)

        if not user_info:
            user_info = {
                "user_id": user_id,
                "username": username,
                "photos": []
            }
            user_data.append(user_info)

        user_info["photos"].append({
            "photo_id": photo_id,
            "file_name": file_name,
            "title": title,
            "upload_date": upload_date
        })

        # Analyze media data
        file_extension = os.path.splitext(file_name)[1].lower()
        upload_date_str = upload_date.split("T")[0]  # Extract date part

        if file_extension in (".mp4", ".avi", ".mkv"):
            media_type = "videos"
        elif file_extension in (".jpg", ".jpeg", ".png", ".gif"):
            media_type = "photos"
        elif file_extension in (".mp3", ".wav", ".aac"):
            media_type = "audios"
        else:
            continue

        media_stats[media_type]["total"] += 1

        # Count media types
        if file_extension not in media_stats[media_type]["types"]:
            media_stats[media_type]["types"][file_extension] = 0
        media_stats[media_type]["types"][file_extension] += 1

        # Count uploads by date
        if upload_date_str not in media_stats[media_type]["uploads_by_date"]:
            media_stats[media_type]["uploads_by_date"][upload_date_str] = 0
        media_stats[media_type]["uploads_by_date"][upload_date_str] += 1

        # Track uploads by user
        if username not in media_stats[media_type]["uploaded_by_user"]:
            media_stats[media_type]["uploaded_by_user"][username] = 0
        media_stats[media_type]["uploaded_by_user"][username] += 1

    # Close the database connection
    conn.close()

    # Determine the directory of the current script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    log_file_path = os.path.join(script_dir, "user_activity_log.json")

    # Save the collected data to a JSON file
    with open(log_file_path, "w") as log_file:
        json.dump(user_data, log_file, indent=4)

    # Save the media statistics to a separate JSON file
    media_stats_path = os.path.join(script_dir, "media_stats.json")
    with open(media_stats_path, "w") as stats_file:
        json.dump(media_stats, stats_file, indent=4)

    # Collect additional user statistics
    def collect_user_stats(rows):
        user_stats = {}
        for row in rows:
            user_id, username, photo_id, file_name, title, upload_date = row

            if user_id not in user_stats:
                user_stats[user_id] = {
                    "username": username,
                    "total_uploads": 0,
                    "last_upload_date": None
                }

            user_stats[user_id]["total_uploads"] += 1
            if not user_stats[user_id]["last_upload_date"] or upload_date > user_stats[user_id]["last_upload_date"]:
                user_stats[user_id]["last_upload_date"] = upload_date

        return user_stats

    # Save user statistics to a separate log file
    user_stats_path = os.path.join(script_dir, "user_stats.json")
    user_stats = collect_user_stats(rows)
    with open(user_stats_path, "w") as user_stats_file:
        json.dump(user_stats, user_stats_file, indent=4)

    print(f"User activity data collected and saved to {log_file_path}")
    print(f"Media statistics collected and saved to {media_stats_path}")
    print(f"User statistics collected and saved to {user_stats_path}")

if __name__ == "__main__":
    collect_user_activity_from_db()