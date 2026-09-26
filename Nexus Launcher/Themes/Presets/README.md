# Nexus Launcher themes

Twenty ready-made themes: ten for **The Bezier** and ten for **WXI**.
Each is a single file, and there are two packs holding all ten of a set
at once — `Nexus Theme Pack.nexustheme` (Bezier) and
`Nexus Theme Pack WXI.nexustheme`.

## Installing one

1. In Nexus, open **Settings**, turn on **Custom theming**.
2. Set the theme to the skin the palette was built for — **The Bezier**
   or **WXI**. A palette only works on its own skin, and the file says
   which one it wants.
3. Open the custom theme menu and choose **Import Theme…**. You can
   select several files at once.
4. Pick the new theme from the palette list.

Importing a theme for the other skin is fine; it just sits there until
you switch to that skin.

## For The Bezier

| Theme | |
| --- | --- |
| Midnight Ember | Near-black charcoal warmed by an ember orange accent |
| Deep Ocean | Cold navy depths with a bright cyan current |
| Nordic Night | Muted polar blue-grey with a frost accent |
| Carbon Violet | Flat carbon black lit by a single violet highlight |
| Forest Dusk | Deep pine greys under a moss green accent |
| Crimson Noir | Neutral black with a deep crimson edge |
| Neon Grid | High contrast synthwave: black glass, cyan, hot magenta |
| Paper White | A clean warm white page with a calm indigo accent |
| Sandstone | Warm beige and clay with a terracotta accent |
| Arctic Mint | Cool near-white with a crisp teal accent |

## For WXI

WXI is the Fluent-styled skin, so these lean Windows 11: rounded chrome,
flatter surfaces, elevation carried by light rather than by borders.

| Theme | |
| --- | --- |
| Graphite Fluent | The neutral Windows grey, with a clean cobalt accent |
| Obsidian Gold | Deep obsidian with a warm brushed gold accent |
| Twilight Indigo | Indigo-tinted dark surfaces under a periwinkle accent |
| Slate Teal | Cool slate greys cut with a deep teal accent |
| Rosewood | Warm dark timber with a muted rose accent |
| Matrix Green | Near-black terminal with a phosphor green accent |
| Monochrome | Pure greyscale, not a trace of hue |
| Cloud Light | The neutral Windows light grey with a cobalt accent |
| Linen | Warm off-white paper with a deep olive accent |
| Blush | Soft warm light with a quiet plum accent |

Every one of them clears WCAG AAA for body text against its own
background, and AA for text drawn on the accent, on the secondary
accent, on a selected row, and on the header band. Borders are checked
to stay visible against the surface behind them.

## The file format

A theme file is JSON. One file can hold a single theme or a pack.

```json
{
  "formatVersion": 1,
  "generator": "Nexus Launcher",
  "themes": [
    {
      "name": "My Theme",
      "skin": "The Bezier",
      "author": "you",
      "description": "One line about it.",
      "colors": {
        "Paint": "#1B1B1E",
        "Brush": "#E8E6E3",
        "Accent Paint": "#E8722C"
      }
    }
  ]
}
```

- `skin` must be one Nexus can recolour: **Basic**, **The Bezier**,
  **WXI**, **Office 2019 Colorful / Black / White / Dark Gray**, or
  **High Contrast**. Anything else is refused on import, because those
  skins draw from pre-rendered images and have no colours to swap.
- `colors` does **not** have to be complete. Anything you leave out
  keeps the skin's own value, so a theme can change three colours and
  say nothing about the other thirty. Names the skin does not have are
  ignored.
- Colours accept `#RGB`, `#RRGGBB` and `#AARRGGBB`, with or without the
  `#`.
- Everything else (`author`, `description`, and any field a later
  version adds) is carried for humans and ignored by the importer.

### The Bezier's colour names

The roles for The Bezier:

| Name | |
| --- | --- |
| `Paint` | The main window background |
| `Paint High` | Raised and hovered surfaces |
| `Paint Shadow` | Recessed surfaces |
| `Paint Deep Shadow` | The deepest surfaces and borders |
| `Brush` | Body text |
| `Brush High` | Emphasised text |
| `Brush Light` | Muted and secondary text |
| `Brush Major`, `Brush Minor` | Lines and separators, strong to faint |
| `Accent Paint` | Selection and focus fill |
| `Accent Paint Light` | The lighter selection fill |
| `Accent Brush` | Text drawn **on** the accent |
| `Accent Brush Light` | The lighter version of that |
| `Key Paint` | The header band behind the tabs |
| `Key Brush`, `Key Brush Light` | Text on the header band |
| `Red` `Green` `Blue` `Yellow` `Purple` | Status and icon colours |
| `Black` `Gray` `White` | Icon greys — on a dark theme `Black` has to be **light**, or icons vanish |
| `alt*` | The same glyph colours in a second state |

### WXI's colour names

WXI does not use Paint/Brush. It names elevation steps instead:

| Name | |
| --- | --- |
| `Background 200` … `-200` | Surfaces by elevation. **200 is the most elevated, and that means lighter in both light and dark themes** — only the range moves. |
| `Foreground 100 / 50 / 25` | Body text, muted, disabled |
| `Edit Background 0 / -50` | Editor fills, normal and recessed |
| `Edit Background -100 / -200` | Editor hover and selected — accent-tinted |
| `Edit Background -300` | Disabled editor fill |
| `Edit Foreground 100 / 50 / 25` | Editor text, muted, disabled |
| `Line 100 / 50 / 25` | Borders, strong to faint |
| `Primary Background 100` | A **tint** of the accent, not the accent itself. Pale in a light theme, dark in a dark one. |
| `Primary Background 0 / -100 / -200` | The accent, then hover and pressed |
| `Primary Foreground 100 / 25` | Text drawn on the accent |
| `Secondary Background`/`Foreground` | The same again for the secondary accent |
| `Red` `Green` `Blue` `Yellow` `Purple` | Status and icon colours |
| `Black` `Gray` `White` | Icon greys — on a dark theme `Black` has to be **light** |
| `Line Gradient 100 / 50` | Left at `#000000`; the skin does not draw them |

Two of WXI's entries are named with a single space. They are separators
in the palette editor, not colours; leave them out of a theme file.

Other skins use different names. To see a skin's own set, build a theme
in Nexus with **New Theme…**, then **Export Current Theme…** — the
exported file lists every colour that skin has, with its current value,
which makes a good starting point.

## Sharing your own

**Export Current Theme…** writes the theme you are using.
**Export All Themes…** writes everything you have made as one pack.
