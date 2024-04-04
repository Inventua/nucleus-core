Use this layout to display a list of links or other content in the *LeftPane* and/or *RightPane* alongside your main content.  The left pane, content
pane and right pane widths are a 1:4:1 ratio.

| Pane             | Position                       | Notes
|------------------|--------------------------------|-------------------------------
| BannerPane       | Top, fills width               | Contains site logo, user account, site menu and breadcrumb controls.
| HeroPane         | Top, fills width               | A full-width non-scrollable pane to display an image and/or key message. Suppressed if empty.
| ContentWrapper   | Middle.                        | Not a pane.  Wrapper for the left/content/right panes.  Scrollable if content in the left or right pane does not fit.
| - LeftPane       | Middle-Left, fills height      | Suppressed if no content.
| - ContentPane    | Middle, fills height & width   | Scrollable if content does not fit.
| - RightPane      | Middle-Right, fills height     | Suppressed if no content.
| FooterPane       | Bottom, fills width            | Contains site terms and privacy policy links (if configured)

**Responsive UI**: 
If the browser width is less than 980px, the left, content and right panes automatically adjust to display underneath each other, and fill 
the full width of the page.