## IFrame module
The IFrame module embeds another HTML page in your site.

## Settings

{.table-25-75}
|                      |                                                                                      |
|----------------------|--------------------------------------------------------------------------------------|
| Url                  | The Url of the content to embed.  If your site uses https then the Url must also use https, as most browsers will not serve (mixed content)[https://developer.mozilla.org/en-US/docs/Web/Security/Mixed_content].  |
| Title                | Title of the IFrame.  The Title should describe the embedded content. People navigating with assistive technology such as a screen reader can use the title to identify the content.   |
| Name                 | You can specify a name for the IFrame, which can be used in the target attribute of some HTML elements and by Javascript. |
| Width                | You can specify the width of the IFrame using any expression which can be interpreted as a (width)[https://developer.mozilla.org/en-US/docs/Web/CSS/width] by a browser.  If you don't specify a width, then width: 100% is used. |
| Height               | You can specify the height of the IFrame using any expression which can be interpreted as a (height)[https://developer.mozilla.org/en-US/docs/Web/CSS/height] by a browser.  If you don't specify a height, no height is specified in the output, but a flex:1 style is added so that the IFrame can grow if your layout and container supports it (see below). |
| Scrolling            | Specifies if a scroll bar is displayed for your content.  You can specify Automatic, Yes or No.  |
| Border               | Specifies whether a border is drawn around the IFrame.  |

> The height, width and border settings add a style attribute and specify their values using CSS. 

> The w3c standard specifies that if an IFrame height is not specified, the default height is 150px.  If your layout and container specifies CSS display:flex, on all of the ancestor elements of the IFrame
and uses flex:1 or some other method to set their heights, the IFrame can grow to fit the height of its parent, because we render a flex:1 CSS style when the height setting is not set.
