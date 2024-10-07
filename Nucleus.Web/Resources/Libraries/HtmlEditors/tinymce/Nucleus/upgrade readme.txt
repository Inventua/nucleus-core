After upgrading tinymce, a change is required to Resources\Libraries\HtmlEditors\tinymce\<version>\skins\ui\tinymce-5\skin.min.css

This change is required because the base CSS for tinymce interferes with code coloring in the Monaco editor.  We use the Monaco
editor in the HTML/code editor.

Find:
tox :not(svg):not(rect){box-sizing:inherit;color:initial;

Remove color:initial.