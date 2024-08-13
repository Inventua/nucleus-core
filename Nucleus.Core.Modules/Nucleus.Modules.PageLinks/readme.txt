2.0.1
- Added logic to automatic mode javascript to prevent extra <ol> elements from being rendered when page content contains heading levels which are configured to be ignored.
- Updated automatic mode so that when the RootSelector option is used, header elements can be at any descendant level, rather than bing a direct child of the specified selector.
- Fixed code which uses data-title as page link text to use .attr instead of .prop.

2.0.0
First version.
