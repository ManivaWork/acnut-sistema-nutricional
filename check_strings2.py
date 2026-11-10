content = open('C:/Users/adri_/OneDrive/Documentos/acnut-sistema-nutricional/frontend/interface/app.js', 'r', encoding='utf-8').read()

lines = content.split('\n')
in_single = False
in_double = False
in_template = False
for i, line in enumerate(lines):
    j = 0
    while j < len(line):
        c = line[j]
        if c == '\\' and j+1 < len(line):
            j += 2
            continue
        if not in_double and not in_template and c == "'":
            in_single = not in_single
        elif not in_single and not in_template and c == '"':
            in_double = not in_double
        elif not in_single and not in_double and c == '`':
            in_template = not in_template
        j += 1

if in_single or in_double or in_template:
    print(f'UNCLOSED at end of file: single={in_single}, double={in_double}, template={in_template}')
else:
    print('All strings closed at end of file')