Use this layout to display a list of links or other content in the *LeftPane* alongside your main content.  The left pane and content
widths are a 1:5 ratio.

| Pane             | Position                       | Notes
|------------------|--------------------------------|-------------------------------
| BannerPane       | Top, fills width               | Contains site logo, user account, site menu and breadcrumb controls.
| OuterWrapper     | Middle.                        | Not a pane.  Wrapper for the left and content panes.
| - LeftPane       | Middle-Left, fills height      | Suppressed if no content.  Scrollable if content does not fit.
| - ContentWrapper | Fills height & width           | Scrollable if content does not fit.
|  - HeroPane      | Fills width                    | A full-width non-scrollable pane to display an image and/or key message. Suppressed if empty.
|  - ContentPane   | Fills height & width           | Suppressed if empty.
| FooterPane       | Bottom, fills width            | Contains site terms and privacy policy links (if configured)

**Responsive UI**: 
If the browser width is less than 980px, the left and content automatically adjust to display underneath each other, and fill 
the full width of the page.