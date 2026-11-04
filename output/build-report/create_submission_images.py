from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'tmp/qr-deps'))
import qrcode
from PIL import Image, ImageDraw, ImageFont

url = 'https://monsterstrikerunner.dldl26607.workers.dev/'
qr = qrcode.QRCode(error_correction=qrcode.constants.ERROR_CORRECT_M, box_size=16, border=4)
qr.add_data(url)
qr.make(fit=True)
qr.make_image(fill_color='black', back_color='white').save(root / 'output/monster-strike-runner-qr.png')

font = ImageFont.truetype('C:/Windows/Fonts/consola.ttf', 20)
canvas = Image.new('RGB', (1540, 1020), '#171c26')
draw = ImageDraw.Draw(canvas)
for x, name, title in [(30, 'build-report-raw.txt', 'BEFORE - Max Size 1024'), (790, 'after-512-raw.txt', 'AFTER - Max Size 512')]:
    draw.text((x, 25), title, font=font, fill='#6edddd')
    lines = (root / 'output/build-report' / name).read_text(encoding='utf-8-sig').splitlines()[:18]
    for i, line in enumerate(lines):
        if i == 1:
            line = 'Uncompressed usage by category (user assets)'
        if line.startswith('Used Assets'):
            line = 'Top used assets (uncompressed):'
        line = line.replace('Assets/Resources/Backgrounds/', '').replace('Assets/Resources/Characters/', '').replace('Packages/com.unity.render-pipelines.universal/Textures/', '')
        draw.text((x, 75 + i * 43), line.expandtabs(1), font=font, fill='#e4e8f0')
draw.text((30, 920), 'Editor.log excerpts rendered as an image; not a Unity UI screenshot.', font=font, fill='#a6b3ca')
canvas.save(root / 'output/build-report/build-report-comparison.png')
