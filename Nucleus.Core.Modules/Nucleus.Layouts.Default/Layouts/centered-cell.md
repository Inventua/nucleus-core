This layout is for special cases when you want to display a small amount of content in the middle of the page, like a login page.

| Pane             | Position                       | Notes
|------------------|--------------------------------|-------------------------------
| BannerPane       | Top, fills width                | Contains site logo, user account, site menu and breadcrumb controls.
| ContentWrapper   | Middle, centered (both directions), 40rem minimum width | Not a pane.  Wrapper.  Scrollable if content does not fit.
| - ContentPane    | Left of content wrapper, 40rem minimum width | Fills available horizontal space, 2:1 ratio with side pane. Suppressed if empty.
| - SidePane       | Right of content wrapper. | Suppressed if empty.
| - SecondaryPane  | Below content/side panes. | Suppressed if empty.
| FooterPane       | Bottom, fills width             | Contains site terms and privacy policy links (if configured)


*Responsive UI*
If the browser width is less than 980px, the side pane automatically adjusts to display underneath the content pane.