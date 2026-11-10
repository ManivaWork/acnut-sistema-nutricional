content = open('C:/Users/adri_/OneDrive/Documentos/acnut-sistema-nutricional/frontend/interface/app.js', 'r', encoding='utf-8').read()
lines = content.split('\n')
for lineno in [7173, 7176]:
    line = lines[lineno]
    print(f'Line {lineno+1}: {repr(line)}')
    for i, c in enumerate(line):
        if c in "'\"`":
            print(f'  {i}: {repr(c)} (ord={ord(c)})')