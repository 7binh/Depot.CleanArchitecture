# Depot Design System

Visual rules for every screen in `web/`. Derived from the EasyTeam reference dashboard.
This file defines **how things look**, not **where things go**. Layout, page structure and
information architecture are decided per feature. Everything below is mandatory unless the
user explicitly overrides it.

**Character in one line:** a calm, light, soft-cornered SaaS interface. White surfaces on a
pale grey field, hairline borders, almost no shadow, one confident blue, and small semantic
colour chips. Quiet everywhere, so the single blue accent and the status colours carry meaning.

How to apply: use the tokens in section 1 through Tailwind utilities (`bg-surface`,
`text-ink-2`, `rounded-card`). Never write raw hex, arbitrary values like `p-[13px]`, or new
global CSS. If a value you need is not here, stop and ask; do not invent one.

---

## 1. Tokens

Already applied in `web/src/styles.css`. These are the only colours, radii, shadows, type sizes
and fonts allowed.

```css
@theme {
  /* Neutrals */
  --color-shell: #f5f5f5; /* page field / sidebar / inset fills */
  --color-surface: #ffffff; /* cards, panels, popovers, inputs on shell */
  --color-surface-sunken: #f7f7f7; /* table header, card footer strip, search field */
  --color-line: #ebebeb; /* every border and divider */
  --color-line-strong: #dcdcdc; /* hovered borders, disabled outlines */

  /* Ink */
  --color-ink: #171717; /* titles, numbers, primary text */
  --color-ink-2: #5c5c5c; /* secondary text, table headers, icons */
  --color-ink-3: #767676; /* captions, placeholders (minimum allowed grey) */
  --color-ink-disabled: #b3b3b3; /* disabled only; never for readable content */

  /* Brand */
  --color-brand: #1e5edb;
  --color-brand-hover: #1a52c2;
  --color-brand-press: #1645a6;
  --color-brand-soft: #eaf1fd; /* icon tiles, selected tint */
  --color-brand-soft-2: #d6e4fb;
  --color-on-brand: #ffffff;

  /* Semantic: each has soft background, readable text, and dot/icon */
  --color-success-bg: #e8f7ee;
  --color-success-fg: #15803d;
  --color-success-dot: #22a559;
  --color-warning-bg: #fff4db;
  --color-warning-fg: #9a5b00;
  --color-warning-dot: #e5a000;
  --color-danger-bg: #fdecec;
  --color-danger-fg: #c42318;
  --color-danger-dot: #e5392d;
  --color-danger-solid: #e5231b; /* count badges only */

  /* Radii */
  --radius-chip: 6px;
  --radius-control: 10px; /* buttons, inputs, dropdowns, nav items, icon tiles */
  --radius-inset: 12px; /* table header, segmented control, progress bar */
  --radius-card: 20px;

  /* Shadows */
  --shadow-hairline: 0 1px 2px rgb(0 0 0 / 0.05);
  --shadow-raised: 0 1px 3px rgb(0 0 0 / 0.06), 0 1px 2px rgb(0 0 0 / 0.04);
  --shadow-popover: 0 8px 24px rgb(0 0 0 / 0.08), 0 2px 6px rgb(0 0 0 / 0.04);

  /* Type scale: size / line height / tracking (/ weight where fixed) */
  --text-title: 1.75rem; /* 28/36, -0.02em, 500 */
  --text-title--line-height: 2.25rem;
  --text-title--letter-spacing: -0.02em;
  --text-title--font-weight: 500;
  --text-stat: 2.5rem; /* 40/44, -0.03em, 400 */
  --text-stat--line-height: 2.75rem;
  --text-stat--letter-spacing: -0.03em;
  --text-stat--font-weight: 400;
  --text-section: 1.25rem; /* 20/28, -0.01em, 500 */
  --text-section--line-height: 1.75rem;
  --text-section--letter-spacing: -0.01em;
  --text-section--font-weight: 500;
  --text-card: 1.125rem; /* 18/26, -0.01em, 500 */
  --text-card--line-height: 1.625rem;
  --text-card--letter-spacing: -0.01em;
  --text-card--font-weight: 500;
  --text-body: 0.9375rem; /* 15/22; add font-medium for row primary text */
  --text-body--line-height: 1.375rem;
  --text-small: 0.875rem; /* 14/20: table header, segmented control, promo body */
  --text-small--line-height: 1.25rem;
  --text-caption: 0.8125rem; /* 13/18: captions, chips, field labels */
  --text-caption--line-height: 1.125rem;
  --text-label: 0.6875rem; /* 11/14, +0.04em, 500: the only uppercase text */
  --text-label--line-height: 0.875rem;
  --text-label--letter-spacing: 0.04em;
  --text-label--font-weight: 500;

  /* Promotional surface: the only gradient allowed */
  --background-image-promo:
    radial-gradient(60% 45% at 30% 8%, rgb(255 255 255 / 0.28), transparent),
    linear-gradient(160deg, #1e5edb 0%, #3b82e8 55%, #8ec5f4 100%);

  /* Type */
  --font-sans: 'Inter Variable', 'Inter', ui-sans-serif, system-ui, 'Segoe UI', sans-serif;
}
```

The real tokens live in `web/src/styles.css`. The `@theme` block there also starts with
`--color-*: initial; --radius-*: initial; --shadow-*: initial; --text-*: initial;`, which
**removes Tailwind's default palette, radii, shadows and text sizes**. Classes such as
`bg-blue-500`, `rounded-lg`, `shadow-md` or `text-xl` therefore do not exist: use only the
token classes (`bg-brand`, `rounded-control`, `shadow-hairline`, `text-body`, `bg-promo`).
Keep `styles.css` and this block identical; edit both together and only when asked.
Inter is loaded from `@fontsource-variable/inter` (imported at the top of `styles.css`).

Note on contrast: the reference uses lighter amber, green and red text on tinted chips. Those
fail WCAG AA, so the `-fg` values above are slightly deeper. Use them; do not lighten them.

---

## 2. Colour rules

- **Surfaces are white.** Cards, panels, popovers are `surface` with a `line` border. The page
  field is `shell`, never white-on-white without a border.
- **One accent: `brand` blue.** It marks the active tab, the primary action, selected nav
  icon tiles, and chart fill. Nothing else is blue, and nothing else gets a second accent.
- **Colour means status.** Green = success/positive/approved. Amber = pending/in progress/
  warning. Red = rejected/negative/error/unread count. Never use these for decoration.
- **Soft tint + deeper text.** A semantic chip is always `*-bg` fill with `*-fg` text and a
  `*-dot` marker. Never a saturated solid fill (except the red count badge).
- **Text is neutral.** `ink` for titles and values, `ink-2` for labels and table headers,
  `ink-3` for captions. No coloured body text except semantic chips and links.
- **Gradient is allowed exactly once pattern:** the promotional/upgrade card.
  `linear-gradient(160deg, #1e5edb 0%, #3b82e8 55%, #8ec5f4 100%)` plus a soft white radial
  glow in the upper area. No other gradients anywhere (no gradient buttons, headers, text).
- Pure `#000` is not used. Ink is `#171717`.

---

## 3. Typography

One family: **Inter** (neo-grotesque sans). Load it with `@fontsource-variable/inter`, not a
CDN. The reference is light and tight, never heavy.

| Role                        | Size / line | Weight | Tracking | Colour      |
| --------------------------- | ----------- | ------ | -------- | ----------- |
| Page title                  | 28 / 36     | 500    | -0.02em  | ink         |
| Stat value (big number)     | 40 / 44     | 400    | -0.03em  | ink         |
| Stat value suffix (`/128`)  | 20 / 28     | 400    | -0.01em  | ink-3       |
| Section / card title        | 20 / 28     | 500    | -0.01em  | ink         |
| Card title in a stat card   | 18 / 26     | 500    | -0.01em  | ink         |
| Body / row primary          | 15 / 22     | 500    | 0        | ink         |
| Body secondary / table cell | 15 / 22     | 400    | 0        | ink-2       |
| Caption / meta              | 13 / 18     | 400    | 0        | ink-3       |
| Chip / badge                | 13 / 16     | 500    | 0        | semantic fg |
| Group label (`MAIN MENU`)   | 11 / 14     | 500    | +0.04em  | ink-3       |

Rules:

- Weights are **400 and 500 only**. No 600, 700, bold, or italic anywhere.
- Large numbers are regular weight, not bold. Their unit/denominator is smaller and grey.
- Sentence case everywhere. The **only** uppercase text is the tiny sidebar-style group label.
- Currency and figures use tabular numerals (`tabular-nums`) in tables and totals; grouping
  commas and two decimals for money (`$512,872.92`).
- Body line length under 75 characters. No text below 11px, no text above 40px.
- Titles never wrap into a second styled line; do not highlight one word in a different
  colour, weight or italic.

---

## 4. Shape: radius, borders, strokes

| Element                                                         | Radius       | Border     |
| --------------------------------------------------------------- | ------------ | ---------- |
| Card / panel                                                    | `card` 20    | 1px `line` |
| Table header strip, segmented control track, progress bar       | `inset` 12   | none       |
| Button, input, dropdown trigger, nav item, icon tile, promo CTA | `control` 10 | see below  |
| Chip, status badge                                              | `chip` 6     | none       |
| Avatar, icon-only circle, count badge                           | full         | none       |
| Legend swatch                                                   | 2px          | none       |

- **Borders are always 1px `line`.** No 2px borders, no coloured borders (except focus ring),
  no dashed borders, no double borders.
- **Dividers are 1px `line`**, inset to the content edge. List rows are separated by a divider
  under each item, not by gaps or zebra stripes.
- **Nested radius:** an inner shape inside a padded container uses a smaller radius than the
  container (card 20 > inset 12 > control 10 > chip 6). Never use the same radius nested.
- **Icons:** outline style only, 20px (16px inside chips), stroke 1.5px, round caps and joins,
  colour `ink-2` by default. No filled icons, no duotone, no emoji. Exceptions: the active nav
  item and the stat-card icon tile, which sit in a `brand-soft` tile with a `brand` icon;
  and the row action icons (`danger-fg` circle-x, `success-fg` circle-check, outline).
- **Icon tile:** 40x40, `control` radius, `brand-soft` background, `brand` icon 20px.

---

## 5. Shadows and elevation

Elevation is expressed mostly by **border and surface colour**, not shadow.

| Level | Use                                                                                      | Token                               |
| ----- | ---------------------------------------------------------------------------------------- | ----------------------------------- |
| 0     | cards, panels, table, list                                                               | border only, **no shadow**          |
| 1     | small white buttons on a card (`Details`, `See all`), dropdown triggers, active nav item | `shadow-hairline` / `shadow-raised` |
| 2     | menus, popovers, dropdown lists, tooltips                                                | `shadow-popover`                    |

Never stack a shadow on top of a card border to make it "pop". Never use coloured shadows,
glows (except the promo card's internal glow), inner shadows or neumorphism. Shadows are
neutral black at 4 to 18% opacity.

---

## 6. Components

### Buttons

All buttons: height 40 (default) or 32 (small), `control` radius, 15px / weight 500, no
uppercase, no trailing arrow added by default (an arrow icon may be used where the action
navigates onward, as `Details ->` and `See all ->` in the reference: 16px outline arrow, 6px gap).

| Variant                              | Fill        | Text        | Border     | Shadow            | Hover                                     | Press         |
| ------------------------------------ | ----------- | ----------- | ---------- | ----------------- | ----------------------------------------- | ------------- |
| Primary                              | `brand`     | `on-brand`  | none       | none              | `brand-hover`                             | `brand-press` |
| Secondary (default for most actions) | `surface`   | `ink`       | 1px `line` | `shadow-hairline` | border `line-strong`, bg `surface-sunken` | bg `shell`    |
| Quiet (on a sunken strip)            | `surface`   | `ink-2`     | none       | `shadow-hairline` | text `ink`                                | bg `shell`    |
| Icon-only                            | transparent | `ink-2`     | none       | none              | bg `surface-sunken`                       | bg `shell`    |
| Destructive                          | `danger-bg` | `danger-fg` | none       | none              | slightly deeper tint                      | deeper        |

- Padding: 16px horizontal (default), 12px (small). Icon + label gap 8px.
- Max one primary button per view region. Full-width buttons allowed only inside narrow
  cards and the promo card (white button on gradient, `brand` text).
- Disabled: `surface-sunken` fill, `ink-disabled` text, no shadow, `cursor-not-allowed`.
- Loading: replace the label with a 16px spinner, keep the width.

### Inputs and search

- Fill `surface-sunken`, **no border**, `control` radius, height 44, 16px horizontal padding,
  16px leading outline icon in `ink-3`, placeholder `ink-3`.
- On a `surface-sunken` parent, use `surface` fill plus 1px `line` border instead.
- Focus: see section 8. Error: 1px `danger-fg` border and `danger-fg` helper text with icon.
- Labels sit above the field at 13/500 `ink-2`; helper text is 13/400 `ink-3`.

### Segmented control (tabs)

- Track: `surface-sunken`, `inset` radius, 4px padding.
- Items: equal width, 36px height, 8px radius, 14px/500.
- Active: `brand` fill, `on-brand` text. Inactive: transparent, `ink-2` text, hover `ink`.
- Only one segment active. Never underline-style tabs.

### Cards

- `surface`, 1px `line`, `card` radius, padding 20px (24px for large panels). No shadow.
- Card header: title on the left at section-title size, optional small secondary button on the
  right. The header is not separated from the body by a divider.
- **Stat card** adds a footer strip: `surface-sunken` background, full-bleed to card edges,
  top corners square, 12px vertical / 20px horizontal padding, 13px `ink-3` text and a small
  `Quiet` button. The footer is the only place a card uses a second surface tone.

### Chips and badges

- Chip: height 24, 8px horizontal padding, `chip` radius, 13/500, `*-bg` fill, `*-fg` text.
- Status chip with dot: 6px round `*-dot` before the label, 6px gap (`Pending`, `Approved`,
  `Rejected`).
- Delta chip beside a big number: same shape; may start with a 14px outline alert icon for
  negative deltas.
- Count badge (notifications): `danger-solid` fill, white 12/500 text, full radius, min 20x20,
  4px horizontal padding, caps at `99+`.
- Never use more than one chip colour in a single cell.

### Table

- Header row: `surface-sunken` fill, `inset` radius, 44px height, 14/400 `ink-2`, no bold,
  no uppercase, no sort icons unless the column sorts.
- Body rows: 64px height, no vertical dividers, 1px `line` divider between rows, no zebra,
  no hover fill darker than `surface-sunken`.
- Person cell: 36px circular avatar, 12px gap, name at body 15/500 `ink`.
- Numeric columns right-aligned with `tabular-nums`; text columns left-aligned.
- Row actions are 20px outline icon buttons, 8px apart, with a 32x32 hit area.

### List item (people / activity)

- Avatar 48px circle, 12px gap, primary line 15/500 `ink`, secondary line 13/400 `ink-3`.
- Items separated by 1px `line` dividers, 16px vertical padding. No card per row.

### Navigation items (sidebar-style)

- Height 44, `control` radius, 12px horizontal padding, 20px outline icon, 12px gap, 15px text.
- Inactive: transparent, `ink-2`. Hover: `surface` at 60% opacity, text `ink`.
- Active: `surface` fill, `ink` text, `shadow-raised`, icon in `brand`. No left bar, no
  coloured fill, no underline.
- Group labels (uppercase 11px) sit above groups with 24px spacing above them.
- A trailing count badge is right-aligned.

### Dropdown trigger and menus

- Trigger: `Secondary` small button with a 16px chevron-down, 6px gap.
- Menu: `surface`, 1px `line`, `control` radius, `shadow-popover`, 4px padding; items 36px
  high, 8px radius, hover `surface-sunken`, selected shows a `brand` check on the right.

### Progress / ratio bar

- Height 56 to 60, `inset` radius, two segments with a 6px gap: filled segment `brand`,
  remainder `line-strong` at 50% tint (`surface-sunken` darker). Both segments rounded.
- Legend below: 8px swatch (radius 2) in the segment colour, label `ink` 15px, value
  right-aligned `ink` with `tabular-nums`.
- Chart palette = `brand` + grey neutrals only. Add semantic colours only when the data is
  semantically good/bad.

### Promotional card

- The single gradient surface (section 2), `card` radius, 20px padding, 40px white icon tile
  top-left (`control` radius, `brand` icon), title 18/500 white, body 14/400 white at 85%
  opacity, full-width white `control` button with `brand` text. Use sparingly, one per screen.

### Avatars

- Always circular, `object-cover`, sizes 24 / 36 / 48 only. Fallback: initials 13/500 on
  `brand-soft` with `brand` text. No square avatars, no avatar ring/border.

### Empty, error, loading

- Empty: a 40px `brand-soft` icon tile, one sentence of what is missing, one `Secondary`
  button to fix it. No illustrations, no mascot.
- Error: `danger-bg` inline banner, `control` radius, 16px padding, 20px outline alert icon,
  states what failed and what to do. Never apologise.
- Loading: skeleton blocks in `surface-sunken` with the same radius as the element they
  replace, subtle pulse only. No full-screen spinner when content can be skeletonised.

---

## 7. Spacing and density

Spacing is a 4px grid. Allowed steps: 4, 8, 12, 16, 20, 24, 32, 40, 48.

- Card padding 20 (24 for large). Gap between sibling cards 16 or 20. Gap between a card
  title and its content 16. Icon-to-label 8 to 12. Row padding 16 vertical.
- The look is **airy but compact**: generous inner padding, small type, short rows.
  Do not pad cards beyond 24 or shrink controls below 32.
- Alignment: text and numbers left-aligned; numeric columns and trailing values right-aligned;
  never centre body copy.

---

## 8. States, focus, motion, accessibility

- **Focus:** every interactive element shows a 2px `brand` ring with 2px offset
  (`focus-visible:outline-2 outline-offset-2 outline-brand`). Never `outline-none` without a
  replacement.
- **Hover:** a quiet change only (border darkens, fill moves one neutral step, text goes
  `ink-2` -> `ink`). No lifts, no scale, no glow, no shadow growth.
- **Selected / active:** `brand` (tabs) or raised white (nav). Not both in the same control.
- **Disabled:** reduced contrast per the Buttons rule, `aria-disabled`, no hover change.
- **Motion:** 150 to 200ms, `ease-out`, on colour/background/border/opacity only. Menus
  fade and move 4px. No entrance animations on page load, no staggered reveals, no parallax,
  no bouncing. Respect `prefers-reduced-motion` by removing all non-essential motion.
- **Contrast:** body text >= 4.5:1, large text and icons >= 3:1. Status is never colour-only:
  chips carry a text label, row actions carry `aria-label`.
- **Hit areas:** at least 32x32 (44x44 on touch).
- **Dark mode:** not part of this system. Do not add a dark theme unless asked.

---

## 9. Copy in the interface

- Sentence case, plain verbs, no filler. Buttons say what happens (`Save changes`,
  `Approve request`, `View all`), never `Submit` or `Click here`.
- The same action keeps the same name across the flow (`Approve` -> toast `Approved`).
- Numbers carry meaning in context: `+12 this month`, `76% completed`, `30 remaining`.
- No exclamation marks, no emoji, no marketing tone inside the app.

---

## 10. Never do this

These break the system. If your output contains any of them, fix it before reporting done.

**Colour**

- No second accent colour (purple, teal, orange, pink) and no random brand-like hues.
- No gradients except the promotional card. No gradient text, buttons, borders or backgrounds.
- No pure black text or `#000` borders; no tinted/blue-grey greys that are not in the tokens.
- No saturated solid status fills (full-green, full-red buttons) except the red count badge.
- No coloured text for emphasis in titles or body copy.
- No raw hex, `rgb()`, or Tailwind palette classes (`bg-blue-500`, `text-gray-400`) in
  components; use the tokens.

**Shape and surface**

- No sharp corners (0 radius) and no pill-shaped buttons; no `rounded-full` except avatars,
  circular icon buttons and count badges.
- Same radius nested inside itself; mixed radii on sibling elements of the same kind.
- No borders thicker than 1px, no dashed or coloured borders (except focus and error).
- No heavy dividers, no zebra tables, no vertical table gridlines.
- No glassmorphism, blur backdrops, neumorphism, textured or patterned backgrounds.

**Shadow**

- No shadow on ordinary cards or panels. No large, coloured, spread or layered "floating
  card" shadows. No hover shadow lift. No inner shadows.

**Typography**

- No bold (600+), italic, underline-for-emphasis (links excepted), or text-shadow.
- No second typeface, no monospace for labels (tabular numerals only), no display/serif font.
- No ALL CAPS except the 11px group label; no letter-spaced uppercase eyebrows over headings.
- No decorative numbering (`01`, `02`) and no `A · B · C` meta strings.
- No highlighting one word of a heading in a different colour or weight.

**Components**

- No icon libraries mixing styles; no filled, duotone, or emoji icons; stroke is always 1.5px.
- No underline-style or pill-outline tabs; segmented control only.
- No gradient or illustrated empty states; no stock illustrations or 3D art.
- No cards-inside-cards-inside-cards; one level of card, inset areas use `surface-sunken`.
- No multiple primary buttons competing in one region; no coloured outline buttons.
- No avatars with borders, square avatars, or avatar stacks beyond +N counters.

**Motion and behaviour**

- No page-load entrance animations, scroll-triggered reveals, bounce, scale-on-hover, or
  looping animation. No animation that ignores `prefers-reduced-motion`.

**Code**

- No ad-hoc global CSS, `!important`, inline `style=""` for visuals, or arbitrary Tailwind
  values (`w-[347px]`, `text-[#333]`). Compose from tokens; extend `@theme` only when the
  user asks.

---

## 11. Pre-merge checklist

- [ ] Every colour, radius, shadow resolves to a token in section 1.
- [ ] Only weights 400/500; only Inter; sentence case; one uppercase label style at most.
- [ ] Cards have a 1px `line` border and no shadow; only menus/popovers use `shadow-popover`.
- [ ] Exactly one blue accent; status colours used only for status.
- [ ] Icons are 1.5px outline at 20px (16px in chips).
- [ ] Focus ring visible on every control; hover is quiet; motion respects reduced-motion.
- [ ] Nothing in section 10 is present.
