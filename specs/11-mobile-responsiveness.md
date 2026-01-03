# Mobile Responsiveness Specification

## Overview

This specification defines mobile responsiveness standards for the Allowance Tracker web application. The app uses Tailwind CSS v4 with a mobile-first approach, ensuring optimal experiences across all device sizes.

## Design Principles

1. **Mobile-First**: Base styles target mobile devices; responsive prefixes add desktop enhancements
2. **Touch-Friendly**: Minimum 44px touch targets (WCAG 2.1 AAA compliance)
3. **Progressive Enhancement**: Core functionality works on all devices; enhanced features on larger screens
4. **Consistent Spacing**: 8px grid system (Tailwind's default spacing scale)
5. **Readable Text**: Minimum 16px body text to prevent zoom on iOS Safari

## Breakpoint Standards

| Breakpoint | Min Width | Target Devices | Layout Behavior |
|------------|-----------|----------------|-----------------|
| (base) | 0px | Small phones | Single column, stacked layouts |
| `sm:` | 640px | Large phones, small tablets | 2-column grids begin |
| `md:` | 768px | Tablets | Sidebar visible, multi-column forms |
| `lg:` | 1024px | Laptops, desktops | 3+ column grids, side-by-side charts |
| `xl:` | 1280px | Large desktops | Max-width containers (1280px) |

### When to Use Each Breakpoint

- **Base (mobile)**: Default styles, single column layouts, stacked content
- **sm**: Start introducing 2-column grids for cards and form fields
- **md**: Major layout shift - sidebar appears, forms go multi-column
- **lg**: Add third column for card grids, show more data
- **xl**: Rarely needed; primarily for max-width constraints

## Component Patterns

### Navigation

**Desktop (md+)**:
- Fixed left sidebar, 264px width (`md:w-64`)
- Always visible with full navigation labels

**Mobile (<md)**:
- Hidden sidebar, hamburger menu in fixed top header
- Sliding drawer overlay with same navigation items
- Header height: 56px (`py-3` with icons)

```jsx
// Pattern: Desktop sidebar
<div className="hidden md:fixed md:inset-y-0 md:flex md:w-64 md:flex-col">

// Pattern: Mobile header
<div className="md:hidden fixed top-0 left-0 right-0 z-40">
```

### Cards & Grids

Standard responsive grid pattern:

```jsx
<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
```

- **Mobile**: 1 column, full width cards
- **Tablet (sm+)**: 2 columns
- **Desktop (lg+)**: 3 columns

### Forms

**Input Styling**:
```jsx
<input className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md
  shadow-sm focus:outline-none focus:ring-primary-500 focus:border-primary-500
  text-base sm:text-sm" />
```

- Use `text-base` on mobile to prevent iOS zoom (16px minimum)
- Grid layouts stack on mobile: `grid-cols-1 sm:grid-cols-2`

**Buttons**:
```jsx
<button className="w-full sm:w-auto px-4 py-2.5 min-h-[44px] ...">
```

- Full width on mobile (`w-full sm:w-auto`)
- Minimum 44px height for touch targets

### Tabs

For components with multiple tabs:

```jsx
<nav className="flex border-b overflow-x-auto scrollbar-hide">
  <button className="flex-shrink-0 px-4 py-3 min-w-[44px]">
    <Icon className="w-5 h-5" />
    <span className="hidden sm:inline ml-2">{label}</span>
  </button>
</nav>
```

- Horizontal scroll on mobile if tabs overflow
- Show icons only on mobile, icons + labels on tablet+
- Minimum 44px touch target per tab

### Charts & Data Visualization

```jsx
<div className="h-[200px] sm:h-[300px]">
  <ResponsiveContainer width="100%" height="100%">
    <LineChart>
      <XAxis tick={{ fontSize: 10 }} angle={-45} textAnchor="end" />
    </LineChart>
  </ResponsiveContainer>
</div>
```

- Reduced height on mobile (200px vs 300px)
- Smaller font size for axis labels
- Angled labels to prevent overlap
- Hide complex labels on mobile, show legend below

### Modals & Dialogs

```jsx
<div className="fixed inset-0 z-50 flex items-center justify-center p-4">
  <div className="w-full max-w-md bg-white rounded-lg shadow-xl">
```

- Full screen minus padding on mobile
- Centered with `max-w-md` constraint
- Always include close button in accessible location

### Lists & Tables

For data-heavy displays, convert tables to card layouts on mobile:

```jsx
// Desktop: Table
<table className="hidden sm:table w-full">

// Mobile: Card list
<div className="sm:hidden space-y-4">
  {items.map(item => <MobileCard key={item.id} {...item} />)}
</div>
```

## Touch Target Guidelines

All interactive elements must meet minimum touch target sizes:

| Element | Minimum Size | Implementation |
|---------|--------------|----------------|
| Buttons | 44x44px | `min-h-[44px] min-w-[44px]` or `py-2.5 px-4` |
| Links | 44x44px tap area | `py-2` with adequate spacing |
| Form inputs | 44px height | `py-2` (default ~40px) or `py-2.5` |
| Icon buttons | 44x44px | `p-2.5` with 24px icon |
| Checkboxes/radios | 44x44px tap area | Wrapper with padding |

## Spacing Standards

Use Tailwind's default spacing scale (based on 4px increments):

| Token | Value | Usage |
|-------|-------|-------|
| `p-4` | 16px | Card padding (mobile) |
| `p-6` | 24px | Card padding (desktop) |
| `gap-4` | 16px | Grid gaps (mobile) |
| `gap-6` | 24px | Grid gaps (desktop) |
| `space-y-4` | 16px | Vertical stack spacing |
| `mb-6` | 24px | Section margins |

## Typography

| Element | Mobile | Desktop |
|---------|--------|---------|
| Page titles | `text-2xl` (24px) | `text-3xl` (30px) |
| Section headers | `text-lg` (18px) | `text-lg` (18px) |
| Body text | `text-base` (16px) | `text-sm` (14px) |
| Labels | `text-sm` (14px) | `text-sm` (14px) |
| Captions | `text-xs` (12px) | `text-xs` (12px) |

Note: Always use `text-base` (16px) for form inputs on mobile to prevent iOS Safari auto-zoom.

## Testing Checklist

### Viewport Widths to Test
- 320px (iPhone SE, small Android)
- 375px (iPhone 12/13/14)
- 414px (iPhone Plus/Max models)
- 768px (iPad portrait)
- 1024px (iPad landscape, small laptops)

### Manual Testing Steps
1. Open dev tools and set mobile viewport
2. Test all navigation paths
3. Fill out all forms
4. Verify touch targets with dev tools overlay
5. Check text truncation and overflow
6. Test with orientation changes
7. Verify modals don't overflow viewport

### Automated Testing
Use Playwright to capture screenshots at each breakpoint:

```javascript
const viewports = [
  { width: 320, height: 568 },  // iPhone SE
  { width: 375, height: 812 },  // iPhone 12
  { width: 768, height: 1024 }, // iPad
  { width: 1280, height: 800 }, // Desktop
];

for (const viewport of viewports) {
  await page.setViewportSize(viewport);
  await page.screenshot({ path: `screenshot-${viewport.width}.png` });
}
```

## Implementation Status

### Completed
- [x] Navigation (Layout.tsx) - Mobile drawer + desktop sidebar
- [x] Dashboard grid - Responsive card layout
- [x] Wish list grid - Responsive card layout
- [x] Modal dialogs - Centered with max-width
- [x] Transaction list - Card-based display

### Updated in This Spec
- [x] ChildDetail header - Stacks on mobile
- [x] Tab navigation - Icons only + horizontal scroll on mobile
- [x] Transaction form - Grid stacks on mobile
- [x] Analytics charts - Reduced height + smaller labels on mobile
