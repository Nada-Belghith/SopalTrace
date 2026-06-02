import os
import json

repo_dir = r"C:\Users\LAPTOP\source\repos\Nada-Belghith\SopalTraceApp"
log_file = r"C:\Users\LAPTOP\.gemini\antigravity\brain\2dec49cf-82e1-44a6-ad5e-f66bdffd3ba0\.system_generated\logs\overview.txt"

def resolve_path(target_file):
    target_file = target_file.replace("\\\\", "\\").replace("/", "\\")
    if "SopalTraceApp" in target_file:
        idx = target_file.rfind("SopalTraceApp")
        parts = target_file[idx:].split("\\", 1)
        if len(parts) > 1:
            return os.path.join(repo_dir, parts[1])
    return target_file

def apply_replace(target_file, start_line, end_line, new_content):
    path = resolve_path(target_file)
    if not os.path.exists(path):
        print(f"File not found: {path}")
        return
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    start_idx = start_line - 1
    end_idx = end_line
    
    if start_idx < 0 or start_idx > len(lines):
        print(f"Index out of bounds in {path}: {start_idx} > {len(lines)}")
        return
        
    new_lines = new_content.splitlines(True)
    if not new_content.endswith("\n"):
        if new_lines:
            new_lines[-1] += "\n"
        else:
            new_lines = ["\n"]
            
    lines = lines[:start_idx] + new_lines + lines[end_idx:]
    
    with open(path, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print(f"Replaced lines {start_line}-{end_line} in: {os.path.basename(path)}")

print("Processing 2dec49cf...")
with open(log_file, "r", encoding="utf-8") as f:
    for line_num, line in enumerate(f, 1):
        try:
            data = json.loads(line.strip())
            if "tool_calls" in data:
                for call in data["tool_calls"]:
                    name = call["name"]
                    args = call.get("args", {})
                    if name in ["replace_file_content", "multi_replace_file_content"]:
                        target_file = json.loads(args["TargetFile"]) if "TargetFile" in args and args["TargetFile"].startswith('"') else args.get("TargetFile", "").strip('"')
                        
                        if name == "replace_file_content":
                            start_line = int(args["StartLine"])
                            end_line = int(args["EndLine"])
                            content = json.loads(args["ReplacementContent"]) if args["ReplacementContent"].startswith('"') else args["ReplacementContent"]
                            apply_replace(target_file, start_line, end_line, content)
                            
                        elif name == "multi_replace_file_content":
                            chunks = json.loads(args["ReplacementChunks"]) if isinstance(args.get("ReplacementChunks"), str) else args.get("ReplacementChunks", [])
                            chunks.sort(key=lambda x: int(x["StartLine"]), reverse=True)
                            for chunk in chunks:
                                apply_replace(target_file, int(chunk["StartLine"]), int(chunk["EndLine"]), chunk["ReplacementContent"])
        except Exception as e:
            print(f"Error on line {line_num}: {e}")
