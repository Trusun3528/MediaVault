from flask import Flask, jsonify, render_template
import os
import json

app = Flask(__name__)

# Define paths to the log files
script_dir = os.path.dirname(os.path.abspath(__file__))
user_activity_log_path = os.path.join(script_dir, "user_activity_log.json")
media_stats_path = os.path.join(script_dir, "media_stats.json")
user_stats_path = os.path.join(script_dir, "user_stats.json")

# Route to display the dashboard
@app.route('/')
def dashboard():
    return render_template('dashboard.html')

# Route to fetch user activity data
@app.route('/api/user-activity')
def get_user_activity():
    with open(user_activity_log_path, 'r') as file:
        data = json.load(file)
    return jsonify(data)

# Route to fetch media statistics
@app.route('/api/media-stats')
def get_media_stats():
    with open(media_stats_path, 'r') as file:
        data = json.load(file)
    return jsonify(data)

# Route to fetch user statistics
@app.route('/api/user-stats')
def get_user_stats():
    with open(user_stats_path, 'r') as file:
        data = json.load(file)
    return jsonify(data)

if __name__ == '__main__':
    app.run(host='127.0.0.1', port=5002, debug=True)