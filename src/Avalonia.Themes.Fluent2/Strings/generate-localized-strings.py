#!/usr/bin/env python3
"""Generates LocalizedControlStrings.cs for Avalonia.Themes.Fluent2.

Sources:
  - Unicode CLDR (cldr-json, cldr-dates-full/<locale>/dateFields.json):
    localized display names for day/month/year/hour/minute/second.
  - microsoft/microsoft-ui-xaml (MIT), controls/dev/CommandBarFlyout/Strings/
    <locale>/Resources.resw: TextCommandLabelCut/Copy/Paste.
"""
import json, subprocess, sys, urllib.request
import defusedxml.ElementTree as ET

OUT = sys.argv[1]

def gh(path):
    r = subprocess.run(['gh', 'api', path], capture_output=True, text=True)
    if r.returncode != 0:
        raise RuntimeError(f"gh api {path}: {r.stderr[:200]}")
    return json.loads(r.stdout)

def raw(url):
    with urllib.request.urlopen(url, timeout=30) as f:
        return f.read().decode('utf-8-sig')

# ---------------- WinUI cut/copy/paste ----------------
WINUI = 'https://raw.githubusercontent.com/microsoft/microsoft-ui-xaml/main/controls/dev/CommandBarFlyout/Strings'
dirs = [e['name'] for e in gh('repos/microsoft/microsoft-ui-xaml/contents/controls/dev/CommandBarFlyout/Strings')
        if e['type'] == 'dir']
print(f"WinUI locales: {len(dirs)}", file=sys.stderr)

text_commands = {}
for d in sorted(dirs):
    xml = None
    for fname in ('Resources.resw', 'resources.resw'):
        try:
            xml = raw(f"{WINUI}/{d}/{fname}")
            break
        except Exception:
            continue
    if xml is None:
        print(f"  ! no resw for {d}", file=sys.stderr)
        continue
    vals = {}
    for data in ET.fromstring(xml).findall('data'):
        vals[data.get('name')] = (data.findtext('value') or '').strip()
    try:
        text_commands[d.lower()] = [vals['TextCommandLabelCut'], vals['TextCommandLabelCopy'], vals['TextCommandLabelPaste']]
    except KeyError:
        print(f"  ! missing labels in {d}", file=sys.stderr)

# Aliases so the CultureInfo parent walk (xx-Script-REGION -> xx-Script -> xx) finds entries.
# Ambiguous languages resolved to the variant CLDR/.NET treats as the default.
PRIORITY = {'en': 'en-us', 'pt': 'pt-br', 'es': 'es-es', 'fr': 'fr-fr', 'zh': 'zh-cn',
            'sr': 'sr-latn-rs', 'nb': 'nb-no', 'nn': 'nn-no'}
aliases = {}
for d in sorted(text_commands):
    parts = d.split('-')
    if len(parts) == 3:  # lang-script-region -> lang-script
        aliases.setdefault(f"{parts[0]}-{parts[1]}", d)
    if len(parts) >= 2:  # lang-... -> lang
        aliases.setdefault(parts[0], d)
for lang, target in PRIORITY.items():
    if target in text_commands:
        aliases[lang] = target
# zh script-level aliases (.NET walks zh-CN -> zh-Hans -> zh)
for a, t in (('zh-hans', 'zh-cn'), ('zh-hant', 'zh-tw')):
    if t in text_commands:
        aliases[a] = t
for a, t in sorted(aliases.items()):
    if a not in text_commands:
        text_commands[a] = text_commands[t]

# ---------------- CLDR date/time fields ----------------
CLDR = 'https://raw.githubusercontent.com/unicode-org/cldr-json/main/cldr-json/cldr-dates-full/main'
available = {e['name'] for e in gh('repos/unicode-org/cldr-json/contents/cldr-json/cldr-dates-full/main')
             if e['type'] == 'dir'}
cands = set()
for d in dirs:
    parts = d.split('-')
    cands.add(parts[0].lower())
    if len(parts) == 3:
        cands.add(f"{parts[0].lower()}-{parts[1].title()}")
cands.add('zh-Hant')  # zh-TW walks through zh-Hant before zh
targets = sorted(c for c in cands if c in available)
print(f"CLDR locales: {len(targets)} of {len(cands)} candidates", file=sys.stderr)

FIELDS = ['day', 'month', 'year', 'hour', 'minute', 'second']
date_fields = {}
for t in targets:
    try:
        f = json.loads(raw(f"{CLDR}/{t}/dateFields.json"))['main'][t]['dates']['fields']
        date_fields[t] = [f[k]['displayName'] for k in FIELDS]
    except Exception as e:
        print(f"  ! {t}: {e}", file=sys.stderr)

# ---------------- emit C# ----------------
def esc(s):
    return s.replace('\\', '\\\\').replace('"', '\\"')

def table(entries):
    lines = []
    for k in sorted(entries):
        vals = ', '.join(f'"{esc(v)}"' for v in entries[k])
        lines.append(f'        ["{k}"] = new[] {{ {vals} }},')
    return '\n'.join(lines)

cs = f'''// Generated file - see the theme README for regeneration notes. Do not edit by hand.
//
// Data sources:
//  - Date/time field names: Unicode CLDR (cldr-json, cldr-dates-full "dateFields"
//    displayName values). Copyright Unicode, Inc.; SPDX-License-Identifier: Unicode-3.0.
//  - Cut/Copy/Paste labels: microsoft/microsoft-ui-xaml, CommandBarFlyout
//    Resources.resw (TextCommandLabel*). Copyright Microsoft Corporation; MIT license.

using System;
using System.Collections.Generic;

namespace Avalonia.Themes.Fluent2.Strings;

internal static class LocalizedControlStrings
{{
    /// <summary>Per-culture [day, month, year, hour, minute, second] display names.</summary>
    internal static readonly Dictionary<string, string[]> DateTimeFields = new(StringComparer.OrdinalIgnoreCase)
    {{
{table(date_fields)}
    }};

    /// <summary>Per-culture [cut, copy, paste] edit command labels.</summary>
    internal static readonly Dictionary<string, string[]> TextCommands = new(StringComparer.OrdinalIgnoreCase)
    {{
{table(text_commands)}
    }};
}}
'''
with open(OUT, 'w', encoding='utf-8') as f:
    f.write(cs)
print(f"wrote {OUT}: {len(date_fields)} date locales, {len(text_commands)} text-command entries", file=sys.stderr)
