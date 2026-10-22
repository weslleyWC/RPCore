"""Read-only texture-reference inventory of ILSpy output; never executes the supplied DLL.

Usage: python scripts/audit-psrp-textures.py --source /path/to/ilspy-output --dll /path/to/PSRP.dll
Dynamic expressions are reported explicitly; this is not a C# interpreter or a gameplay test.
"""
from pathlib import Path
import argparse, collections, hashlib, json, re


def arguments(text, start):
    result, begin, depth, pos = [], start, 0, start
    quote, verbatim = None, False
    while pos < len(text):
        char = text[pos]
        if quote:
            if char == quote:
                if verbatim and pos+1 < len(text) and text[pos+1] == quote:
                    pos += 2
                    continue
                quote = None
            elif char == '\\' and not verbatim:
                pos += 2
                continue
        elif char in "\"'":
            quote, verbatim = char, pos > 0 and text[pos-1] == '@'
        elif char in '([{':
            depth += 1
        elif char in ')]}':
            if char == ')' and depth == 0:
                result.append(text[begin:pos].strip())
                return result
            depth -= 1
        elif char == ',' and depth == 0:
            result.append(text[begin:pos].strip())
            begin = pos+1
        pos += 1
    raise ValueError('Unterminated call near ' + str(start))


def literal(value):
    if value.startswith('@"') and value.endswith('"'):
        return value[2:-1].replace('""', '"')
    if re.fullmatch(r'"(?:[^"\\]|\\.)*"', value, re.S):
        try:
            return json.loads(value)
        except ValueError:
            pass
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--dll', type=Path)
    parser.add_argument('--catalog', type=Path, default=Path(__file__).resolve().parents[1]/'config/Visuals/Catalog.json')
    parser.add_argument('--output', type=Path, default=Path(__file__).resolve().parents[1]/'docs/PSRP-TEXTURE-AUDIT.json')
    args = parser.parse_args()
    catalog = json.loads(args.catalog.read_text())
    dictionaries = {name.lower(): {t['Name'].lower() for t in values} for name, values in catalog['Dictionaries'].items()}
    sources = sorted(args.source.rglob('*.cs'))
    records, path_refs, source_hashes = [], [], {}
    counts = collections.Counter()
    for path in sources:
        code = path.read_text(encoding='utf-8-sig')
        relative = str(path.relative_to(args.source)).replace('\\', '/')
        source_hashes[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
        aliases = collections.defaultdict(set)
        for match in re.finditer(r'\bstring\s+(\w+)\s*=\s*("(?:[^"\\]|\\.)*")', code):
            aliases[match[1]].add(literal(match[2]))
        def record(kind, offset, dictionary, texture):
            resolved = literal(dictionary)
            if resolved is None and len(aliases[dictionary]) == 1:
                resolved = next(iter(aliases[dictionary]))
            name = literal(texture)
            status = 'dynamic_expression'
            if resolved is not None:
                if resolved.lower() not in dictionaries:
                    status = 'dictionary_not_in_supplied_oiv'
                elif name == '':
                    status = 'empty_initial_texture'
                elif name is not None:
                    status = 'present_in_supplied_oiv' if name.lower() in dictionaries[resolved.lower()] else 'literal_not_in_supplied_oiv'
            records.append({'source': relative, 'line': code.count('\n', 0, offset)+1, 'kind': kind,
                            'dictionary_expression': dictionary, 'dictionary': resolved,
                            'texture_expression': texture, 'texture': name, 'status': status})
        for match in re.finditer(r'\bnew\s+(?:LemonUI\.Elements\.)?(ScaledTexture|TextureAsset)\s*\(', code):
            values = arguments(code, match.end())
            kind = match[1]
            counts[kind] += 1
            if len(values) not in (2, 4):
                raise ValueError(f'Unexpected texture constructor arity in {relative}: {len(values)}')
            record(kind, match.start(), values[-2], values[-1])
        for match in re.finditer(r'Function\.Call\s*\(\s*Hash\.DRAW_SPRITE\s*,', code):
            values = arguments(code, match.end())
            counts['DRAW_SPRITE'] += 1
            record('DRAW_SPRITE', match.start(), values[0], values[1])
        if path.name == 'RadialMenu.cs':
            for match in re.finditer(r'\bCreate(?:Action|Page)Slot\s*\(', code):
                values = arguments(code, match.end())
                if len(values) >= 2 and literal(values[1]) is not None:
                    counts['radial_slot_factory'] += 1
                    record('radial_slot_factory', match.start(), '"PSRP_Inventory"', values[1])
        for match in re.finditer(r'"[^"\n]*\.(?:ytd|rpf)[^"\n]*"', code, re.I):
            path_refs.append({'source': relative, 'line': code.count('\n', 0, match.start())+1, 'expression': match[0]})
    report = {
        'scope': 'All C# files from a full ILSpy project export; constructor/native/factory references, not execution or whole-program symbolic analysis.',
        'source_files': len(sources), 'source_sha256': source_hashes,
        'dll_sha256': hashlib.sha256(args.dll.read_bytes()).hexdigest() if args.dll else None,
        'call_counts': dict(counts), 'statuses': dict(collections.Counter(r['status'] for r in records)),
        'direct_ytd_rpf_path_literals': path_refs,
        'archive_from_oiv_assembly': 'mods/update/update.rpf/x64/data/cdimages/scaleform_generic.rpf',
        'dynamic_families_manually_verified': {'PSRP/OutBar': '0..100', 'PSRP_Inventory/RadMenu': '1..8', 'PSRP_PHONE/PhoneWallpaper': '1..10', 'PSRP_Banners/PSRPBANNER': '1..3; SettingsMenu.UpdateAnimation, 5000ms'},
        'entry_points_manually_verified': ['Main initializes and updates RadialMenu.Instance; UiSystem.DrawRadicalMenuBase is a different helper with no call site found.', 'Main initializes SettingsMenu and calls UpdateAnimation each tick.'],
        'records': records,
        'limits': 'Variables, switch expressions, loaded item.Icon values and later Texture mutations are not fully resolved. Missing literals can be initial states, unused helper code, or version differences, not proof of a runtime failure.'
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    print(json.dumps({'source_files': len(sources), 'call_counts': report['call_counts'], 'statuses': report['statuses'], 'direct_path_literals': len(path_refs)}, indent=2))
    for record in records:
        if record['status'] == 'literal_not_in_supplied_oiv':
            print('Not in supplied OIV:', record['source'], record['line'], record['dictionary'], record['texture'])


if __name__ == '__main__':
    main()
