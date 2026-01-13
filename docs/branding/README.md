# San Saba Royalty - SSR System Branding Guide

## Brand Overview

**Company Name:** San Saba Royalty  
**Tagline:** "Pioneers of the Mineral Rights and Royalty Industry"  
**Industry:** Mineral Rights & Royalty Acquisition  
**System:** SSR (Structured Settlement Report) Acquisition Management System  
**Founded:** 2003  
**Portfolio:** $1+ Billion in acquisitions, 10,000+ closed transactions

## Typography

### Primary Fonts (Actual WordPress Theme)

- **Display/Headers:** League Gothic Regular - Primary display font for all headings (h1-h6)
- **Body/Content:** Roboto - Main body text and interface elements
- **Monospace/Data:** System monospace fonts - For data tables and code display
- **Fallback:** Sans-serif system fonts

### Font Usage (Based on Actual CSS)

```css
/* Headers and Branding - League Gothic Regular */
h1, h2, h3, h4, h5, h6 {
  font-family: 'league_gothicregular', sans-serif;
  text-transform: uppercase;
  color: #005a5d;
  font-weight: normal;
  -webkit-font-smoothing: antialiased;
}

/* Body Text and Interface - Roboto */
body, p, div, span, .content {
  font-family: 'Roboto', sans-serif;
  color: #848381;
  line-height: 1.67;
}

/* Buttons and UI Elements - League Gothic */
.btn {
  font-family: 'league_gothicregular', sans-serif;
  text-transform: uppercase;
  font-weight: normal;
}
```

### Heading Scale (Actual CSS Values)

- **H1:** 3.6em - League Gothic Regular, uppercase
- **H2:** 2.4em - League Gothic Regular, uppercase  
- **H3:** 1.8em - League Gothic Regular, uppercase
- **H4:** 1.4em - League Gothic Regular, uppercase
- **H5:** 1em - League Gothic Regular, uppercase
- **H6:** 0.8em - League Gothic Regular, uppercase

## Color Palette

### Primary Brand Colors (From Actual CSS)

- **Teal Primary:** `#005a5d` - Main brand color used throughout site
- **Teal Secondary:** `#2D8B8B` - Secondary brand accent
- **Teal Dark:** `#00292a` - Darker variant for hover states
- **Professional Blue:** `#0b394f` - Secondary brand color for depth

### Supporting Colors (Extracted from CSS)

- **Gray Primary:** `#848381` - Main body text color
- **Gray Secondary:** `#gray` - Used for headings in certain contexts
- **Selection Blue:** `#005a5d` - Text selection background
- **White:** `#FFFFFF` - Clean backgrounds and contrast
- **Light Gray:** `#EEE` - Section backgrounds and dividers

### State Colors

- **Success/Active:** `#005a5d` - Selected states, primary actions
- **Hover States:** `#00292a` - Darker teal for interactive elements
- **Background Overlay:** `rgba(0, 90, 93, 0.6)` - Transparent teal overlay
- **Border/Divider:** `rgba(0, 0, 0, 0.1)` - Subtle element separation

## Updated Tailwind CSS Theme Configuration

### Light Mode Colors (WordPress Theme Aligned)

```javascript
// tailwind.config.js - Updated for WordPress Theme
module.exports = {
  theme: {
    extend: {
      colors: {
        // Primary Brand Colors (from actual CSS)
        'sansaba': {
          50: '#F0F9F9',
          100: '#CCEFEF', 
          200: '#99DFDF',
          300: '#66CFCF',
          400: '#339999',
          500: '#005a5d', // Primary from CSS
          600: '#004d50',
          700: '#003d40',
          800: '#002d30',
          900: '#001d20'
        },
        
        // Professional Blue (from CSS)
        'professional': {
          50: '#F0F7FF',
          100: '#C7E2FF',
          200: '#9FCDFF',
          300: '#66B3FF',
          400: '#3399FF',
          500: '#0b394f', // Secondary from CSS
          600: '#092d40',
          700: '#072131',
          800: '#051922',
          900: '#030d13'
        },
        
        // System Colors (CSS Values)
        'primary': '#005a5d',
        'secondary': '#0b394f',
        'accent': '#2D8B8B',
        'text-primary': '#848381',
        'text-heading': '#005a5d',
        'hover': '#00292a',
        'selection': '#005a5d',
        
        // Neutral Palette (CSS Based)
        'base-50': '#FFFFFF',
        'base-100': '#FEFEFE',
        'base-200': '#EEE',
        'base-300': '#CCC',
        'base-400': '#gray',
        'base-500': '#848381',
        'base-600': '#000',
        'base-content': '#848381',
      },
      
      // Typography (WordPress Theme)
      fontFamily: {
        'heading': ['league_gothicregular', 'sans-serif'],
        'body': ['Roboto', 'sans-serif'],
        'sans': ['Roboto', 'sans-serif'],
        'display': ['league_gothicregular', 'sans-serif']
      }
    }
  }
}
```

## WordPress Theme Integration

### CSS Custom Properties (WordPress Compatible)

```css
:root {
  /* Brand Colors (from actual CSS) */
  --sansaba-primary: #005a5d;
  --sansaba-secondary: #0b394f;
  --sansaba-hover: #00292a;
  
  /* Typography (actual fonts) */
  --font-heading: 'league_gothicregular', sans-serif;
  --font-body: 'Roboto', sans-serif;
  
  /* Spacing (from CSS patterns) */
  --space-unit: 10px; /* Based on padding patterns */
  --border-radius: 0px; /* Theme uses sharp corners */
  --transition: all 0.5s ease; /* From CSS animations */
}
```

### Font Loading (WordPress Theme Fonts)

```html
<!-- League Gothic Regular (Custom Font) -->
@font-face {
  font-family: 'league_gothicregular';
  src: url('../fonts/leaguegothic-regular-webfont.eot');
  src: url('../fonts/leaguegothic-regular-webfont.eot?#iefix') format('embedded-opentype'),
       url('../fonts/leaguegothic-regular-webfont.woff2') format('woff2'),
       url('../fonts/leaguegothic-regular-webfont.woff') format('woff'),
       url('../fonts/leaguegothic-regular-webfont.ttf') format('truetype');
  font-weight: normal;
  font-style: normal;
}

<!-- Google Fonts for Roboto -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap" rel="stylesheet">
```

## UI Components (WordPress Theme Styling)

### Buttons (Actual CSS)

```css
/* Primary Button (from theme CSS) */
.btn {
  border: 0;
  color: #FFF;
  border-radius: 0; /* Sharp corners in theme */
  font-family: 'league_gothicregular', sans-serif;
  text-transform: uppercase;
  font-size: 1.5em;
  padding: 5px 40px;
  -webkit-font-smoothing: antialiased;
  transition: all 0.5s ease;
}

.btn.btn-primary {
  background: #005a5d;
}

.btn.btn-primary:hover {
  background: #00292a;
}

.btn.btn-default {
  background: gray;
}

.btn.btn-secondary {
  background: #0b394f;
}
```

### Form Elements (Theme Styling)

```css
/* Input Fields (from CSS) */
input, select, textarea {
  width: 100%;
  border-radius: 0; /* Sharp corners */
  border: 0;
  padding: 10px;
}

/* Labels */
label, .form label {
  font-size: 14px;
  font-weight: normal;
  font-family: 'Roboto', sans-serif;
}
```

### Navigation (WordPress Theme)

```css
/* Navigation Styling (from CSS) */
.navigation ul li a {
  border-radius: 0;
  font-size: 1.2em;
  font-family: 'Roboto', sans-serif;
}

.navigation ul li.selected a {
  background: #005a5d;
  color: #FFF;
}
```

## Content Guidelines

### Brand Voice & Tone

**Professional Authority**
- Confident expertise in mineral rights using League Gothic headers
- 20+ years of industry experience
- Billion-dollar portfolio credibility

**Approachable Expertise** 
- Clear communication in Roboto body text
- Patient education for complex topics
- Responsive to landowner concerns

**Trustworthy Partnership**
- Transparent process using consistent #005a5d branding
- Reliable closing timelines (<30 days)
- Long-term relationship focus

### Typography Hierarchy (WordPress Theme)

```css
/* Heading System (Actual CSS) */
h1 { font-size: 3.6em; color: #005a5d; } /* Hero Headlines */
h2 { font-size: 2.4em; color: #005a5d; } /* Page Titles */
h3 { font-size: 1.8em; color: #005a5d; } /* Section Headers */
h4 { font-size: 1.4em; color: #005a5d; } /* Subsections */
h5 { font-size: 1em; color: #005a5d; }   /* Small Headers */
h6 { font-size: 0.8em; color: #005a5d; } /* Caption Headers */

/* All headings use League Gothic Regular, uppercase */
/* Body text uses Roboto at 18px base size with #848381 color */
```

## Responsive Design (WordPress Theme Breakpoints)

### Breakpoints (From Actual CSS)

```css
/* Mobile First Approach (from theme CSS) */
@media (max-width: 320px) { /* Small Mobile */ }
@media (max-width: 414px) { /* Mobile */ }
@media (max-width: 768px) { /* Tablet */ }
@media (max-width: 992px) { /* Small Desktop */ }
@media (max-width: 1200px) { /* Desktop */ }
@media (max-width: 1366px) { /* Large Desktop */ }
```

### Mobile Considerations (WordPress Theme)

- League Gothic remains primary heading font across all devices
- Roboto maintains readability at smaller sizes
- #005a5d brand color provides sufficient contrast
- Font sizes scale appropriately per CSS media queries
- Sharp corners (border-radius: 0) maintained on mobile

## WordPress Theme Specific Elements

### Special Classes (From CSS)

```css
/* Content Styling */
.content {
  min-height: 400px;
  line-height: 30px;
  color: #848381; /* Roboto body text */
}

/* Headings in Content */
.content h1, .content h2, .content h3, 
.content h4, .content h5, .content h6 {
  font-family: 'league_gothicregular', sans-serif;
  text-transform: uppercase;
  color: #005a5d;
}

/* Selection Styling */
*::selection {
  background: #005a5d;
  color: #FFF;
}
```

### Menu Container (Full-Screen Menu)

```css
/* Edge Menu System */
.edge-menu {
  background: #005a5d;
  font-family: 'Roboto', sans-serif;
}

.menu-container .item .item-content h4 {
  color: #FFF;
  font-family: 'league_gothicregular', sans-serif;
  text-transform: uppercase;
}
```

## Implementation Guidelines

### WordPress Integration

1. **Font Loading:** Ensure League Gothic Regular fonts are properly loaded
2. **Color Variables:** Use CSS custom properties for consistent theming  
3. **Component Classes:** Follow existing WordPress theme class structure
4. **Responsive Scaling:** Maintain font hierarchy across breakpoints
5. **Brand Consistency:** Keep #005a5d as primary throughout all components

### Quality Assurance Checklist

- [ ] League Gothic Regular displays correctly for all headings
- [ ] Roboto loads properly for body text
- [ ] #005a5d brand color appears consistently
- [ ] Sharp corners (border-radius: 0) maintained
- [ ] Text selection shows #005a5d background
- [ ] Mobile font scaling follows CSS media queries
- [ ] WordPress theme classes integrate properly

---

*This updated brand guide reflects the actual WordPress theme implementation with League Gothic Regular as the primary display font and Roboto as the body font, ensuring consistent branding across all San Saba Royalty SSR system applications.*