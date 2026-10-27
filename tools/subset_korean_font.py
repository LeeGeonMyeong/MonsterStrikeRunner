"""Build the small, redistributable UI font from an upstream Noto Sans KR TTF.

Run with fonttools installed, after adding new Korean UI copy. The original is
downloaded from google/fonts into ignored tmp/fonts; it is not a game asset.
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tmp/fonttools"))
from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1]) if len(sys.argv) > 1 else root / "tmp/fonts/NotoSansKR.ttf"
font = TTFont(source)
if "fvar" in font:
    font = instantiateVariableFont(font, {"wght": 500}, inplace=True)
characters = set(chr(code) for code in range(32, 127))
characters.update("←→↓·…")
for path in (root / "Assets").rglob("*.cs"):
    characters.update(c for c in path.read_text(encoding="utf-8-sig") if ord(c) > 127)
options = subset.Options()
options.name_IDs = [0, 1, 2, 3, 4, 5, 6, 13, 14, 16, 17]
options.name_legacy = True
options.name_languages = [0x409]
subsetter = subset.Subsetter(options=options)
subsetter.populate(text="".join(sorted(characters)))
subsetter.subset(font)
for name_id, value in {1: "Runner Korean", 2: "Regular", 3: "RunnerKorean-UI-v1",
                       4: "Runner Korean Regular", 6: "RunnerKorean-Regular",
                       16: "Runner Korean", 17: "Regular"}.items():
    font["name"].setName(value, name_id, 3, 1, 0x409)
destination = root / "Assets/Resources/Fonts"
destination.mkdir(parents=True, exist_ok=True)
output = destination / "RunnerKorean.ttf"
font.save(output)
(destination / "OFL.txt").write_bytes((root / "tmp/fonts/OFL.txt").read_bytes())
(destination / "FONT-NOTICE.txt").write_text(
    "Runner Korean is a UI subset of Noto Sans KR (weight 500).\n"
    "Source: https://github.com/google/fonts/tree/main/ofl/notosanskr\n"
    "Distributed under SIL Open Font License 1.1; see OFL.txt.\n"
    "Renamed to Runner Korean. Rebuild with tools/subset_korean_font.py when UI copy changes.\n",
    encoding="utf-8")
print(f"UI font: {output.stat().st_size:,} bytes / {len(characters)} requested characters")
