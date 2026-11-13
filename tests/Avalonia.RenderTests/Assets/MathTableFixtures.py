"""Builds STIXTwoMath-Subset.otf and LatinModernMath-Subset.otf and prints the MATH data the tests
compare against: `dump` writes the MATH table as ttx XML, `deltas` resolves GDEF variation store
records of InterVariable.ttf at user-space axis values.

Not run by the build. Requires fontTools (tested with 4.63).

    python MathTableFixtures.py subset <STIXTwoMath-Regular.otf> <latinmodern-math.otf> <out-dir>
    python MathTableFixtures.py check <original.otf> <subset.otf>
    python MathTableFixtures.py dump <font> <out.ttx>
    python MathTableFixtures.py deltas <InterVariable.ttf> <outer>:<inner> ... -- <axis>=<value> ...

Sources: STIX Two Math 2.12 (SIL OFL 1.1, CTAN fonts/stix2-otf) and Latin Modern Math 1.959
(GUST Font License, CTAN fonts/lm-math).
"""

import sys

from fontTools.subset import Options, Subsetter
from fontTools.ttLib import TTFont

# Delimiters, radicals, large operators, over/under braces, arrows and accents, plus a few base
# letters and digits for italics correction, accent attachment and kerning.
MATH_TEXT = "()[]{}|‖⟨⟩√∑∏∫∮⏞⏟→‾^~xyfAVW0123456789"


def subset(source, target):
    font = TTFont(source)
    options = Options()
    options.layout_features = ["*"]
    options.glyph_names = True
    options.notdef_outline = True
    options.name_IDs = ["*"]
    options.name_languages = ["*"]
    options.drop_tables = ["DSIG", "FFTM"]
    subsetter = Subsetter(options)
    subsetter.populate(text=MATH_TEXT)
    subsetter.subset(font)
    font.save(target)


def constructions(font):
    math = font["MATH"].table
    result = {}
    variants = math.MathVariants
    if variants is None:
        return result
    for name, coverage, items in (
        ("V", variants.VertGlyphCoverage, variants.VertGlyphConstruction),
        ("H", variants.HorizGlyphCoverage, variants.HorizGlyphConstruction),
    ):
        if coverage is None:
            continue
        for glyph, construction in zip(coverage.glyphs, items):
            parts = None
            if construction.GlyphAssembly is not None:
                parts = [
                    (p.glyph, p.StartConnectorLength, p.EndConnectorLength, p.FullAdvance, p.PartFlags)
                    for p in construction.GlyphAssembly.PartRecords
                ]
            result[(name, glyph)] = (
                [(v.VariantGlyph, v.AdvanceMeasurement) for v in construction.MathGlyphVariantRecord],
                parts,
            )
    return result


def check(original, subsetted):
    full = constructions(TTFont(original))
    sub = constructions(TTFont(subsetted))
    missing = [key for key in sub if sub[key] != full.get(key)]
    for key in missing:
        print("mismatch", key)
    print(f"{len(sub)} constructions kept, {len(missing)} differ from the original")
    return 1 if missing else 0


def dump(source, target):
    TTFont(source).saveXML(target, tables=["MATH"])


def deltas(source, records, coords):
    from fontTools.varLib.varStore import VarStoreInstancer

    font = TTFont(source)
    axes = font["fvar"].axes
    location = {}
    for axis in axes:
        value = coords.get(axis.axisTag, axis.defaultValue)
        if value < axis.defaultValue:
            location[axis.axisTag] = (value - axis.defaultValue) / (axis.defaultValue - axis.minValue)
        elif value > axis.defaultValue:
            location[axis.axisTag] = (value - axis.defaultValue) / (axis.maxValue - axis.defaultValue)
        else:
            location[axis.axisTag] = 0.0
    if "avar" in font:
        from fontTools.varLib.models import piecewiseLinearMap

        for tag, mapping in font["avar"].segments.items():
            location[tag] = piecewiseLinearMap(location[tag], mapping)
    instancer = VarStoreInstancer(font["GDEF"].table.VarStore, axes, location)
    for outer, inner in records:
        print(f"{outer}:{inner} {instancer[(outer << 16) | inner]!r}")


def main(argv):
    command = argv[1]
    if command == "subset":
        subset(argv[2], f"{argv[4]}/STIXTwoMath-Subset.otf")
        subset(argv[3], f"{argv[4]}/LatinModernMath-Subset.otf")
        return 0
    if command == "check":
        return check(argv[2], argv[3])
    if command == "dump":
        dump(argv[2], argv[3])
        return 0
    if command == "deltas":
        split = argv.index("--")
        records = [tuple(int(x) for x in r.split(":")) for r in argv[3:split]]
        coords = {kv.split("=")[0]: float(kv.split("=")[1]) for kv in argv[split + 1:]}
        deltas(argv[2], records, coords)
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
