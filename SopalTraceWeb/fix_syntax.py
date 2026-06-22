import os

directory = 'src'

for root, _, files in os.walk(directory):
    for file in files:
        if file.endswith('.vue'):
            path = os.path.join(root, file)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()

            if "event.target.value = \\'\\';" in content:
                content = content.replace("event.target.value = \\'\\';", "event.target.value = '';")
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(content)
                print(f'Fixed syntax in {path}')

