/**
 * TinyMCE version 6.8.2 (2023-12-11)
 * 
 * Nucleus changes: This is a copy of the TinyMCE "code" plugin, with support for the Monaco editor added.
 */
!(function ()
{
  "use strict";
  tinymce.util.Tools.resolve("tinymce.PluginManager").add(
    "code_monaco",
    (editor) => (
      ((editor) =>
      {
        editor.addCommand("mceCodeEditor", () =>
        {
          ((editor) =>
          {
            const open = ((editor) => editor.getContent({ source_view: !0 }))(editor);
            editor.windowManager.open
            (
              {
                title: "Source Code",
                size: "large",
                body:
                {
                  type: "panel",
                  items:
                    [
                      { type: "textarea", name: "code" },
                      {
                        type: 'htmlpanel',
                        html: '<div id="tinymce-monaco-editor"></div>'
                      }
                    ]
                },
                buttons: [
                  { type: "cancel", name: "cancel", text: "Cancel" },
                  { type: "submit", name: "save", text: "Save", primary: !0 },
                ],
                initialData: { code: open },
                onSubmit: (api) =>
                {
                  ((editor, api) =>
                  {
                    editor.focus(),
                      editor.undoManager.transact(() =>
                      {
                        editor.setContent(api);
                      }),
                      editor.selection.setCursorLocation(),
                      editor.nodeChanged();
                  })(editor, api.getData().code),
                    api.close();
                },
              }
            );

            if (jQuery().MonacoEditor)
            {
              jQuery('#tinymce-monaco-editor').MonacoEditor({ language: 'html', linkedElement: '.tox-textarea', model: null });

              jQuery('#tinymce-monaco-editor').parent().siblings().css('display', 'none');
              jQuery('#tinymce-monaco-editor').parent().css('flex', '1').css('display', 'flex');
              jQuery('head').append(jQuery('<link rel="stylesheet" type="text/css" href="/Resources/Libraries/HtmlEditors/tinymce/Nucleus/content.min.css">'));
            }
          })(editor);
        });
      })(editor),
      ((editor) =>
      {
        const open = () => editor.execCommand("mceCodeEditor");
        editor.ui.registry.addButton
        (
            "code_monaco",
            { icon: "sourcecode", tooltip: "Source code", onAction: open }
        ),
          editor.ui.registry.addMenuItem("code_monaco", { icon: "sourcecode", text: "Source code", onAction: open });
      })(editor),
      {}
    )
  );
})();
