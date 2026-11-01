"""Read-only sprite measurements and an analytical comparison figure."""
from pathlib import Path
import json
from collections import Counter
import numpy as np
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
samples = [
    ('Brimstone', root / '118_brimstone.png'),
    ('Sacred Heart', root / '182_sacredheart.png'),
    ('Godhead', root / '331_godhead.png'),
    ('AI fireball', root.parent / 'FireballIcon-concept-v1.png'),
]
results = []
fig = Image.new('RGB', (1400, 510), 'white')
draw = ImageDraw.Draw(fig)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 20)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
draw.text((20, 12), 'Same display size | nearest-neighbor display | original files unchanged', fill='black', font=font)
for index, (name, path) in enumerate(samples):
    im = Image.open(path).convert('RGBA')
    a = np.array(im)
    alpha = a[:, :, 3]
    visible = alpha > 0
    opaque = alpha == 255
    ys, xs = np.where(visible)
    colors = np.unique(a[visible, :3], axis=0)
    opaque_colors = np.unique(a[opaque, :3], axis=0)
    info = dict(name=name, size=list(im.size),
                bbox_xywh=[int(xs.min()), int(ys.min()), int(xs.max()-xs.min()+1), int(ys.max()-ys.min()+1)],
                visible_rgb_colors=len(colors), opaque_rgb_colors=len(opaque_colors),
                partial_alpha_pixels=int(((alpha>0)&(alpha<255)).sum()),
                visible_pixels=int(visible.sum()),
                alpha_values=len(np.unique(alpha)),
                most_common_visible_rgba=[(list(k),v) for k,v in Counter(map(tuple,a[visible].tolist())).most_common(5)])
    results.append(info)
    # Display each full canvas at the same on-page size; preserve original samples.
    x = 20 + index * 345
    draw.text((x, 48), name, fill='black', font=font)
    backdrop = Image.new('RGBA', (320,320), '#c9ced5')
    backdrop.alpha_composite(im.resize((320,320), Image.Resampling.NEAREST))
    fig.paste(backdrop.convert('RGB'), (x, 78))
    draw.multiline_text((x, 410), f'{im.width} x {im.height} px\n{len(colors):,} visible RGB colors\n{info["partial_alpha_pixels"]:,} partial-alpha pixels', fill='black', font=small)
fig.save(root/'comparison.png')
(root/'measurements.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
