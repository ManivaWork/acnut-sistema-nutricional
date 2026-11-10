content = open('C:/Users/adri_/OneDrive/Documentos/acnut-sistema-nutricional/frontend/interface/app.js', 'r', encoding='utf-8').read()

lines = content.split('\n')
in_single = False
in_double = False
in_template = False
start_line = -1
for i, line in enumerate(lines):
    j = 0
    while j < len(line):
        c = line[j]
        if c == '\\' and j+1 < len(line):
            j += 2
            continue
        if not in_double and not in_template and c == "'":
            if not in_single:
                start_line = i + 1
            in_single = not in_single
        elif not in_single and not in_template and c == '"':
            in_double = not in_double
        elif not in_single and not in_double and c == '`':
            in_template = not in_template
        j += 1

if in_single:
    print(f'Unclosed single quote started at line {start_line}')
    # Show context around that line
    for k in range(max(0, start_line-3), min(len(lines), start_line+5)):
        print(f'Line {k+1}: {lines[k]}')