---
name: Wahl Mirai
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#424750'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#737781'
  outline-variant: '#c3c6d1'
  surface-tint: '#335f99'
  primary: '#003466'
  on-primary: '#ffffff'
  primary-container: '#1a4b84'
  on-primary-container: '#93bcfc'
  inverse-primary: '#a6c8ff'
  secondary: '#006d37'
  on-secondary: '#ffffff'
  secondary-container: '#6bfe9c'
  on-secondary-container: '#00743a'
  tertiary: '#313536'
  on-tertiary: '#ffffff'
  tertiary-container: '#484b4d'
  on-tertiary-container: '#b8bbbd'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d5e3ff'
  primary-fixed-dim: '#a6c8ff'
  on-primary-fixed: '#001c3b'
  on-primary-fixed-variant: '#144780'
  secondary-fixed: '#6bfe9c'
  secondary-fixed-dim: '#4ae183'
  on-secondary-fixed: '#00210c'
  on-secondary-fixed-variant: '#005228'
  tertiary-fixed: '#e0e3e5'
  tertiary-fixed-dim: '#c4c7c9'
  on-tertiary-fixed: '#191c1e'
  on-tertiary-fixed-variant: '#444749'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
  status-active: '#27AE60'
  status-deleted: '#E74C3C'
  status-voted: '#1A4B84'
  status-graduated: '#94A3B8'
  border-subtle: '#E2E8F0'
typography:
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '700'
    lineHeight: '1.2'
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  headline-sm:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: '1.4'
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.5'
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: '1'
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: '1'
    letterSpacing: 0.05em
  code-sm:
    fontFamily: Courier Prime
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.4'
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  container-max: 1280px
  gutter: 24px
  margin-mobile: 16px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

The brand identity is built on the pillars of **Institutional Integrity, Transparency, and Technological Advancement**. As a digital voting platform for students, the design must bridge the gap between a formal civic process and a modern, user-centric educational tool. 

The chosen style is **Corporate / Modern** with a focus on **Confirmation-Driven Design**. The interface utilizes generous white space to reduce cognitive load during the high-stakes act of voting. The visual language conveys security through structured layouts and refined typography, while soft rounding and subtle elevation prevent the platform from feeling overly bureaucratic or intimidating. The primary goal is to foster trust and ensure that the democratic process is accessible, clear, and efficient for both students and administrators.

## Colors

The palette is anchored by **Institutional Blue**, representing authority and stability. **Clean White** and **Off-White** (Tertiary) are used for surfaces to maximize legibility and create a sense of digital "blank canvas" clarity. 

**Green** is reserved for positive actions (Success/Confirmation), specifically for the "Voted" state and final submission buttons. A functional set of **Named Colors** provides immediate semantic meaning for the student census: 
- **Status Active:** A vibrant green for current voters.
- **Status Deleted:** A clear red for administrative removal.
- **Status Graduated:** A neutral slate for historical records.

The default mode is **Light**, ensuring a "paper-like" familiarity for the voting process, though the high-contrast blue ensures readability across all lighting conditions.

## Typography

This design system uses **Inter** for all primary interfaces to leverage its exceptional legibility and neutral, professional character. 

- **Headlines:** Use tight letter-spacing and heavier weights to establish a clear hierarchy in the Admin Dashboard and on the Ballot (Tarjetón).
- **Body Text:** Set with generous line height to facilitate the reading of long candidate proposals.
- **Labels:** Uppercase or medium-weight labels are used for form fields and table headers to distinguish them from user data.
- **Monospace:** **Courier Prime** is used specifically for technical identifiers (ID numbers) and temporary passwords to ensure distinct character recognition (e.g., distinguishing '1' from 'l').

## Layout & Spacing

The system employs a **Fluid Grid** model that prioritizes focus. 

- **Desktop:** A 12-column grid with a 1280px max-width container. 
- **Candidate Grid (Tarjetón):** Elements are arranged in a responsive grid that adjusts from 1 column on mobile to 3 or 4 columns on desktop, ensuring candidate photos remain prominent.
- **Three-Click Rule:** Layouts are optimized to ensure the voting path (Selection -> Proposal Review -> Confirmation) is achieved in no more than three clicks.
- **White Space:** Heavy vertical padding (`stack-lg`) is used to separate the header from the main interaction area, keeping the focus entirely on the current task.

## Elevation & Depth

Hierarchy is established through **Tonal Layers** and **Ambient Shadows**:

1.  **Base Layer:** The tertiary color (#F8FAFC) acts as the canvas.
2.  **Surface Layer:** White cards house primary content (Candidates, Census Tables). These use a very soft, low-opacity shadow (4% - 8% alpha) to appear slightly lifted.
3.  **The Modal Overlay:** For "Propuestas" (Proposals), a backdrop dimming effect (60% opacity dark blue) is used to completely isolate the voter's focus. The modal itself has a higher elevation with a more pronounced, diffused shadow.
4.  **Logical Deletion:** Records marked as "Eliminado" in the census should lose their elevation entirely, appearing flat or slightly translucent compared to "Active" records.

## Shapes

The shape language is **Rounded** (0.5rem base), striking a balance between institutional structure and modern approachable software.

- **Buttons & Inputs:** Use the standard 0.5rem radius.
- **Candidate Cards:** Use `rounded-lg` (1rem) to create a friendly, modern container for photos.
- **Status Badges:** Utilize pill shapes (3) to distinguish status indicators (Active/Voted) from interactive buttons.
- **Images:** Candidate photos should follow the `rounded-lg` clipping of their parent cards to maintain consistency.

## Components

- **Buttons:** 
    - *Primary:* Solid Blue (#1A4B84) for progress.
    - *Success:* Solid Green (#2ECC71) exclusively for "Confirmar Voto".
    - *Ghost:* Outlined for "Volver" or "Cancelar" actions.
- **Candidate Cards:** Must include a placeholder or student photo, name, and a clear "Ver Propuesta" button. The card should change border color when selected.
- **Census Tables:** Clean rows with `border-subtle` separators. Use `status-active` and `status-deleted` colors for text labels in the "Estado" column.
- **Modals:** Used for "Propuestas". Must feature a clear "X" to close and a primary button at the bottom to proceed to confirmation.
- **Inputs:** High-contrast borders that thicken on focus. Validation errors use a red border and helper text.
- **Data Visualization:** Real-time charts for results should use a dynamic palette that ensures high contrast between adjacent candidates.