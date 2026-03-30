# Design System Document

## 1. Overview & Creative North Star

### Creative North Star: "The Digital Curator"
This design system is built to transform a 3D product management tool into a high-end editorial experience. Moving away from the cluttered HUDs of traditional gaming, we adopt the persona of a "Digital Curator." The interface should feel like an elite physical gallery—clean, expansive, and breathable—where the 3D product is the undisputed hero.

Instead of rigid, boxed-in grids, the system leverages **Intentional Asymmetry**. UI elements are treated as floating architectural layers, utilizing overlapping surfaces and high-contrast typography scales to guide the eye. By shifting the focus from "containers" to "content depth," we create a premium aesthetic that feels both modern and luxurious.

---

## 2. Colors

The palette is anchored by a sophisticated neutral base, punctuated by a vibrant, "Electric Blue" that provides energy and digital soul.

### The Palette
- **Primary (`#0049e8`):** Our signature Electric Blue. Used sparingly for high-impact actions.
- **Surface Hierarchy:** 
    - `surface`: Primary background (`#f5f6f7`).
    - `surface-container-lowest`: Pure white (`#ffffff`) for elevated cards.
    - `surface-container-high`: Subtle contrast (`#e0e3e4`) for secondary groupings.
- **Accents:** Tertiary (`#903985`) is reserved for rare, "legendary" product statuses or unique collectors' markers.

### Visual Rules
- **The "No-Line" Rule:** 1px solid borders are prohibited for sectioning. Boundaries must be defined solely through background color shifts (e.g., a `surface-container-lowest` card sitting on a `surface` background) or tonal transitions.
- **The "Glass & Gradient" Rule:** To achieve a premium look, use Glassmorphism for floating overlays. Apply a semi-transparent `surface` color with a `backdrop-blur` of 20px-40px. 
- **Signature Textures:** Main CTAs must use a linear gradient from `primary` (#0049e8) to `primary-container` (#829bff) at a 135-degree angle. This mimics the light-refracting quality of high-end toy packaging.

---

## 3. Typography

**Inter** is the sole typeface, chosen for its architectural precision and legibility.

- **Editorial Scale:** Use a massive contrast between `display-lg` (3.5rem) and `body-sm` (0.75rem). 
- **Hierarchy through Weight:** Titles should use Bold/Semi-Bold weights to anchor sections, while body text remains Regular or Medium to maintain an airy, sophisticated feel.
- **Brand Identity:** High-end toy names should utilize `display-md` or `headline-lg` with tight letter-spacing (-0.02em) to mimic luxury fashion branding.

---

## 4. Elevation & Depth

In this design system, depth is a narrative tool, not just a visual effect.

- **The Layering Principle:** Stacking `surface-container` tiers is the primary method of hierarchy. A `surface-container-lowest` element (pure white) placed on a `surface-dim` background creates a natural "lift" without visual clutter.
- **Ambient Shadows:** When elements must float (like a 3D viewer control panel), use ultra-diffused shadows. 
    - **Blur:** 24px - 40px. 
    - **Opacity:** 4%-6%.
    - **Color:** Use a tinted version of `on-surface` (dark blue-grey) rather than pure black to keep the lighting natural.
- **The "Ghost Border" Fallback:** If a boundary is strictly required for accessibility, use the `outline-variant` token at 15% opacity. This creates a "suggestion" of a line rather than a hard wall.
- **Nesting:** Inspired by gaming detail screens, use overlapping elements. A product name might overlap the 3D viewer space, anchored by a glassmorphic background blur, creating a sense of three-dimensional space within the 2D UI.

---

## 5. Components

### Buttons
- **Primary:** Gradient fill (`primary` to `primary_container`), `ROUND_FOUR` (0.5rem) corners, and a subtle white inner-glow (1px stroke at 10% opacity) on the top edge.
- **Secondary:** Transparent background with a "Ghost Border" and `on_surface` text.
- **Tertiary:** Text-only with an icon, using `spacing-1` for the gap.

### Cards & Lists
- **Prohibition of Dividers:** Forbid horizontal lines. Use `spacing-4` or `spacing-6` (vertical whitespace) to separate list items.
- **Depth Transitions:** For list items, use a subtle background shift to `surface_container_low` on hover to indicate interactivity.

### Inputs & Fields
- **Minimalist State:** Input fields should not have a background fill. Use a bottom-only `outline_variant` (20% opacity) that transforms into a `primary` (100% opacity) underline upon focus.

### Additional Signature Components
- **3D Orbit Controller:** A floating, glassmorphic circular dial located at the bottom center of the viewer, allowing users to rotate the toy.
- **Status Tags:** Use `secondary_container` with `on_secondary_container` text for product rarity or condition, using `full` roundedness for a pill shape.

---

## 6. Do's and Don'ts

### Do
- **DO** use the Spacing Scale (specifically `3`, `5`, and `8`) to create "breathing room" around 3D assets.
- **DO** overlap text elements over the 3D viewer using glassmorphism to create a sense of integration.
- **DO** use `ROUND_FOUR` (0.5rem) consistently for all interactive containers.

### Don't
- **DON'T** use 100% opaque black shadows; they feel "cheap" and heavy.
- **DON'T** use solid 1px borders to separate content sections; rely on tonal shifts between `surface-container` tiers.
- **DON'T** crowd the 3D viewer. Keep UI elements toward the edges or floating in clearly defined glassmorphic clusters.
- **DON'T** use high-contrast dividers. If you feel the need for a line, increase your whitespace (`spacing-8`) instead.