content = open('C:/Users/adri_/OneDrive/Documentos/acnut-sistema-nutricional/frontend/interface/app.js', 'r', encoding='utf-8').read()

lines = content.split('\n')
in_single = False
in_double = False
in_template = False
for i, line in enumerate(lines):
    old_single = in_single
    old_double = in_double
    old_template = in_template
    j = 0
    while j < len(line):
        c = line[j]
        if c == '\\' and j+1 < len(line):
            j += 2
            continue
        if not in_double and not in_template and c == "'":
            in_single = not in_single
            print(f'Line {i+1}, pos {j}: toggled single to {in_single}')
        elif not in_single and not in_template and c == '"':
            in_double = not in_double
            print(f'Line {i+1}, pos {j}: toggled double to {in_double}')
        elif not in_single and not in_double and c == '`':
            in_template = not in_template
            print(f'Line {i+1}, pos {j}: toggled template to {in_template}')
        j += 1
    if in_single != old_single or in_double != old_double or in_template != old_template:
        print(f'  Line {i+1} end: single={in_single}, double={in_double}, template={in_template} | {line[:80]}')

if in_single or in_double or in_template:
    print(f'UNCLOSED at end: single={in_single}, double={in_double}, template={in_template}')
else:
    print('All strings closed at end of file')