import json
import os
import sys

log_file = r"C:\Users\LAPTOP\.gemini\antigravity\brain\2dec49cf-82e1-44a6-ad5e-f66bdffd3ba0\.system_generated\logs\overview.txt"

with open(log_file, 'r', encoding='utf-8') as f:
    for line_num, line in enumerate(f, 1):
        try:
            data = json.loads(line.strip())
            if "tool_calls" in data:
                for call in data["tool_calls"]:
                    args = call.get("args", {})
                    target_file = None
                    if "TargetFile" in args:
                        try:
                            target_file = json.loads(args["TargetFile"])
                        except:
                            target_file = args["TargetFile"].strip('"')
                            
                    if target_file and "ColumnConfigurator.vue" in target_file and call["name"] == "write_to_file":
                        try:
                            content = json.loads(args["CodeContent"])
                        except:
                            content = args["CodeContent"]
                        
                        os.makedirs(os.path.dirname(target_file), exist_ok=True)
                        with open(target_file, "w", encoding="utf-8") as out:
                            out.write(content)
                        print(f"Restored: {target_file}")
                        
        except Exception as e:
            print(f"Error on line {line_num}: {e}")
