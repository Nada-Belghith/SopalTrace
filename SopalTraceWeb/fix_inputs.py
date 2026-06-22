import os
import re

directory = 'src'

for root, _, files in os.walk(directory):
    for file in files:
        if file.endswith('.vue'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()

            modified = False

            # We look for patterns like:
            # const handleExcelImport = async (event) => {
            #   const file = event.target.files[0];
            #   if (!file) return;
            # And we inject:
            #   event.target.value = '';
            
            pattern = r'(const \w+ = (?:async )?\(event\) => \{\s*const (?:file|fileInput) = event\.target\.files\[0\];\s*if \(!(?:file|fileInput)\) return;\s*)'
            
            if re.search(pattern, content):
                content = re.sub(pattern, r'\1event.target.value = \'\';\n  ', content)
                modified = True

            # Also let's remove the old 'if (event.target) event.target.value = '';' from finally blocks
            if modified:
                content = re.sub(r'\s*if\s*\(\s*event(?:\s*&&\s*event\.target)?\s*\)\s*event\.target\.value\s*=\s*\'\';', '', content)
                
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(content)
                print(f'Fixed {path}')

