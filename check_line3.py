content = open('C:/Users/adri_/OneDrive/Documentos/acnut-sistema-nutricional/frontend/interface/app.js', 'r', encoding='utf-8').read()
lines = content.split('\n')
line = lines[7138]  # 0-indexed
print(f'Line 7139: {repr(line)}')
for i, c in enumerate(line):
    if c in "'\"`":
        print(f'  {i}: {repr(c)} (ord={ord(c)})')