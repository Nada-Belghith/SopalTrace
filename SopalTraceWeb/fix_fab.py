import os
import re

path = 'src/views/QualityPlans/Fabrication/FabPlanEditor.vue'

with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

pattern = r'(const onExcelSelected = async \(event\) => \{\s*const file = event\.target\?\.files\?\.\[0\];\s*if \(!file\) return;\s*)'

if re.search(pattern, content):
    content = re.sub(pattern, r'\1event.target.value = \'\';\n    ', content)
    
    # Also remove the old clear in the finally block
    content = re.sub(r'\s*if\s*\(\s*event(?:\s*&&\s*event\.target)?\s*\)\s*event\.target\.value\s*=\s*\'\';\s*// Reset file input', '', content)
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f'Fixed {path}')
else:
    print('Pattern not found')
