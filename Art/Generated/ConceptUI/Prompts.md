# Concept UI assets

Reference: `Art/References/InGameUI.png`.

Generation: built-in imagegen, transparent background requested. Originals are retained here. Game textures are saved in `Assets/02.Sprites/UI` after nearest-neighbor resizing in Aseprite. Alpha below 128 is cleared and remaining pixels made opaque. No Unity play-mode or build verification was run.

## CardSlash

Create ONE isolated game inventory/card illustration: a short curved silver katana swung in a bold red crescent slash, matching the gray attack card artwork in the reference game screenshot visible in this conversation. Transparent background. No card frame, no labels, no numbers, no letters. The sword blade runs diagonally lower-left to upper-right, charcoal wrapped hilt with a small brass guard, ivory silver blade with one gray shade, red arc above. Chunky readable pixel art, native design approximately 96x96 square pixels, displayed enlarged with exact hard square pixel blocks. Very bold near-black outlines, warm ivory highlights, muted burgundy shadows, warm gray and one vivid coral red. Flat two-tone sprite shading, no glows, no blurry edges, no texture grain. Centered object occupies 80% of square canvas, generous clear transparent margins. Must look like an actual crisp dungeon roguelike pixel item sprite, not a painted illustration. Output a single square sprite, no extra objects, no multiple icons.

Game texture: `Assets/02.Sprites/UI/CardSlash.png`, 128 x 128.

## ItemIcons

Use the attached screenshot ONLY as style and subject reference for a new transparent UI ITEM ICON ATLAS. This is one atlas texture with exactly six small inventory item icons arranged in a perfectly regular 3-column by 2-row grid of equal square cells. Each icon stays centered in its own cell with generous transparent margins, no contact with neighboring cells. Reading left-to-right top-to-bottom the six icons are the screenshot's top-left item row: (1) closed purple magic book with brown gold clasp, (2) white fox mask with red markings, (3) three purple berries with a dark leaf, (4) small hanging red paper charm with a black loop, (5) round pink potion bottle with a dark stopper, (6) bright green four-leaf sprout. No text or symbols resembling letters, no labels, no card frames, no UI panels, no screenshot background, no shadows on the canvas. Preserve the screenshot's bold black stair-step outlines, chunky 32x32-style pixel clusters, simple base plus one shadow per color and tiny ivory highlight. Exact square pixel edges, clean flat colors, no antialiasing or smooth painting. Dark warm plum outlines. Transparent background throughout. The final canvas must have a 3:2 aspect ratio with three equally spaced columns and two equally spaced rows; all six cells exactly equal and all icons comfortably inside the middle 70 percent of their cells. This is one usable sprite atlas image.

Game texture: `Assets/02.Sprites/UI/ItemIcons.png`, 384 x 256. Six manually defined 128 x 128 sprite regions; automatic slicing is not used.

## Native pixel UI shapes

Panel, header, button, slot, badge and health-bar shapes were authored as simple pixel geometry in Aseprite. Stepped corners, solid fills, hard outlines and nine-slice borders follow the reference. These textures contain no baked text; all text is editable UGUI Text.
