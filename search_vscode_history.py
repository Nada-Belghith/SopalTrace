import os
import json
import datetime

history_dir = os.path.join(os.environ.get('APPDATA', ''), 'Code', 'User', 'History')
if not os.path.exists(history_dir):
    print("No history dir found at", history_dir)
else:
    for root, dirs, files in os.walk(history_dir):
        if 'entries.json' in files:
            entries_path = os.path.join(root, 'entries.json')
            try:
                with open(entries_path, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    resource = data.get('resource', '')
                    if 'SopalTraceApp' in resource and '.vue' in resource:
                        print(f"Found history for: {resource}")
                        for entry in data.get('entries', []):
                            entry_id = entry.get('id')
                            timestamp = entry.get('timestamp')
                            backup_path = os.path.join(root, entry_id)
                            dt = datetime.datetime.fromtimestamp(timestamp / 1000.0)
                            print(f"  Backup: {backup_path} at {dt}")
            except Exception as e:
                pass
